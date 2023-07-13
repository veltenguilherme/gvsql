using Npgsql;
using Persistence.Controllers.Base.CustomAttributes;
using Persistence.Controllers.Base.IO.Deserialization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Reflection;
using System.Threading.Tasks;

namespace Persistence.Controllers.Base
{
    public partial class Provider<T>
    {
        internal NpgsqlConnection Conn
        {
            get;
            set;
        }

        internal List<NpgsqlParameter> Parameters
        {
            get;
            set;
        } = new List<NpgsqlParameter>();

        private async Task<NpgsqlCommand> GetCommand(string sql)
        {
            if (string.IsNullOrEmpty(sql))
                throw new ArgumentNullException();

            Conn = new NpgsqlConnection(GetConnectionString(Database.HostName, Database.Port, Database.UserName, Database.Password, Database.Name));
            await Conn.OpenAsync();

            if (Parameters == null || Parameters.Count <= 0)
                return new NpgsqlCommand(sql, Conn, Conn.BeginTransaction());

            NpgsqlCommand npgsqlCommand = new NpgsqlCommand(sql, Conn, Conn.BeginTransaction());
            Parameters.ForEach(x => npgsqlCommand.Parameters.AddWithValue(x.ParameterName, x.Value ?? DBNull.Value));

            return npgsqlCommand;
        }

        private string GetConnectionString(string hostname, int port, string username, string password, string database)
        {
            try
            {
                NpgsqlConnectionStringBuilder npgsqlConnectionStringBuilder = new NpgsqlConnectionStringBuilder
                {
                    Host = hostname,
                    Port = port,
                    Username = username,
                    Password = password,
                    Database = database,
                    Pooling = true,
                    MaxPoolSize = 1024,
                    MinPoolSize = 0
                    //Timeout = 0,
                    //ConnectionIdleLifetime = 1000,
                    //ConnectionPruningInterval = 50
                };

                return npgsqlConnectionStringBuilder.ConnectionString;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        internal async Task<int> ExecuteNonQueryAsync(string sql)
        {
            if (!Database.Exists)
                return default;

            int result = default;

            try
            {
                var command = await GetCommand(sql);
                if (command == null)
                    return result;

                result = await command.ExecuteNonQueryAsync();
                await command?.Transaction?.CommitAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Parameters = new List<NpgsqlParameter>();
                await Conn.DisposeAsync();
            }

            return result;
        }

        internal async Task<object> ExecuteScalarAsync(string sql)
        {
            if (!Database.Exists)
                return default;

            object result = default;

            try
            {
                var command = await GetCommand(sql);
                if (command == null)
                    return result;

                result = await command.ExecuteScalarAsync();
                await command?.Transaction?.CommitAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                this.Parameters = new List<NpgsqlParameter>();
                await Conn.CloseAsync();
                await Conn.DisposeAsync();
            }

            return result;
        }

        internal object ExecuteScalar(string sql)
        {
            if (!Database.Exists)
                return default;

            object result = default;
            NpgsqlConnection npgsqlConnection = new NpgsqlConnection(GetConnectionString(Database.HostName, Database.Port, Database.UserName, Database.Password, Database.Name));

            try
            {
                npgsqlConnection.Open();

                NpgsqlCommand npgsqlCommand = new NpgsqlCommand(sql, npgsqlConnection, npgsqlConnection.BeginTransaction());
                if (Parameters != null && Parameters.Count > 0)
                    Parameters.ForEach(x => npgsqlCommand.Parameters.AddWithValue(x.ParameterName, x.Value ?? DBNull.Value));

                var command = npgsqlCommand;
                if (command == null)
                    return result;

                result = command.ExecuteScalar();
                command?.Transaction?.Commit();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Parameters = new List<NpgsqlParameter>();
                npgsqlConnection.Dispose();
                npgsqlConnection.Close();
            }

            return result;
        }

        internal async Task<NpgsqlDataReader> ExecuteReaderRawSqlAsync(string sql)
        {
            var command = await GetCommand(sql);
            if (command == null) return default;

            return await command.ExecuteReaderAsync();
        }

        internal async Task<List<T>> ExecuteReaderAsync(string sql)
        {
            if (!Database.Exists)
                return default;

            List<T> collection = Activator.CreateInstance<List<T>>();

            try
            {
                var command = await GetCommand(sql);
                if (command == null)
                    return collection;

                foreach (DbDataRecord record in await command.ExecuteReaderAsync())
                {
                    T domain = Deserialize(record);
                    if (domain == null)
                        continue;

                    if (collection == null)
                        collection = new List<T>();

                    collection.Add(domain);
                }
                
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Parameters = new List<NpgsqlParameter>();
                await Conn.DisposeAsync();
            }

            return collection;
        }

        internal List<T> ExecuteReader(string sql)
        {
            if (!Database.Exists)
                return default;

            List<T> collection = Activator.CreateInstance<List<T>>();
            NpgsqlConnection npgsqlConnection = new NpgsqlConnection(GetConnectionString(Database.HostName, Database.Port, Database.UserName, Database.Password, Database.Name));

            try
            {
                npgsqlConnection.Open();

                NpgsqlCommand npgsqlCommand = new NpgsqlCommand(sql, npgsqlConnection, npgsqlConnection.BeginTransaction());
                if (Parameters != null && Parameters.Count > 0)
                    Parameters.ForEach(x => npgsqlCommand.Parameters.AddWithValue(x.ParameterName, x.Value ?? DBNull.Value));

                var command = npgsqlCommand;
                if (command == null)
                    return collection;

                foreach (DbDataRecord dbDataRecord in command.ExecuteReader())
                {
                    T domain = Deserialize(dbDataRecord);
                    if (domain == null)
                        continue;

                    if (collection == null)
                        collection = new List<T>();

                    collection.Add(domain);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Parameters = new List<NpgsqlParameter>();
                npgsqlConnection.Dispose();
                npgsqlConnection.Close();
            }

            return collection;
        }

        private T Deserialize(DbDataRecord record)
        {
            T target = Activator.CreateInstance<T>();
            if (record == null || record == default(DbDataRecord))
                return target;

            Collection deserialization = new Collection(record);
            if (deserialization == null || deserialization.Count < 1)
                return target;

            foreach (PropertyInfo property in typeof(T).GetProperties())
                Deserialize(deserialization, property, target);

            return target;
        }

        private void Deserialize(Collection deserialization, PropertyInfo property, object target, string patternTableName = null)
        {
            if (property.GetMethod == default)
                return;

            if (Utils.IsBaseModel(property.PropertyType.BaseType))
            {
                patternTableName = property.GetCustomAttribute<SqlJoinType>()?.PatternTableName;
                DeserializeChild(deserialization, property, property.GetValue(target), target, patternTableName);
                return;
            }

            if (property.GetCustomAttribute<ColumnAttribute>() == null)
                return;

            string columnName = property.GetCustomAttribute<ColumnAttribute>().Name;
            if (string.IsNullOrEmpty(columnName) || string.IsNullOrWhiteSpace(columnName))
                return;

            string tableName = target.GetType().GetCustomAttribute<TableAttribute>().Name;
            Model model = deserialization.Find(x => x.ColumnName == string.Format($"{(patternTableName == null ? tableName : $"{patternTableName}çç{tableName}")}çç{columnName}"));
            if (model == null)
                model = deserialization.Find(x => x.ColumnName == $"{tableName}çç{columnName}");

            patternTableName = null;

            if (model == null ||
                property.SetMethod == default)
                return;

            property.SetValue(target, model.Value == DBNull.Value ? null : model.Value);
        }

        private void DeserializeChild(Collection deserializationCollection, PropertyInfo property, object child, object target, string patterTableName = default)
        {
            foreach (PropertyInfo childProperty in child.GetType().GetProperties())
                Deserialize(deserializationCollection, childProperty, child, patterTableName);

            property.SetValue(target, child);
        }
    }
}
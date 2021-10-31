using Npgsql;
using Persistence.Controllers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Linq;
using System.Reflection;

namespace Persistence
{
    public class Database
    {
        public static bool Exists { get; set; } = true;
        public static string HostName { get; set; }
        public static int Port { get; set; }
        public static string UserName { get; set; }
        public static string Password { get; set; }
        public static string Name { get; set; }

        public Database(string hostName, int port, string name, string userName, string password)
        {
            HostName = hostName;
            Port = port;
            UserName = userName;
            Password = password;
            Name = name;
        }

        public void Create(List<Structure> schema)
        {
            try
            {
                CreateDb();
            }
            catch
            {
                Exists = false;
            }
            finally
            {
                CreateTables(schema);
                CreateViews(schema);
            }
        }

        private static void CreateDb()
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(GetConnectionString()))
            {
                conn.Open();
                Exists = Convert.ToInt32(new NpgsqlCommand($"SELECT count(*) FROM pg_catalog. pg_database WHERE lower(datname) = lower('{Name}')", conn).ExecuteScalar()) > 0;

                if (Exists)
                    return;

                new NpgsqlCommand($"CREATE DATABASE {Name}", conn).ExecuteNonQuery();
                conn.Close();
                Exists = true;
            }

            using (NpgsqlConnection conn = new NpgsqlConnection(GetConnectionString(Name)))
            {
                conn.Open();
                new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\"", conn).ExecuteNonQuery();
                conn.Close();
            }
        }

        private static void CreateTables(List<Structure> schema)
        {
            if (!Exists)
                return;

            schema.ForEach(x =>
            {
                if (!TableExists(x.Model))
                    Activator.CreateInstance(x.Table, new object[] { true, false });
                else
                {
                    DropColumns(x.Model);
                    CreateColumns(x.Model);
                }
            });
        }

        private static void DropColumns(Type typeModel)
        {
            var model = Activator.CreateInstance(typeModel);

            List<string> columns = new List<string>();
            string tableName = model.GetType().GetCustomAttributesData().First().ConstructorArguments.First().Value.ToString();

            using (NpgsqlConnection conn = new NpgsqlConnection(GetConnectionString(Name)))
            {
                conn.Open();
                string command = $@"SELECT column_name
                                          FROM information_schema.columns
                                         WHERE table_name = '{tableName}'";

                foreach (DbDataRecord record in new NpgsqlCommand(command, conn).ExecuteReader())
                    columns.Add(record.GetString(0));

                conn.Close();
            }

            if (columns.Count <= 0)
                return;

            List<string> droppedColumns = new List<string>();
            columns.ForEach(x =>
            {
                bool drop = false;
                foreach (PropertyInfo prop in model.GetType().GetProperties())
                {
                    string columnName = prop.GetCustomAttribute<ColumnAttribute>()?.Name;
                    if (columnName == null)
                        continue;

                    if (columnName.Equals(x))
                    {
                        drop = false;
                        break;
                    }

                    drop = true;
                }

                if (drop)
                    using (NpgsqlConnection conn = new NpgsqlConnection(GetConnectionString(Name)))
                    {
                        conn.Open();
                        new NpgsqlCommand($"ALTER TABLE {tableName} DROP COLUMN IF EXISTS {x} CASCADE;", conn).ExecuteNonQuery();
                        conn.Close();
                    }
            });
        }

        private static void CreateColumns(Type typeModel)
        {
            var model = Activator.CreateInstance(typeModel);
            using (NpgsqlConnection conn = new NpgsqlConnection(GetConnectionString(Name)))
            {
                try
                {
                    conn.Open();
                    string tableName = model.GetType().GetCustomAttributesData().First().ConstructorArguments.First().Value.ToString();

                    foreach (PropertyInfo prop in model.GetType().GetProperties())
                    {
                        string columnName = default;
                        string type = default;
                        string command = default;

                        try
                        {
                            type = Table<dynamic>.GetDataType(prop.GetCustomAttribute<Controllers.Base.CustomAttributes.TypeInfo>().Type);
                            columnName = prop.GetCustomAttribute<ColumnAttribute>().Name;

                            command = $"ALTER TABLE {tableName} ADD COLUMN IF NOT EXISTS {columnName} {type}";
                            new NpgsqlCommand(command, conn).ExecuteNonQuery();
                        }
                        catch
                        {
                            if (type != default && type.Contains("NOT NULL"))
                            {
                                string typeWithoutNotNull = type.Split(' ').First();
                                object defaultValue = prop.GetCustomAttribute<Controllers.Base.CustomAttributes.TypeInfo>().Value;
                                command = $"ALTER TABLE {tableName} ADD COLUMN IF NOT EXISTS {columnName} {typeWithoutNotNull}";
                                new NpgsqlCommand(command, conn).ExecuteNonQuery();

                                command = $"UPDATE {tableName} SET {columnName} = @{columnName}";
                                var pgsqlCommand = new NpgsqlCommand(command, conn);
                                pgsqlCommand.Parameters.AddWithValue(columnName, defaultValue != default ? defaultValue : Table<dynamic>.GetDefaultValue(typeWithoutNotNull));
                                pgsqlCommand.ExecuteNonQuery();

                                command = $"ALTER TABLE {tableName} ALTER COLUMN {columnName} SET NOT NULL";
                                new NpgsqlCommand(command, conn).ExecuteNonQuery();
                            }

                            continue;
                        }
                    }
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        private void CreateViews(List<Structure> schema)
        {
            if (!Exists)
                return;

            schema.ForEach(x =>
            {
                Activator.CreateInstance(x.Table, new object[] { false, true });
            });
        }

        private static bool TableExists(Type model)
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(GetConnectionString(Name)))
            {
                conn.Open();
                var tableName = new NpgsqlCommand($@"SELECT coalesce(table_name, ' ')
                                                       FROM information_schema.tables
                                                      WHERE table_schema = current_schema()
                                                        AND table_name = '{Activator.CreateInstance(model).GetType().GetCustomAttributesData().First().ConstructorArguments.First().Value}'", conn).ExecuteScalar();
                conn.Close();
                return !(tableName == null || string.IsNullOrEmpty(tableName.ToString()) || string.IsNullOrWhiteSpace(tableName.ToString()));
            }
        }

        private static string GetConnectionString(string db = default)
        {
            try
            {
                NpgsqlConnectionStringBuilder npgsqlConnectionStringBuilder = new NpgsqlConnectionStringBuilder
                {
                    Host = HostName,
                    Port = Port,
                    Username = UserName,
                    Password = Password
                };

                if (db != default)
                    npgsqlConnectionStringBuilder.Database = db;

                return npgsqlConnectionStringBuilder.ConnectionString;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
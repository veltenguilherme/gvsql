using Humanizer;
using Npgsql;
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

        public Database(string hostName, int port, string name, string userName, string password, List<Structure> schema)
        {
            HostName = hostName;
            Port = port;
            UserName = userName;
            Password = password;
            Name = name;

            Create(schema);
        }

        private void Create(List<Structure> schema)
        {
            try
            {
                CreateDb();
                CreateTables(schema);
            }
            catch
            {
                Exists = false;
            }
        }

        private static void CreateDb()
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(GetConnectionString()))
            {
                conn.Open();
                Exists = Convert.ToInt32(new NpgsqlCommand($"SELECT count(*) FROM pg_catalog. pg_database WHERE lower(datname) = lower('{Name}')", conn).ExecuteScalar()) > 0;

                if (Exists)
                {
                    GenerateUuidOssp();
                    return;
                }

                new NpgsqlCommand($"CREATE DATABASE {Name}", conn).ExecuteNonQuery();
                conn.Close();
                Exists = true;

                GenerateUuidOssp();
            }
        }

        private static void GenerateUuidOssp()
        {
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
                Activator.CreateInstance(x.Table);

                if (TableExists(x.Model))
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
                    string tableName = model.GetType().GetCustomAttribute<TableAttribute>().Name;

                    foreach (PropertyInfo prop in model.GetType().GetProperties())
                    {
                        string columnName = default;
                        string type = default;
                        string command = default;

                        try
                        {
                            type = GetDataType(prop.GetCustomAttribute<Controllers.Base.CustomAttributes.SqlType>().Type);
                            columnName = prop.GetCustomAttribute<ColumnAttribute>()?.Name ?? prop.Name.Underscore();

                            command = $"ALTER TABLE {tableName} ADD COLUMN IF NOT EXISTS {columnName} {type}";
                            new NpgsqlCommand(command, conn).ExecuteNonQuery();
                        }
                        catch
                        {
                            if (type != default && type.Contains("NOT NULL"))
                            {
                                string typeWithoutNotNull = type.Split(' ').First();
                                object defaultValue = prop.GetCustomAttribute<Controllers.Base.CustomAttributes.SqlType>().Value;
                                command = $"ALTER TABLE {tableName} ADD COLUMN IF NOT EXISTS {columnName} {typeWithoutNotNull}";
                                new NpgsqlCommand(command, conn).ExecuteNonQuery();

                                command = $"UPDATE {tableName} SET {columnName} = @{columnName}";
                                var pgsqlCommand = new NpgsqlCommand(command, conn);
                                pgsqlCommand.Parameters.AddWithValue(columnName, defaultValue != default ? defaultValue : GetDefaultValue(typeWithoutNotNull));
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

        public static bool TableExists(Type model = default, string name = default)
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(GetConnectionString(Name)))
            {
                conn.Open();
                var tableName = new NpgsqlCommand($@"SELECT coalesce(table_name, ' ')
                                                       FROM information_schema.tables
                                                      WHERE table_schema = current_schema()
                                                        AND table_name = '{name ?? Activator.CreateInstance(model).GetType().GetCustomAttribute<TableAttribute>().Name}'", conn).ExecuteScalar();

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

        private static string GetDataType(Models.SqlTypes type)
        {
            return type switch
            {
                Models.SqlTypes.TEXT_UNIQUE => "text UNIQUE",
                Models.SqlTypes.TEXT_NOT_NULL => "text NOT NULL",
                Models.SqlTypes.TEXT_NOT_NULL_UNIQUE => "text NOT NULL UNIQUE",
                Models.SqlTypes.TEXT => "text",
                Models.SqlTypes.TIMESTAMP_WITHOUT_TIME_ZONE_NOT_NULL => "timestamp without time zone NOT NULL",
                Models.SqlTypes.TIMESTAMP_WITHOUT_TIME_ZONE => "timestamp without time zone",
                Models.SqlTypes.DATE => "date",
                Models.SqlTypes.DATE_NOT_NULL => "date NOT NULL",
                Models.SqlTypes.INTEGER => "integer",
                Models.SqlTypes.INTEGER_NOT_NULL => "integer NOT NULL",
                Models.SqlTypes.INTEGER_NOT_NULL_UNIQUE => "integer NOT NULL UNIQUE",
                Models.SqlTypes.BIG_INT => "bigint",
                Models.SqlTypes.NUMERIC_DEFAULT_VALUE_0 => "numeric DEFAULT 0.00",
                Models.SqlTypes.BOOLEAN => "boolean",
                Models.SqlTypes.BYTEA => "bytea",
                Models.SqlTypes.BYTEA_NOT_NULL => "bytea NOT NULL",
                Models.SqlTypes.GUID => "uuid",
                Models.SqlTypes.GUID_NOT_NULL => "uuid NOT NULL",
                _ => default,
            };
        }

        private static object GetDefaultValue(string type)
        {
            switch (type)
            {
                case "text":
                case "integer":
                case "bigint":
                case "numeric":
                case "bytea":
                default:
                    return 0;

                case "boolean":
                    return false;

                case "uuid":
                    return Guid.NewGuid();

                case "date":
                case "timestamp":
                    return DateTime.MinValue;
            }
        }
    }
}
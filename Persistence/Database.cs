using Npgsql;
using System;

namespace Persistence
{
    public class Database
    {
        internal static string HostName { get; private set; }
        internal static int Port { get; private set; }
        internal static string Name { get; private set; }
        internal static string UserName { get; private set; }
        internal static string Password { get; private set; }
        public static bool Exists { get; set; } = false;

        public Database(string hostName, int port, string databaseName, string userName, string password)
        {
            HostName = hostName;
            Port = port;
            Name = databaseName;
            UserName = userName;
            Password = password;

            //tesete

            this.Create();
        }

        private void Create()
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(GetConnectionString(HostName, Port, UserName, Password, UserName)))
            {
                conn.Open();
                Exists = Convert.ToInt32(new NpgsqlCommand($"SELECT count(*) FROM pg_catalog. pg_database WHERE lower(datname) = lower('{Name}')", conn).ExecuteScalar()) > 0;
                conn.Close();
            }

            if (Exists) return;

            using (NpgsqlConnection conn = new NpgsqlConnection(GetConnectionString(HostName, Port, UserName, Password, UserName)))
            {
                conn.Open();
                new NpgsqlCommand($"CREATE DATABASE {Name}", conn).ExecuteNonQuery();
                conn.Close();
            }

            using (NpgsqlConnection conn = new NpgsqlConnection(GetConnectionString(HostName, Port, UserName, Password, Name)))
            {
                conn.Open();
                new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\"", conn).ExecuteNonQuery();
                conn.Close();
            }
        }

        public void SetTables(DbContext context)
        {
            try
            {
                Exists = true;
            }
            catch { }
        }

        private string GetConnectionString(string hostName, int port, string userName, string password, string databaseName)
        {
            try
            {
                NpgsqlConnectionStringBuilder npgsqlConnectionStringBuilder = new NpgsqlConnectionStringBuilder
                {
                    Host = hostName,
                    Port = port,
                    Username = userName,
                    Password = password,
                    Database = databaseName
                };

                return npgsqlConnectionStringBuilder.ConnectionString;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
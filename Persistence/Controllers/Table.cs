using Persistence.Controllers.Base;
using Persistence.Controllers.Base.Queries;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Reflection;
using System.Threading.Tasks;

namespace Persistence.Controllers
{
    public abstract partial class Table<T> : Controller<T>
    {
        public Table() : base()
        {
            bool exists = Database.TableExists(name: Name);

            if (!exists)
            {
                Provider.ExecuteNonQueryAsync(new Base.Table.Create().Set<T>(Name)).Wait();
                new Base.Trigger.UpdateOrInsert.Create<T>();
                new View<T>();
            }

            var viewCustomAttribute = typeof(T).GetCustomAttribute<Base.CustomAttributes.View>();
            bool viewExists = Database.TableExists(name: $"view_{viewCustomAttribute?.Name ?? Name}");

            if (exists && !viewExists)
                new View<T>();
        }

        internal async Task<T> ToListAsync(object info)
        {
            if (info == null)
                return default;

            string sql = $"select * from view_{Name} where {Name}ççuuid = '{(Guid)info}'";
            var objs = await Provider.ExecuteReaderAsync(sql);

            return objs[0];
        }

        public async Task<List<T>> ToListAsync(Query<T> query, int limit = 0, int offSet = 0) => await new View<T>().ToListAsync(query, limit, offSet);

        public async Task<List<M>> ToListRawAsync<M>(string sql, int limit = 0, int offSet = 0) => await new View<T>().ToListRawAsync<M>(sql, limit, offSet);

        public async Task<List<T>> ToListAsync(int limit = 0, int offSet = 0) => await new View<T>().ToListAsync(limit, offSet);

        public async Task<int> CountAsync()
        {
            try
            {
                string sql = $"select count(*) from {Name}";
                foreach (DbDataRecord record in await Provider.ExecuteReaderRawSqlAsync(sql))
                    return Convert.ToInt32(record.GetValue(0));

                return -1;
            }
            catch (Exception)
            {
                return -1;
            }
            finally
            {
                await Provider.Conn.DisposeAsync();
            }
        }

        public async Task<int> CountAsync(Query<T> query)
        {
            try
            {
                string sql = $"select count(*) from view_{Name} {query.Sql}";
                Provider.Parameters.AddRange(query.NpgsqlParameters);

                foreach (DbDataRecord record in await Provider.ExecuteReaderRawSqlAsync(sql))
                    return Convert.ToInt32(record.GetValue(0));

                return -1;
            }
            catch (Exception)
            {
                return -1;
            }
            finally
            {
                await Provider.Conn.DisposeAsync();
            }
        }

        public async Task<List<T>> ToListAsync(string sql) => await new View<T>().ToListAsync(sql);

        public async Task<List<T>> UpdateOrInsertAsync(List<T> objs)
        {
            if (objs == null || objs.Count <= 0)
                throw new ArgumentNullException();

            foreach (var x in objs)
            {
                T aux = await UpdateOrInsertAsync(x);
                if (aux != null)
                    Add(aux);
            }

            return this;
        }

        public async Task<List<T>> RemoveAsync(List<T> objs)
        {
            if (objs == null || objs.Count <= 0)
                throw new ArgumentNullException();

            foreach (var x in objs)
            {
                string result = await RemoveAsync(x);
                if (!string.IsNullOrEmpty(result))
                    Add(x);
            }

            return this;
        }

        public async Task<string> RemoveAsync(T obj)
        {
            if (obj == null)
                throw new ArgumentNullException();

            Guid? uuid = null;

            foreach (PropertyInfo property in obj.GetType().GetProperties())
            {
                if (property.GetCustomAttribute<KeyAttribute>() != null)
                {
                    uuid = (Guid?)property.GetValue(obj);
                    break;
                }
            }

            if (uuid == null)
                return default;

            return await RemoveAsync(uuid.Value);
        }

        public async Task<string> RemoveAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentNullException();

            if (!Guid.TryParse(id, out Guid uuid))
                throw new ArgumentException("The ID provided is not a valid Guid.", nameof(id));

            return await RemoveAsync(uuid);
        }

        public async Task<string> RemoveAsync(Guid id)
        {
            string sql = $"DELETE FROM public.{Name} WHERE uuid = '{id}';";
            var result = await Provider.ExecuteNonQueryAsync(sql);

            if (result > 0)
                return id.ToString();

            return default;
        }

        public async Task<List<string>> RemoveAsync(List<string> ids)
        {
            if (ids == null || ids.Count <= 0)
                throw new ArgumentNullException();

            List<string> removedIds = new List<string>();

            foreach (var id in ids)
            {
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                if (!Guid.TryParse(id, out Guid uuid))
                    continue;
              
                string result = await RemoveAsync(uuid);
                if (!string.IsNullOrEmpty(result))
                    removedIds.Add(id);
            }

            return removedIds;
        }

        public async Task<int> RemoveAllAsync()
        {
            Clear();
            return await Provider.ExecuteNonQueryAsync($"DELETE FROM public.{Name};");
        }
    }
}
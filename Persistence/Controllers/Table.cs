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
        public Table(bool create, bool dropAndCreateView) : base()
        {
            if (create)
            {
                Provider.ExecuteNonQueryAsync(new Base.Table.Create().Set<T>(Name)).Wait();
                new Base.Trigger.UpdateOrInsert.Create<T>();
                new View<T>();
            }
            else
            if (dropAndCreateView)
                new View<T>(true);
        }

        internal async Task<T> ToListAsync(object info)
        {
            if (info == null)
                return default;

            string sql = $"select * from view_{Name} where {Name}ççuuid = '{(Guid)info}'";
            var objs = await Provider.ExecuteReaderAsync(sql);

            return objs[0];
        }

        public async Task<List<T>> ToListAsync(Query<T> query) => await new View<T>().ToListAsync(query);

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

        public async Task<List<T>> ToListAsync(string sql)
        {
            return await new View<T>().ToListAsync(sql);
        }

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
                int result = await RemoveAsync(x);
                if (result > 0)
                    Add(x);
            }

            return this;
        }

        public async Task<int> RemoveAsync(T obj)
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

            string sql = $"DELETE FROM public.{Name} WHERE uuid = '{uuid}';";
            return await Provider.ExecuteNonQueryAsync(sql);
        }

        public async Task<int> RemoveAllAsync()
        {
            Clear();
            return await Provider.ExecuteNonQueryAsync($"DELETE FROM public.{Name};");
        }
    }
}
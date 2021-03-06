using Persistence.Controllers.Base;
using Persistence.Controllers.Base.Queries;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Threading.Tasks;

namespace Persistence.Controllers
{
    public abstract partial class Table<T> : Controller<T>
    {
        public Table() : base()
        {
            if (!Database.Exists)
            {
                Provider.ExecuteNonQueryAsync(new Base.Table.Create().Set<T>(Name)).Wait();
                new Base.Trigger.UpdateOrInsert.Create<T>();
                new View<T>();
            }
        }

        internal async Task<T> ToListAsync(object info)
        {
            if (info == null) return default;

            string sql = $"select * from view_{Name} where {Name}__uuid = '{(Guid)info}'";
            var objs = await Provider.ExecuteReaderAsync(sql);

            return objs[0];
        }

        public async Task<List<T>> ToListAsync(Query<T> query, bool exception = true)
        {
            var result = await new View<T>().ToListAsync(query);
            if (result.Count <= 0 && exception)
                throw new Exception("Nenhum registro foi encontrado.");

            return result;
        }

        public async Task<List<T>> ToListAsync(string sql, bool exception = true)
        {
            var result = await new View<T>().ToListAsync(sql);
            if (result.Count <= 0 && exception)
                throw new Exception("Nenhum registro foi encontrado.");

            return result;
        }

        public async Task<List<T>> UpdateOrInsertAsync(List<T> objs)
        {
            if (objs == null || objs.Count <= 0) throw new ArgumentNullException();

            foreach (T obj in objs)
            {
                T aux = await UpdateOrInsertAsync(obj);
                if (aux != null) Add(aux);
            }

            return this;
        }

        public async Task<List<T>> RemoveAsync(List<T> objs)
        {
            if (objs == null || objs.Count <= 0) throw new ArgumentNullException();

            foreach (T obj in objs)
            {
                int result = await RemoveAsync(obj);
                if (result > 0) Add(obj);
            }

            return this;
        }

        public async Task<int> RemoveAsync(T obj)
        {
            if (obj == null) throw new ArgumentNullException();

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
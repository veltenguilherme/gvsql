using Persistence.Controllers.Base;
using Persistence.Controllers.Base.Queries;
using Persistence.Controllers.Base.View.Main;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Threading.Tasks;

namespace Persistence.Controllers
{
    public class View<T> : Controller<T>
    {
        public string Alias
        {
            get
            {
                try
                {
                    var viewCustomAttribute = typeof(T).GetCustomAttribute<Base.CustomAttributes.View>();
                    var tableAtribute = typeof(T).GetCustomAttribute<TableAttribute>();

                    return viewCustomAttribute == null ? tableAtribute.Name : viewCustomAttribute.Name;
                }
                catch (Exception)
                {
                    return typeof(T).GetCustomAttribute<TableAttribute>().Name;
                }
            }
        }

        public View(bool drop = false)
        {
            if (drop)
                Provider.ExecuteScalar($"drop view if exists view_{Alias ?? Name}");

            new Create<T>(Name, Alias);
        }

        public async virtual Task<List<T>> ToListAsync(string sql)
        {
            if (string.IsNullOrEmpty(sql))
                throw new ArgumentNullException();

            return await Provider.ExecuteReaderAsync(sql);
        }

        public async virtual Task<List<T>> ToListAsync()
        {
            return await Provider.ExecuteReaderAsync($"select * from view_{Alias ?? Name}");
        }

        public async Task<List<T>> ToListAsync(Query<T> query)
        {
            Provider.Parameters.AddRange(query.NpgsqlParameters);
            return await ToListAsync(GetQuery(query.Sql));
        }

        protected string GetQuery(string sql) => $"select * from view_{Alias ?? Name} {sql}";
    }
}
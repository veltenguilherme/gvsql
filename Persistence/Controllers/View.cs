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
    [Base.CustomAttributes.View("opa")]
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

                    return viewCustomAttribute?.Name ?? tableAtribute.Name;
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

        public async virtual Task<List<T>> ToListAsync(int limit = 0, int offSet = 0)
        {
            var sql = $"select * from view_{Alias ?? Name} offset {offSet}";

            if (limit > 0)
                sql += $" limit {limit}";

            return await Provider.ExecuteReaderAsync(sql);
        }

        public async virtual Task<List<M>> ToListRawAsync<M>(string sql, int limit = 0, int offSet = 0)
        {
            sql = $"{sql} offset {offSet}";

            if (limit > 0)
                sql += $" limit {limit}";

            var provider = new Provider<M>();
            return await provider.ExecuteReaderRawAsync(sql);            
        }

        public async Task<List<T>> ToListAsync(Query<T> query, int limit = 0, int offSet = 0)
        {
            Provider.Parameters.AddRange(query.NpgsqlParameters);

            var sql = $"select * from view_{Alias ?? Name} {query.Sql} offset {offSet}";

            if (limit > 0)
                sql += $" limit {limit}";

            return await Provider.ExecuteReaderAsync(sql);
        }
    }
}
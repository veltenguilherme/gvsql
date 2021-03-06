using Persistence.Controllers.Base;
using Persistence.Controllers.Base.Queries;
using Persistence.Controllers.Base.View.Main;
using System;
using System.Collections.Generic;
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
                    return typeof(T).GetCustomAttribute<Base.CustomAttributes.View>().Name;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public View()
        {
            if (!Database.Exists) new Create<T>(Name, Alias);
        }

        public async virtual Task<List<T>> ToListAsync(string sql)
        {
            if (string.IsNullOrEmpty(sql)) throw new ArgumentNullException();
            return await Provider.ExecuteReaderAsync(sql);
        }

        public async Task<List<T>> ToListAsync(Query<T> query)
        {
            Provider.Parameters.AddRange(query.NpgsqlParameters);
            return await ToListAsync(GetQuery(query.Sql));
        }

        protected string GetQuery(string sql) => $"select * from view_{Alias ?? Name} {sql}";
    }
}
using System.Collections.Generic;

namespace Persistence.Controllers.Base.Trigger.UpdateOrInsert
{
    internal class Create<T> : Trigger<T>
    {
        internal Create()
        {
            GetFunctions().ForEach(x => Provider.ExecuteNonQueryAsync(x).Wait());
            GetTriggers().ForEach(x => Provider.ExecuteNonQueryAsync(x).Wait());
        }

        private List<string> GetTriggers()
        {
            List<string> sqlQueries = new List<string>
            {
               GetTrigger(Resource.trg_dtt_create.Trim().Replace("{0}", GetName("inserted")), Name, GetFunctionName("inserted"), EnmTime.BEFORE, EnmOperation.INSERT),
               GetTrigger(Resource.trg_dtt_create.Trim().Replace("{0}", GetName("updated")), Name, GetFunctionName("updated"), EnmTime.BEFORE, EnmOperation.UPDATE)
            };

            return sqlQueries;
        }

        private List<string> GetFunctions()
        {
            List<string> sql = new List<string>
            {
               GetFunction(Resource.func_dtt_create.Trim().Replace("{0}", GetFunctionName("inserted")), "inserted"),
               GetFunction(Resource.func_dtt_create.Trim().Replace("{0}", GetFunctionName("updated")), "updated")
            };

            return sql;
        }

        private string GetTrigger(string sqlBase, string tableName, string functionName, EnmTime time, EnmOperation operation) => sqlBase.Replace("{1}", time.ToString()).Replace("{2}", operation.ToString()).Replace("{3}", tableName).Replace("{4}", functionName);

        private string GetFunction(string sql, string columnName) => sql.Replace("{1}", columnName);

        private string GetFunctionName(string columnName) => $"func_new_{columnName}";

        private string GetName(string columnName) => $"trg_{columnName}";
    }
}
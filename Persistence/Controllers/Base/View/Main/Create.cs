using Humanizer;
using Persistence.Controllers.Base.CustomAttributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace Persistence.Controllers.Base.View.Main
{
    internal class Create<T>
    {
        private List<KeyValuePair<string, string>> Aliases
        {
            get;
            set;
        } = new List<KeyValuePair<string, string>>();

        private int IndexAlias
        {
            get;
            set;
        } = 0;

        private string PatternTableName
        {
            get;
            set;
        }

        private bool RedundantJoin
        {
            get;
            set;
        } = false;

        private string Name { get; set; }
        private string Alias { get; set; }
        private Provider<T> Provider { get; set; } = new Provider<T>();

        public Create(string name, string alias = default) : base()
        {
            this.Name = name;
            this.Alias = alias;
            Provider.ExecuteNonQueryAsync(Get()).Wait();
        }

        public string Get()
        {
            if (string.IsNullOrEmpty(Name) || string.IsNullOrWhiteSpace(Name))
                return default;

            PatternTableName = Name;

            string sqlBase = Resource.view_create.Trim().Replace("{0}", $"view_{Alias ?? Name}");
            sqlBase = sqlBase.Replace("{3}", Name);

            List<string> sqlParams = new List<string>();
            List<string> sqlQueries = new List<string>();
            List<string> sqlJoins = new List<string>();

            string fKColumnName = string.Empty;
            string joinColumnName = string.Empty;
            string patternTableName = string.Empty;

            bool isLeftJoin = false;

            foreach (PropertyInfo property in typeof(T).GetProperties())
            {
                if (property.GetCustomAttribute<SqlJoin>() != null && !Utils.IsBaseModel(property.PropertyType.BaseType))
                {
                    fKColumnName = property.GetCustomAttribute<ColumnAttribute>()?.Name ?? property.Name.Underscore();
                    patternTableName = property.GetCustomAttribute<SqlJoin>().TableName;
                    joinColumnName = property.GetCustomAttribute<SqlJoin>().ColumnName;
                    isLeftJoin = property.GetCustomAttribute<SqlJoin>().JoinType == Models.SqlJoinTypes.LEFT;
                }

                if (Utils.IsBaseModel(property.PropertyType.BaseType))
                {
                    SetJoin(property.GetValue(Activator.CreateInstance<T>()), property.GetCustomAttribute<SqlJoin>() == null ? Models.SqlJoinTypes.INNER :
                            isLeftJoin ? Models.SqlJoinTypes.LEFT : property.GetCustomAttribute<SqlJoin>().JoinType, Name, joinColumnName,
                             this.GetFkName(fKColumnName, property), ref sqlParams, ref sqlQueries, ref sqlJoins, default, isLeftJoin);
                    continue;
                }

                string columnName = property.GetCustomAttribute<ColumnAttribute>()?.Name ?? property.Name.Underscore();
                if (string.IsNullOrEmpty(columnName) || string.IsNullOrWhiteSpace(columnName))
                    continue;

                SetSqlParam(Name, columnName, default, ref sqlParams);
                SetSqlQuery(Name, default, columnName, ref sqlQueries);
            }

            Utils.RemoveLastComma(ref sqlParams);
            Utils.RemoveLastComma(ref sqlQueries);

            string sqlParam = string.Empty;
            string sqlQuery = string.Empty;
            string sqlJoin = string.Empty;

            sqlParams.ForEach(x => sqlParam += x);
            sqlQueries.ForEach(x => sqlQuery += x);
            sqlJoins.ForEach(x => sqlJoin += x);

            return sqlBase.Replace("{1}", sqlParam).Replace("{2}", string.Format("select {0} from {1} {2}", sqlQuery, Name, sqlJoin));
        }

        private bool IsRedundantJoin(List<string> sqlJoins, string joinTableName)
        {
            int count = 0;
            foreach (string join in sqlJoins)
            {
                if (join.Split(" ".ToCharArray())[2].Equals(joinTableName))
                    ++count;

                if (count == 2)
                    return true;
            }

            return false;
        }

        private void SetJoin(object child, Models.SqlJoinTypes type, string tableName, string joinColumnName,
                             string fkColumnName, ref List<string> sqlParams, ref List<string> sqlQueries,
                             ref List<string> sqlJoins, string patternTableName = default, bool isPatternLeftJoin = false)
        {
            string joinTableName = child.GetType().GetCustomAttribute<TableAttribute>().Name;
            string alias = $"{tableName}çç{fkColumnName}";
            Aliases.Add(new KeyValuePair<string, string>(joinTableName, alias));

            string join = $"{type} join {joinTableName} as {alias} on ({alias}.{joinColumnName} = {tableName}.{fkColumnName}) ";

            if (sqlJoins.Count <= 0)
                sqlJoins.Add(join);
            else
            {
                ++IndexAlias;
                string patternAlias = Aliases[IndexAlias - 1].Value;

                if (patternTableName != default)
                    patternAlias = Aliases.Where(x => x.Key == patternTableName).First().Value;

                if (alias.Split("çç".ToCharArray())[0].Equals(PatternTableName))
                    sqlJoins.Add(join);
                else
                    sqlJoins.Add($"{type} join {joinTableName} as {alias} on ({alias}.{joinColumnName} = {patternAlias}.{fkColumnName}) ");
            }

            RedundantJoin = IsRedundantJoin(sqlJoins, joinTableName);

            if (RedundantJoin)
            {
                RedundantJoin = false;
                if (patternTableName == default)
                    joinTableName = $"{tableName}çç{joinTableName}";                
            }

            foreach (PropertyInfo property in child.GetType().GetProperties())
            {
                if (property.GetCustomAttribute<SqlJoin>() != null && !Utils.IsBaseModel(property.PropertyType.BaseType))
                {
                    fkColumnName = property.GetCustomAttribute<ColumnAttribute>()?.Name ?? property.Name.Underscore();
                    joinColumnName = property.GetCustomAttribute<SqlJoin>().ColumnName;
                }

                if (Utils.IsBaseModel(property.PropertyType.BaseType))
                {
                    SetJoin(property.GetValue(child), property.GetCustomAttribute<SqlJoin>() == null ? Models.SqlJoinTypes.INNER :
                            isPatternLeftJoin ? Models.SqlJoinTypes.LEFT : property.GetCustomAttribute<SqlJoin>().JoinType,
                            joinTableName, joinColumnName, this.GetFkName(fkColumnName, property),
                            ref sqlParams, ref sqlQueries, ref sqlJoins,
                            patternTableName, isPatternLeftJoin);
                    continue;
                }

                string columnName = property.GetCustomAttribute<ColumnAttribute>()?.Name ?? property.Name.Underscore();
    

                if (string.IsNullOrEmpty(columnName) || string.IsNullOrWhiteSpace(columnName))
                    continue;

                SetSqlParam(joinTableName, columnName, patternTableName, ref sqlParams);
                SetSqlQuery(joinTableName, patternTableName, columnName, ref sqlQueries);
            }
        }

        private string GetFkName(string fkColumnName, PropertyInfo property) => property.GetCustomAttribute<SqlJoin>()?.FkName ?? fkColumnName;

        private void SetSqlParam(string tableName, string columnName, string patternTableName, ref List<string> sqlParams)
        {
            if (!(string.IsNullOrEmpty(patternTableName) || string.IsNullOrWhiteSpace(patternTableName)))
                sqlParams.Add($"{patternTableName}çç{tableName}çç{columnName},");
            else
                sqlParams.Add($"{tableName}çç{columnName},");
        }

        private void SetSqlQuery(string tableName, string patternTable, string columnName, ref List<string> sql)
        {
            if (tableName.Equals(PatternTableName))
            {
                sql.Add($"{tableName}.{columnName},");
                return;
            }

            string[] aux = tableName.Split("çç".ToCharArray());
            aux = aux.Where(x => !(string.IsNullOrWhiteSpace(x) && string.IsNullOrEmpty(x))).ToArray();

            List<KeyValuePair<string, string>> objs = Aliases.Where(x => x.Key == (aux.Length == 1 ? tableName : aux[1])).ToList();

            if (objs.Count > 1 && !(string.IsNullOrWhiteSpace(patternTable) && string.IsNullOrEmpty(patternTable)))
                objs = objs.Where(x => x.Value.Split("çç".ToCharArray()).First().Equals(patternTable)).ToList();
            else
            {
                sql.Add($"{objs?.Last().Value}.{columnName},");
                return;
            }

            if (objs.Count <= 0)
                return;

            sql.Add($"{objs?.First().Value}.{columnName},");
        }
    }
}
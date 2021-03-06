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

        private string PatterTableName
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
            if (string.IsNullOrEmpty(Name) || string.IsNullOrWhiteSpace(Name)) return default;

            PatterTableName = Name;

            string sqlBase = Resource.view_create.Trim().Replace("{0}", $"view_{Alias ?? Name}");
            List<string> sqlParams = new List<string>();
            List<string> sqlQueries = new List<string>();
            List<string> sqlJoins = new List<string>();

            string fKColumnName = string.Empty;
            string joinColumnName = string.Empty;

            bool isLeftJoin = false;

            foreach (PropertyInfo property in typeof(T).GetProperties())
            {
                if (property.GetCustomAttribute<Fk>() != null)
                {
                    fKColumnName = property.GetCustomAttribute<ColumnAttribute>().Name;
                    joinColumnName = property.GetCustomAttribute<Fk>().ColumnName;
                }

                if (Utils.IsBaseModel(property.PropertyType.BaseType))
                {
                    isLeftJoin = property.GetCustomAttribute<JoinType>().Type == Models.JoinType.LEFT;
                    SetJoin(property.GetValue(Activator.CreateInstance<T>()), property.GetCustomAttribute<JoinType>() == null ? Models.JoinType.INNER :
                                              isLeftJoin ? Models.JoinType.LEFT : property.GetCustomAttribute<JoinType>().Type, Name, joinColumnName,
                                              property.GetCustomAttribute<ColumnAttribute>().Name, ref sqlParams, ref sqlQueries, ref sqlJoins, default, isLeftJoin);
                    continue;
                }

                string columnName = property.GetCustomAttribute<ColumnAttribute>() == null ? null : property.GetCustomAttribute<ColumnAttribute>().Name;
                if (string.IsNullOrEmpty(columnName) || string.IsNullOrWhiteSpace(columnName)) continue;

                SetSqlParam(Name, columnName, ref sqlParams);
                SetSqlQuery(Name, columnName, ref sqlQueries);
            }

            Utils.RemoveLastComma(ref sqlParams);
            Utils.RemoveLastComma(ref sqlQueries);

            string sqlParam = string.Empty;
            string sqlQuery = string.Empty;
            string sqlJoin = string.Empty;

            sqlParams.ForEach(x => sqlParam += x);
            sqlQueries.ForEach(x => sqlQuery += x);
            sqlJoins.ForEach(x => sqlJoin += x);

            return sqlBase.Replace("{1}", sqlParam).Replace("{2}", string.Format("select {0} from {1} {2};", sqlQuery, Name, sqlJoin));
        }

        private bool IsRedundantJoin(List<string> sqlJoins, string joinTableName)
        {
            int count = 0;
            foreach (string join in sqlJoins)
            {
                if (join.Split(" ".ToCharArray())[2].Equals(joinTableName)) ++count;
                if (count == 2) return true;
            }

            return false;
        }

        private void SetJoin(object child, Models.JoinType type, string tableName, string joinColumnName,
                             string fkColumnName, ref List<string> sqlParams, ref List<string> sqlQueries,
                             ref List<string> sqlJoins, string patternTable = default, bool isPatternLeftJoin = false)
        {
            string joinTableName = child.GetType().GetCustomAttribute<TableAttribute>().Name;
            string alias = $"{tableName}__{fkColumnName}";
            Aliases.Add(new KeyValuePair<string, string>(joinTableName, alias));

            string join = $"{type} join {joinTableName} as {alias} on ({alias}.{joinColumnName} = {tableName}.{fkColumnName}) ";

            if (sqlJoins.Count <= 0) sqlJoins.Add(join);
            else
            {
                ++IndexAlias;
                string paternAlias = Aliases[IndexAlias - 1].Value;

                if (alias.Split("__".ToCharArray())[0].Equals(PatterTableName)) sqlJoins.Add(join);
                else sqlJoins.Add($"{type} join {joinTableName} as {alias} on ({alias}.{joinColumnName} = {paternAlias}.{fkColumnName}) ");
            }

            RedundantJoin = IsRedundantJoin(sqlJoins, joinTableName);
            foreach (PropertyInfo property in child.GetType().GetProperties())
            {
                if (Utils.IsBaseModel(property.PropertyType.BaseType))
                {
                    SetJoin(property.GetValue(child), property.GetCustomAttribute<JoinType>() == null ? Models.JoinType.INNER :
                            isPatternLeftJoin ? Models.JoinType.LEFT : property.GetCustomAttribute<JoinType>().Type,
                            joinTableName, joinColumnName, property.GetCustomAttribute<ColumnAttribute>().Name,
                            ref sqlParams, ref sqlQueries, ref sqlJoins,
                            property.GetCustomAttribute<JoinType>() == null ? default :
                            property.GetCustomAttribute<JoinType>().PatternTableName, isPatternLeftJoin);
                    continue;
                }

                string columnName = property.GetCustomAttribute<ColumnAttribute>()?.Name;

                if (RedundantJoin)
                {
                    RedundantJoin = false;
                    if (patternTable == default)
                    {
                        string redundantColumnName = sqlJoins.Where(x => x.Split('(').First().Contains(joinTableName) &&
                                                                         x.Split('(').First().Contains(tableName)).ToList().First().Split("__".ToCharArray()).First().Split("as ".ToCharArray())[1];

                        for (int i = 0; i < sqlParams.Count; i++)
                        {
                            if (!sqlParams[i].Contains(joinTableName)) continue;
                            sqlParams[i] = $"{redundantColumnName}__{sqlParams[i]}";
                        }

                        joinTableName = $"{tableName}__{joinTableName}";
                    }

                    patternTable = default;
                }

                if (string.IsNullOrEmpty(columnName) || string.IsNullOrWhiteSpace(columnName)) continue;

                SetSqlParam(joinTableName, columnName, ref sqlParams);
                SetSqlQuery(joinTableName, columnName, ref sqlQueries);
            }
        }

        private void SetSqlParam(string tableName, string columnName, ref List<string> sqlParams) => sqlParams.Add($"{tableName}__{columnName},");

        private void SetSqlQuery(string tableName, string columnName, ref List<string> sql)
        {
            if (tableName.Equals(PatterTableName))
            {
                sql.Add($"{tableName}.{columnName},");
                return;
            }

            List<KeyValuePair<string, string>> objs = Aliases.Where(x => x.Key == (tableName.Split("__".ToCharArray()).Length == 1 ? tableName : tableName.Split("__".ToCharArray())[1])).ToList();
            if (objs.Count > 1) objs = objs.Where(x => x.Value.Split("__".ToCharArray()).First().Equals(tableName.Split("__".ToCharArray()).First())).ToList();

            sql.Add($"{objs[0].Value}.{columnName},");
        }
    }
}
using Npgsql;
using Persistence.Controllers.Base.Queries.Exp;
using Persistence.Controllers.Base.Queries.Exp.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Persistence.Controllers.Base.Queries
{
    public class Query<T> : BaseExpression<T>
    {
        internal string Sql
        {
            get;
            set;
        } = "where ";

        private List<BaseExpressionSide<T>> BinaryLeftExpressions
        {
            get;
            set;
        } = new List<BaseExpressionSide<T>>();

        private List<BaseExpressionSide<T>> BinaryRightExpressions
        {
            get;
            set;
        } = new List<BaseExpressionSide<T>>();

        private List<ExpressionTypeIndex<T>> ExpressionsType
        {
            get;
            set;
        } = new List<ExpressionTypeIndex<T>>();

        internal List<NpgsqlParameter> NpgsqlParameters
        {
            get;
            set;
        } = new List<NpgsqlParameter>();

        public Query(Expression<Func<T, bool>> func)
        {
            SetFunc((BinaryExpression)func.Body);
            SetSql();
        }

        private void SetSql()
        {
            List<int> mainIndexes = new List<int>();
            ExpressionsType.ForEach(x => SetLstMainIndex(x, ref mainIndexes));

            for (int i = 0; i <= Index; i++)
            {
                string columnName = default;
                object value = default;
                string logicOperator = default;
                int mainIndex = 0;

                try
                {
                    columnName = BinaryLeftExpressions.Single(x => x.Index == i).Obj;
                    value = BinaryRightExpressions.Single(x => x.Index == i).Obj;
                    logicOperator = GetExpressionType(ExpressionsType.Single(x => x.Index == i).Type);
                }
                catch
                {
                    continue;
                }

                Sql += GetSqlWhere(columnName, logicOperator, value);
                if (IsMainIndex(mainIndexes, i, ref mainIndex))
                    Sql += $" {GetExpressionType(ExpressionsType.Single(x => x.Index == mainIndex).Type)} ";
            }
        }

        private bool IsMainIndex(List<int> mainIndexes, int index, ref int mainIndex)
        {
            foreach (int aux in mainIndexes)
            {
                if ((index - 1) == aux || (index + 1) == aux)
                {
                    mainIndex = aux;
                    return true;
                }
            }

            return false;
        }

        private void SetLstMainIndex(ExpressionTypeIndex<T> expressionTypeIndex, ref List<int> mainIndexes)
        {
            if (BinaryRightExpressions.Where(x => x.Index == expressionTypeIndex.Index).ToList().Count > 0)
                return;

            mainIndexes.Add(expressionTypeIndex.Index);
        }

        private string GetSqlWhere(string columnName, string logicOperator, object value)
        {
            NpgsqlParameters.Add(new NpgsqlParameter(columnName, value));

            return (value?.GetType().Name) switch
            {
                nameof(DateTime) => ((DateTime)value).Hour <= 0 ? $"cast({columnName} as date){logicOperator}@{columnName}" :
                                                                  $"{columnName}{logicOperator}@{columnName}",
                _ => $"{columnName}{logicOperator}@{columnName}",
            };
        }

        private string GetExpressionType(ExpressionType enmExpressionType)
        {
            return enmExpressionType switch
            {
                ExpressionType.Equal => "=",
                ExpressionType.AndAlso => "and",
                ExpressionType.OrElse => "or",
                ExpressionType.LessThanOrEqual => "<=",
                ExpressionType.LessThan => "<",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.GreaterThan => ">",
                ExpressionType.NotEqual => "<>",
                _ => default,
            };
        }

        private void SetFunc(BinaryExpression binaryExpression)
        {
            ExpressionsType.Add(new ExpressionTypeIndex<T>(Index, binaryExpression.NodeType));
            foreach (PropertyInfo property in binaryExpression.GetType().GetProperties())
            {
                if (property.Name.ToUpper().Equals(EnmExpressionSide.RIGHT.ToString().ToUpper()))
                {
                    object right = property.GetValue(binaryExpression);
                    if (right is BinaryExpression expression)
                    {
                        ++Index;
                        SetFunc(expression);
                        continue;
                    }

                    SetFunc(right, BinaryRightExpressions, EnmExpressionSide.RIGHT, Index);
                }

                if (property.Name.ToUpper().Equals(EnmExpressionSide.LEFT.ToString().ToUpper()))
                {
                    object left = property.GetValue(binaryExpression);
                    if (left is BinaryExpression expression)
                    {
                        ++Index;
                        SetFunc(expression);
                        continue;
                    }

                    SetFunc(left, BinaryLeftExpressions, EnmExpressionSide.LEFT, Index);
                }
            }
        }

        private void SetFunc(dynamic obj, List<BaseExpressionSide<T>> sideExpressions, EnmExpressionSide type, int index)
        {
            switch (type)
            {
                case EnmExpressionSide.LEFT:
                    sideExpressions.Add(new ExpressionLeft<T>(index, obj)); return;
                case EnmExpressionSide.RIGHT:
                    sideExpressions.Add(new ExpressionRight<T>(index, obj)); return;
                default: return;
            }
        }
    }
}
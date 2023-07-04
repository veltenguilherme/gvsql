using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;

namespace Persistence.Controllers.Base.Queries.Exp.Base
{
    public abstract class BaseExpressionSide<T> : BaseExpression<T>
    {
        public BaseExpressionSide(int index, dynamic obj)
        {
            Obj = GetObj(obj);
            Index = index;
        }

        private dynamic GetObj(object obj) => this is ExpressionLeft<T> ? (dynamic)GetObjCastLeft((Expression)obj) : this is ExpressionRight<T> ?
                                              Expression.Lambda(GetObjCastRight((Expression)obj)).Compile().DynamicInvoke() : (dynamic)null;

        private object GetObjCastLeft(Expression obj)
        {
            return obj.NodeType switch
            {
                ExpressionType.Call => (((MethodCallExpression)obj).Object as MemberExpression).Member.Name,
                ExpressionType.MemberAccess => GetObjCastLeftChild(obj),
                _ => null,
            };
        }

        private object GetObjCastLeftChild(object obj)
        {
            MemberExpression memberExpression = default;
            string columnName = default;
            string tableName = default;

            try
            {
                memberExpression = ((MemberExpression)((MemberExpression)obj).Expression);
                if (memberExpression != null)
                {
                    SetTableName(memberExpression, memberExpression.Type.GetCustomAttribute<TableAttribute>().Name, ref tableName);
                    columnName = ((MemberExpression)obj).Member.GetCustomAttribute<ColumnAttribute>().Name;
                }
            }
            catch
            {
                tableName = ((ParameterExpression)((MemberExpression)obj).Expression).Type.GetCustomAttribute<TableAttribute>().Name;
                columnName = ((MemberExpression)obj).Member.GetCustomAttribute<ColumnAttribute>().Name;
            }

            return $"{tableName}{columnName}";
        }

        private void SetTableName(MemberExpression memberExpression, string lastTableName, ref string tableName)
        {
            Expression exp = default;
            try
            {
                exp = ((MemberExpression)memberExpression.Expression);
                string aux = exp.Type.GetCustomAttribute<TableAttribute>().Name;

                if (tableName == default)
                    tableName += $"{aux}çç{lastTableName}";
                else
                    tableName = default;
            }
            catch
            {
                if (tableName == default)
                    tableName = memberExpression.Type.GetCustomAttribute<TableAttribute>().Name;
            }
        }

        private dynamic GetObjCastRight(Expression obj)
        {
            return obj.NodeType switch
            {
                ExpressionType.Call => (MethodCallExpression)obj,
                ExpressionType.MemberAccess => (MemberExpression)obj,
                ExpressionType.Convert => (UnaryExpression)obj,
                ExpressionType.Constant => (ConstantExpression)obj,
                _ => null,
            };
        }
    }

    public enum EnmExpressionSide
    {
        LEFT = 1,
        RIGHT
    }
}
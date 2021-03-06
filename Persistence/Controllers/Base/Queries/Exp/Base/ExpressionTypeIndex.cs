using System.Linq.Expressions;

namespace Persistence.Controllers.Base.Queries.Exp.Base
{
    internal class ExpressionTypeIndex<T> : BaseExpression<T>
    {
        internal ExpressionType Type
        {
            get;
            set;
        }

        internal ExpressionTypeIndex(int index, ExpressionType type)
        {
            Index = index;
            Type = type;
        }
    }
}
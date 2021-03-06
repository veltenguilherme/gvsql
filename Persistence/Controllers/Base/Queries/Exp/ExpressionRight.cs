using Persistence.Controllers.Base.Queries.Exp.Base;

namespace Persistence.Controllers.Base.Queries.Exp
{
    public class ExpressionRight<T> : BaseExpressionSide<T>
    {
        public ExpressionRight(int index, object obj) : base(index, obj)
        {
        }
    }
}
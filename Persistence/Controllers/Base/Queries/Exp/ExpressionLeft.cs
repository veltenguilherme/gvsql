using Persistence.Controllers.Base.Queries.Exp.Base;

namespace Persistence.Controllers.Base.Queries.Exp
{
    public class ExpressionLeft<T> : BaseExpressionSide<T>
    {
        public ExpressionLeft(int index, object obj) : base(index, obj)
        {
        }
    }
}
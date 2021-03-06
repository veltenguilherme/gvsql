namespace Persistence.Controllers.Base.Queries.Exp.Base
{
    public abstract class BaseExpression<T>
    {
        public int Index { get; set; } = 0;
        public dynamic Obj { get; set; }
    }
}
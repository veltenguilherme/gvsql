using Persistence.Controllers.Base;

namespace Persistence.Controllers
{
    internal abstract class Trigger<T> : Controller<T>
    {
    }

    internal enum EnmTime
    {
        AFTER = 1,
        BEFORE
    }

    internal enum EnmOperation
    {
        INSERT = 1,
        UPDATE
    }
}
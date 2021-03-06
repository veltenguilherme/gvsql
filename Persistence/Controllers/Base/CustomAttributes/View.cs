using System;

namespace Persistence.Controllers.Base.CustomAttributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class View : Attribute
    {
        public string Name { get; set; }

        public View(string alias)
        {
            Name = alias;
        }
    }
}
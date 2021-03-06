using System;
using System.Collections.Generic;
using System.Linq;

namespace Persistence.Controllers.Base.CustomAttributes
{
    internal static class Utils
    {
        private const string MODEL_NAME = "Model`1";

        internal static void RemoveLastComma(ref List<string> text)
        {
            string value = text.Last().Replace(",", "");
            text.RemoveAt(text.Count - 1);
            text.Add(value);
        }

        internal static bool IsBaseModel(Type type)
        {
            if (type.BaseType == null ||
               (!MODEL_NAME.Equals(type.Name))) return false;

            return true;
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace Persistence.Controllers.Base
{
    public abstract class Controller<T> : List<T>

    {
        protected T this[Predicate<T> predicate] => Find(predicate);

        private new T Find(Predicate<T> predicate)
        {
            if (predicate == null)
                return default(T);
            try
            {
                return FindAll(predicate).SingleOrDefault();
            }
            catch (Exception)
            {
                return default(T);
            }
        }

        protected string Name
        {
            get
            {
                try
                {
                    return typeof(T).GetCustomAttribute<TableAttribute>().Name;
                }
                catch
                {
                    throw new Exception($"{typeof(T)} com nome inválido");
                }
            }
        }

        protected Provider<T> Provider { get; set; } = new Provider<T>();

        protected bool IsDefaultValue(PropertyInfo property, T obj)
        {
            if (IsDefaultNullableValue(property, obj))
                return true;

            try
            {
                var name = property.GetCustomAttribute<CustomAttributes.TypeInfo>().Type.ToString().ToUpper();
            }
            catch
            {
                return true;
            }

            switch (property.PropertyType.Name)
            {
                case "String":
                    return string.IsNullOrEmpty(Convert.ToString(property.GetValue(obj))) &&
                           !"TEXT".Equals(property.GetCustomAttribute<CustomAttributes.TypeInfo>().Type.ToString().ToUpper());

                case "Byte[]":
                    return property.GetValue(obj) == null;

                default: break;
            }

            return false;
        }

        protected object GetValue(T obj, PropertyInfo property)
        {
            var aux = property.GetValue(obj);
            if (property.PropertyType.IsEnum)
                aux = (int)property.GetValue(obj);

            return aux;
        }

        private bool IsDefaultNullableValue(PropertyInfo property, T obj)
        {
            string fullName = property.PropertyType.FullName;

            if (fullName.Contains("System.Nullable`1[[System.Int32") ||
                fullName.Contains("System.Nullable`1[[System.Boolean") ||
                fullName.Contains("System.Nullable`1[[System.Decimal") ||
                fullName.Contains("System.Nullable`1[[System.DateTime") ||
                fullName.Contains("System.Nullable`1[[System.Guid"))
                return property.GetValue(obj) == null;

            return false;
        }
    }
}
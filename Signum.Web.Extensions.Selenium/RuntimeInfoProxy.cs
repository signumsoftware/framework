using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;

namespace Signum.Web.Selenium
{
    public class RuntimeInfoProxy
    {
        public bool IsNew { get; private set; }
        public int? IdOrNull { get; private set; }
        public Type EntityType { get; private set; }
        public long? Ticks { get; private set; }

        public static RuntimeInfoProxy FromFormValue(string formValue)
        {
            string[] parts = formValue.Split(new[] { ";" }, StringSplitOptions.None);
            if (parts.Length != 4)
                throw new ArgumentException("Incorrect sfRuntimeInfo format: {0}".Formato(formValue));

            string entityTypeString = parts[0];

            return new RuntimeInfoProxy
            {
                EntityType = string.IsNullOrEmpty(entityTypeString) ? null : TypeLogic.GetType(entityTypeString),
                IdOrNull = (parts[1].HasText()) ? int.Parse(parts[1]) : (int?)null,
                IsNew = parts[2] == "n",
                Ticks = parts.Length == 4 && parts[3].HasText() ? long.Parse(parts[3]) : (long?)null
            };
        }

        public override string ToString()
        {
            if (IdOrNull != null && IsNew)
                throw new ArgumentException("Invalid RuntimeInfo parameters: IdOrNull={0} and IsNew=true".Formato(IdOrNull));

            if (EntityType != null && EntityType.IsLite())
                throw new ArgumentException("RuntimeInfo's RuntimeType cannot be of type Lite. Use ExtractLite or construct a RuntimeInfo<T> instead");

            return "{0};{1};{2};{3}".Formato(
                (EntityType == null) ? "" : TypeLogic.GetCleanName(EntityType),
                IdOrNull.TryToString(),
                IsNew ? "n" : "o",
                Ticks
                );
        }

        public Lite<Entity> ToLite()
        {
            if (IsNew)
                throw new InvalidOperationException("The RuntimeInfo represents a new entity");

            if (this.EntityType == null)
                return null;

            return Lite.Create(this.EntityType, this.IdOrNull.Value);
        }
    }
}

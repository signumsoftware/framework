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
        public PrimaryKey? IdOrNull { get; private set; }
        public Type EntityType { get; private set; }
        public string EntityTypeString { get; private set; }
        public long? Ticks { get; private set; }

        public static RuntimeInfoProxy FromLite(Lite<IEntity> lite)
        {
            return new RuntimeInfoProxy
            {
                EntityType = lite.EntityType,
                IdOrNull = lite.IdOrNull,
                IsNew = false,
                Ticks = null,
            };
        }

        public static RuntimeInfoProxy FromFormValue(string formValue)
        {
            string[] parts = formValue.Split(new[] { ";" }, StringSplitOptions.None);
            if (parts.Length != 4)
                throw new ArgumentException("Incorrect sfRuntimeInfo format: {0}".FormatWith(formValue));

            string entityTypeString = parts[0];

            Type type = string.IsNullOrEmpty(entityTypeString) ? null : TypeLogic.NameToType.TryGetC(entityTypeString);

            return new RuntimeInfoProxy
            {
                EntityTypeString = entityTypeString,
                EntityType = type,
                IdOrNull = (parts[1].HasText()) ? PrimaryKey.Parse(parts[1], type) : (PrimaryKey?)null,
                IsNew = parts[2] == "n",
                Ticks = parts.Length == 4 && parts[3].HasText() ? long.Parse(parts[3]) : (long?)null
            };
        }

        public override string ToString()
        {
            if (IdOrNull != null && IsNew)
                throw new ArgumentException("Invalid RuntimeInfo parameters: IdOrNull={0} and IsNew=true".FormatWith(IdOrNull));

            if (EntityType != null && EntityType.IsLite())
                throw new ArgumentException("RuntimeInfo's RuntimeType cannot be of type Lite. Use ExtractLite or construct a RuntimeInfo<T> instead");

            return "{0};{1};{2};{3}".FormatWith(
                (EntityType == null) ? "" : TypeLogic.GetCleanName(EntityType),
                IdOrNull.TryToString(),
                IsNew ? "n" : "o",
                Ticks
                );
        }

        public Lite<Entity> ToLite(string toString = null)
        {
            if (IsNew)
                throw new InvalidOperationException("The RuntimeInfo represents a new entity");

            if (this.EntityType == null)
                return null;

            return Lite.Create(this.EntityType, this.IdOrNull.Value, toString);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;

namespace WizardProjectName
{
    public static class Reflector
    {
        public static void Set(Assembly entitiesAssembly)
        {
            AssemblyName assemblyName = entitiesAssembly.GetReferencedAssemblies().SingleOrDefault(an => an.Name == "Signum.Entities");
            if (assemblyName == null)
                throw new ApplicationException("The assembly has no reference to Signum.Entities");

            Assembly assembly = Assembly.Load(assemblyName);

            IIdentifiable = assembly.GetType("Signum.Entities.IIdentifiable", true);
            IdentifiableEntity = assembly.GetType("Signum.Entities.IdentifiableEntity", true);
            Entity = assembly.GetType("Signum.Entities.Entity", true);
            EmbeddedEntity = assembly.GetType("Signum.Entities.EmbeddedEntity", true);
            Modifiable = assembly.GetType("Signum.Entities.Modifiable", true);
            ModifiableEntity = assembly.GetType("Signum.Entities.ModifiableEntity", true);
            MList = assembly.GetType("Signum.Entities.MList`1", true);
            Lite = assembly.GetType("Signum.Entities.Lite`1", true);
            LowPopulationAttribute = assembly.GetType("Signum.Entities.LowPopulationAttribute", true);
            LowProperty = LowPopulationAttribute.GetProperty("Low");

            CommonProperties = Entity
               .GetProperties(BindingFlags.Instance | BindingFlags.Public)
               .Select(p => p.Name)
               .ToList();

        }

        public static Type IIdentifiable { get; private set; }
        public static Type IdentifiableEntity { get; private set; }
        public static Type EmbeddedEntity { get; private set; }
        public static Type Modifiable { get; private set; }
        public static Type ModifiableEntity { get; private set; }
        public static Type Entity { get; private set; }
        public static Type MList { get; private set; }
        public static Type LowPopulationAttribute { get; private set; }
        public static PropertyInfo LowProperty { get; private set; }
        public static Type Lite { get; private set; }

        public static List<string> CommonProperties { get; private set; }

        public static Type CollectionType(Type ft)
        {
            if (!typeof(IEnumerable).IsAssignableFrom(ft))
                return null;

            Type interf = ft.GetInterfaces().SingleOrDefault(ti => ti.IsGenericType &&
                        ti.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            if (interf == null)
                return null;

            return interf.GetGenericArguments()[0];
        }

        public static bool IsModifiable(Type t)
        {
            return Modifiable.IsAssignableFrom(t);
        }

        public static bool IsMList(Type ft)
        {
            return CollectionType(ft) != null && IsModifiable(ft);
        }

        public static bool IsIIdentifiable(Type type)
        {
            return IIdentifiable.IsAssignableFrom(type);
        }

        public static bool IsLowPopulation(Type type)
        {
            Attribute lpa = type.GetCustomAttributes(LowPopulationAttribute, true).Cast<Attribute>().SingleOrDefault();
            if (lpa != null)
                return (bool)LowProperty.GetValue(lpa, null);

            return !Entity.IsAssignableFrom(type);
        }

        public static Type ExtractLite(Type liteType)
        {
            if (liteType.IsGenericType && liteType.GetGenericTypeDefinition() == Lite)
                return liteType.GetGenericArguments()[0];
            return null;
        }
    }

}

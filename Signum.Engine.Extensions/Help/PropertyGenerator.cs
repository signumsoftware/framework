using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Reflection;
using System.Reflection;
using Signum.Utilities;
using Signum.Utilities.Reflection;

namespace Signum.Engine.Help
{
    internal class PropertyGenerator
    {
        internal static Dictionary<string, PropertyInfo> GenerateProperties(Type type)
        {
            return Reflector.PublicInstancePropertiesInOrder(type)
                .SelectMany(pi =>
                {
                    KeyValuePair<string, PropertyInfo> pair = new KeyValuePair<string, PropertyInfo>(pi.Name, pi);

                    if (Reflector.IsEmbeddedEntity(pi.PropertyType))
                    {
                        var stack = GenerateEmbeddedProperties(pi.PropertyType, pi.Name + ".");
                        return stack.PreAnd(pair);
                    }

                    if (Reflector.IsMList(pi.PropertyType))
                    {
                        Type colType = ReflectionTools.CollectionType(pi.PropertyType);
                        if (Reflector.IsEmbeddedEntity(colType))
                        {
                            var list = GenerateEmbeddedProperties(colType, pi.Name + "/");
                            return list.PreAnd(pair);
                        }
                    }

                    return new[] { pair };
                }).ToDictionary();
        }

        internal static Dictionary<string, PropertyInfo> GenerateEmbeddedProperties(Type type, string prefix)
        {
            return Reflector.PublicInstancePropertiesInOrder(type)
                .SelectMany(pi =>
                {
                    KeyValuePair<string, PropertyInfo> pair = new KeyValuePair<string, PropertyInfo>(prefix + pi.Name, pi);

                    if (Reflector.IsEmbeddedEntity(pi.PropertyType))
                    {
                        var list = GenerateEmbeddedProperties(pi.PropertyType, prefix + pi.Name + ".");
                        return list.PreAnd(pair);
                    }

                    return new[] { pair };
                }).ToDictionary();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Signum.Entities.Reflection;
using System.Reflection;
using Signum.Utilities;
using Signum.Entities.Operations;
using Signum.Engine.DynamicQuery;
using Signum.Utilities.Reflection;
using Signum.Engine.Operations;

namespace Signum.Engine.Help
{
    class EntityHelp
    {
        public Type Type;
        public Dictionary<string, PropertyInfo> Properties;
        public Dictionary<Enum, OperationInfo> Operations;
        public Dictionary<object, IDynamicQuery> Queries;

        public bool HasHelp()
        {
            return Properties != null && Operations != null && _Queries != null;
        }

        public static EntityHelp Create(Type t, Assembly targetAssembly, HashSet<Assembly> forbiddenAssemblies)
        {
            return new EntityHelp
            {
                Type = t,
                Properties = GenerateProperties(t)
                            .ToDictionary()
                            .Collapse(),

                Operations = OperationLogic.GetAllOperationInfos(t)
                            .Where(oi => !forbiddenAssemblies.Contains(oi.Key.GetType().Assembly))
                            .ToDictionary(a => a.Key)
                            .Collapse(),

                Queries = (from qn in DynamicQueryManager.Current.GetQueryNames(t)
                           select qn).Distinct(kvp => kvp.Key)
                           .Where(oi => !forbiddenAssemblies.Contains(oi.Key.GetType().Assembly))
                           .ToDictionary()
                           .Collapse()
            };
        }

        static List<KeyValuePair<string, PropertyInfo>> GenerateProperties(Type type)
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
                            var stack = GenerateEmbeddedProperties(colType, pi.Name + "/");
                            return stack.PreAnd(pair);
                        }
                    }

                    return new[] { pair };
                }).ToList();
        }

        static List<KeyValuePair<string, PropertyInfo>> GenerateEmbeddedProperties(Type type, string prefix)
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
                }).ToList();
        }

        public static EntityHelp CreateOverride(Type t, Assembly targetAssembly, Assembly overriderAssembly)
        {
            return new EntityHelp
            {
                Type = t,

                Properties = null,

                Operations = OperationLogic.GetAllOperationInfos(t)
                            .Where(oi => oi.Key.GetType().Assembly == overriderAssembly)
                            .ToDictionary(a => a.Key)
                            .Collapse(),

                Queries = (from qn in DynamicQueryManager.Current.GetQueryNames(t)
                           select qn)
                           .Distinct(kvp => kvp.Key)
                           .Where(oi => oi.Key.GetType().Assembly == overriderAssembly)
                           .ToDictionary()
                           .Collapse()
            };
        }

        public XElement ToXml()
        {
            return new XElement(_Entity, new XAttribute(_Name, Type.Name),
                       Properties.TryCC(ps => new XElement(_Properties,
                         ps.Select(p => new XElement(_Property, new XAttribute(_Name, p.Key), HelpGenerator.GetPropertyHelp(Type, p.Value))))),
                       Operations.TryCC(os => new XElement(_Operations,
                         os.Select(o => new XElement(_Operation, new XAttribute(_Key, o.Key), HelpGenerator.GetOperationHelp(Type, o.Value))))),
                       Queries.TryCC(os => new XElement(_Queries,
                         os.Select(o => new XElement(_Query, new XAttribute(_Key, o.Key), HelpGenerator.GetQueryHelp(Type, o.Value)))))
                   );
        }

        static readonly XName _Name = "Name";
        static readonly XName _Key = "Key";
        static readonly XName _Entity = "Entity";
        static readonly XName _Properties = "Properties";
        static readonly XName _Property = "Property";
        static readonly XName _Operations = "Operations";
        static readonly XName _Operation = "Operation";
        static readonly XName _Queries = "Queries";
        static readonly XName _Query = "Query";
    }

    static class EntityHelpExtensions
    {
        public static Dictionary<K, V> Collapse<K, V>(this Dictionary<K, V> collection)
        {
            if (collection.Count == 0)
                return null;
            return collection;
        }
    }
}

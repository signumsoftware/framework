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
    public class EntityHelp
    {
        public Type Type;
        public string Description;
        public Dictionary<string, PropertyHelp> Properties;
        public Dictionary<Enum, OperationHelp> Operations;
        public Dictionary<object, QueryHelp> Queries;

        public bool HasHelp()
        {
            return Properties != null && Operations != null && _Queries != null;
        }

        public static EntityHelp Create(Type t, Assembly targetAssembly, HashSet<Assembly> forbiddenAssemblies)
        {
            return new EntityHelp
            {
                Type = t,
                Properties = PropertyGenerator.GenerateProperties(t)
                            .ToDictionary(
                                kvp=>kvp.Key, 
                                kvp=>new PropertyHelp(kvp.Value, HelpGenerator.GetPropertyHelp(t, kvp.Value), null)) 
                            .Collapse(),

                Operations = OperationLogic.GetAllOperationInfos(t)
                            .Where(oi => !forbiddenAssemblies.Contains(oi.Key.GetType().Assembly))
                            .ToDictionary(
                                oi => oi.Key,
                                oi => new OperationHelp(oi, HelpGenerator.GetOperationHelp(t, oi), null))
                            .Collapse(),

                Queries = DynamicQueryManager.Current.GetQueryNames(t)
                           .Distinct(kvp => kvp.Key)
                           .Where(qn => !forbiddenAssemblies.Contains(qn.Key.GetType().Assembly))
                           .ToDictionary(
                                kvp => kvp.Key, 
                                kvp => new QueryHelp(kvp.Value, HelpGenerator.GetQueryHelp(t, kvp.Value), null))
                           .Collapse()
            };
        }

        public static EntityHelp CreateOverride(Type t, Assembly targetAssembly, Assembly overriderAssembly)
        {
            return new EntityHelp
            {
                Type = t,

                Properties = null,

                Operations = OperationLogic.GetAllOperationInfos(t)
                            .Where(oi => oi.Key.GetType().Assembly == overriderAssembly)
                             .ToDictionary(
                                oi => oi.Key,
                                oi => new OperationHelp(oi, HelpGenerator.GetOperationHelp(t, oi), null))
                            .Collapse(),

                Queries = DynamicQueryManager.Current.GetQueryNames(t)
                           .Distinct(kvp => kvp.Key)
                           .Where(oi => oi.Key.GetType().Assembly == overriderAssembly)
                            .ToDictionary(
                                kvp => kvp.Key,
                                kvp => new QueryHelp(kvp.Value, HelpGenerator.GetQueryHelp(t, kvp.Value), null))
                           .Collapse()
            };
        }

     
       

    

        public XElement ToXml()
        {
            return new XElement(_Entity, new XAttribute(_Name, Type.Name),
                       Description.TryCC(d=>new XElement(_Description, d)),
                       Properties.TryCC(ps => new XElement(_Properties,
                         ps.Select(p => new XElement(_Property, new XAttribute(_Name, p.Key), p.Value.Description)))),
                       Operations.TryCC(os => new XElement(_Operations,
                         os.Select(o => new XElement(_Operation, new XAttribute(_Key, o.Key), o.Value)))),
                       Queries.TryCC(qs => new XElement(_Queries,
                         qs.Select(q => new XElement(_Query, new XAttribute(_Key, q.Key), q.Value))))
                   );
        }

        static readonly XName _Name = "Name";
        static readonly XName _Key = "Key";
        static readonly XName _Entity = "Entity";
        static readonly XName _Description = "Description";
        static readonly XName _Properties = "Properties";
        static readonly XName _Property = "Property";
        static readonly XName _Operations = "Operations";
        static readonly XName _Operation = "Operation";
        static readonly XName _Queries = "Queries";
        static readonly XName _Query = "Query";

        public static EntityHelp Load(Type type, XElement element, string sourceFile)
        {
            var properties = PropertyGenerator.GenerateProperties(type);
            var operations = OperationLogic.GetAllOperationInfos(type).ToDictionary(oi => oi.Key.ToString());
            var queries = DynamicQueryManager.Current.GetQueryNames(type).ToDictionary(oi => oi.Key.ToString());

            return new EntityHelp
            {
                Type = type,

                Description = element.Element(_Description).TryCC(d => d.Value),

                Properties = element.Element(_Properties).TryCC(ps => ps.Elements(_Property).ToDictionary(
                    p => p.Attribute(_Name).Value,
                    p => new PropertyHelp(properties[p.Attribute(_Name).Value], p.Value, sourceFile))),

                Operations = element.Element(_Operations).TryCC(os => os.Elements(_Operation).ToDictionary(
                    o => operations[o.Attribute(_Name).Value].Key,
                    o => new OperationHelp(operations[o.Attribute(_Name).Value], o.Value, sourceFile))),

                Queries = element.Element(_Queries).TryCC(qs => qs.Elements(_Query).ToDictionary(
                    q => queries[q.Attribute(_Name).Value].Key,
                    q => new QueryHelp(queries[q.Attribute(_Name).Value].Value, q.Value, sourceFile))),
            };
        }
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


    public class PropertyHelp
    {
        public PropertyHelp(PropertyInfo propertyInfo, string description, string sourceFile)
        {
            this.PropertyInfo = propertyInfo;
            this.Description = description; 
            this.SourceFile = sourceFile; 
        }

        public string SourceFile {get;private set;}
        public PropertyInfo PropertyInfo { get; private set; }
        public string Description { get; private set; }
    }

    public class OperationHelp
    {
        public OperationHelp(OperationInfo operationInfo, string description, string sourceFile)
        {
            this.OperationInfo = operationInfo;
            this.Description = description; 
            this.SourceFile = sourceFile; 
        }

        public string SourceFile {get;private set;}
        public OperationInfo OperationInfo { get; private set; }
        public string Description { get; private set; }
    }

    public class QueryHelp
    {
        public QueryHelp(IDynamicQuery dynamicQuery, string description, string sourceFile)
        {
            this.DynamicQuery = dynamicQuery;
            this.Description = description; 
            this.SourceFile = sourceFile; 
        }

        public string SourceFile {get;private set;}
        public IDynamicQuery DynamicQuery { get; private set; }
        public string Description { get; private set; }
    }
}

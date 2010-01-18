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
using Signum.Entities.DynamicQuery;
using Signum.Engine.Maps;

namespace Signum.Engine.Help
{
    public class EntityHelp
    {
        public Type Type;
        public string Description;
        public Dictionary<string, PropertyHelp> Properties;
        public Dictionary<string, Dictionary<string, PropertyHelp>> PropertiesRelations;
        public Dictionary<Enum, string> Operations;
        public Dictionary<object, string> Queries;
        public string FileName;

        public static EntityHelp Create(Type t)
        {
            return new EntityHelp
            {
                Type = t,
                //Description = "Type description for {0} here".Formato(t.NiceName()),
                Description = "",
                Properties = PropertyGenerator.GenerateProperties(t)
                            .ToDictionary(
                                kvp=>kvp.Key, 
                                kvp=>new PropertyHelp(kvp.Value, HelpGenerator.GetPropertyHelp(t, kvp.Value))),

                Operations = OperationLogic.GetAllOperationInfos(t)
                            .ToDictionary(
                                oi => oi.Key,
                                oi => HelpGenerator.GetOperationHelp(t, oi)),

                Queries = DynamicQueryManager.Current.GetQueryNames(t)
                           .ToDictionary(
                                kvp => kvp.Key, 
                                kvp => HelpGenerator.GetQueryHelp(t, kvp.Value))
            };            
        }
        
        public XElement ToXml()
        {
            return new XElement(_Entity, new XAttribute(_Name, Type.Name),
                       new XElement(_Description, Description),
                       Properties.Map(ps => ps == null || ps.Count == 0 ? null : new XElement(_Properties,
                         ps.Select(p => new XElement(_Property, new XAttribute(_Name, p.Key), new XAttribute(_Info,p.Value.Info), p.Value.UserDescription )))),
                       Operations.Map(os => os == null || os.Count == 0 ? null : new XElement(_Operations,
                         os.Select(o => new XElement(_Operation, new XAttribute(_Key, OperationDN.UniqueKey(o.Key)), o.Value)))),
                       Queries.Map(qs => qs == null || qs.Count == 0 ? null : new XElement(_Queries,
                         qs.Select(q => new XElement(_Query, new XAttribute(_Key, QueryUtils.GetQueryName(q.Key)), q.Value))))
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
        static readonly XName _Info = "Info";

        public static EntityHelp Load(Type type, XElement element, string sourceFile)
        {
            var properties = PropertyGenerator.GenerateProperties(type);
            var operations = OperationLogic.GetAllOperationInfos(type).ToDictionary(oi => OperationDN.UniqueKey(oi.Key));
            var queries = DynamicQueryManager.Current.GetQueryNames(type).ToDictionary(oi => QueryUtils.GetQueryName(oi.Key));

            return new EntityHelp
            {
                Type = type,
                FileName = sourceFile,
                Description = element.Element(_Description).TryCC(d => d.Value),

                Properties = element.Element(_Properties).TryCC(ps => ps.Elements(_Property).ToDictionary(
                    p => p.Attribute(_Name).Value,
                    p => new PropertyHelp(properties[p.Attribute(_Name).Value], p.Attribute(_Info).Value,p.Value))),

                Operations = element.Element(_Operations).TryCC(os => os.Elements(_Operation).ToDictionary(
                    o => operations[o.Attribute(_Key).Value].Key,
                    o => o.Value)),

                Queries = element.Element(_Queries).TryCC(qs => qs.Elements(_Query).ToDictionary(
                    q => queries[q.Attribute(_Key).Value].Key,
                    q => q.Value)),
            };
        }
    }

    public class PropertyHelp
    {
        public PropertyHelp(PropertyInfo propertyInfo, string info)
        {
            this.PropertyInfo = propertyInfo;
            this.Info = info;
        }

        public PropertyHelp(PropertyInfo propertyInfo, string info, string userDescription)
        {
            this.PropertyInfo = propertyInfo;
            this.Info = info;
            this.UserDescription = userDescription;
        }

        public string Info { get; private set; }
        public string UserDescription { get; set; }
        public PropertyInfo PropertyInfo { get; private set; }

        public override string ToString()
        {
            return PropertyInfo.NiceName() + " | " + this.Info + (this.UserDescription.HasText() ? " | " + this.UserDescription : "");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Signum.Entities.Reflection;
using System.Reflection;
using Signum.Utilities;
using Signum.Engine.DynamicQuery;
using Signum.Utilities.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Engine.Maps;
using System.Text.RegularExpressions;
using Signum.Entities.Basics;
using Signum.Entities;
using System.IO;
using System.Globalization;
using Signum.Engine.Operations;
using Signum.Entities.Help;
using Signum.Engine.Basics;

namespace Signum.Engine.Help
{
    public class AppendixHelp
    {
        public readonly string UniqueName;
        public readonly string Title;
        public readonly string Description;
        public readonly CultureInfo Culture;
        public readonly AppendixHelpDN Entity;

        public AppendixHelp(CultureInfo culture, AppendixHelpDN entity)
        {
            Culture = culture;
            UniqueName = entity.UniqueName;
            Title = entity.Title;
            Description = entity.Description;
            Entity = entity;
        }
    }

    public class NamespaceHelp
    {
        public readonly string Namespace;
        public readonly string Before;
        public readonly string Title;
        public readonly string Description;
        public readonly CultureInfo Culture;
        public readonly NamespaceHelpDN Entity;

        public NamespaceHelp(string @namespace, CultureInfo culture, NamespaceHelpDN entity)
        {
            Culture = culture;
            Namespace = @namespace;

            var clean = @namespace.Replace(".Entities", "");

            Title = entity.Try(a => a.Title.DefaultText(null)) ?? clean.TryAfterLast('.') ?? clean;

            Before = clean.TryBeforeLast('.');

            Description = entity == null ? null : entity.Description;
            Entity = entity ?? new NamespaceHelpDN
            {
                Culture = this.Culture.ToCultureInfoDN(),
                Name = this.Namespace
            };
        }

    }

    public class EntityHelp
    {
        public readonly Type Type;
        public readonly CultureInfo Culture;

        public readonly EntityHelpDN Entity;

        public readonly string Info;
        public readonly string Description;

        public readonly Dictionary<string, PropertyHelp> Properties;
        public readonly Dictionary<OperationSymbol, OperationHelp> Operations;
        public Dictionary<object, QueryHelp> Queries
        {
            get { return HelpLogic.GetQueryHelps(this.Type).ToDictionary(qh => qh.Key); }
        }

        public EntityHelp(Type type, CultureInfo culture, EntityHelpDN entity)
        {
            Type = type;
            Culture = culture;
            Info = HelpGenerator.GetEntityHelp(type);
            
            Properties = PropertyRoute.GenerateRoutes(type)
                        .ToDictionary(
                            pp => pp.PropertyString(),
                            pp => new PropertyHelp(pp, HelpGenerator.GetPropertyHelp(pp)));

            Operations = GetOperations(type)
                        .ToDictionary(
                            oi => oi.OperationSymbol,
                            oi => new OperationHelp(oi.OperationSymbol, HelpGenerator.GetOperationHelp(type, oi)));

            if (entity != null)
            {
                Entity = entity;

                Description = entity.Description;

                foreach (var tranProp in entity.Properties)
                {
                    Properties.GetOrThrow(tranProp.Property.Path).UserDescription = tranProp.Description;
                }

                foreach (var transOper in entity.Operations)
                {
                    Operations.GetOrThrow(transOper.Operation).UserDescription = transOper.Description;
                }
            }
            else
            {
                Entity = new EntityHelpDN
                {
                    Culture = this.Culture.ToCultureInfoDN(),
                    Type = this.Type.ToTypeDN(),
                };
            }

            Entity.Properties.AddRange(
               PropertyRouteLogic.RetrieveOrGenerateProperties(this.Type.ToTypeDN())
               .Except(Entity.Properties.Select(a => a.Property))
               .Select(pr => new PropertyRouteHelpDN
               {
                   Property = pr,
                   Description = null,
               }));

            Entity.Operations.AddRange(
                Operations.Keys
                .Except(Entity.Operations.Select(a => a.Operation))
                .Select(oper => new OperationHelpDN
                {
                    Operation = oper,
                    Description = null,
                }));

            Entity.Queries.AddRange(this.Queries.Values.Select(a => a.Entity).ToList());
        }

        public static IEnumerable<OperationInfo> GetOperations(Type type)
        {
            return OperationLogic.GetAllOperationInfos(type)
                                    .Where(oi => OperationLogic.FindTypes(oi.OperationSymbol).Any(TypeLogic.TypeToDN.ContainsKey));
        }

       
    }

    public class PropertyHelp
    {
        public PropertyHelp(PropertyRoute propertyRoute, string info)
        {
            if(propertyRoute.PropertyRouteType != PropertyRouteType.FieldOrProperty)
                throw new ArgumentException("propertyRoute should be of type Property"); 

            this.PropertyRoute = propertyRoute;
            this.Info = info;
        }

        public readonly string Info;
        public string UserDescription;
        public readonly PropertyRoute PropertyRoute;
        public PropertyInfo PropertyInfo { get { return PropertyRoute.PropertyInfo; } }

        public override string ToString()
        {
            return Info + (UserDescription.HasText() ? " | " + UserDescription : "");
        }
    }

    public class OperationHelp
    {
        public OperationHelp(OperationSymbol operationSymbol, string info)
        {
            this.OperationSymbol = operationSymbol;
            this.Info = info;
        }

        public readonly OperationSymbol OperationSymbol;
        public readonly string Info;
        public string UserDescription;

        public override string ToString()
        {
            return Info + (UserDescription.HasText() ? " | " + UserDescription : "");
        }
    }

    public class QueryHelp
    {
        public readonly object Key;
        public readonly CultureInfo Culture;

        public readonly QueryHelpDN Entity;
        public readonly string UserDescription;
        public readonly string Info;
        public readonly Dictionary<string, QueryColumnHelp> Columns;
        

        public QueryHelp(object key, CultureInfo ci, QueryHelpDN entity)
        {
            Key = key;
            Culture = ci;
            Info = HelpGenerator.GetQueryHelp(DynamicQueryManager.Current.GetQuery(key).Core.Value);
            Columns = DynamicQueryManager.Current.GetQuery(key).Core.Value.StaticColumns.ToDictionary(
                            kvp => kvp.Name,
                            kvp => new QueryColumnHelp(kvp.Name, HelpGenerator.GetQueryColumnHelp(kvp)));


            if (entity != null)
            {
                Entity = entity;

                UserDescription = entity.Description;

                foreach (var tranColumn in entity.Columns)
                {
                    Columns.GetOrThrow(tranColumn.ColumnName).UserDescription = tranColumn.Description;
                }
            }
            else
            {
                Entity = new QueryHelpDN
                {
                    Culture = this.Culture.ToCultureInfoDN(),
                    Query = QueryLogic.GetQuery(this.Key),
                };
            }

            Entity.Columns.AddRange(
               DynamicQueryManager.Current.GetQuery(this.Key).Core.Value.StaticColumns.Select(a => a.Name)
               .Except(Entity.Columns.Select(a => a.ColumnName))
               .Select(pr => new QueryColumnHelpDN
               {
                   ColumnName = pr,
                   Description = null,
               }));
        }
    }

    public class QueryColumnHelp
    {
        public string Name;
        public string Info;
        public string UserDescription;

        public QueryColumnHelp(string name, string info)
        {
            this.Name = name;
            this.Info = info;
        }
    }
}

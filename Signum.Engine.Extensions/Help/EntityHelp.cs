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

        public readonly bool HasEntity; 
        public readonly Lazy<EntityHelpDN> Entity;

        public readonly string Info;
        public readonly string Description;

        public readonly Dictionary<PropertyRoute, PropertyHelp> Properties;
        public readonly Dictionary<OperationSymbol, OperationHelp> Operations;
        public readonly Dictionary<object, QueryHelp> Queries;

        public EntityHelp(Type type, CultureInfo culture, EntityHelpDN entity)
        {
            Type = type;
            Culture = culture;
            Info = HelpGenerator.GetEntityHelp(type);
            
            Properties = PropertyRoute.GenerateRoutes(type)
                        .ToDictionary(
                            pp => pp,
                            pp => new PropertyHelp(pp, HelpGenerator.GetPropertyHelp(pp)));

            Operations = HelpLogic.GetOperationHelps(this.Type).ToDictionary(a=>a.OperationSymbol);

            Queries = HelpLogic.GetQueryHelps(this.Type).ToDictionary(qh => qh.Key);

            if (entity != null)
            {
                HasEntity = true;

                Description = entity.Description;

                foreach (var tranProp in entity.Properties)
                {
                    Properties.GetOrThrow(tranProp.Property.ToPropertyRoute()).UserDescription = tranProp.Description;
                }

                foreach (var transOper in entity.Operations)
                {
                    Operations.GetOrThrow(transOper.Operation).UserDescription = transOper.Description;
                }
            }

            Entity = new Lazy<EntityHelpDN>(() =>
            {
                if (entity == null)
                    entity = new EntityHelpDN
                    {
                        Culture = this.Culture.ToCultureInfoDN(),
                        Type = this.Type.ToTypeDN(),
                    };

                entity.Properties.AddRange(
                   PropertyRouteLogic.RetrieveOrGenerateProperties(this.Type.ToTypeDN())
                   .Except(entity.Properties.Select(a => a.Property))
                   .Select(pr => new PropertyRouteHelpDN
                   {
                       Property = pr,
                       Description = null,
                   }));

                entity.Operations.AddRange(this.Operations.Values.Select(o => o.Entity.Value).ToList());

                entity.Queries.AddRange(this.Queries.Values.Select(a => a.Entity.Value).ToList());

                return entity;
            });
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
        public OperationHelp(OperationSymbol operationSymbol, CultureInfo ci, OperationHelpDN entity)
        {
            this.OperationSymbol = operationSymbol;
            this.Culture = ci;

            this.Info = HelpGenerator.GetOperationHelp(operationSymbol);

            if (entity != null)
            {
                HasEntity = true;

                UserDescription = entity.Description;
            }

            Entity = new Lazy<OperationHelpDN>(() =>
            {
                if (entity == null)
                    entity = new OperationHelpDN
                    {
                        Culture = this.Culture.ToCultureInfoDN(),
                        Operation = this.OperationSymbol,
                    };

                return entity;
            });

        }

        public readonly CultureInfo Culture;
        public readonly OperationSymbol OperationSymbol;
        public readonly bool HasEntity;
        public readonly Lazy<OperationHelpDN> Entity;
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

        public readonly bool HasEntity;
        public readonly Lazy<QueryHelpDN> Entity;
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
                            kvp => new QueryColumnHelp(kvp.Name, kvp.DisplayName(), HelpGenerator.GetQueryColumnHelp(kvp)));

            if (entity != null)
            {
                HasEntity = true;

                UserDescription = entity.Description;

                foreach (var tranColumn in entity.Columns)
                {
                    Columns.GetOrThrow(tranColumn.ColumnName).UserDescription = tranColumn.Description;
                }
            }

            Entity = new Lazy<QueryHelpDN>(() =>
            {
                if (entity == null)
                    entity = new QueryHelpDN
                    {
                        Culture = this.Culture.ToCultureInfoDN(),
                        Query = QueryLogic.GetQuery(this.Key),
                    };

                entity.Columns.AddRange(
                     DynamicQueryManager.Current.GetQuery(this.Key).Core.Value.StaticColumns.Select(a => a.Name)
                     .Except(entity.Columns.Select(a => a.ColumnName))
                     .Select(pr => new QueryColumnHelpDN
                     {
                         ColumnName = pr,
                         Description = null,
                     }));

                return entity;
            });
        }
    }

    public class QueryColumnHelp
    {
        public string Name;
        public string NiceName; 
        public string Info;
        public string UserDescription;

        public QueryColumnHelp(string name, string niceName, string info)
        {
            this.NiceName = niceName;
            this.Name = name;
            this.Info = info;
        }
    }
}

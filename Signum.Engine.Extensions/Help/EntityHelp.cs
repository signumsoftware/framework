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

    public abstract class BaseHelp
    {
        public abstract string IsAllowed();

        public void AssertAllowed()
        {
            string error = IsAllowed();
            if (error != null)
                throw new UnauthorizedAccessException(EngineMessage.UnauthorizedAccessTo0Because1.NiceToString().FormatWith(this.GetType(), error));
        }

        public abstract override string ToString();
    }

    public class AppendixHelp : BaseHelp
    {
        public readonly string UniqueName;
        public readonly string Title;
        public readonly string Description;
        public readonly CultureInfo Culture;
        public readonly AppendixHelpEntity Entity;

        public AppendixHelp(CultureInfo culture, AppendixHelpEntity entity)
        {
            Culture = culture;
            UniqueName = entity.UniqueName;
            Title = entity.Title;
            Description = entity.Description;
            Entity = entity;
        }

        public override string IsAllowed()
        {
            return null;
        }

        public override string ToString()
        {
            return "Appendix " + UniqueName;
        }
    }

    public class NamespaceHelp : BaseHelp
    {
        public readonly string Namespace;
        public readonly string Before;
        public readonly string Title;
        public readonly string Description;
        public readonly CultureInfo Culture;
        public readonly NamespaceHelpEntity Entity;
        public readonly Type[] Types;

        public NamespaceHelp(string @namespace, CultureInfo culture, NamespaceHelpEntity entity, Type[] types)
        {
            Culture = culture;
            Namespace = @namespace;

            Types = types;

            var clean = @namespace.Replace(".Entities", "");

            Title = entity.Try(a => a.Title.DefaultText(null)) ?? clean.TryAfterLast('.') ?? clean;

            Before = clean.TryBeforeLast('.');

            Description = entity == null ? null : entity.Description;
            Entity = entity ?? new NamespaceHelpEntity
            {
                Culture = this.Culture.ToCultureInfoEntity(),
                Name = this.Namespace
            };
        }


        public override string IsAllowed()
        {
            Schema s = Schema.Current;

            if (Types.Any(t => s.IsAllowed(t, inUserInterface: true) == null))
                return null;

            return "all the types in the nemespace are not allowed";
        }

        public override string ToString()
        {
            return "Namespace " + Namespace;
        }
    }

    public class EntityHelp : BaseHelp
    {
        public readonly Type Type;
        public readonly CultureInfo Culture;

        public readonly bool HasEntity; 
        public readonly Lazy<EntityHelpEntity> Entity;

        public readonly string Info;
        public readonly string Description;

        public readonly Dictionary<PropertyRoute, PropertyHelp> Properties;
        public readonly Dictionary<OperationSymbol, OperationHelp> Operations;
        public readonly Dictionary<object, QueryHelp> Queries;

        public EntityHelp(Type type, CultureInfo culture, EntityHelpEntity entity)
        {
            Type = type;
            Culture = culture;
            Info = HelpGenerator.GetEntityHelp(type);
            
            Properties = PropertyRoute.GenerateRoutes(type)
                        .ToDictionary(
                            pp => pp,
                            pp => new PropertyHelp(pp, HelpGenerator.GetPropertyHelp(pp)));


            var allOperations = HelpLogic.CachedOperationsHelp();

            Operations = OperationLogic.GetAllOperationInfos(type).Select(oi=>allOperations.GetOrThrow(oi.OperationSymbol)).ToDictionary(a=>a.OperationSymbol);

            var allQueries = HelpLogic.CachedQueriesHelp();

            Queries =  HelpLogic.TypeToQuery.Value.TryGetC(this.Type).EmptyIfNull().Select(a=>allQueries.GetOrThrow(a)).ToDictionary(qh => qh.QueryName);

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

            Entity = new Lazy<EntityHelpEntity>(() => HelpLogic.GlobalContext(() =>
            {
                if (entity == null)
                    entity = new EntityHelpEntity
                    {
                        Culture = this.Culture.ToCultureInfoEntity(),
                        Type = this.Type.ToTypeEntity(),
                    };

                entity.Properties.AddRange(
                   PropertyRouteLogic.RetrieveOrGenerateProperties(this.Type.ToTypeEntity())
                   .Except(entity.Properties.Select(a => a.Property))
                   .Select(pr => new PropertyRouteHelpEntity
                   {
                       Property = pr,
                       Description = null,
                   }));

                entity.Operations.AddRange(this.Operations.Values.Select(o => o.Entity.Value).ToList());

                entity.Queries.AddRange(this.Queries.Values.Select(a => a.Entity.Value).ToList());

                return entity;
            }));
        }

        public override string IsAllowed()
        {
            return Schema.Current.IsAllowed(Type, inUserInterface: true);
        }

        public override string ToString()
        {
            return "Type " + TypeLogic.GetCleanName(Type); 
        }
       
    }

    public class PropertyHelp : BaseHelp
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

        public override string IsAllowed()
        {
            return PropertyRoute.IsAllowed();
        }

        public override string ToString()
        {
            return "Property " + this.PropertyRoute.ToString();
        }
    }

    public class OperationHelp : BaseHelp
    {
        public OperationHelp(OperationSymbol operationSymbol, CultureInfo ci, OperationHelpEntity entity)
        {
            this.OperationSymbol = operationSymbol;
            this.Culture = ci;

            this.Info = HelpGenerator.GetOperationHelp(operationSymbol);

            if (entity != null)
            {
                HasEntity = true;

                UserDescription = entity.Description;
            }

            Entity = new Lazy<OperationHelpEntity>(() => HelpLogic.GlobalContext(() =>
            {
                if (entity == null)
                    entity = new OperationHelpEntity
                    {
                        Culture = this.Culture.ToCultureInfoEntity(),
                        Operation = this.OperationSymbol,
                    };

                return entity;
            }));

        }

        public readonly CultureInfo Culture;
        public readonly OperationSymbol OperationSymbol;
        public readonly bool HasEntity;
        public readonly Lazy<OperationHelpEntity> Entity;
        public readonly string Info;
        public string UserDescription;

        public override string IsAllowed()
        {
            return OperationLogic.OperationAllowed(OperationSymbol, inUserInterface: true) ? null :
                OperationMessage.Operation01IsNotAuthorized.NiceToString(this.OperationSymbol.NiceToString(), this.OperationSymbol.Key);
        }

        public override string ToString()
        {
            return "Operation " + this.OperationSymbol.Key;
        }
    }

    public class QueryHelp : BaseHelp
    {
        public readonly object QueryName;
        public readonly CultureInfo Culture;

        public readonly bool HasEntity;
        public readonly Lazy<QueryHelpEntity> Entity;
        public readonly string UserDescription;
        public readonly string Info;
        public readonly Dictionary<string, QueryColumnHelp> Columns;

        public QueryHelp(object queryName, CultureInfo ci, QueryHelpEntity entity)
        {
            QueryName = queryName;
            Culture = ci;
            Info = HelpGenerator.GetQueryHelp(DynamicQueryManager.Current.GetQuery(queryName).Core.Value);
            Columns = DynamicQueryManager.Current.GetQuery(queryName).Core.Value.StaticColumns.ToDictionary(
                            cf => cf.Name,
                            cf => new QueryColumnHelp(cf, cf.DisplayName(), HelpGenerator.GetQueryColumnHelp(cf)));

            if (entity != null)
            {
                HasEntity = true;

                UserDescription = entity.Description;

                foreach (var tranColumn in entity.Columns)
                {
                    Columns.GetOrThrow(tranColumn.ColumnName).UserDescription = tranColumn.Description;
                }
            }

            Entity = new Lazy<QueryHelpEntity>(() => HelpLogic.GlobalContext(() =>
            {
                if (entity == null)
                    entity = new QueryHelpEntity
                    {
                        Culture = this.Culture.ToCultureInfoEntity(),
                        Query = QueryLogic.GetQueryEntity(this.QueryName),
                    };

                entity.Columns.AddRange(
                     DynamicQueryManager.Current.GetQuery(this.QueryName).Core.Value.StaticColumns.Select(a => a.Name)
                     .Except(entity.Columns.Select(a => a.ColumnName))
                     .Select(pr => new QueryColumnHelpEntity
                     {
                         ColumnName = pr,
                         Description = null,
                     }));

                return entity;
            }));
        }

        public override string ToString()
        {
            return "Query " + QueryUtils.GetQueryUniqueKey(this.QueryName);
        }

        public override string IsAllowed()
        {
            return DynamicQueryManager.Current.QueryAllowed(this.QueryName) ? null :
                "Access to query {0} not allowed".FormatWith(QueryUtils.GetQueryUniqueKey(this.QueryName)); 
        }
    }

    public class QueryColumnHelp : BaseHelp
    {
        public ColumnDescriptionFactory Column;
        public string NiceName; 
        public string Info;
        public string UserDescription;

        public QueryColumnHelp(ColumnDescriptionFactory column, string niceName, string info)
        {
            this.Column = column;
            this.NiceName = niceName;
            this.Info = info;
        }

        public override string IsAllowed()
        {
            return Column.IsAllowed();
        }

        public override string ToString()
        {
            return "Column " + Column.Name;
        }
    }
}

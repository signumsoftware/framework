using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Signum.Utilities;
using Signum.Engine.DynamicQuery;
using Signum.Entities.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Entities.Basics;
using Signum.Entities;
using System.Globalization;
using Signum.Engine.Operations;
using Signum.Entities.Help;
using Signum.Engine.Basics;
using Newtonsoft.Json;
using DocumentFormat.OpenXml.EMMA;
using Signum.Entities.Reflection;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Engine.Help
{
    public abstract class BaseHelp
    {
        public abstract string? IsAllowed();

        public void AssertAllowed()
        {
            string? error = IsAllowed();
            if (error != null)
                throw new UnauthorizedAccessException(EngineMessage.UnauthorizedAccessTo0Because1.NiceToString().FormatWith(this.GetType(), error));
        }

        public abstract override string ToString();
    }
    
    public class NamespaceHelp : BaseHelp
    {
        public readonly string Namespace;
        public readonly string? Before;
        public readonly string Title;
        public readonly string? Description;
        [JsonIgnore]
        public readonly CultureInfo Culture;
        [JsonIgnore]
        public readonly NamespaceHelpEntity? DBEntity;
        [JsonIgnore]
        public readonly Type[] Types;


        public NamespaceHelp(string @namespace, CultureInfo culture, NamespaceHelpEntity? entity, Type[] types)
        {
            Culture = culture;
            Namespace = @namespace;

            Types = types;

            var clean = @namespace.Replace(".Entities", "");

            Title = entity?.Let(a => a.Title.DefaultToNull()) ?? clean.TryAfterLast('.') ?? clean;

            Before = clean.TryBeforeLast('.');

            Description = entity?.Description;
            DBEntity = entity;
        }

        public NamespaceHelpEntity Entity
        {
            get
            {
                var result = new NamespaceHelpEntity
                {
                    Culture = this.Culture.ToCultureInfoEntity(),
                    Name = this.Namespace,
                };

                if (DBEntity != null)
                {
                    result.Title = DBEntity.Title;
                    result.Description = DBEntity.Description;
                    result.SetId(DBEntity.Id);
                    result.SetIsNew(DBEntity.IsNew);
                    result.Ticks = DBEntity.Ticks;
                }


                return result;
            }
        }

        public EntityItem[] AllowedTypes
        {
            get
            {
                Schema s = Schema.Current;
                return Types.Where(t => s.IsAllowed(t, inUserInterface: true) == null).Select(t => new EntityItem(t)).ToArray();
            }
        }

        public override string? IsAllowed()
        {
            if (AllowedTypes.Any())
                return null;

            return "all the types in the nemespace are not allowed";
        }

        public override string ToString()
        {
            return "Namespace " + Namespace;
        }
    }

    public class EntityItem
    {
        public string CleanName;
        public bool HasDescription;

        public EntityItem(Type t)
        {
            CleanName = TypeLogic.GetCleanName(t);
            HasDescription = HelpLogic.GetTypeHelp(t).HasEntity;
        }
    }

    public class TypeHelp : BaseHelp
    {
        public readonly Type Type;
        public readonly CultureInfo Culture;

        public readonly bool HasEntity; 

        public TypeHelpEntity? DBEntity; 

        public readonly string Info;

        public readonly Dictionary<PropertyRoute, PropertyHelp> Properties;
        public readonly Dictionary<OperationSymbol, OperationHelp> Operations;
        public readonly Dictionary<object, QueryHelp> Queries;

        public TypeHelp(Type type, CultureInfo culture, TypeHelpEntity? entity)
        {
            Type = type;
            Culture = culture;
            Info = HelpGenerator.GetEntityHelp(type);

            var props = DBEntity?.Properties.ToDictionaryEx(a => a.Property.ToPropertyRoute(), a => a.Info);
            var opers = DBEntity?.Operations.ToDictionaryEx(a => a.Operation, a => a.Info);

            Properties = PropertyRoute.GenerateRoutes(type)
                        .ToDictionary(pp => pp, pp => new PropertyHelp(pp, props?.TryGetC(pp)));

            Operations = OperationLogic.TypeOperations(type)
                        .ToDictionary(op => op.OperationSymbol, op => new OperationHelp(op.OperationSymbol, type, opers?.TryGetC(op.OperationSymbol)));

         
            var allQueries = HelpLogic.CachedQueriesHelp();

            Queries = HelpLogic.TypeToQuery.Value.TryGetC(this.Type).EmptyIfNull().Select(a => allQueries.GetOrThrow(a)).ToDictionary(qh => qh.QueryName);

            DBEntity = entity;
        }

        public TypeHelpEntity GetEntity()
        {
            var result = new TypeHelpEntity
            {
                Culture = this.Culture.ToCultureInfoEntity(),
                Type = this.Type.ToTypeEntity(),
                Description = DBEntity?.Description,
                Info = Info
            };

            result.Properties.AddRange(
                from pre in PropertyRouteLogic.RetrieveOrGenerateProperties(this.Type.ToTypeEntity())
                let pr = pre.ToPropertyRoute()
                where !(pr.PropertyInfo != null && pr.PropertyInfo.SetMethod == null && ExpressionCleaner.HasExpansions(pr.PropertyInfo.DeclaringType!, pr.PropertyInfo))
                let ph = Properties.GetOrThrow(pre.ToPropertyRoute())
                where ph.IsAllowed() == null
                select new PropertyRouteHelpEmbedded
                {
                    Property = pre,
                    Info = ph.Info,
                    Description = ph.UserDescription,
                });

            result.Operations.AddRange(
               from oh in Operations.Values
               where oh.IsAllowed() == null
               select new OperationHelpEmbedded
               {
                   Operation = oh.OperationSymbol,
                   Info = oh.Info,
                   Description = oh.UserDescription,
               });

            result.Queries.AddRange(
                from qn in QueryLogic.Queries.GetTypeQueries(this.Type).Keys
                let qh = HelpLogic.GetQueryHelp(qn)
                where qh.IsAllowed() == null
                select qh.GetEntity());

            if (DBEntity != null)
            {
                result.SetId(DBEntity.Id);
                result.SetIsNew(DBEntity.IsNew);
                result.Ticks = DBEntity.Ticks;
            }
            return result;
        }

        public override string? IsAllowed()
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
        public PropertyHelp(PropertyRoute propertyRoute, string? userDescription)
        {
            if(propertyRoute.PropertyRouteType != PropertyRouteType.FieldOrProperty)
                throw new ArgumentException("propertyRoute should be of type Property"); 

            this.PropertyRoute = propertyRoute;
            this.Info = HelpGenerator.GetPropertyHelp(propertyRoute);
            this.UserDescription = userDescription;
        }

        public readonly string Info;
        public readonly PropertyRoute PropertyRoute;
        public readonly string? UserDescription;
        public PropertyInfo PropertyInfo { get { return PropertyRoute.PropertyInfo!; } }

        public override string? IsAllowed()
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
        public readonly OperationSymbol OperationSymbol;
        public readonly Type Type;
        public readonly string Info;
        public readonly string? UserDescription;

        public OperationHelp(OperationSymbol operationSymbol, Type type, string? userDescription)
        {
            this.OperationSymbol = operationSymbol;
            this.Type = type;
            this.Info = HelpGenerator.GetOperationHelp(type, operationSymbol);
            this.UserDescription = userDescription;
        }
        
        public override string? IsAllowed()
        {
            return OperationLogic.OperationAllowed(OperationSymbol, this.Type, inUserInterface: true) ? null :
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
        public readonly string Info;
        public readonly Dictionary<string, QueryColumnHelp> Columns;
        public readonly QueryHelpEntity? DBEntity;
        public readonly string? UserDescription;

        public QueryHelp(object queryName, CultureInfo ci, QueryHelpEntity? entity)
        {
            QueryName = queryName;
            Culture = ci;
            Info = HelpGenerator.GetQueryHelp(QueryLogic.Queries.GetQuery(queryName).Core.Value);
            var cols = entity?.Columns.ToDictionary(a => a.ColumnName, a => a.Description);
            Columns = QueryLogic.Queries.GetQuery(queryName).Core.Value.StaticColumns.ToDictionary(
                            cf => cf.Name,
                            cf => new QueryColumnHelp(cf, cf.DisplayName(), HelpGenerator.GetQueryColumnHelp(cf), cols?.TryGetCN(cf.Name)));

            DBEntity = entity;
            UserDescription = entity?.Description;
        }

        public QueryHelpEntity GetEntity()
        {
            var cd = DBEntity?.Columns.ToDictionary(a => a.ColumnName, a => a.Description);

            var result = new QueryHelpEntity
            {
                Culture = this.Culture.ToCultureInfoEntity(),
                Query = QueryLogic.GetQueryEntity(this.QueryName),
                Description = DBEntity?.Description,
                Info = Info,
                Columns = this.Columns.Values.Where(a => a.Column.IsAllowed() == null)
                .Select(c => new QueryColumnHelpEmbedded
                {
                    ColumnName = c.Column.Name,
                    Description = cd?.TryGetCN(c.Column.Name)!,
                    NiceName = c.NiceName,
                    Info = c.Info,
                }).ToMList()
            };

            if (DBEntity != null)
            {
                result.SetId(DBEntity.Id);
                result.SetIsNew(DBEntity.IsNew);
                result.Ticks = DBEntity.Ticks;
            }
            return result;
        }

        public override string ToString()
        {
            return "Query " + QueryUtils.GetKey(this.QueryName);
        }

        public override string? IsAllowed()
        {
            return QueryLogic.Queries.QueryAllowed(this.QueryName, false) ? null :
                "Access to query {0} not allowed".FormatWith(QueryUtils.GetKey(this.QueryName)); 
        }
    }

    public class QueryColumnHelp : BaseHelp
    {
        public ColumnDescriptionFactory Column;
        public string NiceName; 
        public string Info;
        public string? UserDescription; 

        public QueryColumnHelp(ColumnDescriptionFactory column, string niceName, string info, string? userDescription)
        {
            this.Column = column;
            this.NiceName = niceName;
            this.Info = info;
            this.UserDescription = userDescription;
        }

        public override string? IsAllowed()
        {
            return Column.IsAllowed();
        }

        public override string ToString()
        {
            return "Column " + Column.Name;
        }
    }
}

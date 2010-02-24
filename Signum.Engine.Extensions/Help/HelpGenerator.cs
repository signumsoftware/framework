using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Operations;
using Signum.Utilities;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Linq;
using Signum.Engine.Extensions.Properties;
using Signum.Utilities.ExpressionTrees;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Entities;
using Signum.Engine.Maps;
using System.IO;

namespace Signum.Engine.Help
{
    public static class HelpGenerator
    {
        public static string GetPropertyHelp(Type type, PropertyInfo pi)
        {
            string validations = Validator.GetOrCreatePropertyPack(pi.DeclaringType, pi.Name).Validators.CommaAnd(v => v.HelpMessage);

            if (validations.HasText())
                validations = Resources.Should + validations;

            if (Reflector.IsIIdentifiable(pi.PropertyType))
            {
                return EntityProperty(type, pi, pi.PropertyType, pi.PropertyType.TypeLink()) + validations;
            }
            else if (typeof(Lite).IsAssignableFrom(pi.PropertyType))
            {
                Type cleanType = Reflector.ExtractLite(pi.PropertyType);

                return EntityProperty(type, pi, cleanType, cleanType.TypeLink()) + validations;
            }
            else if (Reflector.IsEmbeddedEntity(pi.PropertyType))
            {
                return EntityProperty(type, pi, pi.PropertyType, pi.PropertyType.NiceName());
            }
            else if (Reflector.IsMList(pi.PropertyType))
            {
                Type elemType = ReflectionTools.CollectionType(pi.PropertyType);

                if (Reflector.IsIIdentifiable(elemType))
                {
                    return Resources._0IsACollectionOfElements1.Formato(pi.NiceName(), elemType.TypeLink()) + validations;
                }
                else if (typeof(Lite).IsAssignableFrom(elemType))
                {
                    return Resources._0IsACollectionOfElements1.Formato(pi.NiceName(), Reflector.ExtractLite(elemType).TypeLink()) + validations;
                }
                else if (Reflector.IsEmbeddedEntity(pi.PropertyType))
                {
                    return Resources._0IsACollectionOfElements1.Formato(pi.NiceName(), elemType.NiceName()) + validations;
                }
                else
                {
                    string valueType = ValueType(elemType, pi);
                    return Resources._0IsACollectionOfElements1.Formato(pi.NiceName(), valueType) + validations;
                }
            }
            else
            {
                string valueType = ValueType(pi.PropertyType, pi);

                Gender gender = NaturalLanguageTools.GetGender(valueType);

                return Resources.ResourceManager.GetGenderAwareResource("_0IsA1", gender).Formato(pi.NiceName(), valueType) + validations;
            }
        }

        static string EntityProperty(Type type, PropertyInfo pi, Type propertyType, string typeName)
        {
            if (pi.IsDefaultName())
                return
                    propertyType.GetGenderAwareResource(() => Resources.The0).Formato(typeName) + " " +
                    type.GetGenderAwareResource(() => Resources.OfThe0).Formato(type.NiceName());
            else
                return
                    propertyType.GetGenderAwareResource(() => Resources._0IsA1).Formato(pi.NiceName(), typeName);
        }

        static string ValueType(Type propertyType, PropertyInfo pi)
        {
            UnitAttribute unit = pi.SingleAttribute<UnitAttribute>();

            Type cleanType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            string typeName =
                    cleanType.IsEnum ? Resources.ValueLike0.Formato(Enum.GetValues(cleanType).Cast<Enum>().CommaOr(e => e.NiceToString())) :
                    cleanType == typeof(decimal) && unit != null && unit.UnitName == "€" ? Resources.Amount :
                    cleanType == typeof(DateTime) && Validator.GetOrCreatePropertyPack(pi).Validators.OfType<DateTimePrecissionValidatorAttribute>().SingleOrDefault().Map(a=>a != null && a.Precision == DateTimePrecision.Days) ? Resources.Date :
                    NaturalTypeDescription(cleanType);

            string orNull = Nullable.GetUnderlyingType(pi.PropertyType) != null ? Resources.OrNull : null;

            return typeName.Add(unit != null ? Resources.ExpressedIn + unit.UnitName : null, " ").Add(orNull, " ");
        }

        static string TypeLink(this Type type)
        {
            return "[" + WikiFormat.EntityLink + WikiFormat.Separator + type.Name + "]";
        }

        static string NaturalTypeDescription(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    return Resources.Check;

                case TypeCode.Char:
                    return Resources.Character;

                case TypeCode.DateTime:
                    return Resources.DateTime;

                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return Resources.Integer;

                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return Resources.Value;

                case TypeCode.String:
                    return Resources.String;
            }

            return type.Name;
        }

        public static string GetOperationHelp(Type type, OperationInfo operationInfo)
        {
            switch (operationInfo.OperationType)
            {
                case OperationType.Execute: return type.GetGenderAwareResource(()=>Resources.Call0Over1OfThe2).Formato(
                    operationInfo.Key.NiceToString(),
                    operationInfo.Lite.Value ? Resources.TheDatabaseVersion : Resources.YourVersion, 
                    type.NiceName());
                case OperationType.Delete: return Resources.RemovesThe0FromTheDatabase.Formato(type.NiceName());
                case OperationType.Constructor: return
                    type.GetGenderAwareResource(() => Resources.ConstructsANew0).Formato(type.NiceName());
                case OperationType.ConstructorFrom: return
                    operationInfo.ReturnType.GetGenderAwareResource(() => Resources.ConstructsANew0).Formato(operationInfo.ReturnType.NiceName()) + " " +
                    type.GetGenderAwareResource(()=>Resources.From0OfThe1).Formato(operationInfo.Lite.Value ? Resources.TheDatabaseVersion  : Resources.YourVersion, type.NiceName());
                case OperationType.ConstructorFromMany: return
                    operationInfo.ReturnType.GetGenderAwareResource(()=>Resources.ConstructsANew0).Formato(operationInfo.ReturnType.NiceName()) + " " +
                    type.GetGenderAwareResource(()=>Resources.FromMany0).Formato(type.NicePluralName());
            }

            return "";
        }

        public static string GetQueryHelp(IDynamicQuery dynamicQuery)
        {
            Type entityType = dynamicQuery.EntityColumn().DefaultEntityType();

            if (dynamicQuery.Expression != null)
            {
                Expression expression;
                try
                {
                    expression = DbQueryProvider.Clean(dynamicQuery.Expression);
                }
                catch (Exception)
                {
                    expression = MetaEvaluator.Clean(dynamicQuery.Expression);
                }

                List<Type> types = TableGatherer.GatherTables(expression);
                if (types.Count == 1 && types.Contains(entityType))
                    return Resources.QueryOf0.Formato(entityType.NicePluralName());
                else
                    return Resources.QueryOf0Connecting1.Formato(entityType.NicePluralName(), types.CommaAnd(t => t.NiceName()));
            }

            return Resources.QueryOf0.Formato(entityType.NicePluralName());
        }
    }

    internal class TableGatherer : ExpressionVisitor
    {
        List<Type> result = new List<Type>();

        public static List<Type> GatherTables(Expression expression)
        {
            TableGatherer visitor = new TableGatherer();
            visitor.Visit(expression);
            return visitor.result;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            Type type = TableType(c.Value);
            if(type != null)
                result.Add(type);

            return c;
        }

        public Type TableType(object value)
        {
            if (value == null)
                return null;

            Type type = value.GetType();
            if (!typeof(IQueryable).IsAssignableFrom(type))
                return null;

            IQueryable query = (IQueryable)value;

            if (!type.IsInstantiationOf(typeof(Query<>)))
                throw new InvalidOperationException("{0} belongs to another kind ok Linq Provider");

            if (!query.IsBase())
                throw new InvalidOperationException("ConstantExpression with a complex IQueryable unexpected at this stage");

            return query.ElementType;
        }
    }

    public static class WikiFormat
    {
        public const string EntityLink = "e";
        public const string PropertyLink = "p";
        public const string QueryLink = "q";
        public const string OperationLink = "o";
        public const string HyperLink = "h";
        public const string WikiLink = "w";
        public const string NameSpaceLink = "n";

        public const string Separator = ":";
    }
}

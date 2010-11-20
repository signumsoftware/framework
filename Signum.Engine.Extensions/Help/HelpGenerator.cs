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
        public static string GetPropertyHelp(PropertyRoute pr)
        {
            string validations = Validator.GetOrCreatePropertyPack(pr).Validators.CommaAnd(v => v.HelpMessage);

            if (validations.HasText())
                validations = Resources.Should + validations;

            if (Reflector.IsIIdentifiable(pr.Type))
            {
                return EntityProperty(pr, pr.Type, pr.Type.TypeLink()) + validations;
            }
            else if (typeof(Lite).IsAssignableFrom(pr.Type))
            {
                Type cleanType = Reflector.ExtractLite(pr.Type);

                return EntityProperty(pr, cleanType, cleanType.TypeLink()) + validations;
            }
            else if (Reflector.IsEmbeddedEntity(pr.Type))
            {
                return EntityProperty(pr, pr.Type, pr.Type.NiceName());
            }
            else if (Reflector.IsMList(pr.Type))
            {
                Type elemType = pr.Type.ElementType();

                if (Reflector.IsIIdentifiable(elemType))
                {
                    return Resources._0IsACollectionOfElements1.Formato(pr.PropertyInfo.NiceName(), elemType.TypeLink()) + validations;
                }
                else if (typeof(Lite).IsAssignableFrom(elemType))
                {
                    return Resources._0IsACollectionOfElements1.Formato(pr.PropertyInfo.NiceName(), Reflector.ExtractLite(elemType).TypeLink()) + validations;
                }
                else if (Reflector.IsEmbeddedEntity(pr.PropertyInfo.PropertyType))
                {
                    return Resources._0IsACollectionOfElements1.Formato(pr.PropertyInfo.NiceName(), elemType.NiceName()) + validations;
                }
                else
                {
                    string valueType = ValueType(elemType, pr);
                    return Resources._0IsACollectionOfElements1.Formato(pr.PropertyInfo.NiceName(), valueType) + validations;
                }
            }
            else
            {
                string valueType = ValueType(pr.Type, pr);

                Gender gender = NaturalLanguageTools.GetGender(valueType);

                return Resources.ResourceManager.GetGenderAwareResource("_0IsA1", gender).Formato(pr.PropertyInfo.NiceName(), valueType) + validations;
            }
        }

        static string EntityProperty(PropertyRoute pr, Type propertyType, string typeName)
        {
            if (pr.PropertyInfo.IsDefaultName())
                return
                    propertyType.GetGenderAwareResource(() => Resources.The0).Formato(typeName) + " " +
                    pr.Parent.Type.GetGenderAwareResource(() => Resources.OfThe0).Formato(pr.Parent.Type.NiceName());
            else
                return
                    propertyType.GetGenderAwareResource(() => Resources._0IsA1).Formato(pr.PropertyInfo.NiceName(), typeName);
        }

        static string ValueType(Type propertyType, PropertyRoute pr)
        {
            UnitAttribute unit = pr.PropertyInfo.SingleAttribute<UnitAttribute>();

            Type cleanType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            string typeName =
                    cleanType.IsEnum ? Resources.ValueLike0.Formato(Enum.GetValues(cleanType).Cast<Enum>().CommaOr(e => e.NiceToString())) :
                    cleanType == typeof(decimal) && unit != null && unit.UnitName == "€" ? Resources.Amount :
                    cleanType == typeof(DateTime) && Validator.GetOrCreatePropertyPack(pr).Validators.OfType<DateTimePrecissionValidatorAttribute>().SingleOrDefault().Map(a => a != null && a.Precision == DateTimePrecision.Days) ? Resources.Date :
                    NaturalTypeDescription(cleanType);

            string orNull = Nullable.GetUnderlyingType(pr.Type) != null ? Resources.OrNull : null;

            return typeName.Add(unit != null ? Resources.ExpressedIn + unit.UnitName : null, " ").Add(orNull, " ");
        }

        static string TypeLink(this Type type)
        {
            return "[e:" + type.Name + "]";
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

    internal class TableGatherer : SimpleExpressionVisitor
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
                throw new InvalidOperationException(Resources.BelongsToAnotherKindOkLinqProvider);

            if (!query.IsBase())
                throw new InvalidOperationException(Resources.ConstantExpressionWithAComplexIQueryableUnexpectedAt);

            return query.ElementType;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using Signum.Entities.Basics;
using Signum.Engine.Basics;

namespace Signum.Engine.Help
{
    public static class HelpGenerator
    {
        public static string GetPropertyHelp(PropertyRoute pr)
        {
            string validations = Validator.TryGetPropertyValidator(pr).Validators.CommaAnd(v => v.HelpMessage);

            if (validations.HasText())
                validations = Resources.Should + validations;

            if (Reflector.IsIIdentifiable(pr.Type))
            {
                Implementations imp = Schema.Current.FindImplementations(pr); 

                return EntityProperty(pr, pr.Type, imp.TypeLinks(pr.Type)) + validations;
            }
            else if (pr.Type.IsLite())
            {
                Type cleanType = Lite.Extract(pr.Type);

                Implementations imp = Schema.Current.FindImplementations(pr); 

                return EntityProperty(pr, cleanType, imp.TypeLinks(cleanType)) + validations;
            }
            else if (Reflector.IsEmbeddedEntity(pr.Type))
            {
                return EntityProperty(pr, pr.Type, pr.Type.NiceName());
            }
            else if (Reflector.IsMList(pr.Type))
            {
                Type elemType = pr.Type.ElementType();

                if (elemType.IsIIdentifiable())
                {
                    Implementations imp = Schema.Current.FindImplementations(pr.Add("Item")); 

                    return Resources._0IsACollectionOfElements1.Formato(pr.PropertyInfo.NiceName(), imp.TypeLinks(elemType)) + validations;
                }
                else if (elemType.IsLite())
                {   
                    Implementations imp = Schema.Current.FindImplementations(pr.Add("Item"));

                    return Resources._0IsACollectionOfElements1.Formato(pr.PropertyInfo.NiceName(), imp.TypeLinks(Lite.Extract(elemType))) + validations;
                }
                else if (Reflector.IsEmbeddedEntity(elemType))
                {
                    return Resources._0IsACollectionOfElements1.Formato(pr.PropertyInfo.NiceName(), elemType.NiceName()) + validations;
                }
                else
                {
                    string valueType = ValueType(pr.Add("Item"));
                    return Resources._0IsACollectionOfElements1.Formato(pr.PropertyInfo.NiceName(), valueType) + validations;
                }
            }
            else
            {
                string valueType = ValueType(pr);

                return Resources._0IsA1.ForGenderAndNumber(NaturalLanguageTools.GetGender(valueType)).Formato(pr.PropertyInfo.NiceName(), valueType) + validations;
            }
        }

        static string EntityProperty(PropertyRoute pr, Type propertyType, string typeName)
        {
            if (pr.PropertyInfo.IsDefaultName())
                return
                    Resources.The0.ForGenderAndNumber(propertyType.GetGender()).Formato(typeName) + " " +
                    Resources.OfThe0.ForGenderAndNumber(pr.Parent.Type.GetGender()).Formato(pr.Parent.Type.NiceName());
            else
                return
                    Resources._0IsA1.Formato(propertyType.GetGender()).Formato(pr.PropertyInfo.NiceName(), typeName);
        }

        static string ValueType(PropertyRoute pr)
        {
            Type type = pr.Type;
            string format = Reflector.FormatString(pr);
            string unit = pr.PropertyInfo.SingleAttribute<UnitAttribute>().TryCC(u=>u.UnitName);
            return ValueType(type, format, unit);
        }

        private static string ValueType(Type type, string format, string unit)
        {
            Type cleanType = Nullable.GetUnderlyingType(type) ?? type;

            string typeName =
                    cleanType.IsEnum ? Resources.ValueLike0.Formato(Enum.GetValues(cleanType).Cast<Enum>().CommaOr(e => e.NiceToString())) :
                    cleanType == typeof(decimal) && unit != null && unit == "€" ? Resources.Amount :
                    cleanType == typeof(DateTime) && format == "d" ? Resources.Date :
                    NaturalTypeDescription(cleanType);

            string orNull = Nullable.GetUnderlyingType(type) != null ? Resources.OrNull : null;

            return typeName.Add(" ", unit != null ? Resources.ExpressedIn + unit : null).Add(" ", orNull);
        }

        static string TypeLinks(this Implementations implementations, Type type)
        {
            if (implementations.IsByAll)
                return Resources.Any + " " + type.TypeLink();

            return implementations.Types.CommaOr(TypeLink);
        }

        static string TypeLink(this Type type)
        {
            string cleanName = TypeLogic.TryGetCleanName(type);
            if (cleanName.HasText())
                return "[e:" + cleanName + "]";
            return type.NiceName();
        }

        static string PropertyLink(this PropertyRoute route)
        {
            string cleanName = TypeLogic.GetCleanName(route.RootType);
            return "[p:" + cleanName + "." + route.PropertyString() + "]";
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
                case OperationType.Execute: return Resources.Call0Over1OfThe2.ForGenderAndNumber(type.GetGender()).Formato(
                    operationInfo.Key.NiceToString(),
                    operationInfo.Lite.Value ? Resources.TheDatabaseVersion : Resources.YourVersion, 
                    type.NiceName());
                case OperationType.Delete: return Resources.RemovesThe0FromTheDatabase.Formato(type.NiceName());
                case OperationType.Constructor: return
                    Resources.ConstructsANew0.ForGenderAndNumber(type.GetGender()).Formato(type.NiceName());
                case OperationType.ConstructorFrom: return
                    Resources.ConstructsANew0.ForGenderAndNumber(operationInfo.ReturnType.GetGender()).Formato(operationInfo.ReturnType.NiceName()) + " " +
                    Resources.From0OfThe1.ForGenderAndNumber(type.GetGender()).Formato(operationInfo.Lite.Value ? Resources.TheDatabaseVersion : Resources.YourVersion, type.NiceName());
                case OperationType.ConstructorFromMany: return
                    Resources.ConstructsANew0.ForGenderAndNumber(operationInfo.ReturnType.GetGender()).Formato(operationInfo.ReturnType.NiceName()) + " " +
                    Resources.FromMany0.ForGenderAndNumber(type.GetGender()).Formato(type.NicePluralName());
            }

            return "";
        }

        public static string GetQueryHelp(IDynamicQuery dynamicQuery)
        {
            ColumnDescriptionFactory cdf = dynamicQuery.EntityColumn();

            return Resources.QueryOf0.Formato(cdf.Implementations.Value.TypeLinks(Lite.Extract(cdf.Type)));
        }

        internal static string GetQueryColumnHelp(ColumnDescriptionFactory kvp)
        {
            string typeDesc = QueryColumnType(kvp);

            if (kvp.PropertyRoutes != null)
                return Resources._0IsA1AndShows2.Formato(kvp.DisplayName(), typeDesc, kvp.PropertyRoutes.CommaAnd(pr =>
                    pr.PropertyRouteType == PropertyRouteType.Root ? TypeLink(pr.RootType) :
                    Resources.TheProperty0.Formato(PropertyLink(pr.PropertyRouteType == PropertyRouteType.LiteEntity ? pr.Parent: pr))));
            else
                return Resources._0IsACalculated1.Formato(kvp.DisplayName(), typeDesc);
        }

        private static string QueryColumnType(ColumnDescriptionFactory kvp)
        {
            var cleanType = kvp.Type.CleanType();

            if (Reflector.IsIIdentifiable(cleanType))
            {
                return kvp.Implementations.Value.TypeLinks(cleanType);
            }
            else if (Reflector.IsEmbeddedEntity(kvp.Type))
            {
                return kvp.Type.NiceName();
            }
            else
            {
                return ValueType(kvp.Type, kvp.Format, kvp.Unit);
            }
        }
    }
}

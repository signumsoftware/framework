using System;
using System.Linq;
using Signum.Utilities;
using Signum.Entities.Reflection;
using Signum.Engine.DynamicQuery;
using System.Reflection;
using Signum.Entities;
using Signum.Engine.Maps;
using Signum.Engine.Basics;
using Signum.Entities.Help;
using Signum.Engine.Operations;
using Signum.Utilities.Reflection;

namespace Signum.Engine.Help
{
    public static class HelpGenerator
    {
        public static string GetPropertyHelp(PropertyRoute pr)
        {
            string? validations = Validator.TryGetPropertyValidator(pr)?.Let(vs => vs.Validators.Where(v => !(v is NotNullValidatorAttribute)).CommaAnd(v => v.HelpMessage));

            if (validations.HasText())
                validations = HelpMessage.Should.NiceToString() + validations;

            validations += ".";

            if (Reflector.IsIEntity(pr.Type))
            {
                Implementations imp = Schema.Current.FindImplementations(pr);

                return EntityProperty(pr, pr.Type, imp.TypeLinks(pr.Type)) + validations;
            }
            else if (pr.Type.IsLite())
            {
                Type cleanType = Lite.Extract(pr.Type)!;

                Implementations imp = Schema.Current.FindImplementations(pr);

                return EntityProperty(pr, cleanType, imp.TypeLinks(cleanType)) + validations;
            }
            else if (Reflector.IsEmbeddedEntity(pr.Type))
            {
                return EntityProperty(pr, pr.Type, pr.Type.NiceName());
            }
            else if (Reflector.IsMList(pr.Type))
            {
                Type elemType = pr.Type.ElementType()!;

                if (elemType.IsIEntity())
                {
                    Implementations imp = Schema.Current.FindImplementations(pr.Add("Item"));

                    return HelpMessage._0IsACollectionOfElements1.NiceToString(pr.PropertyInfo!.NiceName(), imp.TypeLinks(elemType)) + validations;
                }
                else if (elemType.IsLite())
                {
                    Implementations imp = Schema.Current.FindImplementations(pr.Add("Item"));

                    return HelpMessage._0IsACollectionOfElements1.NiceToString(pr.PropertyInfo!.NiceName(), imp.TypeLinks(Lite.Extract(elemType)!)) + validations;
                }
                else if (Reflector.IsEmbeddedEntity(elemType))
                {
                    return HelpMessage._0IsACollectionOfElements1.NiceToString(pr.PropertyInfo!.NiceName(), elemType.NiceName()) + validations;
                }
                else
                {
                    string valueType = ValueType(pr.Add("Item"));
                    return HelpMessage._0IsACollectionOfElements1.NiceToString(pr.PropertyInfo!.NiceName(), valueType) + validations;
                }
            }
            else if (pr.Type.UnNullify() == typeof(PrimaryKey))
            {
                var vt = ValueType(PrimaryKey.Type(pr.RootType), false, null, null);
                return HelpMessage._0IsThePrimaryKeyOf1OfType2.NiceToString().FormatWith(pr.PropertyInfo!.NiceName(), pr.RootType.NiceName(), vt) + validations;
            }
            else
            {
                string valueType = ValueType(pr);

                return HelpMessage._0IsA1.NiceToString().ForGenderAndNumber(NaturalLanguageTools.GetGender(valueType)).FormatWith(pr.PropertyInfo!.NiceName(), valueType) + validations;
            }
        }

        static string EntityProperty(PropertyRoute pr, Type propertyType, string typeName)
        {
            var orNull = IsNullable(pr) == true ? HelpMessage.OrNull.NiceToString() : null;

            if (pr.PropertyInfo!.IsDefaultName())
                return
                    HelpMessage.The0.NiceToString().ForGenderAndNumber(propertyType.GetGender()).FormatWith(typeName) + " " +
                    HelpMessage.OfThe0.NiceToString().ForGenderAndNumber(pr.Parent!.Type.GetGender()).FormatWith(pr.Parent.Type.NiceName()).Add(" ", orNull);
            else
                return
                    HelpMessage._0IsA1.NiceToString().ForGenderAndNumber(propertyType.GetGender()).FormatWith(pr.PropertyInfo!.NiceName(), typeName).Add(" ", orNull);
        }

        static string ValueType(PropertyRoute pr)
        {
            Type type = pr.Type;
            string? format = Reflector.FormatString(pr);
            string? unit = pr.PropertyInfo?.GetCustomAttribute<UnitAttribute>()?.UnitName;
            bool? nullable = IsNullable(pr);
            return ValueType(type, nullable, format, unit);
        }

        private static bool? IsNullable(PropertyRoute pr)
        {
            if (pr.PropertyInfo == null)
                return null;

            return (Validator.TryGetPropertyValidator(pr)?.Validators.Any(a => a is NotNullValidatorAttribute) ?? false) ? false :
                            pr.PropertyInfo!.IsNullable() == true ? true : (bool?)null;
        }

        private static string ValueType(Type type, bool? nullable, string? format, string? unit)
        {
            Type cleanType = Nullable.GetUnderlyingType(type) ?? type;

            string typeName =
                    cleanType.IsEnum ? HelpMessage.ValueLike0.NiceToString(Enum.GetValues(cleanType).Cast<Enum>().CommaOr(e => e.NiceToString())) :
                    cleanType == typeof(decimal) && unit != null && unit == "€" ? HelpMessage.Amount.NiceToString() :
                    cleanType == typeof(DateTime) && format == "d" ? HelpMessage.Date.NiceToString() :
                    NaturalTypeDescription(cleanType);

            string? orNull = nullable ?? (Nullable.GetUnderlyingType(type) != null) ? HelpMessage.OrNull.NiceToString() : null;

            return typeName.Add(" ", unit != null ? HelpMessage.ExpressedIn.NiceToString() + unit : null).Add(" ", orNull);
        }

        static string TypeLinks(this Implementations implementations, Type type)
        {
            if (implementations.IsByAll)
                return HelpMessage.Any.NiceToString() + " " + type.TypeLink();

            return implementations.Types.CommaOr(TypeLink);
        }

        static string TypeLink(this Type type)
        {
            string? cleanName = TypeLogic.TryGetCleanName(type);
            if (cleanName.HasText())
                return "[t:" + cleanName + "]";
            return type.NiceName();
        }

        static string PropertyLink(this PropertyRoute route)
        {
            string cleanName = TypeLogic.GetCleanName(route.RootType);
            return "[p:" + cleanName + "." + route.PropertyString().Replace("[", "[[").Replace("]", "]]") + "]";
        }

        static string NaturalTypeDescription(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    return HelpMessage.BooleanValue.NiceToString();

                case TypeCode.Char:
                    return HelpMessage.Character.NiceToString();

                case TypeCode.DateTime:
                    return HelpMessage.DateTime.NiceToString();

                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return HelpMessage.Integer.NiceToString();

                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return HelpMessage.Value.NiceToString();

                case TypeCode.String:
                    return HelpMessage.String.NiceToString();
            }

            return type.Name;
        }

        public static string GetOperationHelp(Type type, OperationSymbol symbol)
        {
            var operationInfo = OperationLogic.GetOperationInfo(type, symbol);

            switch (operationInfo.OperationType)
            {
                case OperationType.Execute:
                    return HelpMessage.Call0Over1OfThe2.NiceToString().ForGenderAndNumber(type.GetGender()).FormatWith(
operationInfo.OperationSymbol.NiceToString(),
operationInfo.CanBeModified.Value ? HelpMessage.YourVersion.NiceToString() : HelpMessage.TheDatabaseVersion.NiceToString(),
type.NiceName());
                case OperationType.Delete: return HelpMessage.RemovesThe0FromTheDatabase.NiceToString(type.NiceName());
                case OperationType.Constructor:
                    return
HelpMessage.ConstructsANew0.NiceToString().ForGenderAndNumber(type.GetGender()).FormatWith(type.NiceName());
                case OperationType.ConstructorFrom:
                    return
HelpMessage.ConstructsANew0.NiceToString().ForGenderAndNumber(operationInfo.ReturnType!.GetGender()).FormatWith(operationInfo.ReturnType!.NiceName()) + " " +
HelpMessage.From0OfThe1.NiceToString().ForGenderAndNumber(type.GetGender()).FormatWith(operationInfo.CanBeModified.Value ? HelpMessage.YourVersion.NiceToString() : HelpMessage.TheDatabaseVersion.NiceToString(), type.NiceName());
                case OperationType.ConstructorFromMany:
                    return
HelpMessage.ConstructsANew0.NiceToString().ForGenderAndNumber(operationInfo.ReturnType!.GetGender()).FormatWith(operationInfo.ReturnType!.NiceName()) + " " +
HelpMessage.FromMany0.NiceToString().ForGenderAndNumber(type.GetGender()).FormatWith(type.NicePluralName());
            }

            return "";
        }

        public static string GetQueryHelp(IDynamicQueryCore dynamicQuery)
        {
            ColumnDescriptionFactory cdf = dynamicQuery.EntityColumnFactory();

            return HelpMessage.QueryOf0.NiceToString(cdf.Implementations.Value.TypeLinks(Lite.Extract(cdf.Type)!));
        }

        internal static string GetQueryColumnHelp(ColumnDescriptionFactory kvp)
        {
            string typeDesc = QueryColumnType(kvp);

            if (kvp.PropertyRoutes != null)
                return HelpMessage._0IsA1AndShows2.NiceToString(kvp.DisplayName(), typeDesc, kvp.PropertyRoutes.CommaAnd(pr =>
                    pr.PropertyRouteType == PropertyRouteType.Root ? TypeLink(pr.RootType) :
                    HelpMessage.TheProperty0.NiceToString(PropertyLink(pr.PropertyRouteType == PropertyRouteType.LiteEntity ? pr.Parent! : pr))));
            else
                return HelpMessage._0IsACalculated1.NiceToString(kvp.DisplayName(), typeDesc);
        }

        private static string QueryColumnType(ColumnDescriptionFactory kvp)
        {
            var cleanType = kvp.Type.CleanType();

            if (Reflector.IsIEntity(cleanType))
            {
                return kvp.Implementations.Value.TypeLinks(cleanType);
            }
            else if (Reflector.IsEmbeddedEntity(kvp.Type))
            {
                return kvp.Type.NiceName();
            }
            else
            {
                var pr = kvp.PropertyRoutes?.Only();
                return ValueType(kvp.Type, pr == null ? null : IsNullable(pr), kvp.Format, kvp.Unit);
            }
        }

        internal static string GetEntityHelp(Type type)
        {
            string typeIs = HelpMessage._0IsA1.NiceToString().ForGenderAndNumber(type.BaseType!.GetGender()).FormatWith(type.NiceName(), type.BaseType!.NiceName());

            string kind = HelpKindMessage.HisMainFunctionIsTo0.NiceToString(GetEntityKindMessage(EntityKindCache.GetEntityKind(type), EntityKindCache.GetEntityData(type), type.GetGender()));

            return typeIs + ". " + kind + ".";
        }
        
        private static string GetEntityKindMessage(EntityKind entityKind, EntityData entityData, char? gender)
        {
            var data =
                entityData == EntityData.Master ? HelpKindMessage.AndIsRarelyCreatedOrModified.NiceToString().ForGenderAndNumber(gender) :
                HelpKindMessage.AndAreFrequentlyCreatedOrModified.NiceToString().ForGenderAndNumber(gender);

            switch (entityKind)
            {
                case EntityKind.SystemString: return HelpKindMessage.ClassifyOtherEntities.NiceToString() + data + HelpKindMessage.AutomaticallyByTheSystem.NiceToString();
                case EntityKind.System: return HelpKindMessage.StoreInformationOnItsOwn.NiceToString() + data + HelpKindMessage.AutomaticallyByTheSystem.NiceToString();
                case EntityKind.Relational: return HelpKindMessage.RelateOtherEntities.NiceToString() + data;
                case EntityKind.String: return HelpKindMessage.ClassifyOtherEntities.NiceToString() + data;
                case EntityKind.Shared: return HelpKindMessage.StoreInformationSharedByOtherEntities.NiceToString() + data;
                case EntityKind.Main: return HelpKindMessage.StoreInformationOnItsOwn.NiceToString() + data;
                case EntityKind.Part: return HelpKindMessage.StorePartOfTheInformationOfAnotherEntity.NiceToString() + data;
                case EntityKind.SharedPart: return HelpKindMessage.StorePartsOfInformationSharedByDifferentEntities.NiceToString() + data;
                default: throw new InvalidOperationException("Unexpected {0}".FormatWith(entityKind));
            }
        }
    }
}

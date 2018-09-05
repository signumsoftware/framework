using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.Reflection;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Utilities;
using Signum.Entities.Reflection;
using Signum.Utilities.ExpressionTrees;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Collections.Concurrent;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public abstract class QueryToken : IEquatable<QueryToken>
    {
        public int Priority = 0;

        public abstract override string ToString();
        public abstract string NiceName();
        public abstract string Format { get; }
        public abstract string Unit { get; }
        public abstract Type Type { get; }
        public abstract string Key { get; }

        public virtual bool IsGroupable
        {
            get
            {
                switch (QueryUtils.TryGetFilterType(this.Type))
                {
                    case FilterType.Boolean:
                    case FilterType.Enum:
                    case FilterType.Guid:
                    case FilterType.Integer:
                    case FilterType.Lite:
                    case FilterType.String:
                        return true;

                    case FilterType.Decimal:
                    case FilterType.Embedded:
                        return false;

                    case FilterType.DateTime:
                        {

                            PropertyRoute route = this.GetPropertyRoute();

                            if (route != null && route.PropertyRouteType == PropertyRouteType.FieldOrProperty)
                            {
                                var pp = Validator.TryGetPropertyValidator(route);
                                if (pp != null)
                                {
                                    DateTimePrecissionValidatorAttribute datetimePrecission = pp.Validators.OfType<DateTimePrecissionValidatorAttribute>().SingleOrDefaultEx();

                                    if (datetimePrecission != null && datetimePrecission.Precision == DateTimePrecision.Days)
                                        return true;

                                }
                            }

                            return false;
                        }
                }

                return false;
            }
        }

        protected abstract List<QueryToken> SubTokensOverride(SubTokensOptions options);

        public virtual object QueryName
        {
            get { return this.parent.QueryName; }
        }

        public Func<object, T> GetAccessor<T>(BuildExpressionContext context)
        {
            return Expression.Lambda<Func<object, T>>(this.BuildExpression(context), context.Parameter).Compile();
        }

        public Expression BuildExpression(BuildExpressionContext context)
        {
            if (context.Replacemens != null && context.Replacemens.TryGetValue(this, out Expression result))
                return result;

            return BuildExpressionInternal(context);
        }

        protected abstract Expression BuildExpressionInternal(BuildExpressionContext context);

        public abstract PropertyRoute GetPropertyRoute();

        internal PropertyRoute AddPropertyRoute(PropertyInfo pi)
        {
            if (typeof(ModelEntity).IsAssignableFrom(Type))
                return PropertyRoute.Root(Type).Add(pi);

            Type type = Lite.Extract(Type); //Because Add doesn't work with lites
            if (type != null)
                return PropertyRoute.Root(type).Add(pi);

            PropertyRoute pr = GetPropertyRoute();
            if (pr == null)
                return null;

            return pr.Add(pi);
        }

        public abstract Implementations? GetImplementations();
        public abstract string IsAllowed();

        public abstract QueryToken Clone();

        QueryToken parent;
        public QueryToken Parent
        {
            get { return parent; }
        }

        public QueryToken(QueryToken parent)
        {
            this.parent = parent;
        }

        static ConcurrentDictionary<(QueryToken, SubTokensOptions), Dictionary<string, QueryToken>> subTokensOverrideCache =
            new ConcurrentDictionary<(QueryToken, SubTokensOptions), Dictionary<string, QueryToken>>();

        public QueryToken SubTokenInternal(string key, SubTokensOptions options)
        {
            var result = CachedSubTokensOverride(options).TryGetC(key) ?? OnEntityExtension(this).SingleOrDefaultEx(a => a.Key == key);

            if (result == null)
                return null;

            string allowed = result.IsAllowed();
            if (allowed != null)
                throw new UnauthorizedAccessException($"Access to token '{this.FullKey()}.{key}' in query '{QueryUtils.GetKey(this.QueryName)}' is not allowed because: {allowed}");

            return result;
        }

        public List<QueryToken> SubTokensInternal(SubTokensOptions options)
        {
            return CachedSubTokensOverride(options).Values
                .Concat(OnEntityExtension(this))
                .Where(t => t.IsAllowed() == null)
                .OrderByDescending(a => a.Priority)
                .ThenBy(a => a.ToString())
                .ToList();
        }

        Dictionary<string, QueryToken> CachedSubTokensOverride(SubTokensOptions options)
        {
            return subTokensOverrideCache.GetOrAdd((this, options), (tup) => tup.Item1.SubTokensOverride(tup.Item2).ToDictionaryEx(a => a.Key, "subtokens for " + this.Key));
        }

        public static Func<QueryToken, Type, SubTokensOptions, List<QueryToken>> ImplementedByAllSubTokens = (quetyToken, type, options) => throw new NotImplementedException("QueryToken.ImplementedByAllSubTokens not set");

        public static Func<Type, bool> IsSystemVersioned = t => false;

        protected List<QueryToken> SubTokensBase(Type type, SubTokensOptions options, Implementations? implementations)
        {
            var ut = type.UnNullify();
            if (ut == typeof(DateTime))
                return DateTimeProperties(this, DateTimePrecision.Milliseconds).AndHasValue(this);

            if (ut == typeof(float) || ut == typeof(double) || ut == typeof(decimal))
                return StepTokens(this, 4).AndHasValue(this);

            if (ut == typeof(int) || ut == typeof(long) || ut == typeof(short))
                return StepTokens(this, 0).AndModuloTokens(this).AndHasValue(this);

            if (ut == typeof(string))
                return StringTokens().AndHasValue(this);

            Type cleanType = type.CleanType();
            if (cleanType.IsIEntity())
            {
                if (implementations.Value.IsByAll)
                    return ImplementedByAllSubTokens(this, type, options); // new[] { EntityPropertyToken.IdProperty(this) };

                var onlyType = implementations.Value.Types.Only();

                if (onlyType != null && onlyType == cleanType)
                    return new[] {
                        EntityPropertyToken.IdProperty(this),
                        new EntityToStringToken(this),
                        IsSystemVersioned(onlyType) ? new SystemTimeToken(this, SystemTimeProperty.SystemValidFrom): null,
                        IsSystemVersioned(onlyType) ? new SystemTimeToken(this, SystemTimeProperty.SystemValidTo): null,
                    }
                    .NotNull()
                    .Concat(EntityProperties(onlyType)).ToList().AndHasValue(this); ;

                return implementations.Value.Types.Select(t => (QueryToken)new AsTypeToken(this, t)).ToList().AndHasValue(this);
            }

            if (type.IsEmbeddedEntity() || type.IsModelEntity())
            {
                return EntityProperties(type).OrderBy(a => a.ToString()).ToList().AndHasValue(this);
            }

            if (IsCollection(type))
            {
                return CollectionProperties(this, options);
            }

            return new List<QueryToken>();
        }

        public List<QueryToken> StringTokens()
        {
            return new List<QueryToken>
            {
                new NetPropertyToken(this, ReflectionTools.GetPropertyInfo((string str) => str.Length), ()=>QueryTokenMessage.Length.NiceToString())
            };
        }

        public static IEnumerable<QueryToken> OnEntityExtension(QueryToken parent)
        {
            if (EntityExtensions == null)
                throw new InvalidOperationException("QuertToken.EntityExtensions function not set");

            return EntityExtensions(parent);
        }

        public static Func<QueryToken, IEnumerable<QueryToken>> EntityExtensions;


        public static List<QueryToken> DateTimeProperties(QueryToken parent, DateTimePrecision precission)
        {
            string utc = TimeZoneManager.Mode == TimeZoneMode.Utc ? "Utc - " : "";

            return new List<QueryToken>
            {
                new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Year), () => utc + QueryTokenMessage.Year.NiceToString()),
                new NetPropertyToken(parent, ReflectionTools.GetMethodInfo((DateTime dt ) => dt.Quarter()), ()=> utc + QueryTokenMessage.Quarter.NiceToString()),
                new DatePartStartToken(parent, QueryTokenMessage.QuarterStart),
                new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Month),() => utc + QueryTokenMessage.Month.NiceToString()),
                new DatePartStartToken(parent, QueryTokenMessage.MonthStart),
                new NetPropertyToken(parent, ReflectionTools.GetMethodInfo((DateTime dt ) => dt.WeekNumber()), ()=> utc + QueryTokenMessage.WeekNumber.NiceToString()),
                new DatePartStartToken(parent, QueryTokenMessage.WeekStart),
                new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Day), () => utc + QueryTokenMessage.Day.NiceToString()),
                new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.DayOfYear), () => utc + QueryTokenMessage.DayOfYear.NiceToString()),
                new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.DayOfWeek), () => utc + QueryTokenMessage.DayOfWeek.NiceToString()),
                new DateToken(parent),
                precission < DateTimePrecision.Hours ? null: new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Hour), () => utc + QueryTokenMessage.Hour.NiceToString()),
                precission < DateTimePrecision.Hours ? null: new DatePartStartToken(parent, QueryTokenMessage.HourStart),
                precission < DateTimePrecision.Minutes ? null: new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Minute), () => utc + QueryTokenMessage.Minute.NiceToString()),
                precission < DateTimePrecision.Minutes ? null: new DatePartStartToken(parent, QueryTokenMessage.MinuteStart),
                precission < DateTimePrecision.Seconds ? null: new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Second), () => utc + QueryTokenMessage.Second.NiceToString()),
                precission < DateTimePrecision.Seconds ? null: new DatePartStartToken(parent, QueryTokenMessage.SecondStart),
                precission < DateTimePrecision.Milliseconds? null: new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Millisecond), () => utc + QueryTokenMessage.Millisecond.NiceToString()),
            }.NotNull().ToList();
        }

        public static List<QueryToken> StepTokens(QueryToken parent, int decimals)
        {
            return new List<QueryToken>
            {
                decimals >= 4? new StepToken(parent, 0.0001m): null,
                decimals >= 3? new StepToken(parent, 0.001m) : null,
                decimals >= 2? new StepToken(parent, 0.01m) : null,
                decimals >= 1? new StepToken(parent, 0.1m) : null,
                new StepToken(parent, 1m),
                new StepToken(parent, 10m),
                new StepToken(parent, 100m),
                new StepToken(parent, 1000m),
                new StepToken(parent, 10000m),
                new StepToken(parent, 100000m),
                new StepToken(parent, 1000000m),
            }.NotNull().ToList();
        }

        public static List<QueryToken> CollectionProperties(QueryToken parent, SubTokensOptions options)
        {
            if (parent.HasAllOrAny())
                options = options & ~SubTokensOptions.CanElement;

            List<QueryToken> tokens = new List<QueryToken>() { new CountToken(parent) };

            if ((options & SubTokensOptions.CanElement) == SubTokensOptions.CanElement)
                tokens.AddRange(EnumExtensions.GetValues<CollectionElementType>().Select(cet => new CollectionElementToken(parent, cet)));

            if ((options & SubTokensOptions.CanAnyAll) == SubTokensOptions.CanAnyAll)
                tokens.AddRange(EnumExtensions.GetValues<CollectionAnyAllType>().Select(caat => new CollectionAnyAllToken(parent, caat)));

            return tokens;
        }

        public virtual bool HasAllOrAny()
        {
            return Parent != null && Parent.HasAllOrAny();
        }

        public virtual bool HasElement()
        {
            return Parent != null && Parent.HasElement();
        }

        IEnumerable<QueryToken> EntityProperties(Type type)
        {
            var result = from p in Reflector.PublicInstancePropertiesInOrder(type)
                         where Reflector.QueryableProperty(type, p)
                         select (QueryToken)new EntityPropertyToken(this, p, this.AddPropertyRoute(p));

            if (!type.IsEntity())
                return result;

            var mixinProperties = from mt in MixinDeclarations.GetMixinDeclarations(type)
                                  from p in Reflector.PublicInstancePropertiesInOrder(mt)
                                  where Reflector.QueryableProperty(mt, p)
                                  select (QueryToken)new EntityPropertyToken(this, p, PropertyRoute.Root(type).Add(mt).Add(p));

            return result.Concat(mixinProperties);
        }

        public string FullKey()
        {
            if (Parent == null)
                return Key;

            return Parent.FullKey() + "." + Key;
        }

        public override bool Equals(object obj)
        {
            return obj is QueryToken && obj.GetType() == this.GetType() && Equals((QueryToken)obj);
        }

        public bool Equals(QueryToken other)
        {
            return other != null && other.QueryName.Equals(this.QueryName) && other.FullKey() == this.FullKey();
        }

        public override int GetHashCode()
        {
            return this.FullKey().GetHashCode() ^ this.QueryName.GetHashCode();
        }

        public virtual string TypeColor
        {
            get
            {
                if (IsCollection(Type))
                    return "#CE6700";

                switch (QueryUtils.TryGetFilterType(Type))
                {
                    case FilterType.Integer:
                    case FilterType.Decimal:
                    case FilterType.String:
                    case FilterType.Guid:
                    case FilterType.Boolean: return "#000000";
                    case FilterType.DateTime: return "#5100A1";
                    case FilterType.Enum: return "#800046";
                    case FilterType.Lite: return "#2B91AF";
                    case FilterType.Embedded: return "#156F8A";
                    default: return "#7D7D7D";
                }
            }
        }

        public string NiceTypeName
        {
            get
            {
                Type type = Type.CleanType();

                if (IsCollection(type))
                {
                    return QueryTokenMessage.ListOf0.NiceToString().FormatWith(GetNiceTypeName(Type.ElementType(), GetElementImplementations()));
                }

                return GetNiceTypeName(Type, GetImplementations());
            }
        }

        protected internal virtual Implementations? GetElementImplementations()
        {
            var pr = GetPropertyRoute();
            if (pr != null)
                return pr.Add("Item").TryGetImplementations();

            return null;
        }

        public static bool IsCollection(Type type)
        {
            return type != typeof(string) && type != typeof(byte[]) && type.ElementType() != null;
        }

        static string GetNiceTypeName(Type type, Implementations? implementations)
        {
            switch (QueryUtils.TryGetFilterType(type))
            {
                case FilterType.Integer: return QueryTokenMessage.Number.NiceToString();
                case FilterType.Decimal: return QueryTokenMessage.DecimalNumber.NiceToString();
                case FilterType.String: return QueryTokenMessage.Text.NiceToString();
                case FilterType.DateTime: return QueryTokenMessage.DateTime.NiceToString();
                case FilterType.Boolean: return QueryTokenMessage.Check.NiceToString();
                case FilterType.Guid: return QueryTokenMessage.GlobalUniqueIdentifier.NiceToString();
                case FilterType.Enum: return type.UnNullify().NiceName();
                case FilterType.Lite:
                    {
                        var cleanType = type.CleanType();
                        var imp = implementations.Value;

                        if (imp.IsByAll)
                            return QueryTokenMessage.AnyEntity.NiceToString();

                        return imp.Types.CommaOr(t => t.NiceName());
                    }
                case FilterType.Embedded: return QueryTokenMessage.Embedded0.NiceToString().FormatWith(type.NiceName());
                default: return type.TypeName();
            }
        }

        public bool ContainsKey(string key)
        {
            return this.Key == key || this.Parent != null && this.Parent.ContainsKey(key);
        }
    }

    public class BuildExpressionContext
    {
        public BuildExpressionContext(Type tupleType, ParameterExpression parameter, Dictionary<QueryToken, Expression> replacemens)
        {
            this.Parameter = parameter;
            this.Replacemens = replacemens;
        }

        public readonly Type TupleType;
        public readonly ParameterExpression Parameter;
        public readonly Dictionary<QueryToken, Expression> Replacemens;

        internal Expression<Func<object, Lite<Entity>>> GetEntitySelector()
        {
            return Expression.Lambda<Func<object, Lite<Entity>>>(Replacemens.Single(a=>a.Key.FullKey() == "Entity").Value, Parameter);
        }
    }


    public enum QueryTokenMessage
    {
        [Description("({0} as {1})")]
        _0As1,
        [Description(" and ")]
        And,
        [Description("any entity")]
        AnyEntity,
        [Description("As {0}")]
        As0,
        [Description("check")]
        Check,
        [Description("Column {0} not found")]
        Column0NotFound,
        Count,
        Date,
        [Description("date and time")]
        DateTime,
        Day,
        DayOfWeek,
        DayOfYear,
        [Description("decimal number")]
        DecimalNumber,
        [Description("embedded {0}")]
        Embedded0,
        [Description("global unique identifier")]
        GlobalUniqueIdentifier,
        Hour,
        [Description("list of {0}")]
        ListOf0,
        Millisecond,
        Minute,
        Month,
        [Description("Month Start")]
        MonthStart,
        [Description("Quarter")]
        Quarter,
        [Description("Quarter Start")]
        QuarterStart,
        [Description("Week Start")]
        WeekStart,
        [Description("Hour Start")]
        HourStart,
        [Description("Minute Start")]
        MinuteStart,
        [Description("Second Start")]
        SecondStart,
        [Description("More than one column named {0}")]
        MoreThanOneColumnNamed0,
        [Description("number")]
        Number,
        [Description(" of ")]
        Of,
        Second,
        [Description("text")]
        Text,
        Year,
        WeekNumber,
        [Description("{0} step {1}")]
        _0Steps1,
        [Description("Step {0}")]
        Step0,
        Length,
        [Description("{0} has value")]
        _0HasValue,
        [Description("Has value")]
        HasValue,
        [Description("Modulo {0}")]
        Modulo0,
        [Description("{0} mod {1}")]
        _0Mod1,
        Null,
        Not,
        Distinct
    }
}

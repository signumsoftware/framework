using Signum.Utilities.Reflection;
using Signum.Entities.Reflection;
using System.ComponentModel;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Signum.Entities.DynamicQuery;

[DebuggerDisplay("{FullKey(),nq}")]
public abstract class QueryToken : IEquatable<QueryToken>
{
    public int Priority = 0;

    public abstract override string ToString();
    public abstract string NiceName();
    public abstract string? Format { get; }
    public abstract string? Unit { get; }
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
                case FilterType.Time:
                    return false;

                case FilterType.DateTime:
                    {
                        if (this.Type.UnNullify() == typeof(DateOnly))
                            return true;

                        PropertyRoute? route = this.GetPropertyRoute();

                        if (route != null && route.PropertyRouteType == PropertyRouteType.FieldOrProperty)
                        {
                            if (route.Type.UnNullify() == typeof(DateOnly))
                                return true;

                            var pp = Validator.TryGetPropertyValidator(route);
                            if (pp != null)
                            {
                                DateTimePrecisionValidatorAttribute? datetimePrecision = pp.Validators.OfType<DateTimePrecisionValidatorAttribute>().SingleOrDefaultEx();

                                if (datetimePrecision != null && datetimePrecision.Precision == DateTimePrecision.Days)
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

    public virtual object QueryName => this.Parent!.QueryName;

    public Func<object, T> GetAccessor<T>(BuildExpressionContext context)
    {
        return Expression.Lambda<Func<object, T>>(this.BuildExpression(context), context.Parameter).Compile();
    }

    public Expression BuildExpression(BuildExpressionContext context, bool searchToArray = false)
    {
        if(context.Replacements.TryGetValue(this, out var result))
            return result.GetExpression();

        if (searchToArray)
        {
            var cta = this.HasToArray();
            if (cta != null)
                return CollectionToArrayToken.BuildToArrayExpression(this, cta, context);
        }

        return BuildExpressionInternal(context);
    }

    protected abstract Expression BuildExpressionInternal(BuildExpressionContext context);

    public abstract PropertyRoute? GetPropertyRoute();

    internal PropertyRoute? NormalizePropertyRoute()
    {
        if (typeof(ModelEntity).IsAssignableFrom(Type))
            return PropertyRoute.Root(Type);

        Type? type = Lite.Extract(Type); //Because Add doesn't work with lites
        if (type != null)
            return PropertyRoute.Root(type);

        PropertyRoute? pr = GetPropertyRoute();
        if (pr == null)
            return null;

        return pr;
    }

    public abstract Implementations? GetImplementations();
    public abstract string? IsAllowed();

    public abstract QueryToken Clone();

    public abstract QueryToken? Parent { get; }

    public QueryToken()
    {
    }

    static ConcurrentDictionary<(QueryToken token, SubTokensOptions options), Dictionary<string, QueryToken>> subTokensOverrideCache =
        new ConcurrentDictionary<(QueryToken token, SubTokensOptions options), Dictionary<string, QueryToken>>();

    public QueryToken? SubTokenInternal(string key, SubTokensOptions options)
    {
        var result = CachedSubTokensOverride(options).TryGetC(key) ?? OnDynamicEntityExtension(this).SingleOrDefaultEx(a => a.Key == key);

        if (result == null)
            return null;

        string? allowed = result.IsAllowed();
        if (allowed != null)
            throw new UnauthorizedAccessException($"Access to token '{this.FullKey()}.{key}' in query '{QueryUtils.GetKey(this.QueryName)}' is not allowed because: {allowed}");

        return result;
    }

    public List<QueryToken> SubTokensInternal(SubTokensOptions options)
    {
        return CachedSubTokensOverride(options).Values
            .Concat(OnDynamicEntityExtension(this))
            .Where(t => t.IsAllowed() == null)
            .OrderByDescending(a => a.Priority)
            .ThenBy(a => a.ToString())
            .ToList();
    }

    Dictionary<string, QueryToken> CachedSubTokensOverride(SubTokensOptions options)
    {
        return subTokensOverrideCache.GetOrAdd((this, options), (tup) =>
        {
            var dictionary = tup.token.SubTokensOverride(tup.options).ToDictionaryEx(a => a.Key, "subtokens for " + this.Key);

            foreach (var item in OnStaticEntityExtension(tup.Item1))
            {
                if (!dictionary.ContainsKey(item.Key)) //Prevent interface extensions overriding normal members
                {
                    dictionary.Add(item.Key, item);
                }
            }

            return dictionary;
        });
    }

    public static Func<QueryToken, Type, SubTokensOptions, List<QueryToken>> ImplementedByAllSubTokens = (quetyToken, type, options) => throw new NotImplementedException("QueryToken.ImplementedByAllSubTokens not set");

    public static Func<Type, bool> IsSystemVersioned = t => false;

    protected List<QueryToken> SubTokensBase(Type type, SubTokensOptions options, Implementations? implementations)
    {
        var ut = type.UnNullify();
        if (ut == typeof(DateTime))
            return DateTimeProperties(this, DateTimePrecision.Milliseconds).AndHasValue(this);

        if (ut == typeof(DateOnly))
            return DateOnlyProperties(this).AndHasValue(this);

        if (ut == typeof(TimeSpan))
            return TimeSpanProperties(this, DateTimePrecision.Milliseconds).AndHasValue(this);

        if (ut == typeof(float) || ut == typeof(double) || ut == typeof(decimal))
            return StepTokens(this, 4).AndHasValue(this);

        if (ut == typeof(int) || ut == typeof(long) || ut == typeof(short))
            return StepTokens(this, 0).AndModuloTokens(this).AndHasValue(this);

        if (ut == typeof(string))
            return StringTokens().AndHasValue(this);

        if (ut == typeof(Guid))
            return new List<QueryToken>().AndHasValue(this);

        Type cleanType = type.CleanType();
        if (cleanType.IsIEntity())
        {
            if (implementations!.Value.IsByAll)
                return ImplementedByAllSubTokens(this, type, options).PreAnd(new EntityTypeToken(this)).ToList().AndHasValue(this); // new[] { EntityPropertyToken.IdProperty(this) };

            var onlyType = implementations.Value.Types.Only();

            if (onlyType != null && onlyType == cleanType)
                return new[] {
                    EntityPropertyToken.IdProperty(this),
                    new EntityToStringToken(this),
                    IsSystemVersioned(onlyType) ? new SystemTimeToken(this, SystemTimeProperty.SystemValidFrom): null,
                    IsSystemVersioned(onlyType) ? new SystemTimeToken(this, SystemTimeProperty.SystemValidTo): null,
                    ((options & SubTokensOptions.CanOperation) != 0) ? new OperationsToken(this) : null,
                }
                .NotNull()
                .Concat(EntityProperties(onlyType)).ToList().AndHasValue(this);

            return implementations.Value.Types.Select(t => (QueryToken)new AsTypeToken(this, t)).PreAnd(new EntityTypeToken(this)).ToList().AndHasValue(this);
        }

        if (type.IsEmbeddedEntity() || type.IsModelEntity())
        {
            return EntityProperties(type).OrderBy(a => a.ToString()).ToList().AndHasValue(this);
        }

        if (IsCollection(type))
        {
            return CollectionProperties(this, options).AndHasValue(this);
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

    public static Func<QueryToken, IEnumerable<QueryToken>>? StaticEntityExtensions;
    public static IEnumerable<QueryToken> OnStaticEntityExtension(QueryToken parent)
    {
        if (StaticEntityExtensions == null)
            throw new InvalidOperationException("QuertToken.StaticEntityExtensions function not set");

        return StaticEntityExtensions(parent);
    }


    public static Func<QueryToken, IEnumerable<QueryToken>>? DynamicEntityExtensions;
    public static IEnumerable<QueryToken> OnDynamicEntityExtension(QueryToken parent)
    {
        if (DynamicEntityExtensions == null)
            throw new InvalidOperationException("QuertToken.DynamicEntityExtensions function not set");

        return DynamicEntityExtensions(parent);
    }



    public static List<QueryToken> DateTimeProperties(QueryToken parent, DateTimePrecision precision)
    {
        string utc = Clock.Mode == TimeZoneMode.Utc ? "Utc - " : "";

        return new List<QueryToken?>
        {
            new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Year), () => utc + QueryTokenMessage.Year.NiceToString(), format:"0000"),
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
            precision < DateTimePrecision.Hours ? null: new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.TimeOfDay), () => utc + QueryTokenMessage.TimeOfDay.NiceToString()),
            precision < DateTimePrecision.Hours ? null: new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Hour), () => utc + QueryTokenMessage.Hour.NiceToString()),
            precision < DateTimePrecision.Hours ? null: new DatePartStartToken(parent, QueryTokenMessage.HourStart),
            precision < DateTimePrecision.Minutes ? null: new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Minute), () => utc + QueryTokenMessage.Minute.NiceToString()),
            precision < DateTimePrecision.Minutes ? null: new DatePartStartToken(parent, QueryTokenMessage.MinuteStart),
            precision < DateTimePrecision.Seconds ? null: new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Second), () => utc + QueryTokenMessage.Second.NiceToString()),
            precision < DateTimePrecision.Seconds ? null: new DatePartStartToken(parent, QueryTokenMessage.SecondStart),
            precision < DateTimePrecision.Milliseconds? null: new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Millisecond), () => utc + QueryTokenMessage.Millisecond.NiceToString()),
        }.NotNull().ToList();
    }

    public static List<QueryToken> TimeSpanProperties(QueryToken parent, DateTimePrecision precision)
    {
        return new List<QueryToken?>
        {
            precision < DateTimePrecision.Hours ? null: new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((TimeSpan dt)=>dt.Hours), () => QueryTokenMessage.Hour.NiceToString()),
            precision < DateTimePrecision.Hours ? null: new DatePartStartToken(parent, QueryTokenMessage.HourStart),
            precision < DateTimePrecision.Minutes ? null:  new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((TimeSpan dt)=>dt.Minutes), () => QueryTokenMessage.Minute.NiceToString()),
            precision < DateTimePrecision.Minutes ? null: new DatePartStartToken(parent, QueryTokenMessage.MinuteStart),
            precision < DateTimePrecision.Seconds ? null:  new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((TimeSpan dt)=>dt.Seconds), () => QueryTokenMessage.Second.NiceToString()),
            precision < DateTimePrecision.Seconds ? null: new DatePartStartToken(parent, QueryTokenMessage.SecondStart),
            precision < DateTimePrecision.Milliseconds ? null:  new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((TimeSpan dt)=>dt.Milliseconds), () => QueryTokenMessage.Millisecond.NiceToString()),


            new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((TimeSpan dt)=>dt.TotalDays), () => QueryTokenMessage.TotalDays.NiceToString()),
            precision < DateTimePrecision.Hours ? null: new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((TimeSpan dt)=>dt.TotalHours), () => QueryTokenMessage.TotalHours.NiceToString()),
            precision < DateTimePrecision.Minutes ? null:  new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((TimeSpan dt)=>dt.TotalMinutes), () => QueryTokenMessage.TotalMinutes.NiceToString()),
            precision < DateTimePrecision.Seconds ? null:  new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((TimeSpan dt)=>dt.TotalSeconds), () => QueryTokenMessage.TotalSeconds.NiceToString()),
            precision < DateTimePrecision.Milliseconds ? null:  new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((TimeSpan dt)=>dt.TotalMilliseconds), () => QueryTokenMessage.TotalMilliseconds.NiceToString()),
        }.NotNull().ToList();
    }

    public static List<QueryToken> TimeOnlyProperties(QueryToken parent, DateTimePrecision precision)
    {
        string utc = Clock.Mode == TimeZoneMode.Utc ? "Utc - " : "";

        return new List<QueryToken?>
        {
            precision < DateTimePrecision.Hours ? null: new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((TimeOnly dt)=>dt.Hour), () => QueryTokenMessage.Hour.NiceToString()),
            new DatePartStartToken(parent, QueryTokenMessage.HourStart),
            precision < DateTimePrecision.Minutes ? null:  new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((TimeOnly dt)=>dt.Minute), () => QueryTokenMessage.Minute.NiceToString()),
            new DatePartStartToken(parent, QueryTokenMessage.MinuteStart),
            precision < DateTimePrecision.Seconds ? null:  new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((TimeOnly dt)=>dt.Second), () => QueryTokenMessage.Second.NiceToString()),
            new DatePartStartToken(parent, QueryTokenMessage.SecondStart),
            precision < DateTimePrecision.Milliseconds ? null:  new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((TimeOnly dt)=>dt.Millisecond), () => QueryTokenMessage.Millisecond.NiceToString()),

        }.NotNull().ToList();
    }

    public static List<QueryToken> DateOnlyProperties(QueryToken parent)
    {

        return new List<QueryToken?>
        {
            new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateOnly dt)=>dt.Year), () => QueryTokenMessage.Year.NiceToString()),
            new NetPropertyToken(parent, ReflectionTools.GetMethodInfo((DateOnly dt ) => dt.Quarter()), ()=> QueryTokenMessage.Quarter.NiceToString()),
            new DatePartStartToken(parent, QueryTokenMessage.QuarterStart),
            new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateOnly dt)=>dt.Month),() => QueryTokenMessage.Month.NiceToString()),
            new DatePartStartToken(parent, QueryTokenMessage.MonthStart),
            new NetPropertyToken(parent, ReflectionTools.GetMethodInfo((DateOnly dt ) => dt.WeekNumber()), ()=> QueryTokenMessage.WeekNumber.NiceToString()),
            new DatePartStartToken(parent, QueryTokenMessage.WeekStart),
            new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateOnly dt)=>dt.Day), () => QueryTokenMessage.Day.NiceToString()),
            new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateOnly dt)=>dt.DayOfYear), () => QueryTokenMessage.DayOfYear.NiceToString()),
            new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateOnly dt)=>dt.DayOfWeek), () => QueryTokenMessage.DayOfWeek.NiceToString()),
        }.NotNull().ToList();
    }

    public static List<QueryToken> StepTokens(QueryToken parent, int decimals)
    {
        return new List<QueryToken?>
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
            options &= ~(SubTokensOptions.CanElement | SubTokensOptions.CanToArray | SubTokensOptions.CanOperation | SubTokensOptions.CanAggregate);

        if (parent.HasToArray() != null)
            options &= ~(SubTokensOptions.CanAnyAll | SubTokensOptions.CanToArray | SubTokensOptions.CanOperation | SubTokensOptions.CanAggregate);

        List<QueryToken> tokens = new List<QueryToken>() { new CountToken(parent) };

        if (options.HasFlag(SubTokensOptions.CanElement))
            tokens.AddRange(EnumExtensions.GetValues<CollectionElementType>().Select(cet => new CollectionElementToken(parent, cet)));

        if (options.HasFlag(SubTokensOptions.CanAnyAll))
            tokens.AddRange(EnumExtensions.GetValues<CollectionAnyAllType>().Select(caat => new CollectionAnyAllToken(parent, caat)));

        if (options.HasFlag(SubTokensOptions.CanToArray))
            tokens.AddRange(EnumExtensions.GetValues<CollectionToArrayType>().Select(ctat => new CollectionToArrayToken(parent, ctat)));

        return tokens;
    }

    public virtual CollectionToArrayToken? HasToArray()
    {
        return Parent?.HasToArray();
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
        var normalizedPr = NormalizePropertyRoute();

        var result = from p in Reflector.PublicInstancePropertiesInOrder(type)
                     where Reflector.QueryableProperty(type, p)
                     select (QueryToken)new EntityPropertyToken(this, p, (normalizedPr?.Add(p))!);

        var mixinProperties = from mt in MixinDeclarations.GetMixinDeclarations(type)
                              from p in Reflector.PublicInstancePropertiesInOrder(mt)
                              where Reflector.QueryableProperty(mt, p)
                              select (QueryToken)new EntityPropertyToken(this, p, (normalizedPr?.Add(mt).Add(p))!);

        return result.Concat(mixinProperties);
    }

    public string FullKey()
    {
        if (Parent == null)
            return Key;

        return Parent.FullKey() + "." + Key;
    }

    public override bool Equals(object? obj)
    {
        return obj is QueryToken token && obj.GetType() == this.GetType() && Equals(token);
    }

    public bool Equals(QueryToken? other)
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

            return QueryUtils.TryGetFilterType(Type) switch
            {
                FilterType.Integer or 
                FilterType.Decimal or 
                FilterType.String or 
                FilterType.Guid or 
                FilterType.Boolean => "#000000",
                FilterType.DateTime => "#5100A1",
                FilterType.Time => "#9956db",
                FilterType.Enum => "#800046",
                FilterType.Lite => "#2B91AF",
                FilterType.Embedded => "#156F8A",
                _ => "#7D7D7D",
            };
        }
    }

    public string NiceTypeName
    {
        get
        {
            Type type = Type.CleanType();

            if (IsCollection(type))
            {
                return QueryTokenMessage.ListOf0.NiceToString().FormatWith(GetNiceTypeName(Type.ElementType()!, GetElementImplementations()));
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
        if (type == typeof(CellOperationDTO))
            return QueryTokenMessage.CellOperation.NiceToString();
        if (type == typeof(OperationsToken))
            return QueryTokenMessage.ContainerOfCellOperations.NiceToString();
        switch (QueryUtils.TryGetFilterType(type))
        {
            case FilterType.Integer: return QueryTokenMessage.Number.NiceToString();
            case FilterType.Decimal: return QueryTokenMessage.DecimalNumber.NiceToString();
            case FilterType.String: return QueryTokenMessage.Text.NiceToString();
            case FilterType.Time: return QueryTokenMessage.TimeOfDay.NiceToString();
            case FilterType.DateTime:
                if (type.UnNullify() == typeof(DateOnly))
                    return QueryTokenMessage.Date.NiceToString();

                return QueryTokenMessage.DateTime.NiceToString();
            case FilterType.Boolean: return QueryTokenMessage.Check.NiceToString();
            case FilterType.Guid: return QueryTokenMessage.GlobalUniqueIdentifier.NiceToString();
            case FilterType.Enum: return type.UnNullify().NiceName();
            case FilterType.Lite:
                {
                    var cleanType = type.CleanType();
                    var imp = implementations!.Value;

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

    internal bool Dominates(QueryToken t)
    {
        if (t is CollectionAnyAllToken)
            return false;

        if (t is CollectionElementToken)
            return false;

        if (t.Parent == null)
            return false;

        return t.Parent.Equals(this) || this.Dominates(t.Parent);
    }
}

public class BuildExpressionContext
{
    public BuildExpressionContext(Type elementType, ParameterExpression parameter, Dictionary<QueryToken, ExpressionBox> replacements)
    {
        this.ElementType = elementType;
        this.Parameter = parameter;
        this.Replacements = replacements;
    }

    public readonly Type ElementType;
    public readonly ParameterExpression Parameter;
    public readonly Dictionary<QueryToken, ExpressionBox> Replacements;

    public LambdaExpression GetEntitySelector()
    {
        var entityColumn = Replacements.Single(a => a.Key.FullKey() == "Entity").Value;

        return Expression.Lambda(Expression.Convert(entityColumn.GetExpression(), typeof(Lite<Entity>)       ), Parameter);
    }

    public LambdaExpression GetEntityFullSelector()
    {
        var entityColumn = Replacements.Single(a => a.Key.FullKey() == "Entity").Value;

        return Expression.Lambda(Expression.Convert(entityColumn.GetExpression().ExtractEntity(false), typeof(Entity)), Parameter);
    }

    internal List<CollectionElementToken> SubQueries()
    {
        return Replacements
            .Where(a => a.Value.SubQueryContext != null)
            .Select(a => (CollectionElementToken)a.Key)
            .ToList();
    }
}

public struct ExpressionBox
{
    public readonly Expression RawExpression;
    public readonly PropertyRoute? MListElementRoute;
    public readonly BuildExpressionContext? SubQueryContext;

    public ExpressionBox(Expression rawExpression, PropertyRoute? mlistElementRoute = null, BuildExpressionContext? subQueryContext = null)
    {
        this.RawExpression = rawExpression;
        this.MListElementRoute = mlistElementRoute;
        this.SubQueryContext = subQueryContext;
    }

    public Expression GetExpression()
    {
        if (RawExpression.Type.IsInstantiationOf(typeof(MListElement<,>)))
            return Expression.Property(RawExpression, "Element").BuildLiteNullifyUnwrapPrimaryKey(new[] { MListElementRoute! });

        return RawExpression;
    }

    public override string ToString()
    {
        return new { RawExpression, MListElementRoute, SubQueryContext }.ToString()!;
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
    [Description("date and time with time zone")]
    DateTimeOffset,
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
    TotalDays,
    TotalHours,
    TotalSeconds,
    TotalMinutes,
    TotalMilliseconds,
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
    TimeOfDay,
    [Description("More than one column named {0}")]
    MoreThanOneColumnNamed0,
    [Description("number")]
    Number,
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
    Distinct,
    [Description("{0} of {1}")]
    _0Of1,

    [Description("RowOrder")]
    RowOrder,

    [Description("RowID")]
    RowId,

    CellOperation,
    ContainerOfCellOperations,
    [Description("Entity Type")]
    EntityType
}

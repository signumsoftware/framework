using Signum.Utilities.Reflection;
using System.ComponentModel;
using System.Collections.Concurrent;
using System.Diagnostics;
using Signum.Engine.Maps;
using NpgsqlTypes;

namespace Signum.DynamicQuery.Tokens;

[DebuggerDisplay("{FullKey(),nq}")]
public abstract class QueryToken : IEquatable<QueryToken>
{
    public int Priority = 0;

    public abstract override string ToString();
    public abstract string NiceName();
    public abstract string? Format { get; }
    public abstract string? Unit { get; }
    public abstract Type Type { get; }
    public virtual DateTimeKind DateTimeKind => DateTimeKind.Unspecified;
    public abstract string Key { get; }
    bool? autoExpand; 
    public bool AutoExpand 
    {

        get
        {
            return autoExpand ??= CalculateAutoExpand();
        }
    }

    public virtual bool HideInAutoExpand => false;

    private bool CalculateAutoExpand()
    {
        if (!AutoExpandInternal)
            return false;

        for (var p = this.Parent; p != null; p = p.Parent)
        {
            if (!p.AutoExpand)
                break;

            if (p.Type == this.Type)
                return false;
        }

        return true;
    }

    protected virtual bool AutoExpandInternal
    {
        get
        {
            var pr = this.GetPropertyRoute();
            var attr = pr != null && pr.PropertyRouteType is PropertyRouteType.FieldOrProperty  or PropertyRouteType.MListItems ? 
                Schema.Current.Settings.FieldAttribute<AutoExpandSubTokensAttribute>(pr) : 
                null;

            if (attr != null)
                return attr.AutoExpand;


            var t = this.Type;
            if (t.IsEmbeddedEntity())
                return true;

            if (t.IsMList())
                return true;

            if (t.IsEntity() || t.IsLite())
            {
                var imps = this.GetImplementations();
                if (imps == null || imps.Value.IsByAll)
                    return false;

                if (imps.Value.Types.Count() != 1)
                    return true;

                return false;
            }

            return false;
        }
    }
       

    public virtual bool IsGroupable
    {
        get
        {
            switch (QueryUtils.TryGetFilterType(Type))
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
                        if (Type.UnNullify() == typeof(DateOnly))
                            return true;

                        PropertyRoute? route = GetPropertyRoute();

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

    public virtual object QueryName => Parent!.QueryName;

    public Func<object, T> GetAccessor<T>(BuildExpressionContext context)
    {
        return Expression.Lambda<Func<object, T>>(BuildExpression(context), context.Parameter).Compile();
    }

    public static Func<QueryToken, BuildExpressionContext, Expression?>? IsValueHidden;

    private Expression WithHidden(Expression expression, BuildExpressionContext context)
    {
        var isHidden = IsValueHidden?.Invoke(this, context);

        if (isHidden == null)
            return expression;

        if (isHidden is ConstantExpression ce && ce.Value != null)
        {
            if(ce.Value.Equals(true))
                return Expression.Constant(null, expression.Type.Nullify());
            if (ce.Value.Equals(false))
                return expression;
        }

        return Expression.Condition(isHidden, Expression.Constant(null, expression.Type.Nullify()), expression.Nullify());
    }

    public Expression BuildExpression(BuildExpressionContext context, bool searchToArray = false)
    {

        if (context.Replacements.TryGetValue(this, out var result))
        {
            var exp = result.GetExpression();
            if (result.AlreadyHidden)
                return exp;

            return WithHidden(exp, context);
        }

        if (searchToArray)
        {
            var cta = HasToArray();
            if (cta != null)
                return WithHidden(CollectionToArrayToken.BuildToArrayExpression(this, cta, context), context);
        }

        return WithHidden(BuildExpressionInternal(context), context);
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

    public bool IsEntity() => this is ColumnToken ct && ct.Column.IsEntity;

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
        if (this is ManualContainerToken mc)
            return new ManualToken(mc, key, mc.Parent!.Type);

        var result = CachedSubTokensOverride(options).TryGetC(key);

        if (result == null)
            return null;

        string? allowed = result.IsAllowed();
        if (allowed != null)
            throw new UnauthorizedAccessException($"Access to token '{FullKey()}.{key}' in query '{QueryUtils.GetKey(QueryName)}' is not allowed because: {allowed}");

        return result;
    }

    public List<QueryToken> SubTokensInternal(SubTokensOptions options)
    {
        return CachedSubTokensOverride(options).Values
            .Where(t => t.IsAllowed() == null)
            .OrderByDescending(a => a.Priority)
            .ThenBy(a => a.ToString())
            .ToList();
    }

    Dictionary<string, QueryToken> CachedSubTokensOverride(SubTokensOptions options)
    {
        if (this.AvoidCacheSubTokens)
            return this.SubTokensOverride(options).ToDictionary(a => a.Key);

        return subTokensOverrideCache.GetOrAdd((this, options), (tup) =>
        {
            var dictionary = tup.token.SubTokensOverride(tup.options).ToDictionaryEx(a => a.Key, "subtokens for " + Key);

            foreach (var item in QueryLogic.Expressions.GetExtensionsTokens(tup.Item1))
            {
                if (!dictionary.ContainsKey(item.Key)) //Prevent interface extensions overriding normal members
                {
                    dictionary.Add(item.Key, item);
                }
            }

            foreach (var item in QueryLogic.Expressions.GetExtensionsWithParameterTokens(this))
            {
                dictionary.Add(item.Key, item);
            }

            return dictionary;
        });
    }

    protected List<QueryToken> SubTokensBase(Type type, SubTokensOptions options, Implementations? implementations)
    {
        var ut = type.UnNullify();
        if (ut == typeof(DateTime))
            return DateTimeProperties(this, DateTimePrecision.Milliseconds).AndHasValue(this);

        if (ut == typeof(DateTimeOffset))
            return DateTimeOffsetProperties(this, DateTimePrecision.Milliseconds).AndHasValue(this);

        if (ut == typeof(DateOnly))
            return DateOnlyProperties(this).AndHasValue(this);

        if (ut == typeof(TimeSpan))
            return TimeSpanProperties(this, DateTimePrecision.Milliseconds).AndHasValue(this);

        if (ut == typeof(TimeOnly))
            return TimeOnlyProperties(this, DateTimePrecision.Milliseconds).AndHasValue(this);

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
                return QueryLogic.GetImplementedByAllSubTokens(this, type, options).PreAnd(new EntityTypeToken(this)).ToList().AndHasValue(this); // new[] { EntityPropertyToken.IdProperty(this) };

            var onlyType = implementations.Value.Types.Only();

            if (onlyType != null && onlyType == cleanType)
            {
                var sv = QueryLogic.IsSystemVersioned(onlyType);
                var pid = QueryLogic.HasPartitionId(onlyType);

                return new[] {
                    EntityPropertyToken.IdProperty(this),
                    new EntityToStringToken(this),
                    pid ? EntityPropertyToken.PartitionIdProperty(this) : null,
                    sv ? new SystemTimeToken(this, SystemTimeProperty.SystemValidFrom): null,
                    sv  ? new SystemTimeToken(this, SystemTimeProperty.SystemValidTo): null,
                    (options & SubTokensOptions.CanOperation) != 0 ? new OperationsContainerToken(this) : null,
                    (options & SubTokensOptions.CanManual) != 0 ? new QuickLinksToken(this) : null,
                }
                .NotNull()
                .Concat(EntityProperties(onlyType))
                .Concat(TsVectorColumns(onlyType))
                .ToList().AndHasValue(this);
            }

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

    private IEnumerable<QueryToken> TsVectorColumns(Type onlyType)
    {
        var table = Schema.Current.Tables.TryGetC(onlyType);

        if (table != null)
        {
            foreach (var item in table.Columns.Values.OfType<PostgresTsVectorColumn>())
            {
                yield return new PgTsVectorColumnToken(this, item);
            }
        }
    }

    protected IEnumerable<QueryToken> TsVectorColumns(EntityPropertyToken mlistProperty)
    {
        var mlistTable = ((FieldMList)Schema.Current.Field(mlistProperty.PropertyRoute)).TableMList;

        if (mlistTable != null)
        {
            foreach (var item in mlistTable.Columns.Values.OfType<PostgresTsVectorColumn>())
            {
                yield return new PgTsVectorColumnToken(this, item);
            }
        }
    }

    public List<QueryToken> StringTokens()
    {
        return new List<QueryToken>
        {
            new NetPropertyToken(this, ReflectionTools.GetPropertyInfo((string str) => str.Length), ()=>QueryTokenMessage.Length.NiceToString())
        };
    }







    public static List<QueryToken> DateTimeProperties(QueryToken parent, DateTimePrecision precision)
    {
        var kind = parent.DateTimeKind.DefaultToNull() ?? (Clock.Mode == TimeZoneMode.Utc ? DateTimeKind.Utc : DateTimeKind.Local);

        string utc = kind == DateTimeKind.Utc ? "Utc - " : "";


        return new List<QueryToken?>
        {
            new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Year), () => utc + QueryTokenDateMessage.Year.NiceToString(), format:"0000"){ Priority = - (int)QueryTokenDateMessage.Year  },
            new NetPropertyToken(parent, ReflectionTools.GetMethodInfo((DateTime dt ) => dt.Quarter()), ()=> utc + QueryTokenDateMessage.Quarter.NiceToString()){ Priority = - (int)QueryTokenDateMessage.Quarter },
            new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Month),() => utc + QueryTokenDateMessage.Month.NiceToString()){ Priority = - (int)QueryTokenDateMessage.Month  },
            new NetPropertyToken(parent, ReflectionTools.GetMethodInfo((DateTime dt ) => dt.WeekNumber()), ()=> utc + QueryTokenDateMessage.WeekNumber.NiceToString()){ Priority = - (int)QueryTokenDateMessage.WeekNumber },

            new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.DayOfYear), () => utc + QueryTokenDateMessage.DayOfYear.NiceToString()){ Priority = - (int)QueryTokenDateMessage.DayOfYear  },
            new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Day), () => utc + QueryTokenDateMessage.Day.NiceToString()){ Priority = - (int)QueryTokenDateMessage.Day },
            new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.DayOfWeek), () => utc + QueryTokenDateMessage.DayOfWeek.NiceToString()) { Priority = - (int)QueryTokenDateMessage.DayOfWeek },
            
            precision < DateTimePrecision.Hours ? null: new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Hour), () => utc + QueryTokenDateMessage.Hour.NiceToString()){ Priority = - (int)QueryTokenDateMessage.Hour },
            precision < DateTimePrecision.Minutes ? null: new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Minute), () => utc + QueryTokenDateMessage.Minute.NiceToString()){ Priority = - (int)QueryTokenDateMessage.Minute },
            precision < DateTimePrecision.Seconds ? null: new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Second), () => utc + QueryTokenDateMessage.Second.NiceToString()){ Priority = - (int)QueryTokenDateMessage.Second },
            precision < DateTimePrecision.Milliseconds? null: new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Millisecond), () => utc + QueryTokenDateMessage.Millisecond.NiceToString()){ Priority = - (int)QueryTokenDateMessage.Millisecond },

            new DateToken(parent) {  Priority = - (int)QueryTokenDateMessage.Date },
            precision < DateTimePrecision.Hours ? null: new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.TimeOfDay), () => utc + QueryTokenDateMessage.TimeOfDay.NiceToString()){ Priority = - (int)QueryTokenDateMessage.TimeOfDay },

            new DatePartStartToken(parent, QueryTokenDateMessage.QuarterStart),
            new DatePartStartToken(parent, QueryTokenDateMessage.MonthStart),
            new DatePartStartToken(parent, QueryTokenDateMessage.WeekStart),

            precision < DateTimePrecision.Hours ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Hours, 12),
            precision < DateTimePrecision.Hours ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Hours, 6),
            precision < DateTimePrecision.Hours ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Hours, 4),
            precision < DateTimePrecision.Hours ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Hours, 3),
            precision < DateTimePrecision.Hours ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Hours, 2),
            precision < DateTimePrecision.Hours ? null: new DatePartStartToken(parent, QueryTokenDateMessage.HourStart),

            precision < DateTimePrecision.Minutes ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Minutes, 30),
            precision < DateTimePrecision.Minutes ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Minutes, 20),
            precision < DateTimePrecision.Minutes ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Minutes, 10),
            precision < DateTimePrecision.Minutes ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Minutes, 5),
            precision < DateTimePrecision.Minutes ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Minutes, 4),
            precision < DateTimePrecision.Minutes ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Minutes, 3),
            precision < DateTimePrecision.Minutes ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Minutes, 2),
            precision < DateTimePrecision.Minutes ? null: new DatePartStartToken(parent, QueryTokenDateMessage.MinuteStart),

            precision < DateTimePrecision.Seconds ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Seconds, 30),
            precision < DateTimePrecision.Seconds ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Seconds, 20),
            precision < DateTimePrecision.Seconds ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Seconds, 10),
            precision < DateTimePrecision.Seconds ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Seconds, 5),
            precision < DateTimePrecision.Seconds ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Seconds, 4),
            precision < DateTimePrecision.Seconds ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Seconds, 3),
            precision < DateTimePrecision.Seconds ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Seconds, 2),
            precision < DateTimePrecision.Seconds ? null: new DatePartStartToken(parent, QueryTokenDateMessage.SecondStart),


            precision < DateTimePrecision.Milliseconds ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Milliseconds, 500),
            precision < DateTimePrecision.Milliseconds ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Milliseconds, 200),
            precision < DateTimePrecision.Milliseconds ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Milliseconds, 100),

        }.NotNull().ToList();
    }


    public static List<QueryToken> DateTimeOffsetProperties(QueryToken parent, DateTimePrecision precision)
    {

        return new List<QueryToken?>
        {
            new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTimeOffset dt)=>dt.UtcDateTime), () => QueryTokenDateMessage.UtcDateTime.NiceToString()),
            new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTimeOffset dt)=>dt.DateTime), () => QueryTokenDateMessage.DateTimePart.NiceToString()),
        }.NotNull().ToList();
    }

    public static List<QueryToken> TimeSpanProperties(QueryToken parent, DateTimePrecision precision)
    {
        return new List<QueryToken?>
        {
            precision < DateTimePrecision.Hours ? null: new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((TimeSpan dt)=>dt.Hours), () => QueryTokenDateMessage.Hour.NiceToString()){ Priority = - (int)QueryTokenDateMessage.Hour  },
            precision < DateTimePrecision.Minutes ? null:  new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((TimeSpan dt)=>dt.Minutes), () => QueryTokenDateMessage.Minute.NiceToString()){ Priority = - (int)QueryTokenDateMessage.Minute  },
            precision < DateTimePrecision.Seconds ? null:  new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((TimeSpan dt)=>dt.Seconds), () => QueryTokenDateMessage.Second.NiceToString()){ Priority = - (int)QueryTokenDateMessage.Second  },
            precision < DateTimePrecision.Milliseconds ? null:  new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((TimeSpan dt)=>dt.Milliseconds), () => QueryTokenDateMessage.Millisecond.NiceToString()){ Priority = - (int)QueryTokenDateMessage.Millisecond },

             new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((TimeSpan dt)=>dt.TotalDays), () => QueryTokenDateMessage.TotalDays.NiceToString()){ Priority = -(int) QueryTokenDateMessage.TotalDays },
            precision < DateTimePrecision.Hours ? null: new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((TimeSpan dt)=>dt.TotalHours), () => QueryTokenDateMessage.TotalHours.NiceToString()) { Priority = -(int) QueryTokenDateMessage.TotalHours },
            precision < DateTimePrecision.Minutes ? null:  new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((TimeSpan dt)=>dt.TotalMinutes), () => QueryTokenDateMessage.TotalMinutes.NiceToString()){ Priority = -(int) QueryTokenDateMessage.TotalMinutes },
            precision < DateTimePrecision.Seconds ? null:  new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((TimeSpan dt)=>dt.TotalSeconds), () => QueryTokenDateMessage.TotalSeconds.NiceToString()){ Priority = -(int) QueryTokenDateMessage.TotalSeconds },
            precision < DateTimePrecision.Milliseconds ? null:  new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((TimeSpan dt)=>dt.TotalMilliseconds), () => QueryTokenDateMessage.TotalMilliseconds.NiceToString()){ Priority = -(int) QueryTokenDateMessage.TotalMilliseconds },

            precision < DateTimePrecision.Hours ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Hours, 12),
            precision < DateTimePrecision.Hours ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Hours, 6),
            precision < DateTimePrecision.Hours ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Hours, 4),
            precision < DateTimePrecision.Hours ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Hours, 3),
            precision < DateTimePrecision.Hours ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Hours, 2),
            precision < DateTimePrecision.Hours ? null: new DatePartStartToken(parent, QueryTokenDateMessage.HourStart),

            precision < DateTimePrecision.Minutes ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Minutes, 30),
            precision < DateTimePrecision.Minutes ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Minutes, 20),
            precision < DateTimePrecision.Minutes ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Minutes, 10),
            precision < DateTimePrecision.Minutes ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Minutes, 5),
            precision < DateTimePrecision.Minutes ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Minutes, 4),
            precision < DateTimePrecision.Minutes ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Minutes, 3),
            precision < DateTimePrecision.Minutes ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Minutes, 2),
            precision < DateTimePrecision.Minutes ? null: new DatePartStartToken(parent, QueryTokenDateMessage.MinuteStart),

            precision < DateTimePrecision.Seconds ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Seconds, 30),
            precision < DateTimePrecision.Seconds ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Seconds, 20),
            precision < DateTimePrecision.Seconds ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Seconds, 10),
            precision < DateTimePrecision.Seconds ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Seconds, 5),
            precision < DateTimePrecision.Seconds ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Seconds, 4),
            precision < DateTimePrecision.Seconds ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Seconds, 3),
            precision < DateTimePrecision.Seconds ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Seconds, 2),
            precision < DateTimePrecision.Seconds ? null: new DatePartStartToken(parent, QueryTokenDateMessage.SecondStart),


            precision < DateTimePrecision.Milliseconds ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Milliseconds, 500),
            precision < DateTimePrecision.Milliseconds ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Milliseconds, 200),
            precision < DateTimePrecision.Milliseconds ? null: new DatePartStartToken(parent, QueryTokenDateMessage.Every0Milliseconds, 100),

           
        }.NotNull().ToList();
    }

    public static List<QueryToken> TimeOnlyProperties(QueryToken parent, DateTimePrecision precision)
    {
        string utc = Clock.Mode == TimeZoneMode.Utc ? "Utc - " : "";

        return new List<QueryToken?>
        {
            precision < DateTimePrecision.Hours ? null: new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((TimeOnly dt)=>dt.Hour), () => QueryTokenDateMessage.Hour.NiceToString()){ Priority = - (int)QueryTokenDateMessage.Hour  },
            precision < DateTimePrecision.Minutes ? null:  new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((TimeOnly dt)=>dt.Minute), () => QueryTokenDateMessage.Minute.NiceToString()){ Priority = - (int)QueryTokenDateMessage.Minute  },
            precision < DateTimePrecision.Seconds ? null:  new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((TimeOnly dt)=>dt.Second), () => QueryTokenDateMessage.Second.NiceToString()){ Priority = - (int)QueryTokenDateMessage.Second  },
            precision < DateTimePrecision.Milliseconds ? null:  new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((TimeOnly dt)=>dt.Millisecond), () => QueryTokenDateMessage.Millisecond.NiceToString()){ Priority = - (int)QueryTokenDateMessage.Millisecond  },
            
            new DatePartStartToken(parent, QueryTokenDateMessage.HourStart),
            new DatePartStartToken(parent, QueryTokenDateMessage.MinuteStart),
            new DatePartStartToken(parent, QueryTokenDateMessage.SecondStart),

        }.NotNull().ToList();
    }

    public static List<QueryToken> DateOnlyProperties(QueryToken parent)
    {

        return new List<QueryToken?>
        {
            new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateOnly dt)=>dt.Year), () => QueryTokenDateMessage.Year.NiceToString()),
            new NetPropertyToken(parent, ReflectionTools.GetMethodInfo((DateOnly dt ) => dt.Quarter()), ()=> QueryTokenDateMessage.Quarter.NiceToString()),
            new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateOnly dt)=>dt.Month),() => QueryTokenDateMessage.Month.NiceToString()),
            new NetPropertyToken(parent, ReflectionTools.GetMethodInfo((DateOnly dt ) => dt.WeekNumber()), ()=> QueryTokenDateMessage.WeekNumber.NiceToString()),
            new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateOnly dt)=>dt.DayOfYear), () => QueryTokenDateMessage.DayOfYear.NiceToString()),
            new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateOnly dt)=>dt.Day), () => QueryTokenDateMessage.Day.NiceToString()),
            new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateOnly dt)=>dt.DayOfWeek), () => QueryTokenDateMessage.DayOfWeek.NiceToString()),
            new DatePartStartToken(parent, QueryTokenDateMessage.QuarterStart),
            new DatePartStartToken(parent, QueryTokenDateMessage.WeekStart),
            new DatePartStartToken(parent, QueryTokenDateMessage.MonthStart),
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
            options &= ~(SubTokensOptions.CanElement | SubTokensOptions.CanToArray | SubTokensOptions.CanOperation | SubTokensOptions.CanAggregate | SubTokensOptions.CanManual);

        if (parent.HasToArray() != null)
            options &= ~(SubTokensOptions.CanAnyAll | SubTokensOptions.CanToArray | SubTokensOptions.CanOperation | SubTokensOptions.CanAggregate | SubTokensOptions.CanManual);

        List<QueryToken> tokens = new List<QueryToken>() { new CountToken(parent) };

        if (options.HasFlag(SubTokensOptions.CanElement))
            tokens.AddRange(EnumExtensions.GetValues<CollectionElementType>().Select(cet => new CollectionElementToken(parent, cet)));

        if (options.HasFlag(SubTokensOptions.CanNested))
            tokens.AddRange(new CollectionNestedToken(parent));

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

    public virtual CollectionNestedToken? HasNested()
    {
        return Parent?.HasNested();
    }

    public virtual bool HasElement()
    {
        return Parent != null && Parent.HasElement();
    }

    public virtual CollectionToArrayToken? HasCollectionToArray()
    {
        return Parent == null ? null : Parent.HasCollectionToArray();
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
        return obj is QueryToken token && obj.GetType() == GetType() && Equals(token);
    }

    public bool Equals(QueryToken? other)
    {
        return other != null && other.QueryName.Equals(QueryName) && other.FullKey() == FullKey();
    }

    public override int GetHashCode()
    {
        return FullKey().GetHashCode() ^ QueryName.GetHashCode();
    }


    public virtual string NiceTypeName
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

    public virtual bool AvoidCacheSubTokens => false;

    protected internal virtual Implementations? GetElementImplementations()
    {
        var pr = GetPropertyRoute();
        if (pr != null)
            return pr.Add("Item").TryGetImplementations();

        return null;
    }

    public static bool IsCollection(Type type)
    {
        return type != typeof(string) && type != typeof(byte[]) && type.ElementType() != null && type != typeof(NpgsqlTsVector);
    }

    static string GetNiceTypeName(Type type, Implementations? implementations)
    {
        if (type == typeof(CellOperationDTO))
            return QueryTokenMessage.CellOperation.NiceToString();
        if (type == typeof(OperationsContainerToken))
            return QueryTokenMessage.ContainerOfCellOperations.NiceToString();
        if (type == typeof(IndexerContainerToken))
            return QueryTokenMessage.IndexerContainer.NiceToString();
        switch (QueryUtils.TryGetFilterType(type))
        {
            case FilterType.Integer: return QueryTokenMessage.Number.NiceToString();
            case FilterType.Decimal: return QueryTokenMessage.DecimalNumber.NiceToString();
            case FilterType.String: return QueryTokenMessage.Text.NiceToString();
            case FilterType.Time: return QueryTokenDateMessage.TimeOfDay.NiceToString();
            case FilterType.DateTime:
                if (type.UnNullify() == typeof(DateOnly))
                    return QueryTokenDateMessage.Date.NiceToString();

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
        return Key == key || Parent != null && Parent.ContainsKey(key);
    }

    internal bool Dominates(QueryToken t)
    {
        if (t is CollectionNestedToken)
            return false; 

        if (t is CollectionAnyAllToken)
            return false;

        if (t is CollectionElementToken)
            return false;

        if (t.Parent == null)
            return false;

        return t.Parent.Equals(this) || Dominates(t.Parent);
    }
}

public class BuildExpressionContext
{
    public BuildExpressionContext(Type elementType, ParameterExpression parameter, Dictionary<QueryToken, ExpressionBox> replacements, List<Filter>? filters, List<Order>? orders, Pagination? pagination)
    {
        this.ElementType = elementType;
        this.Parameter = parameter;
        this.Replacements = replacements;
        this.Filters = filters;
        this.Orders = orders;
        this.Pagination = pagination;
    }

    public readonly Type ElementType;
    public readonly ParameterExpression Parameter;
    public readonly Dictionary<QueryToken, ExpressionBox> Replacements;
    public readonly List<Filter>? Filters; //For SubQueries and  Snippet keyword detection
    public readonly List<Order>? Orders; //For SubQueries  detection
    public readonly Pagination? Pagination; 

    public Expression<Func<object, T>>? TryGetSelectorUntyped<T>(string key)
    {
        var expBox = Replacements.SingleOrDefault(a => a.Key.FullKey() == key).Value;
        if (expBox.RawExpression == null)
            return null;

        var param = Expression.Parameter(typeof(object), "obj");
        var cast = Expression.Convert(param, ElementType);

        var body = ExpressionReplacer.Replace(expBox.GetExpression(), new Dictionary<ParameterExpression, Expression>()
        {
            {  Parameter, cast }
        });

        return Expression.Lambda<Func<object, T>>(Expression.Convert(body, typeof(T)), param);
    }


    public LambdaExpression GetEntitySelector()
    {
        var expBox = Replacements.Single(a => a.Key.FullKey() == "Entity").Value;

        return Expression.Lambda(Expression.Convert(expBox.GetExpression(), typeof(Lite<Entity>)), Parameter);
    }

    public LambdaExpression GetEntityFullSelector()
    {
        var entityColumn = Replacements.Single(a => a.Key.FullKey() == "Entity").Value;

        return Expression.Lambda(Expression.Convert(entityColumn.GetExpression().ExtractEntity(false), typeof(Entity)), Parameter);
    }

    //internal List<CollectionNestedToken> SubQueries()
    //{
    //    return Replacements
    //        .Where(a => a.Value.SubQueryContext != null)
    //        .Select(a => (CollectionNestedToken)a.Key)
    //        .ToList();
    //}
}

public struct ExpressionBox
{
    public readonly Expression RawExpression;
    public readonly PropertyRoute? MListElementRoute;
    public readonly BuildExpressionContext? SubQueryContext;
    public readonly bool AlreadyHidden;

    public ExpressionBox(Expression rawExpression, PropertyRoute? mlistElementRoute = null, BuildExpressionContext? subQueryContext = null, bool alreadyHidden = false)
    {
        RawExpression = rawExpression;
        MListElementRoute = mlistElementRoute;
        SubQueryContext = subQueryContext;
        AlreadyHidden = alreadyHidden;
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
   
    [Description("decimal number")]
    DecimalNumber,
    [Description("embedded {0}")]
    Embedded0,
    [Description("global unique identifier")]
    GlobalUniqueIdentifier,

    [Description("list of {0}")]
    ListOf0,

    TimeOfDay,
    Date,
    [Description("date and time")]
    DateTime,
    [Description("date and time with time zone")]
    DateTimeOffset,

    [Description("More than one column named {0}")]
    MoreThanOneColumnNamed0,
    [Description("number")]
    Number,
    [Description("text")]
    Text,

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
    EntityType,

    [Description("Match Rank")]
    MatchRank,

    [Description("Match Rank for {0}")]
    MatchRankFor0,

    [Description("Match Snippet")]
    MatchSnippet,

    [Description("Snippet for {0}")]
    SnippetOf0,
    PartitionId,
    Nested,
    IndexerContainer,
    Operations,
}

//The order of the elemens matters!
public enum QueryTokenDateMessage
{
    TimeOfDay = 1,
    Date,

    Year,
    [Description("Quarter")]
    Quarter,
    Month,
    WeekNumber,
    DayOfYear,
    Day,
    Days,
    DayOfWeek,
    Hour,
    Minute,
    Second,
    Millisecond,


    [Description("UTC - DateTime")]
    UtcDateTime,
    [Description("DateTime part")]
    DateTimePart,

    TotalDays,
    TotalHours,
    TotalSeconds,
    TotalMinutes,
    TotalMilliseconds,

    [Description("Month Start")]
    MonthStart,
    [Description("Quarter Start")]
    QuarterStart,
    [Description("Week Start")]
    WeekStart,

    [Description("Every {0} Hours")]
    Every0Hours,
    [Description("Hour Start")]
    HourStart,
    [Description("Every {0} Minutes")]
    Every0Minutes,
    [Description("Minute Start")]
    MinuteStart,
    [Description("Every {0} Seconds")]
    Every0Seconds,
    [Description("Second Start")]
    SecondStart,
    [Description("Every {0} Milliseconds")]
    Every0Milliseconds,

    [Description("Every {0} {1}")]
    Every01,

    [Description("{0} steps x {1} rows = {2} total rows (aprox)")]
    _0Steps1Rows2TotalRowsAprox,

    SplitQueries,
}

[InTypeScript(true), DescriptionOptions(DescriptionOptions.All)]
public enum ContainerTokenKey
{
    [Description("[Operations]")]
    Operations,
    [Description("[QuickLinks]")]
    QuickLinks,
}

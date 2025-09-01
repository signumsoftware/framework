using Signum.Engine.Maps;

namespace Signum.DynamicQuery.Tokens;

public class ColumnToken : QueryToken
{
    ColumnDescription column;
    public ColumnDescription Column { get { return column; } }

    object queryName;
    public override object QueryName
    {
        get { return queryName; }
    }

    public override QueryToken? Parent => null;

    public ColumnToken(ColumnDescription column, object queryName)
    {
        this.column = column ?? throw new ArgumentNullException(nameof(column));
        this.queryName = queryName ?? throw new ArgumentNullException(nameof(queryName));
    }

    protected override bool AutoExpandInternal => Column.IsEntity || base.AutoExpandInternal;

    public override string Key
    {
        get { return Column.Name; }
    }

    public override string ToString()
    {
        return Column.ToString();
    }

    public override Type Type
    {
        get { return Column.Type; }
    }

    public override string? Format
    {
        get { return Column.Format; }
    }

    public override string? Unit
    {
        get { return Column.Unit; }
    }

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {
        throw new InvalidOperationException("ColumnToken {0} not found on replacements".FormatWith(this));
    }

    protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
    {
        var uType = Column.Type.UnNullify();

        if (uType == typeof(DateTime) || uType == typeof(DateTimeOffset))
        {
            if (Column.PropertyRoutes != null)
            {
                DateTimePrecision? precision =
                    Column.PropertyRoutes
                    .Select(pr => Validator.TryGetPropertyValidator(pr.Parent!.Type, pr.PropertyInfo!.Name)?.Validators.OfType<DateTimePrecisionValidatorAttribute>().SingleOrDefaultEx())
                    .Select(dtp => dtp?.Precision)
                    .Distinct()
                    .Only();

                if (precision != null)
                    return DateTimeProperties(this, precision.Value).AndHasValue(this);
            }

            if (Column.Format == "d")
                return DateTimeProperties(this, DateTimePrecision.Days).AndHasValue(this);
        }

        if (uType == typeof(TimeSpan))
        {
            if (Column.PropertyRoutes != null)
            {
                DateTimePrecision? precision =
                    Column.PropertyRoutes
                    .Select(pr => Validator.TryGetPropertyValidator(pr.Parent!.Type, pr.PropertyInfo!.Name)?.Validators.OfType<TimePrecisionValidatorAttribute>().SingleOrDefaultEx())
                    .Select(dtp => dtp?.Precision)
                    .Distinct()
                    .Only();

                if (precision != null)
                    return TimeSpanProperties(this, precision.Value).AndHasValue(this);
            }
        }

        if (uType == typeof(TimeOnly))
        {
            if (Column.PropertyRoutes != null)
            {
                DateTimePrecision? precision =
                    Column.PropertyRoutes
                    .Select(pr => Validator.TryGetPropertyValidator(pr.Parent!.Type, pr.PropertyInfo!.Name)?.Validators.OfType<TimePrecisionValidatorAttribute>().SingleOrDefaultEx())
                    .Select(dtp => dtp?.Precision)
                    .Distinct()
                    .Only();

                if (precision != null)
                    return TimeOnlyProperties(this, precision.Value).AndHasValue(this);
            }
        }


        if (uType == typeof(double) ||
            uType == typeof(float) ||
            uType == typeof(decimal))
        {
            if (Column.PropertyRoutes != null)
            {
                int? decimalPlaces =
                    Column.PropertyRoutes
                    .Select(pr => Validator.TryGetPropertyValidator(pr.Parent!.Type, pr.PropertyInfo!.Name)?.Validators.OfType<DecimalsValidatorAttribute>().SingleOrDefaultEx())
                    .Select(dtp => dtp?.DecimalPlaces)
                    .Distinct()
                    .Only();

                if (decimalPlaces != null)
                    return StepTokens(this, decimalPlaces.Value).AndHasValue(this);
            }

            if (Column.Format != null)
                return StepTokens(this, Reflector.NumDecimals(Column.Format)).AndHasValue(this);
        }

        if (uType == typeof(string))
        {
            PropertyRoute? route = this.GetPropertyRoute();
            var result = StringTokens();

            if (route != null && EntityPropertyToken.HasFullTextIndex(route))
            {
                result.Add(new FullTextRankToken(this));
            }

            if (route != null && EntityPropertyToken.HasSnippet(route) && (options & SubTokensOptions.CanSnippet) != 0)
            {
                result.Add(new StringSnippetToken(this));
            }

            if (route != null && PropertyRouteTranslationLogic.IsTranslateable(route))
            {
                result.Add(new TranslatedToken(this));
            }

            return result.AndHasValue(this);
        }

        return SubTokensBase(Column.Type, options, Column.Implementations);
    }

    public override Implementations? GetImplementations()
    {
        return Column.Implementations;
    }

    public override string? IsAllowed()
    {
        return null;  //If it wasn't, sould be filtered before
    }

    public override PropertyRoute? GetPropertyRoute()
    {
        if (Column.PropertyRoutes != null)
            return Column.PropertyRoutes[0]; //HACK: compatibility with IU entitiy elements

        Type? type = Lite.Extract(Type); // Useful?
        if (type != null && type.IsIEntity())
        {
            var implementations = Column.Implementations;
            if (implementations != null && !implementations.Value.IsByAll)
            {
                var imp = implementations.Value.Types.Only();
                if (imp == null)
                    return null;

                return PropertyRoute.Root(imp);
            }
        }

        return null;
    }

   

    public override string NiceName()
    {
        return Column.DisplayName;
    }

    public override QueryToken Clone()
    {
        return new ColumnToken(Column, queryName);
    }
}

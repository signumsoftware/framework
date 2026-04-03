using Signum.DynamicQuery.Tokens;

namespace Signum.UserAssets.Queries;

public sealed class QueryTokenEmbedded : EmbeddedEntity, IEquatable<QueryTokenEmbedded>
{
    private QueryTokenEmbedded()
    {
    }

    public QueryTokenEmbedded(QueryToken token)
    {
        if (token == null)
            throw new ArgumentNullException(nameof(token));

        this.token = token;
    }

    public QueryTokenEmbedded(string tokenString)
    {
        if (string.IsNullOrEmpty(tokenString))
            throw new ArgumentNullException(nameof(tokenString));

        TokenString = tokenString;
    }

    [StringLengthValidator(Min = 1, Max = 200), NotNullValidator(DisabledInModelBinder = true)]
    public string TokenString { get; set; }

    [Ignore]
    QueryToken token;
    [HiddenProperty]
    public QueryToken Token
    {
        get
        {
            if (parseException != null && token == null)
                throw parseException;

            return token;
        }
    }

    [HiddenProperty]
    public QueryToken TryToken
    {
        get { return token; }
    }

    [Ignore]
    Exception? parseException;
    [HiddenProperty]
    public Exception? ParseException
    {
        get { return parseException; }
    }

    protected override void PreSaving(PreSavingContext ctx)
    {
        if (token != null)
            TokenString = token.FullKey();
    }

    public void ParseData(ModifiableEntity context, QueryDescription description, SubTokensOptions options)
    {
        try
        {
            token = QueryUtils.Parse(TokenString, description, options);
        }
        catch (Exception e)
        {
            parseException = new FormatException("{0} {1}: {2}\n{3}".FormatWith(context.GetType().Name, (context as Entity)?.IdOrNull, context, e.Message), e);

            if (Transaction.HasRollbackedTransaction != null)
                throw;
        }
    }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(TokenString) && token == null)
        {
            return parseException != null ? parseException.Message : ValidationMessage._0IsNotSet.NiceToString().FormatWith(pi.NiceName());
        }

        return base.PropertyValidation(pi);
    }

    public override string ToString()
    {
        if (token != null)
            return token.FullKey();

        return TokenString;
    }

    public override bool Equals(object? obj) => obj is QueryTokenEmbedded qte && Equals(qte);
    public bool Equals(QueryTokenEmbedded? other)
    {
        if (other == null)
            return false;

        return GetTokenString() == other.GetTokenString();
    }

    public string GetTokenString()
    {
        return token != null ? token.FullKey() : TokenString;
    }

    public override int GetHashCode()
    {
        return GetTokenString().GetHashCode();
    }

    public QueryTokenEmbedded Clone() => new QueryTokenEmbedded
    {
        TokenString = TokenString,
        token = token
    };
}


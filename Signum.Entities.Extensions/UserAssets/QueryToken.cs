using System;
using System.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;

namespace Signum.Entities.UserAssets
{
    [Serializable, InTypeScript(Undefined = false)]
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

            this.TokenString = tokenString;
        }

        [StringLengthValidator(AllowNulls = false, Min = 1, Max = 200), InTypeScript(Undefined = false, Null = false)]
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
        Exception parseException;
        [HiddenProperty]
        public Exception ParseException
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
                parseException = new FormatException("{0} {1}: {2}\r\n{3}".FormatWith(context.GetType().Name, (context as Entity)?.IdOrNull, context, e.Message), e);
            }
        }

        protected override string PropertyValidation(PropertyInfo pi)
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

        public bool Equals(QueryTokenEmbedded other)
        {
            return this.GetTokenString() == other.GetTokenString();
        }

        public string GetTokenString()
        {
            return this.token != null ? this.token.FullKey() : this.TokenString;
        }

        public override bool Equals(object obj)
        {
            return obj is QueryTokenEmbedded && this.Equals((QueryTokenEmbedded)obj);
        }

        public override int GetHashCode()
        {
            return this.GetTokenString().GetHashCode();
        }

        public QueryTokenEmbedded Clone() => new QueryTokenEmbedded
        {
            TokenString = TokenString,
            token = token
        };
    }

}

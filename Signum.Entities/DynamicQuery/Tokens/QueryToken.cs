using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.Reflection;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Utilities;
using Signum.Entities.Properties;
using Signum.Entities.Reflection;
using Signum.Utilities.ExpressionTrees; 
using System.Text.RegularExpressions;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public abstract class QueryToken : IEquatable<QueryToken>
    {
        public bool Subordinated { get; set; }

        public string SubordinatedToString
        {
            get
            {
                if (Subordinated)
                    return "- " + ToString();

                return ToString();
            }
        }

        public abstract override string ToString();
        public abstract string NiceName();
        public abstract string Format { get; }
        public abstract string Unit { get; }
        public abstract Type Type { get; }
        public abstract string Key { get; }
        protected abstract List<QueryToken> SubTokensInternal();

        public Expression BuildExpression(BuildExpressionContext context)
        {
            Expression result;
            if (context.Replacemens != null && context.Replacemens.TryGetValue(this, out result))
                return result;

            return BuildExpressionInternal(context); 
        }

        protected abstract Expression BuildExpressionInternal(BuildExpressionContext context);

        public abstract PropertyRoute GetPropertyRoute();
        public abstract Implementations? GetImplementations();
        public abstract string IsAllowed();

        public abstract QueryToken Clone();

        public QueryToken Parent { get; private set; }

        public QueryToken(QueryToken parent)
        {
            this.Parent = parent;
        }

        public static QueryToken NewColumn(ColumnDescription column)
        {
            return new ColumnToken(column);
        }

        public List<QueryToken> SubTokens()
        {
            var result = this.SubTokensInternal();

            result.AddRange(OnEntityExtension(this));

            if (result.IsEmpty())
                return new List<QueryToken>();

            result.RemoveAll(t => t.IsAllowed() != null);

            result.Sort((a, b) =>
            {
                return
                    PriorityCompare(a.Key, b.Key, s => s == "Id") ??
                    PriorityCompare(a.Key, b.Key, s => s == "ToString") ??
                    PriorityCompare(a.Key, b.Key, s => s.StartsWith("(")) ??
                    string.Compare(a.ToString(), b.ToString());
            }); 

            return result;
        }

        public int? PriorityCompare(string a, string b, Func<string, bool> isPriority)
        {
            if (isPriority(a))
            {
                if (isPriority(b))
                    return string.Compare(a, b);
                return -1;
            }

            if (isPriority(b))
                return 1;

            return null;
        }

        protected List<QueryToken> SubTokensBase(Type type, Implementations? implementations)
        {
            var ut = type.UnNullify();
            if (ut == typeof(DateTime))
                return DateTimeProperties(this, DateTimePrecision.Milliseconds);

            if (ut == typeof(float) || ut == typeof(double) || ut == typeof(decimal))
                return DecimalProperties(this);

            Type cleanType = type.CleanType();
            if (cleanType.IsIIdentifiable())
            {
                if (implementations.Value.IsByAll)
                    return new List<QueryToken>(); // new[] { EntityPropertyToken.IdProperty(this) };

                var onlyType = implementations.Value.Types.Only();

                if (onlyType != null && onlyType == cleanType)
                    return new[] { EntityPropertyToken.IdProperty(this), new EntityToStringToken(this) }
                        .Concat(EntityProperties(onlyType)).ToList();

                return implementations.Value.Types.Select(t => (QueryToken)new AsTypeToken(this, t)).ToList();
            }

            if (type.IsEmbeddedEntity())
            {
                return EntityProperties(type).OrderBy(a => a.ToString()).ToList();
            }

            if(IsCollecction(type))
            {
                return CollectionProperties(this);
            }

            if (typeof(IQueryTokenBag).IsAssignableFrom(type))
            {
                return BagProperties(type).OrderBy(a => a.ToString()).ToList();
            }

            return new List<QueryToken>();
        }

        public static IEnumerable<QueryToken> OnEntityExtension(QueryToken parent)
        {
            if (EntityExtensions == null)
                throw new InvalidOperationException("QuertToken.EntityExtensions function not set");

            return EntityExtensions(parent);
        }

        public static Func<QueryToken, IEnumerable<QueryToken>>  EntityExtensions;
        

        public static List<QueryToken> DateTimeProperties(QueryToken parent, DateTimePrecision precission)
        {
            string utc = TimeZoneManager.Mode == TimeZoneMode.Utc ? "Utc - " : "";

            return new List<QueryToken>
            {
                new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Year), utc + Resources.Year), 
                new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Month), utc + Resources.Month), 
                new MonthStartToken(parent), 

                new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Day), utc + Resources.Day),
                new DayOfYearToken(parent), 
                new DayOfWeekToken(parent), 
                new DateToken(parent), 
                precission < DateTimePrecision.Hours ? null: new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Hour), utc + Resources.Hour), 
                precission < DateTimePrecision.Minutes ? null: new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Minute), utc + Resources.Minute), 
                precission < DateTimePrecision.Seconds ? null: new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Second), utc + Resources.Second), 
                precission < DateTimePrecision.Milliseconds? null: new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Millisecond), utc + Resources.Millisecond), 
            }.NotNull().ToList();
        }

        public static List<QueryToken> DecimalProperties(QueryToken parent)
        {
            return new List<QueryToken>
            {
                new CeilToken(parent),
                new FloorToken(parent),
            }; 
        }

        public static List<QueryToken> CollectionProperties(QueryToken parent)
        {
            return new QueryToken[]
            {
                new CountToken(parent),
                parent.HasAllOrAny() ?null: new CollectionElementToken(parent, CollectionElementType.Element),
                new CollectionElementToken(parent, CollectionElementType.Any),
                new CollectionElementToken(parent, CollectionElementType.All),
            }.NotNull().ToList();
        }

        public virtual bool HasAllOrAny()
        {
            return Parent != null && Parent.HasAllOrAny(); 
        }

        IEnumerable<QueryToken> EntityProperties(Type type)
        {
            return Reflector.PublicInstancePropertiesInOrder(type)
                  .Where(p => Reflector.QueryableProperty(type, p))
                  .Select(p => (QueryToken)new EntityPropertyToken(this, p));
        }

        IEnumerable<QueryToken> BagProperties(Type type)
        {
            return Reflector.PublicInstancePropertiesInOrder(type)
                  .Select(p => (QueryToken)new BagPropertyToken(this, p));
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
            return other != null && other.FullKey() == this.FullKey();
        }

        public override int GetHashCode()
        {
            return this.FullKey().GetHashCode();
        }

        public virtual string TypeColor
        {
            get
            {
                if (IsCollecction(Type))
                    return "#CE6700";

                switch (QueryUtils.TryGetFilterType(Type))
                {
                    case FilterType.Integer:
                    case FilterType.Decimal:
                    case FilterType.String:
                    case FilterType.Guid: 
                    case FilterType.Boolean: return "#000000";
                    case FilterType.DateTime: return "#8000FF";
                    case FilterType.Enum: return "#B00061";
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

                if (IsCollecction(type))
                {
                    return Resources.ListOf0.Formato(GetNiceTypeName(Type.ElementType(), GetElementImplementations()));
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

        public bool IsCollecction(Type type)
        {
            return type != typeof(string) && type != typeof(byte[]) && type.ElementType() != null;
        }

        static string GetNiceTypeName(Type type, Implementations? implementations)
        {
            switch (QueryUtils.TryGetFilterType(type))
            {
                case FilterType.Integer: return Resources.Number;
                case FilterType.Decimal: return Resources.DecimalNumber;
                case FilterType.String: return Resources.Text;
                case FilterType.DateTime:  return Resources.DateTime;
                case FilterType.Boolean: return Resources.Check;
                case FilterType.Guid: return Resources.GlobalUniqueIdentifier;
                case FilterType.Enum: return type.UnNullify().NiceName();
                case FilterType.Lite:
                {
                    var cleanType = type.CleanType();
                    var imp = implementations.Value;

                    if (imp.IsByAll)
                        return Combine(cleanType, Resources.AnyEntity);
                            

                    if (imp.Types.Only() == type.CleanType())
                        return cleanType.NiceName();

                    return Combine(cleanType, imp.Types.CommaOr(t => t.NiceName()));
                }
                case FilterType.Embedded: return Resources.Embedded0.Formato(type.NiceName());
                default: return type.TypeName();
            }
        }

        private static string Combine(Type cleanType, string implementations)
        {
            if(cleanType == typeof(IIdentifiable) || cleanType == typeof(IdentifiableEntity))
                return implementations;

            return "{0} ({1})".Formato(cleanType.NiceName(), implementations);
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
    }
}

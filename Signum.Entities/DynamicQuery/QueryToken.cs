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
        public abstract override string ToString();
        public abstract string NiceName();
        public abstract string Format { get; }
        public abstract string Unit { get; }
        public abstract Type Type { get; }
        public abstract string Key { get; }
        protected abstract QueryToken[] SubTokensInternal();

        public Expression BuildExpression(BuildExpressionContext context)
        {
            Expression result;
            if (context.Replacemens != null && context.Replacemens.TryGetValue(this, out result))
                return result;

            return BuildExpressionInternal(context); 
        }

        protected abstract Expression BuildExpressionInternal(BuildExpressionContext context);

        public abstract PropertyRoute GetPropertyRoute();
        public abstract Implementations Implementations();
        public abstract bool IsAllowed();

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

        public QueryToken[] SubTokens()
        {
            var result = this.SubTokensInternal();

            if (result == null)
                return null;

            return result.Where(t => t.IsAllowed()).ToArray();
        }

        protected QueryToken[] SubTokensBase(Type type, Implementations implementations)
        {
            if (type.UnNullify() == typeof(DateTime))
            {
                return DateTimeProperties(this, DateTimePrecision.Milliseconds);
            }

            Type cleanType = Reflector.ExtractLite(type) ?? type;
            if (cleanType.IsIIdentifiable())
            {
                if (implementations != null)
                {
                    if (implementations.IsByAll)
                        return null; // new[] { EntityPropertyToken.IdProperty(this) };

                    return ((ImplementedByAttribute)implementations).ImplementedTypes.Select(t => (QueryToken)new AsTypeToken(this, t)).ToArray();

                    //return new[] { EntityPropertyToken.IdProperty(this), EntityPropertyToken.ToStrProperty(this) }
                    //    .Concat(asPropesties).Concat(EntityProperties(cleanType)).ToArray();
                }

                return new[] { EntityPropertyToken.IdProperty(this), EntityPropertyToken.ToStrProperty(this) }
                    .Concat(EntityProperties(cleanType)).ToArray();
            }
            else if (cleanType.IsEmbeddedEntity())
            {
                return EntityProperties(cleanType).ToArray();
            }

            return null;
        }

        public static QueryToken[] DateTimeProperties(QueryToken parent, DateTimePrecision precission)
        {
            string utc = TimeZoneManager.Mode == TimeZoneMode.Utc ? "Utc - " : "";

            return new QueryToken[]
            {
                new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Year), utc + Resources.Year), 
                new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Month), utc + Resources.Month), 
                new MonthStartQueryToken(parent), 
                new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Day), utc + Resources.Day),
                new DateQueryToken(parent), 
                precission < DateTimePrecision.Hours ? null: new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Hour), utc + Resources.Hour), 
                precission < DateTimePrecision.Minutes ? null: new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Minute), utc + Resources.Minute), 
                precission < DateTimePrecision.Seconds ? null: new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Second), utc + Resources.Second), 
                precission < DateTimePrecision.Milliseconds? null: new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo((DateTime dt)=>dt.Millisecond), utc + Resources.Millisecond), 
            }.NotNull().ToArray();
        }

        public static QueryToken[] CollectionProperties(QueryToken parent)
        {
            return new QueryToken[]
            {
                new NetPropertyToken(parent, parent.Type.GetProperty("Count", BindingFlags.Public| BindingFlags.Instance), Resources.Count),
                parent.HasAllOrAny() ?null: new CollectionElementToken(parent, CollectionElementType.Element),
                new CollectionElementToken(parent, CollectionElementType.Any),
                new CollectionElementToken(parent, CollectionElementType.All),
            }.NotNull().ToArray();
        }

        public virtual bool HasAllOrAny()
        {
            return Parent != null && Parent.HasAllOrAny(); 
        }

        IEnumerable<QueryToken> EntityProperties(Type type)
        {
            return Reflector.PublicInstancePropertiesInOrder(type)
                  .Where(p => Reflector.QueryableProperty(type, p))
                  .Select(p => (QueryToken)new EntityPropertyToken(this, p)).OrderBy(a => a.ToString());
        }

        static MethodInfo miToLite = ReflectionTools.GetMethodInfo((IdentifiableEntity ident) => ident.ToLite()).GetGenericMethodDefinition();
        protected static Expression ExtractEntity(Expression expression, bool idAndToStr)
        {
            if (Reflector.ExtractLite(expression.Type) != null)
            {
                MethodCallExpression mce = expression as MethodCallExpression;
                if (mce != null && mce.Method.IsInstantiationOf(miToLite))
                    return mce.Arguments[0];

                if (!idAndToStr)
                    return Expression.Property(expression, "Entity");
            }
            return expression;
        }

        protected static Expression BuildLite(Expression expression)
        {
            if (Reflector.IsIIdentifiable(expression.Type))
                return Expression.Call(miToLite.MakeGenericMethod(expression.Type), expression);

            return expression;
        }

        public static Type BuildLite(Type type)
        {
            if (Reflector.IsIIdentifiable(type))
                return Reflector.GenerateLite(type);

            return type;
        }

        public string FullKey()
        {
            if (Parent == null)
                return Key;

            return Parent.FullKey() + "." + Key;
        }

        public virtual QueryToken MatchPart(string key)
        {
            return key == Key ? this : null;
        }

        public bool Equals(QueryToken other)
        {
            return other != null && other.FullKey() == this.FullKey();
        }

        public override int GetHashCode()
        {
            return this.FullKey().GetHashCode();
        }
    }

    [Serializable]
    public class NetPropertyToken : QueryToken
    {
        public PropertyInfo PropertyInfo { get; private set; }
        public string DisplayName { get; private set; }

        internal NetPropertyToken(QueryToken parent, Expression<Func<object>> pi, string displayName): 
            this(parent, ReflectionTools.GetPropertyInfo(pi), displayName)
        {
        
        }

        internal NetPropertyToken(QueryToken parent, PropertyInfo pi, string displayName)
            : base(parent)
        {
            if (parent == null)
                throw new ArgumentNullException("parent");

            if (pi == null)
                throw new ArgumentNullException("pi");

            if (displayName == null)
                throw new ArgumentNullException("displayName");

            this.DisplayName = displayName;
            this.PropertyInfo = pi;
        }

        public override Type Type
        {
            get { return PropertyInfo.PropertyType; }
        }

        public override string ToString()
        {
            return DisplayName;
        }

        public override string Key
        {
            get { return PropertyInfo.Name; }
        }

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var result = Parent.BuildExpression(context);

            return Expression.Property(result.UnNullify(), PropertyInfo);
        }

        protected override QueryToken[] SubTokensInternal()
        {
            return null;
        }

        public override string Format
        {
            get { return null; }
        }

        public override string Unit
        {
            get { return null; }
        }

        public override Implementations Implementations()
        {
            return null;
        }

        public override bool IsAllowed()
        {
            return Parent.IsAllowed();
        }

        public override PropertyRoute GetPropertyRoute()
        {
            return null;
        }

        public override string NiceName()
        {
            return DisplayName + Resources.Of + Parent.ToString();
        }

        public override QueryToken Clone()
        {
            return new NetPropertyToken(Parent.Clone(), PropertyInfo, DisplayName);
        }
    }

    [Serializable]
    public class EntityPropertyToken : QueryToken
    {
        public PropertyInfo PropertyInfo { get; private set; }
        public bool IsSpecial { get; private set; }
        
        public static QueryToken IdProperty(QueryToken parent)
        {
            return new EntityPropertyToken(parent, ReflectionTools.GetPropertyInfo((IdentifiableEntity e) => e.Id));
        }

        public static QueryToken ToStrProperty(QueryToken parent)
        {
            return new EntityPropertyToken(parent, ReflectionTools.GetPropertyInfo((IdentifiableEntity e) => e.ToStr));
        }

        internal EntityPropertyToken(QueryToken parent, PropertyInfo pi, bool isSpecial = false)
            : base(parent)
        {
            if (pi == null)
                throw new ArgumentNullException("pi");

            this.PropertyInfo = pi;

            this.IsSpecial = isSpecial;
        }

        public override Type Type
        {
            get { return BuildLite(PropertyInfo.PropertyType); }
        }

        public override string ToString()
        {
            if (IsSpecial)
                return "[{0}]".Formato(PropertyInfo.NiceName());

            return PropertyInfo.NiceName();
        }

        public override string Key
        {
            get { return PropertyInfo.Name; }
        }

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var baseExpression = Parent.BuildExpression(context);

            if (PropertyInfo.Is((IdentifiableEntity ident) => ident.Id) ||
                PropertyInfo.Is((IdentifiableEntity ident) => ident.ToStr))
            {
                baseExpression = ExtractEntity(baseExpression, true);

                return Expression.Property(baseExpression, PropertyInfo.Name); // Late binding over Lite or Identifiable
            }

            baseExpression = ExtractEntity(baseExpression, false);

            Expression result = Expression.Property(baseExpression, PropertyInfo);

            return BuildLite(result);
        }

        protected override QueryToken[] SubTokensInternal()
        {
            if (PropertyInfo.PropertyType.UnNullify() == typeof(DateTime))
            {  
                PropertyRoute route = this.GetPropertyRoute();

                if (route != null)
                {
                    var att = Validator.GetOrCreatePropertyPack(route.Parent.Type, route.PropertyInfo.Name).TryCC(pp =>
                        pp.Validators.OfType<DateTimePrecissionValidatorAttribute>().SingleOrDefault());
                    if (att != null)
                    {
                        return DateTimeProperties(this, att.Precision);
                    }
                }
            }

            if (Reflector.IsMList(PropertyInfo.PropertyType))
            {
                return NetPropertyToken.CollectionProperties(this);
            }

            return SubTokensBase(PropertyInfo.PropertyType, Implementations());
        }

        public override Implementations Implementations()
        {
            return GetPropertyRoute().GetImplementations();
        }

        public override string Format
        {
            get { return Reflector.FormatString(this.GetPropertyRoute()); }
        }

        public override string Unit
        {
            get { return PropertyInfo.SingleAttribute<UnitAttribute>().TryCC(u => u.UnitName); }
        }

        public override bool IsAllowed()
        {
            PropertyRoute route = GetPropertyRoute();

            return Parent.IsAllowed() && (route == null || route.IsAllowed());
        }

        public override PropertyRoute GetPropertyRoute()
        {
            PropertyRoute parent = Parent.GetPropertyRoute();
            if (parent == null)
                return null;

            Type type = Reflector.ExtractLite(parent.Type);
            if (type != null)
                return PropertyRoute.Root(type).Add(PropertyInfo);

            return parent.Add(PropertyInfo);
        }

        public override string NiceName()
        {
            return PropertyInfo.NiceName() + Resources.Of + Parent.ToString();
        }

        public override QueryToken Clone()
        {
            return new EntityPropertyToken(Parent.Clone(), PropertyInfo);
        }
    }

    [Serializable]
    public class AsTypeToken : QueryToken
    {
        Type type;
        internal AsTypeToken(QueryToken parent, Type type)
            : base(parent)
        {
            if (parent == null)
                throw new ArgumentNullException("parent");

            if (type == null)
                throw new ArgumentNullException("type");

            this.type = type;
        }

        public override Type Type
        {
            get { return BuildLite(type); }
        }

        public override string ToString()
        {
            return Resources.As0.Formato(Type.NiceName());
        }

        public override string Key
        {
            get { return "({0})".Formato(type.FullName.Replace(".", ":")); }
        }

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            Expression baseExpression = Parent.BuildExpression(context);

            Expression result = Expression.TypeAs(ExtractEntity(baseExpression, false), type);

            return BuildLite(result);
        }

        protected override QueryToken[] SubTokensInternal()
        {
            return SubTokensBase(type, null);
        }

        public override string Format
        {
            get { return null; }
        }

        public override string Unit
        {
            get { return null; }
        }

        public override Implementations Implementations()
        {
            return null;
        }

        public override bool IsAllowed()
        {
            return Parent.IsAllowed();
        }

        public override PropertyRoute GetPropertyRoute()
        {
            return PropertyRoute.Root(type);
        }

        public override string NiceName()
        {
            return Resources._0As1.Formato(Parent.ToString(), type.NiceName());
        }

        public override QueryToken Clone()
        {
            return new AsTypeToken(Parent.Clone(), Type); 
        }
    }

    [Serializable]
    public class ColumnToken : QueryToken
    {
        public ColumnDescription Column { get; private set; }

        internal ColumnToken(ColumnDescription column)
            : base(null)
        {
            if (column == null)
                throw new ArgumentNullException("column");

            this.Column = column;
        }

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

        public override string Format
        {
            get { return Column.Format; }
        }

        public override string Unit
        {
            get { return Column.Unit; }
        }

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            throw new InvalidOperationException("BuildExpressionInternal not supported for ColumnToken");
        }

        protected override QueryToken[] SubTokensInternal()
        {
            if (Column.Type.UnNullify() == typeof(DateTime))
            {
                if (Column.PropertyRoutes != null)
                {
                    DateTimePrecision? precission = 
                        Column.PropertyRoutes.Select(pr => Validator.GetOrCreatePropertyPack(pr.Parent.Type, pr.PropertyInfo.Name)
                        .Validators.OfType<DateTimePrecissionValidatorAttribute>().SingleOrDefault())
                        .Select(dtp => dtp.TryCS(d => d.Precision)).Distinct().Only();

                    if (precission != null)
                        return DateTimeProperties(this, precission.Value);
                }

                if (Column.Format == "d")
                    return DateTimeProperties(this, DateTimePrecision.Days);

            }

            return SubTokensBase(Column.Type, Column.Implementations);
        }

        public override Implementations Implementations()
        {
            return Column.Implementations;
        }

        public override bool IsAllowed()
        {
            return true;  //If it wasn't, sould be filtered before
        }

        public override PropertyRoute GetPropertyRoute()
        {
            if (Column.PropertyRoutes != null)
                return Column.PropertyRoutes[0]; //HACK: compatibility with IU entitiy elements

            Type type = Reflector.ExtractLite(Type);
            if (type != null && typeof(IdentifiableEntity).IsAssignableFrom(type))
                return PropertyRoute.Root(type);

            return null;
        }

        public override string NiceName()
        {
            return Column.DisplayName;
        }

        public override QueryToken Clone()
        {
            return new ColumnToken(Column);
        }
    }

    [Serializable]
    public class MonthStartQueryToken : QueryToken
    {
        internal MonthStartQueryToken(QueryToken parent)
            : base(parent)
        {
        }

        public override string ToString()
        {
            return Resources.MonthStart;
        }

        public override string NiceName()
        {
            return Resources.MonthStart + Resources.Of + Parent.ToString();
        }

        public override string Format
        {
            get { return "Y"; }
        }

        public override string Unit
        {
            get { return null; }
        }

        public override Type Type
        {
            get { return typeof(DateTime); }
        }

        public override string Key
        {
            get { return "MonthStart"; }
        }

        protected override QueryToken[] SubTokensInternal()
        {
            return null;
        }

        static MethodInfo miMonthStart = ReflectionTools.GetMethodInfo(() => DateTimeExtensions.MonthStart(DateTime.MinValue));

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var exp = Parent.BuildExpression(context);
            
            return Expression.Call(miMonthStart, exp.UnNullify());
        }

        public override PropertyRoute GetPropertyRoute()
        {
            return Parent.GetPropertyRoute();
        }

        public override Implementations Implementations()
        {
            return null;
        }

        public override bool IsAllowed()
        {
            return Parent.IsAllowed();
        }

        public override QueryToken Clone()
        {
            return new MonthStartQueryToken(Parent.Clone());
        }
    }


    [Serializable]
    public class DateQueryToken : QueryToken
    {
        internal DateQueryToken(QueryToken parent)
            : base(parent)
        {
        }

        public override string ToString()
        {
            return Resources.Date;
        }

        public override string NiceName()
        {
            return Resources.Date + Resources.Of + Parent.ToString();
        }

        public override string Format
        {
            get { return "d"; }
        }

        public override string Unit
        {
            get { return null; }
        }

        public override Type Type
        {
            get { return typeof(DateTime); }
        }

        public override string Key
        {
            get { return "Date"; }
        }

        protected override QueryToken[] SubTokensInternal()
        {
            return null;
        }

        static PropertyInfo miDate = ReflectionTools.GetPropertyInfo((DateTime d) => d.Date);

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var exp = Parent.BuildExpression(context);

            return Expression.Property(exp.UnNullify(), miDate);
        }

        public override PropertyRoute GetPropertyRoute()
        {
            return Parent.GetPropertyRoute();
        }

        public override Implementations Implementations()
        {
            return null;
        }

        public override bool IsAllowed()
        {
            return Parent.IsAllowed();
        }

        public override QueryToken Clone()
        {
            return new DateQueryToken(Parent.Clone()); 
        }
    }

    [ForceLocalization]
    public enum CollectionElementType
    {
        Element,
        Any,
        All
    }

    [Serializable]
    public class CollectionElementToken : QueryToken
    {
        public CollectionElementType ElementType { get; private set; }

        internal CollectionElementToken(QueryToken parent, CollectionElementType type)
            : base(parent)
        {
            if (parent.Type.ElementType() == null)
                throw new InvalidOperationException("not a collection");

            this.ElementType = type; 
        }

        public override Type Type
        {
            get { return BuildLite(Parent.Type.ElementType());  }
        }

        public override string ToString()
        {
            return ElementType.NiceToString();
        }

        public override string Key
        {
            get { return ElementType.ToString(); }
        }

      

        protected override QueryToken[] SubTokensInternal()
        {
            if (Type.ElementType() != null)
            {
                return NetPropertyToken.CollectionProperties(this);
            }

            return SubTokensBase(Type, Implementations());
        }

        public override Implementations Implementations()
        {
            return Parent.Implementations();
        }

        public override string Format
        {
            get { return Parent.Format; }
        }

        public override string Unit
        {
            get { return Parent.Unit; }
        }

        public override bool IsAllowed()
        {
            return Parent.IsAllowed(); 
        }

        public override bool HasAllOrAny()
        {
            return ElementType == CollectionElementType.All || ElementType == CollectionElementType.Any;
        }

        public override PropertyRoute GetPropertyRoute()
        {
            PropertyRoute parent = Parent.GetPropertyRoute();
            if (parent == null)
                return null;

            return parent.Add("Item");
        }

        public override string NiceName()
        {
            if(ElementType != CollectionElementType.Element)
                throw new InvalidOperationException("NiceName not supported for {0}".Formato(ElementType));

            Type parentElement = Parent.Type.ElementType().CleanType();

            if (parentElement.IsModifiableEntity())
                return parentElement.NiceName();

            return "Element of " + Parent.NiceName();
        }

        public override QueryToken Clone()
        {
            return new CollectionElementToken(Parent.Clone(), ElementType);
        }

        static MethodInfo miAnyE = ReflectionTools.GetMethodInfo((IEnumerable<string> col) => col.Any(null)).GetGenericMethodDefinition();
        static MethodInfo miAllE = ReflectionTools.GetMethodInfo((IEnumerable<string> col) => col.All(null)).GetGenericMethodDefinition();
        static MethodInfo miAnyQ = ReflectionTools.GetMethodInfo((IQueryable<string> col) => col.Any(null)).GetGenericMethodDefinition();
        static MethodInfo miAllQ = ReflectionTools.GetMethodInfo((IQueryable<string> col) => col.All(null)).GetGenericMethodDefinition();

        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            throw new InvalidOperationException("CollectionElementToken does not support this method");
        }

        internal Expression BuildExpressionLambda(BuildExpressionContext context, LambdaExpression lambda)
        {
            MethodInfo mi = typeof(IQueryable).IsAssignableFrom(Parent.Type) ? (ElementType == CollectionElementType.All ? miAllQ: miAnyQ ) : 
                                                                               (ElementType == CollectionElementType.All ? miAllE: miAnyE );

            var collection = Parent.BuildExpression(context);

            return Expression.Call(mi.MakeGenericMethod(Parent.Type.ElementType()), collection, lambda);
        }

        internal ParameterExpression CreateParameter()
        {
            return Expression.Parameter(Parent.Type.ElementType());
        }

        internal Expression CreateExpression(ParameterExpression parameter)
        {
            return BuildLite(parameter);
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

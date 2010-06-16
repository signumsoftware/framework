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
     
        public abstract Expression BuildExpression(Expression expression);

        public abstract PropertyRoute GetPropertyRoute();
        public abstract Implementations Implementations();
        public abstract bool IsAllowed();

        public QueryToken Parent { get; private set; }

        public QueryToken(QueryToken parent)
        {
            this.Parent = parent;
        }

        public static QueryToken NewColumn(StaticColumn column)
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
                return NetPropertyToken.DateTimeProperties(this, DateTimePrecision.Milliseconds);
            }

            Type cleanType = Reflector.ExtractLite(type) ?? type;
            if (typeof(IIdentifiable).IsAssignableFrom(cleanType))
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
            else if(typeof(EmbeddedEntity).IsAssignableFrom(cleanType))
            {
                return EntityProperties(cleanType).ToArray();
            }

            return null;
        }

        IEnumerable<QueryToken> EntityProperties(Type type)
        {
            return Reflector.PublicInstancePropertiesInOrder(type)
                  .Where(p => Reflector.QueryableProperty(type, p))
                  .Select(p => (QueryToken)new EntityPropertyToken(this, p));
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

        static Regex regex = new Regex(@"^(?<token>[^\.]+)(\.(?<token>(\([^\)]+\))|([^\.]+)))*$", RegexOptions.ExplicitCapture | RegexOptions.Singleline);

        public static QueryToken Parse(QueryDescription queryDescription, string tokenString)
        {
            Match m = regex.Match(tokenString);
            if (!m.Success)
                throw new FormatException("Invalid QueryToken string"); 

            string[] tokens = m.Groups["token"].Captures.Cast<Capture>().Select(c=>c.Value).ToArray(); 

            string first = tokens.First();

            StaticColumn column = queryDescription.StaticColumns.Where(a => a.Name == first).Single(
                Resources.Column0NotFoundOnQuery1.Formato(first, QueryUtils.GetNiceQueryName(queryDescription.QueryName)),
                Resources.MoreThanOneColumnNamed0OnQuery1.Formato(first, QueryUtils.GetNiceQueryName(queryDescription.QueryName)));

            var result = QueryToken.NewColumn(column);

            foreach (var token in tokens.Skip(1))
            {
                result = result.SubTokensInternal().Where(k => k.Key == token).Single(
                      Resources.Token0NotCompatibleWith1.Formato(first, result),
                      Resources.MoreThanOneTokenWithKey0FoundOn1.Formato(first, result));
            }

            return result;
        }

        public bool Equals(QueryToken other)
        {
            return other != null && other.FullKey() == this.FullKey();
        }
    }

    [Serializable]
    public class NetPropertyToken : QueryToken
    {
        public PropertyInfo PropertyInfo { get; private set; }
        public string DisplayName { get; private set; }

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

        public override Expression BuildExpression(Expression expression)
        {
            var result = Parent.BuildExpression(expression);
            if (Nullable.GetUnderlyingType(result.Type) != null)
                result = Expression.Property(result, "Value");
            return Expression.Property(result, PropertyInfo);
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

        static QueryToken NewNetProperty<T, S>(QueryToken parent, Expression<Func<T, S>> property, string propertyName)
        {
            return new NetPropertyToken(parent, ReflectionTools.GetPropertyInfo(property), propertyName);
        }

        public static QueryToken[] DateTimeProperties(QueryToken parent, DateTimePrecision precission)
        {
            string utc = TimeZoneManager.Mode == TimeZoneMode.Utc ? "Utc - " : "";

            return new[]
            {
                NewNetProperty(parent, (DateTime dt)=>dt.Year,utc + Resources.Year), 
                NewNetProperty(parent, (DateTime dt)=>dt.Month,utc + Resources.Month), 
                NewNetProperty(parent, (DateTime dt)=>dt.Day,utc + Resources.Day), 
                precission < DateTimePrecision.Hours ? null: NewNetProperty(parent, (DateTime dt)=>dt.Hour,utc + Resources.Hour), 
                precission < DateTimePrecision.Minutes ? null: NewNetProperty(parent, (DateTime dt)=>dt.Minute,utc + Resources.Minute), 
                precission < DateTimePrecision.Seconds ? null: NewNetProperty(parent, (DateTime dt)=>dt.Second,utc + Resources.Second), 
                precission < DateTimePrecision.Milliseconds? null: NewNetProperty(parent, (DateTime dt)=>dt.Millisecond,utc + Resources.Millisecond), 
            }.NotNull().ToArray();
        }

        public static QueryToken[] CollectionProperties(QueryToken parent)
        {
            return new[]
            {
                new NetPropertyToken(parent, parent.Type.GetProperty("Count", BindingFlags.Public| BindingFlags.Instance), Resources.Count)
            };
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
            return DisplayName + Resources.Of + Parent.NiceName();
        }
    }

    [Serializable]
    public class EntityPropertyToken : QueryToken
    {
        public PropertyInfo PropertyInfo { get; private set; }
        
        public static QueryToken IdProperty(QueryToken parent)
        {
            return new EntityPropertyToken(parent, ReflectionTools.GetPropertyInfo((IdentifiableEntity e) => e.Id));
        }

        public static QueryToken ToStrProperty(QueryToken parent)
        {
            return new EntityPropertyToken(parent, ReflectionTools.GetPropertyInfo((IdentifiableEntity e) => e.ToStr));
        }

        internal EntityPropertyToken(QueryToken parent, PropertyInfo pi)
            : base(parent)
        {
            if (pi == null)
                throw new ArgumentNullException("pi");

            this.PropertyInfo = pi;
        }

        public override Type Type
        {
            get { return BuildLite(PropertyInfo.PropertyType); }
        }

        public override string ToString()
        {
            return PropertyInfo.NiceName();
        }

        public override string Key
        {
            get { return PropertyInfo.Name; }
        }

        public override Expression BuildExpression(Expression expression)
        {
            var baseExpression = Parent.BuildExpression(expression);

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
                        return NetPropertyToken.DateTimeProperties(this, att.Precision);
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
            return PropertyInfo.NiceName() + Resources.Of + Parent.NiceName();
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
            get { return "({0})".Formato(type); }
        }

        public override Expression BuildExpression(Expression expression)
        {
            Expression baseExpression = Parent.BuildExpression(expression);

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
            return Resources._0As1.Formato(Parent.NiceName(), type.NiceName());
        }
    }

    [Serializable]
    public class ColumnToken : QueryToken
    {
        public StaticColumn Column { get; private set; }

        internal ColumnToken(StaticColumn column)
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
            return Column.DisplayName;
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

        public override Expression BuildExpression(Expression expression)
        {
            //No base
            return Expression.Property(expression, Column.Name);
        }

        protected override QueryToken[] SubTokensInternal()
        {
            if (Column.Type.UnNullify() == typeof(DateTime))
            {
                if (Column.PropertyRoute != null)
                {
                    var att = Validator.GetOrCreatePropertyPack(Column.PropertyRoute.Parent.Type, Column.PropertyRoute.PropertyInfo.Name)
                        .Validators.OfType<DateTimePrecissionValidatorAttribute>().SingleOrDefault();
                    if (att != null)
                        return NetPropertyToken.DateTimeProperties(this, att.Precision);
                }

                if (Column.Format == "d")
                    return NetPropertyToken.DateTimeProperties(this, DateTimePrecision.Days);

            }

            return SubTokensBase(Column.Type, Column.Implementations);
        }

        public override Implementations Implementations()
        {
            return Column.Implementations;
        }

        public override bool IsAllowed()
        {
            return true;  // Is it wasn't it sould be filtered before
        }

        public override PropertyRoute GetPropertyRoute()
        {
            if (Column.PropertyRoute != null)
                return Column.PropertyRoute;

            Type type = Reflector.ExtractLite(Type);
            if (type != null && typeof(IdentifiableEntity).IsAssignableFrom(type))
                return PropertyRoute.Root(type);

            return null;
        }

        public override string NiceName()
        {
            return Column.DisplayName;
        }
    }
}

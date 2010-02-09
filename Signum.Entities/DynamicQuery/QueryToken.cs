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
        public abstract QueryToken[] SubTokens();
     
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

        protected QueryToken[] SubTokens(Type type, Implementations implementations)
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
                        return new[] { EntityPropertyToken.IdProperty(this) };

                    var asPropesties = ((ImplementedByAttribute)implementations).ImplementedTypes.Select(t => (QueryToken)new AsTypeToken(this, t)).ToArray();

                    return new[] { EntityPropertyToken.IdProperty(this), EntityPropertyToken.ToStrProperty(this) }
                        .Concat(asPropesties).Concat(EntityProperties(cleanType)).ToArray();
                }

                return new[] { EntityPropertyToken.IdProperty(this), EntityPropertyToken.ToStrProperty(this) }
                    .Concat(EntityProperties(cleanType)).ToArray();
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

        public static QueryToken Parse(object queryName, QueryDescription queryDescription, string tokenString)
        {
            string[] tokens = tokenString.Split('.');

            string first = tokens.First();

            StaticColumn column = queryDescription.StaticColumns.Where(a => a.Name == first).Single(
                "Column {0} not found on query {1}".Formato(first, QueryUtils.GetNiceQueryName(queryName)),
                "More than one column named {0} on query {1}".Formato(first, QueryUtils.GetNiceQueryName(queryName)));

            var result = QueryToken.NewColumn(column);

            foreach (var token in tokens.Skip(1))
            {
                result = result.SubTokens().Where(k => k.Key == token).Single(
                      "Token {0} not compatible with {1}".Formato(first, result),
                      "More than one token with key {0} found on {1}".Formato(first, result));
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
            return Expression.Property(result, PropertyInfo);
        }

        public override QueryToken[] SubTokens()
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
            return new[]
            {
                NewNetProperty(parent, (DateTime dt)=>dt.Year, Resources.Year), 
                NewNetProperty(parent, (DateTime dt)=>dt.Month, Resources.Month), 
                NewNetProperty(parent, (DateTime dt)=>dt.Day, Resources.Day), 
                precission < DateTimePrecision.Hours ? null: NewNetProperty(parent, (DateTime dt)=>dt.Hour, Resources.Hour), 
                precission < DateTimePrecision.Minutes ? null: NewNetProperty(parent, (DateTime dt)=>dt.Minute, Resources.Minute), 
                precission < DateTimePrecision.Seconds ? null: NewNetProperty(parent, (DateTime dt)=>dt.Second, Resources.Second), 
                precission < DateTimePrecision.Milliseconds? null: NewNetProperty(parent, (DateTime dt)=>dt.Millisecond, Resources.Millisecond), 
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
        public Implementations implementations;

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

        public override QueryToken[] SubTokens()
        {
            if (PropertyInfo.PropertyType.UnNullify() == typeof(DateTime))
            {
                if (PropertyInfo != null)
                {
                    var att = Validator.GetOrCreatePropertyPack(PropertyInfo).TryCC(pp =>
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

            return SubTokens(PropertyInfo.PropertyType, implementations);
        }

        public override Implementations Implementations()
        {
            return implementations;
        }

        public override string Format
        {
            get { return Reflector.FormatString(PropertyInfo); }
        }

        public override string Unit
        {
            get { return PropertyInfo.SingleAttribute<UnitAttribute>().TryCC(u => u.UnitName); }
        }

        public override bool IsAllowed()
        {
            return Parent.IsAllowed() && GetPropertyRoute().IsAllowed();
        }

        public override PropertyRoute GetPropertyRoute()
        {
            return Parent.GetPropertyRoute().Add(PropertyInfo);
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

        public override QueryToken[] SubTokens()
        {
            return SubTokens(type, null);
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

        public override QueryToken[] SubTokens()
        {
            if (Column.Type.UnNullify() == typeof(DateTime))
            {
                if (Column.PropertyRoute != null)
                {
                    var att = Validator.GetOrCreatePropertyPack(Column.PropertyRoute.PropertyInfo)
                        .Validators.OfType<DateTimePrecissionValidatorAttribute>().SingleOrDefault();
                    if (att != null)
                        return NetPropertyToken.DateTimeProperties(this, att.Precision);
                }

                if (Column.Format == "d")
                    return NetPropertyToken.DateTimeProperties(this, DateTimePrecision.Days);

            }

            return SubTokens(Column.Type, Column.Implementations);
        }

        public override Implementations Implementations()
        {
            return Column.Implementations;
        }

        public override bool IsAllowed()
        {
            return Column.IsAllowed();
        }

        public override PropertyRoute GetPropertyRoute()
        {
            Type type = Reflector.ExtractLite(Type);
            if (type != null)
                return PropertyRoute.Root(type);

            return null;
        }

        public override string NiceName()
        {
            return Column.DisplayName;
        }
    }
}

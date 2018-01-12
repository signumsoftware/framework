using Signum.Entities.Basics;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Signum.Entities
{
    [Serializable]
    public class PropertyRoute : IEquatable<PropertyRoute>, ISerializable
    {
        Type type;
        public PropertyRouteType PropertyRouteType { get; private set; }
        public FieldInfo FieldInfo { get; private set; }
        public PropertyInfo PropertyInfo { get; private set; }
        public PropertyRoute Parent { get; private set; }

        public MemberInfo[] Members
        {
            get
            {
                return this.Follow(a => a.Parent).Select(a =>
                    a.PropertyRouteType == Entities.PropertyRouteType.Mixin ? a.type :
                    a.FieldInfo ?? (MemberInfo)a.PropertyInfo).Reverse().Skip(1).ToArray();
            }
        }

        public PropertyInfo[] Properties
        {
            get { return this.Follow(a => a.Parent).Select(a => a.PropertyInfo).Reverse().Skip(1).ToArray(); }
        }

        public static PropertyRoute Construct<T, S>(Expression<Func<T, S>> propertyRoute)
            where T : IRootEntity
        {
            return Root(typeof(T)).Continue(propertyRoute);
        }

        public PropertyRoute Continue<T, S>(Expression<Func<T, S>> propertyRoute)
        {
            if (typeof(T) != this.Type)
                throw new InvalidOperationException("Type mismatch between {0} and {1}".FormatWith(typeof(T).TypeName(), this.Type.TypeName()));

            var list = Reflector.GetMemberList(propertyRoute);

            return Continue(list);
        }

        public PropertyRoute Continue(MemberInfo[] list)
        {
            var result = this;

            foreach (var mi in list)
            {
                result = result.Add(mi);
            }
            return result;
        }

        public PropertyRoute Add(string fieldOrProperty)
        {
            return Add(GetMember(fieldOrProperty));
        }

        MemberInfo GetMember(string fieldOrProperty)
        {
            MemberInfo mi = (MemberInfo)Type.GetProperty(fieldOrProperty, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, null, IsCollection() ? new[] { typeof(int) } : new Type[0], null) ??
                            (MemberInfo)Type.GetField(fieldOrProperty, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (mi == null && Type.IsEntity())
            {
                string name = ExtractMixin(fieldOrProperty);

                mi = MixinDeclarations.GetMixinDeclarations(Type).FirstOrDefault(t => t.Name == name);
            }

            if (mi == null)
                throw new InvalidOperationException("{0}.{1} does not exist".FormatWith(this, fieldOrProperty));

            return mi;
        }

        private bool IsCollection()
        {
            return Type.ElementType() != null && Type != typeof(string);
        }

        static string ExtractMixin(string fieldOrProperty)
        {
            Match match = Regex.Match(fieldOrProperty, @"^\[(?<type>.*)\]$");

            if (!match.Success)
                return null;

            return match.Groups["type"].Value;
        }

        public PropertyRoute Add(MemberInfo member)
        {
            using (HeavyProfiler.LogNoStackTrace("PR.Add", () => member.Name))
            {
                if (member is MethodInfo && ((MethodInfo)member).IsInstantiationOf(MixinDeclarations.miMixin))
                    member = ((MethodInfo)member).GetGenericArguments()[0];

                if (this.Type.IsIEntity() && PropertyRouteType != PropertyRouteType.Root)
                {
                    Implementations imp = GetImplementations();

                    Type only;
                    if (imp.IsByAll || (only = imp.Types.Only()) == null)
                        throw new InvalidOperationException("Attempt to make a PropertyRoute on a {0}. Cast first".FormatWith(imp));

                    return new PropertyRoute(Root(only), member);
                }

                return new PropertyRoute(this, member);
            }
        }

        PropertyRoute(PropertyRoute parent, MemberInfo fieldOrProperty)
        {
            SetParentAndProperty(parent, fieldOrProperty);
        }

        void SetParentAndProperty(PropertyRoute parent, MemberInfo fieldOrProperty)
        {
            if (fieldOrProperty == null)
                throw new ArgumentNullException("fieldOrProperty");

            this.Parent = parent ?? throw new ArgumentNullException("parent");

            if (parent.Type.IsIEntity() && parent.PropertyRouteType != PropertyRouteType.Root)
                throw new ArgumentException("Parent can not be a non-root Identifiable");

            if (fieldOrProperty is PropertyInfo && parent.IsCollection())
            {
                if (fieldOrProperty.Name != "Item")
                    throw new NotSupportedException("PropertyInfo {0} is not supported".FormatWith(fieldOrProperty.Name));

                PropertyInfo = (PropertyInfo)fieldOrProperty;
                PropertyRouteType = PropertyRouteType.MListItems;
            }
            else if (fieldOrProperty is PropertyInfo && parent.Type.IsLite())
            {
                if (fieldOrProperty.Name != "Entity" && fieldOrProperty.Name != "EntityOrNull")
                    throw new NotSupportedException("PropertyInfo {0} is not supported".FormatWith(fieldOrProperty.Name));

                PropertyInfo = (PropertyInfo)fieldOrProperty;
                PropertyRouteType = PropertyRouteType.LiteEntity;
            }
            else if (typeof(Entity).IsAssignableFrom(parent.type) && fieldOrProperty is Type)
            {
                MixinDeclarations.AssertDeclared(parent.type, (Type)fieldOrProperty);

                type = (Type)fieldOrProperty;
                PropertyRouteType = PropertyRouteType.Mixin;
            }
            else if (typeof(ModifiableEntity).IsAssignableFrom(parent.Type) || typeof(IRootEntity).IsAssignableFrom(parent.Type))
            {
                PropertyRouteType = PropertyRouteType.FieldOrProperty;
                if (fieldOrProperty is PropertyInfo)
                {
                    if (!parent.Type.Follow(a => a.BaseType).Contains(fieldOrProperty.DeclaringType))
                    {
                        var pi = (PropertyInfo)fieldOrProperty;

                        if (!parent.Type.GetInterfaces().Contains(fieldOrProperty.DeclaringType))
                            throw new ArgumentException("PropertyInfo {0} not found on {1}".FormatWith(pi.PropertyName(), parent.Type));

                        var otherProperty = parent.Type.Follow(a => a.BaseType)
                            .Select(a => a.GetProperty(fieldOrProperty.Name, BindingFlags.Public | BindingFlags.Instance, null, null, new Type[0], null)).NotNull().FirstEx();

                        fieldOrProperty = otherProperty ?? throw new ArgumentException("PropertyInfo {0} not found on {1}".FormatWith(pi.PropertyName(), parent.Type));
                    }

                    PropertyInfo = (PropertyInfo)fieldOrProperty;
                    FieldInfo = Reflector.TryFindFieldInfo(Parent.Type, PropertyInfo);
                }
                else if (fieldOrProperty is MethodInfo && ((MethodInfo)fieldOrProperty).Name == "ToString")
                {
                    FieldInfo = (FieldInfo)fiToStr;
                    PropertyInfo = null;
                }
                else
                {
                    FieldInfo = (FieldInfo)fieldOrProperty;
                    PropertyInfo = Reflector.TryFindPropertyInfo(FieldInfo);
                }
            }
            else
                throw new NotSupportedException("Properties of {0} not supported".FormatWith(parent.Type));

        }



        static readonly FieldInfo fiToStr = ReflectionTools.GetFieldInfo((Entity e) => e.toStr);

        public static PropertyRoute Root(Type rootEntity)
        {
            return new PropertyRoute(rootEntity);
        }

        PropertyRoute(Type type)
        {
            SetRootType(type);
        }

        void SetRootType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (!typeof(IRootEntity).IsAssignableFrom(type))
                throw new ArgumentException("Type must implement IPropertyRouteRoot");

            if (type.IsAbstract)
                throw new ArgumentException("Type must be non-abstract");

            this.type = type;
            this.PropertyRouteType = PropertyRouteType.Root;
        }

        public Type Type
        {
            get
            {
                if (type != null)
                    return type;

                if (FieldInfo != null)
                    return FieldInfo.FieldType;

                if (PropertyInfo != null)
                    return PropertyInfo.PropertyType;

                throw new InvalidOperationException("No FieldInfo or PropertyInfo");
            }
        }

        public Type RootType
        {
            get
            {
                if (type != null && type.IsIRootEntity())
                    return type;

                return Parent.RootType;
            }
        }

        public override string ToString()
        {
            switch (PropertyRouteType)
            {
                case PropertyRouteType.Root:
                    return "({0})".FormatWith(typeof(Entity).IsAssignableFrom(type) ? TypeEntity.GetCleanName(type) : type.Name);
                case PropertyRouteType.FieldOrProperty:
                    return Parent.ToString() + (Parent.PropertyRouteType == PropertyRouteType.MListItems ? "" : ".") + (PropertyInfo != null ? PropertyInfo.Name : FieldInfo.Name);
                case PropertyRouteType.Mixin:
                    return Parent.ToString() + "[{0}]".FormatWith(type.Name);
                case PropertyRouteType.MListItems:
                    return Parent.ToString() + "/";
                case PropertyRouteType.LiteEntity:
                    return Parent.ToString() + ".Entity";
            }
            throw new InvalidOperationException();
        }

        public string PropertyString()
        {
            switch (PropertyRouteType)
            {
                case PropertyRouteType.Root:
                    throw new InvalidOperationException("Root has no PropertyString");
                case PropertyRouteType.FieldOrProperty:
                    switch (Parent.PropertyRouteType)
                    {
                        case PropertyRouteType.Root: return (PropertyInfo != null ? PropertyInfo.Name : FieldInfo.Name);
                        case PropertyRouteType.FieldOrProperty:
                        case PropertyRouteType.Mixin:
                            return Parent.PropertyString() + "." + (PropertyInfo != null ? PropertyInfo.Name : FieldInfo.Name);
                        case PropertyRouteType.MListItems: return Parent.PropertyString() + (PropertyInfo != null ? PropertyInfo.Name : FieldInfo.Name);
                        default: throw new InvalidOperationException();
                    }

                case PropertyRouteType.Mixin:
                    return "[{0}]".FormatWith(type.Name);
                case PropertyRouteType.MListItems:
                    return Parent.PropertyString() + "/";
                case PropertyRouteType.LiteEntity:
                    return Parent.ToString() + ".Entity";
            }
            throw new InvalidOperationException();
        }

        public static PropertyRoute Parse(string fullToString)
        {
            var typeParentheses = fullToString.Before('.');

            if (!typeParentheses.StartsWith("(") || !(typeParentheses.EndsWith(")") || typeParentheses.EndsWith("]")))
                throw new FormatException("fullToString should start with the type between parentheses");

            var startType = typeParentheses.IndexOf('(') + 1;
            var cleanType = typeParentheses.Substring(startType, typeParentheses.IndexOf(')') - startType);

            var type = TypeEntity.TryGetType(cleanType);

            if (type == null)
                throw new FormatException("Type {0} is not recognized".FormatWith(typeParentheses));

            var propertyRoute = fullToString.After(".");
            var startMixin = typeParentheses.IndexOf("[");
            if (startMixin > 0)
                propertyRoute = "{0}.{1}".FormatWith(typeParentheses.Substring(startMixin), propertyRoute);

            return Parse(type, propertyRoute);
        }

        public bool IsId()
        {
            return this.PropertyInfo != null && ReflectionTools.PropertyEquals(this.PropertyInfo, piId);
        }

        public static PropertyRoute Parse(Type rootType, string propertyString)
        {
            PropertyRoute result = PropertyRoute.Root(rootType);

            foreach (var part in propertyString.Replace("/", ".Item.").TrimEnd('.').Split('.'))
            {
                result = result.Add(part);
            }

            return result;
        }

        public static void SetFindImplementationsCallback(Func<PropertyRoute, Implementations> findImplementations)
        {
            FindImplementations = findImplementations;
        }

        static Func<PropertyRoute, Implementations> FindImplementations;

        public Implementations? TryGetImplementations()
        {
            if (this.Type.CleanType().IsIEntity() && PropertyRouteType != Entities.PropertyRouteType.Root)
                return GetImplementations();

            return null;
        }

        public Implementations GetImplementations()
        {
            if (FindImplementations == null)
                throw new InvalidOperationException("PropertyRoute.FindImplementations not set");

            return FindImplementations(this);
        }

        public static void SetIsAllowedCallback(Func<PropertyRoute, string> isAllowed)
        {
            IsAllowedCallback = isAllowed;
        }

        static Func<PropertyRoute, string> IsAllowedCallback;

        public string IsAllowed()
        {
            if (IsAllowedCallback != null)
                return IsAllowedCallback(this);

            return null;
        }

        static PropertyInfo piId = ReflectionTools.GetPropertyInfo((Entity a) => a.Id);


        public static List<PropertyRoute> GenerateRoutes(Type type, bool includeIgnored = true)
        {
            PropertyRoute root = PropertyRoute.Root(type);
            List<PropertyRoute> result = new List<PropertyRoute>();

            if (type.IsEntity())
                result.Add(root.Add(piId));

            foreach (PropertyInfo pi in Reflector.PublicInstancePropertiesInOrder(type))
            {
                PropertyRoute route = root.Add(pi);
                result.Add(route);

                if (Reflector.IsEmbeddedEntity(pi.PropertyType))
                    result.AddRange(GenerateEmbeddedProperties(route, includeIgnored));

                if (Reflector.IsMList(pi.PropertyType))
                {
                    Type colType = pi.PropertyType.ElementType();
                    if (Reflector.IsEmbeddedEntity(colType))
                        result.AddRange(GenerateEmbeddedProperties(route.Add("Item"), includeIgnored));
                }
            }

            foreach (var t in MixinDeclarations.GetMixinDeclarations(type))
            {
                result.AddRange(GenerateEmbeddedProperties(root.Add(t), includeIgnored));
            }

            return result;
        }

        static List<PropertyRoute> GenerateEmbeddedProperties(PropertyRoute embeddedProperty, bool includeIgnored)
        {
            List<PropertyRoute> result = new List<PropertyRoute>();
            foreach (var pi in Reflector.PublicInstancePropertiesInOrder(embeddedProperty.Type))
            {
                if (includeIgnored || !pi.HasAttribute<IgnoreAttribute>())
                {
                    PropertyRoute route = embeddedProperty.Add(pi);
                    result.AddRange(route);

                    if (Reflector.IsEmbeddedEntity(pi.PropertyType))
                        result.AddRange(GenerateEmbeddedProperties(route, includeIgnored));

                    if (Reflector.IsMList(pi.PropertyType))
                    {
                        Type colType = pi.PropertyType.ElementType();
                        if (Reflector.IsEmbeddedEntity(colType))
                            result.AddRange(GenerateEmbeddedProperties(route.Add("Item"), includeIgnored));
                    }
                }
            }

            return result;
        }

        public bool Equals(PropertyRoute other)
        {
            if (other.PropertyRouteType != this.PropertyRouteType)
                return false;

            if (Type != other.Type)
                return false;

            if (!FieldsEquals(other))
                return false;

            if (!PropertyEquals(other))
                return false;

            return object.Equals(Parent, other.Parent);
        }

        private bool FieldsEquals(PropertyRoute other)
        {
            if (FieldInfo == null)
                return other.FieldInfo == null;

            return other.FieldInfo != null && ReflectionTools.FieldEquals(FieldInfo, other.FieldInfo);
        }

        private bool PropertyEquals(PropertyRoute other)
        {
            if (PropertyInfo == null)
                return other.PropertyInfo == null;

            return other.PropertyInfo != null && ReflectionTools.PropertyEquals(PropertyInfo, other.PropertyInfo);
        }

        public override int GetHashCode()
        {
            return this.RootType.GetHashCode() ^ (this.PropertyRouteType == Entities.PropertyRouteType.Root ? 0 : this.PropertyString().GetHashCode());
        }

        public override bool Equals(object obj)
        {
            PropertyRoute other = obj as PropertyRoute;

            if (obj == null)
                return false;

            return Equals(other);
        }

        public PropertyRoute SimplifyToProperty()
        {
            switch (PropertyRouteType)
            {
                case PropertyRouteType.FieldOrProperty: return this;
                case PropertyRouteType.LiteEntity:
                case PropertyRouteType.MListItems: return this.Parent.SimplifyToProperty();
                default:
                    throw new InvalidOperationException("PropertyRoute of type {0} not expected".FormatWith(PropertyRouteType));
            }
        }

        public PropertyRoute SimplifyToPropertyOrRoot()
        {
            switch (PropertyRouteType)
            {
                case PropertyRouteType.Root:
                case PropertyRouteType.FieldOrProperty: return this;
                case PropertyRouteType.LiteEntity:
                case PropertyRouteType.MListItems:
                case PropertyRouteType.Mixin: return this.Parent.SimplifyToPropertyOrRoot();
                default:
                    throw new InvalidOperationException("PropertyRoute of type {0} not expected".FormatWith(PropertyRouteType));
            }
        }

        private PropertyRoute(SerializationInfo info, StreamingContext ctxt)
        {
            string rootName = info.GetString("rootType");

            Type root = Type.GetType(rootName);

            string route = info.GetString("property");

            if (route == null)
                this.SetRootType(root);
            else
            {
                string before = route.TryBeforeLast(".");

                if (before != null)
                {
                    var parent = Parse(root, before);

                    SetParentAndProperty(parent, parent.GetMember(route.AfterLast('.')));
                }
                else
                {
                    var parent = Root(root);

                    SetParentAndProperty(parent, parent.GetMember(route));
                }
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("rootType", RootType.AssemblyQualifiedName);

            string property =
                PropertyRouteType == Entities.PropertyRouteType.Root ? null :
                (PropertyRouteType == Entities.PropertyRouteType.LiteEntity ? this.Parent.PropertyString() + ".Entity" :
                this.PropertyString()).Replace("/", ".Item.").TrimEnd('.');

            info.AddValue("property", property);
        }

        public PropertyRoute GetMListItemsRoute()
        {
            for (var r = this; r != null; r = r.Parent)
            {
                if (r.PropertyRouteType == PropertyRouteType.MListItems)
                    return r;
            }

            return null;
        }

        /// <typeparam name="T">The RootType or the type of MListElement</typeparam>
        /// <typeparam name="R">Result type</typeparam>
        /// <returns></returns>
        public Expression<Func<T, R>> GetLambdaExpression<T, R>(bool safeNullAccess, PropertyRoute skipBefore = null)
        {
            ParameterExpression pe = Expression.Parameter(typeof(T));
            Expression exp = null;

            var steps = this.Follow(a => a.Parent).Reverse();
            if (skipBefore != null)
                steps = steps.SkipWhile(a => !a.Equals(skipBefore));

            foreach (var p in steps)
            {
                switch (p.PropertyRouteType)
                {
                    case PropertyRouteType.Root:
                    case PropertyRouteType.MListItems:
                        exp = pe.TryConvert(p.Type);
                        break;
                    case PropertyRouteType.FieldOrProperty:
                        var memberExp = Expression.MakeMemberAccess(exp, (MemberInfo)p.PropertyInfo ?? p.FieldInfo);
                        if (exp.Type.IsEmbeddedEntity() && safeNullAccess)
                            exp = Expression.Condition(
                                Expression.Equal(exp, Expression.Constant(null, exp.Type)),
                                Expression.Constant(null, memberExp.Type.Nullify()),
                                memberExp.Nullify());
                        else
                            exp = memberExp;
                        break;
                    case PropertyRouteType.Mixin:
                        exp = Expression.Call(exp, MixinDeclarations.miMixin.MakeGenericMethod(p.Type));
                        break;
                    case PropertyRouteType.LiteEntity:
                        exp = Expression.Property(exp, "Entity");
                        break;
                    default:
                        throw new InvalidOperationException("Unexpected {0}".FormatWith(p.PropertyRouteType));
                }
            }

            var selector = Expression.Lambda<Func<T, R>>(exp, pe);
            return selector;
        }


        public bool IsToStringProperty()
        {
            return PropertyRouteType == PropertyRouteType.FieldOrProperty &&
                Parent.PropertyRouteType == PropertyRouteType.Root &&
                PropertyInfo != null && ReflectionTools.PropertyEquals(PropertyInfo, piToStringProperty);
        }

        static readonly PropertyInfo piToStringProperty = ReflectionTools.GetPropertyInfo((Entity ident) => ident.ToStringProperty);

        /// <returns>null means impossible to determine because lack of NotifyChildPropertyAttribute</returns>
        public bool? MatchesProperty(ModifiableEntity entity, PropertyInfo pi)
        {
            if (this.PropertyRouteType != PropertyRouteType.FieldOrProperty)
                return false;

            if (!ReflectionTools.PropertyEquals(PropertyInfo, pi))
                return false;

            return this.Parent.MatchesEntity(entity);
        }

        private bool? MatchesEntity(ModifiableEntity entity)
        {
            if (this.Type != entity.GetType())
                return false;

            if (this.PropertyRouteType == PropertyRouteType.Root)
                return true;


            switch (this.PropertyRouteType)
            {
                case PropertyRouteType.Root: return true;

                case PropertyRouteType.FieldOrProperty:
                    {
                        var parentEntity = entity.GetParentEntity();

                        var result = this.Parent.MatchesEntity(parentEntity);
                        if (result != true)
                            return result;

                        return this.PropertyInfo.GetValue(parentEntity) == entity;
                    }

                case PropertyRouteType.Mixin:
                    {
                        var mixin = (MixinEntity)entity;
                        var mainEntity = mixin.MainEntity;

                        var result = this.Parent.MatchesEntity(mainEntity);
                        if (result != true)
                            return result;

                        return mainEntity.Mixins.Contains(mixin);
                    }
                case PropertyRouteType.MListItems:
                    {
                        var parentEntity = entity.GetParentEntity();

                        var result = this.Parent.Parent.MatchesEntity(parentEntity);
                        if (result != true)
                            return result;

                        var list = (IList)this.PropertyInfo.GetValue(parentEntity);

                        return list != null && list.Contains(entity);
                    }

                case PropertyRouteType.LiteEntity:
                default:
                    throw new NotImplementedException();
            }
        }
    }

    public interface IImplementationsFinder
    {
        Implementations FindImplementations(PropertyRoute route);
    }

    public enum PropertyRouteType
    {
        Root,
        FieldOrProperty,
        Mixin,
        LiteEntity,
        MListItems,
    }
}

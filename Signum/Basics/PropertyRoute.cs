using Signum.Engine.Maps;
using Signum.Utilities.DataStructures;
using Signum.Utilities.Reflection;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Signum.Basics;

public class PropertyRoute : IEquatable<PropertyRoute>
{
    Type? type;
    public PropertyRouteType PropertyRouteType { get; private set; }
    public FieldInfo? FieldInfo { get; private set; }
    public PropertyInfo? PropertyInfo { get; private set; }
    public PropertyRoute? Parent { get; private set; }

    public MemberInfo[] Members
    {
        get
        {
            return this.Follow(a => a.Parent).Select(a =>
                a.PropertyRouteType == PropertyRouteType.Mixin ? a.type! :
                a.FieldInfo ?? (MemberInfo)a.PropertyInfo!).Reverse().Skip(1).ToArray();
        }
    }

    public PropertyInfo[] Properties
    {
        get { return this.Follow(a => a.Parent).Select(a => a.PropertyInfo!).Reverse().Skip(1).ToArray(); }
    }

    [ForceEagerEvaluation]
    public static PropertyRoute Construct<T, S>(Expression<Func<T, S>> lambda, bool avoidLastCasting = false)
        where T : IRootEntity
    {
        return Root(typeof(T)).Continue(lambda, avoidLastCasting);
    }


    public PropertyRoute Continue<T, S>(Expression<Func<T, S>> lambda, bool avoidLastCasting = false)
    {
        if (typeof(T) != this.Type)
            throw new InvalidOperationException("Type mismatch between {0} and {1}".FormatWith(typeof(T).TypeName(), this.Type.TypeName()));

        var list = avoidLastCasting && lambda.Body is UnaryExpression u && u.NodeType == ExpressionType.Convert ?
            Reflector.GetMemberListUntyped(Expression.Lambda(u.Operand, lambda.Parameters)) :
            Reflector.GetMemberList(lambda);

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

    public PropertyRoute AddMany(string fieldOrProperties)
    {
        var result = this;
        foreach (var field in fieldOrProperties.Split('.'))
            result = result.Add(field);
        return result;
    }

    public PropertyRoute Add(string fieldOrProperty)
    {
        return Add(GetMember(fieldOrProperty));
    }

    MemberInfo GetMember(string fieldOrProperty)
    {
        if (fieldOrProperty.Contains("."))
            throw new ArgumentException($"{nameof(fieldOrProperty)} contains '.'");

        if (fieldOrProperty.StartsWith("["))
        {
            var mixinName = ExtractMixin(fieldOrProperty);

            return MixinDeclarations.GetMixinDeclarations(Type).FirstOrDefault(t => t.Name == mixinName) ??
                throw new InvalidOperationException("{0}{1} does not exist".FormatWith(this, fieldOrProperty));
        }
        else
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            return (MemberInfo?)Type.GetProperty(fieldOrProperty, flags, null, null, IsCollection() ? new[] { typeof(int) } : new Type[0], null) ??
                (MemberInfo?)Type.GetField(fieldOrProperty, flags) ??
                throw new InvalidOperationException("{0}.{1} does not exist".FormatWith(this, fieldOrProperty));
        }
    }

    private bool IsCollection()
    {
        return Type.ElementType() != null && Type != typeof(string);
    }

    static string? ExtractMixin(string fieldOrProperty)
    {
        Match match = Regex.Match(fieldOrProperty, @"^\[(?<type>.*)\]$");

        if (!match.Success)
            return null;

        return match.Groups["type"].Value;
    }


    ConcurrentDictionary<(PropertyRoute pr, MemberInfo member), PropertyRoute> addCache = new();
    public PropertyRoute Add(MemberInfo member)
    {
        return addCache.GetOrAdd((this, member), pair => AddImp(pair.pr, pair.member));
    }

    static PropertyRoute AddImp(PropertyRoute pr, MemberInfo member)
    {
        using (HeavyProfiler.LogNoStackTrace("PR.Add", () => member.Name))
        {
            if (member is MethodInfo && ((MethodInfo)member).IsInstantiationOf(MixinDeclarations.miMixin))
                member = ((MethodInfo)member).GetGenericArguments()[0];

            if (pr.Type.IsIEntity() && pr.PropertyRouteType != PropertyRouteType.Root)
            {
                Implementations imp = pr.GetImplementations();

                Type? only;
                if (imp.IsByAll || (only = imp.Types.Only()) == null)
                    throw new InvalidOperationException("Attempt to make a PropertyRoute on a {0}. Cast first".FormatWith(imp));

                return new PropertyRoute(Root(only), member);
            }

            return new PropertyRoute(pr, member);
        }
    }

    PropertyRoute(PropertyRoute parent, MemberInfo fieldOrProperty)
    {
        if (fieldOrProperty == null)
            throw new ArgumentNullException(nameof(fieldOrProperty));

        this.Parent = parent ?? throw new ArgumentNullException(nameof(parent));

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
        else if (typeof(ModifiableEntity).IsAssignableFrom(parent.Type) && fieldOrProperty is Type)
        {
            MixinDeclarations.AssertDeclared(parent.Type!, (Type)fieldOrProperty);

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


    static readonly FieldInfo fiToStr = ReflectionTools.GetFieldInfo((Entity e) => e.ToStr);


    static ConcurrentDictionary<Type, PropertyRoute> rootCache = new();
    public static PropertyRoute Root(Type rootEntity)
    {
        return rootCache.GetOrAdd(rootEntity, type => new PropertyRoute(type));
    }

    PropertyRoute(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (!typeof(IRootEntity).IsAssignableFrom(type))
            throw new ArgumentException($"Type {type.FullName} must implement IPropertyRouteRoot");

        if (type.IsAbstract)
            throw new ArgumentException($"Type {type.FullName} must be non-abstract");

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
            var root = this;
            while (root.PropertyRouteType != PropertyRouteType.Root)
                root = root.Parent!;

            return root.type!;
        }
    }

    string? cachedToString;
    public override string ToString() => cachedToString ??= CalculateToString();
    string CalculateToString() 
    {
        switch (PropertyRouteType)
        {
            case PropertyRouteType.Root:
                return "({0})".FormatWith(typeof(Entity).IsAssignableFrom(type!) ? TypeLogic.GetCleanName(type!) : type!.Name);
            case PropertyRouteType.FieldOrProperty:
                return Parent!.ToString() + (Parent!.PropertyRouteType == PropertyRouteType.MListItems ? "" : ".") + (PropertyInfo != null ? PropertyInfo.Name : FieldInfo!.Name);
            case PropertyRouteType.Mixin:
                return Parent!.ToString() + "[{0}]".FormatWith(type!.Name);
            case PropertyRouteType.MListItems:
                return Parent!.ToString() + "/";
            case PropertyRouteType.LiteEntity:
                return Parent!.ToString() + ".Entity";
        }
        throw new InvalidOperationException();
    }

    string? cachedPropertyString; 

    public string PropertyString() => cachedPropertyString??= CalculatePropertyString();
    string CalculatePropertyString()
    {
        switch (PropertyRouteType)
        {
            case PropertyRouteType.Root:
                throw new InvalidOperationException("Root has no PropertyString");
            case PropertyRouteType.FieldOrProperty:
                switch (Parent!.PropertyRouteType)
                {
                    case PropertyRouteType.Root: return (PropertyInfo != null ? PropertyInfo.Name : FieldInfo!.Name);
                    case PropertyRouteType.FieldOrProperty:
                    case PropertyRouteType.Mixin:
                        return Parent!.PropertyString() + "." + (PropertyInfo != null ? PropertyInfo.Name : FieldInfo!.Name);
                    case PropertyRouteType.MListItems: return Parent!.PropertyString() + (PropertyInfo != null ? PropertyInfo.Name : FieldInfo!.Name);
                    default: throw new InvalidOperationException();
                }

            case PropertyRouteType.Mixin:
                return (Parent!.PropertyRouteType == PropertyRouteType.Root ? "" : Parent!.PropertyString()) + "[{0}]".FormatWith(type!.Name);
            case PropertyRouteType.MListItems:
                return Parent!.PropertyString() + "/";
            case PropertyRouteType.LiteEntity:
                return Parent!.ToString() + ".Entity";
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

        var type = TypeLogic.TryGetType(cleanType);

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
        Sequence<string> splitMixin(string text)
        {
            if (text.Length == 0)
                return new Sequence<string>();

            if (text.Contains("["))
                return new Sequence<string>
                {
                    splitMixin(text.Before("[")),
                    "[" + text.Between("[", "]") + "]",
                    splitMixin(text.After("]")),
                };

            return new Sequence<string> { text };
        }

        Sequence<string> splitDot(string text)
        {
            if (text.Contains("."))
                return new Sequence<string>
                {
                    splitMixin(text.Before(".")),
                    splitDot(text.After("."))
                };

            return splitMixin(text);
        }

        Sequence<string> splitIndexer(string text)
        {
            if (text.Contains("/"))
                return new Sequence<string>
                {
                    splitDot(text.Before("/")),
                    "Item",
                    splitIndexer(text.After("/")),
                };

            return splitDot(text);
        }



        PropertyRoute result = PropertyRoute.Root(rootType);
        var parts = splitIndexer(propertyString);
        foreach (var part in parts)
        {
            result = result.Add(part);
        }

        return result;
    }

    public static void SetFindImplementationsCallback(Func<PropertyRoute, Implementations> findImplementations)
    {
        FindImplementations = findImplementations;
    }

    static Func<PropertyRoute, Implementations>? FindImplementations;

    public Implementations? TryGetImplementations()
    {
        if (this.Type.CleanType().IsIEntity() && PropertyRouteType != PropertyRouteType.Root)
            return GetImplementations();

        return null;
    }

    public Implementations GetImplementations()
    {
        if (FindImplementations == null)
            throw new InvalidOperationException("PropertyRoute.FindImplementations not set");

        return FindImplementations(this);
    }

    public static void SetIsAllowedCallback(Func<PropertyRoute, string?> isAllowed)
    {
        IsAllowedCallback = isAllowed;
    }

    static Func<PropertyRoute, string?>? IsAllowedCallback;

    public string? IsAllowed()
    {
        if (IsAllowedCallback != null)
            return IsAllowedCallback(this);

        return null;
    }

    static readonly PropertyInfo piId = ReflectionTools.GetPropertyInfo((Entity a) => a.Id);


    public static List<PropertyRoute> GenerateRoutes(Type type, bool includeIgnored = true, bool includeMixinItself = false, bool includeMListElements = false)
    {
        PropertyRoute root = PropertyRoute.Root(type);
        List<PropertyRoute> result = new List<PropertyRoute>();

        if (type.IsEntity())
            result.Add(root.Add(piId));

        foreach (PropertyInfo pi in Reflector.PublicInstancePropertiesInOrder(type))
        {
            if (includeIgnored || !pi.HasAttribute<IgnoreAttribute>())
            {
                PropertyRoute route = root.Add(pi);
                result.Add(route);

                if (Reflector.IsEmbeddedEntity(pi.PropertyType))
                    result.AddRange(GenerateEmbeddedProperties(route, includeIgnored, includeMListElements, includeMixinItself));

                if (Reflector.IsMList(pi.PropertyType))
                {
                    PropertyRoute itemRoute = route.Add("Item");

                    if (includeMListElements)
                        result.Add(itemRoute);

                    Type colType = pi.PropertyType.ElementType()!;
                    if (Reflector.IsEmbeddedEntity(colType))
                        result.AddRange(GenerateEmbeddedProperties(itemRoute, includeIgnored, includeMListElements, includeMixinItself));
                }
            }
        }

        foreach (var t in MixinDeclarations.GetMixinDeclarations(type))
        {
            var mixinRoute = root.Add(t);

            if (includeMixinItself)
                result.Add(mixinRoute);

            result.AddRange(GenerateEmbeddedProperties(mixinRoute, includeIgnored, includeMListElements, includeMixinItself));
        }

        return result;
    }

    static List<PropertyRoute> GenerateEmbeddedProperties(PropertyRoute embeddedRoute, bool includeIgnored, bool includeMListElements, bool includeMixinItself)
    {
        List<PropertyRoute> result = new List<PropertyRoute>();
        foreach (var pi in Reflector.PublicInstancePropertiesInOrder(embeddedRoute.Type))
        {
            if (includeIgnored || !pi.HasAttribute<IgnoreAttribute>())
            {
                PropertyRoute route = embeddedRoute.Add(pi);
                result.AddRange(route);

                if (Reflector.IsEmbeddedEntity(pi.PropertyType))
                    result.AddRange(GenerateEmbeddedProperties(route, includeIgnored, includeMListElements, includeMixinItself));

                if (Reflector.IsMList(pi.PropertyType))
                {
                    PropertyRoute itemRoute = route.Add("Item");

                    if (includeMListElements)
                        result.Add(itemRoute);

                    Type colType = pi.PropertyType.ElementType()!;
                    if (Reflector.IsEmbeddedEntity(colType))
                        result.AddRange(GenerateEmbeddedProperties(itemRoute, includeIgnored, includeMListElements, includeMixinItself));
                }
            }
        }

        foreach (var t in MixinDeclarations.GetMixinDeclarations(embeddedRoute.Type))
        {
            var mixinRoute = embeddedRoute.Add(t);

            if (includeMixinItself)
                result.Add(mixinRoute);

            result.AddRange(GenerateEmbeddedProperties(mixinRoute, includeIgnored, includeMListElements, includeMixinItself));
        }

        return result;
    }

    public override int GetHashCode()
    {
        return this.RootType.GetHashCode() ^ (this.PropertyRouteType == PropertyRouteType.Root ? 0 : this.PropertyString().GetHashCode());
    }

    public override bool Equals(object? obj) => obj is PropertyRoute pr && Equals(pr);
    public bool Equals(PropertyRoute? other)
    {

        if (other == null)
            return false;

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


    public PropertyRoute SimplifyToProperty()
    {
        switch (PropertyRouteType)
        {
            case PropertyRouteType.FieldOrProperty: return this;
            case PropertyRouteType.LiteEntity:
            case PropertyRouteType.MListItems: return this.Parent!.SimplifyToProperty();
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
            case PropertyRouteType.Mixin: return this.Parent!.SimplifyToPropertyOrRoot();
            default:
                throw new InvalidOperationException("PropertyRoute of type {0} not expected".FormatWith(PropertyRouteType));
        }
    }

    public PropertyRoute? GetMListItemsRoute()
    {
        for (PropertyRoute? r = this; r != null; r = r.Parent)
        {
            if (r.PropertyRouteType == PropertyRouteType.MListItems)
                return r;
        }

        return null;
    }

    /// <typeparam name="T">The RootType or the type of MListElement</typeparam>
    /// <typeparam name="R">Result type</typeparam>
    /// <returns></returns>
    public Expression<Func<T, R>> GetLambdaExpression<T, R>(bool safeNullAccess, PropertyRoute? skipBefore = null)
    {
        ParameterExpression pe = Expression.Parameter(typeof(T), "p");
        Expression exp = GetBody(safeNullAccess, skipBefore, pe, typeof(R));

        var selector = Expression.Lambda<Func<T, R>>(exp, pe);
        return selector;
    }

    /// <typeparam name="T">The RootType or the type of MListElement</typeparam>
    /// <typeparam name="R">Result type</typeparam>
    /// <returns></returns>
    public LambdaExpression GetLambdaExpression(Type fromType, Type resultType, bool safeNullAccess, PropertyRoute? skipBefore = null)
    {
        ParameterExpression pe = Expression.Parameter(fromType, "p");
        Expression exp = GetBody(safeNullAccess, skipBefore, pe, resultType);

        var selector = Expression.Lambda(exp, pe);
        return selector;
    }

    static bool IsPotentiallyNull(Expression exp)
    {
        return exp!.Type.IsEmbeddedEntity() || exp is ConditionalExpression /*Conditional Embedded with Mixin*/;
    }

    Expression GetBody(bool safeNullAccess, PropertyRoute? skipBefore, ParameterExpression pe, Type resultType)
    {
        Expression exp = null!;

        var steps = this.Follow(a => a.Parent).Reverse();
        if (skipBefore != null)
        {
            if (!steps.Contains(skipBefore))
                throw new InvalidOperationException($"{skipBefore} is not a prefix of {this}");
            steps = steps.SkipWhile(a => !a.Equals(skipBefore!));
        }

        foreach (var p in steps)
        {
            switch (p.PropertyRouteType)
            {
                case PropertyRouteType.Root:
                case PropertyRouteType.MListItems:
                    exp = pe.TryConvert(p.Type);
                    break;
                case PropertyRouteType.FieldOrProperty:
                    var memberExp = Expression.MakeMemberAccess(exp, (MemberInfo?)p.PropertyInfo ?? p.FieldInfo!);
                    if (IsPotentiallyNull(exp) && safeNullAccess)
                        exp = Expression.Condition(
                            Expression.Equal(exp, Expression.Constant(null, exp!.Type)),
                            Expression.Constant(null, memberExp.Type.Nullify()),
                            memberExp.Nullify());
                    else
                        exp = memberExp;
                    break;
                case PropertyRouteType.Mixin:
                    if (IsPotentiallyNull(exp) && safeNullAccess)
                        exp = Expression.Condition(
                            Expression.Equal(exp!, Expression.Constant(null, exp.Type)),
                            Expression.Constant(null, p.Type),
                            Expression.Call(exp, MixinDeclarations.miMixin.MakeGenericMethod(p.Type)));
                    else
                        exp = Expression.Call(exp, MixinDeclarations.miMixin.MakeGenericMethod(p.Type));
                    break;
                case PropertyRouteType.LiteEntity:
                    if (exp!.Type.IsEmbeddedEntity() && safeNullAccess)
                        exp = Expression.Condition(
                            Expression.Equal(exp, Expression.Constant(null, exp!.Type)),
                            Expression.Constant(null, exp.Type.CleanType()),
                            Expression.Property(exp!, "Entity"));
                    else
                        exp = Expression.Property(exp!, "Entity");
                    break;
                default:
                    throw new InvalidOperationException("Unexpected {0}".FormatWith(p.PropertyRouteType));
            }
        }

        if (exp.Type != resultType)
            exp = Expression.Convert(exp, resultType);
        return exp;
    }

    public bool IsToStringProperty()
    {
        return PropertyRouteType == PropertyRouteType.FieldOrProperty &&
            Parent!.PropertyRouteType == PropertyRouteType.Root &&
            PropertyInfo != null && ReflectionTools.PropertyEquals(PropertyInfo, piToStringProperty);
    }

    static readonly PropertyInfo piToStringProperty = ReflectionTools.GetPropertyInfo((Entity ident) => ident.ToStringProperty);

    public bool? MatchesEntity(ModifiableEntity entity)
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
                    var parentEntity = entity.TryGetParentEntity<ModifiableEntity>();

                    if (parentEntity == null)
                        return null;

                    var result = this.Parent!.MatchesEntity(parentEntity);
                    if (result != true)
                        return result;

                    return this.PropertyInfo!.GetValue(parentEntity) == entity;
                }

            case PropertyRouteType.Mixin:
                {
                    var mixin = (MixinEntity)entity;
                    var mainEntity = mixin.MainEntity;

                    var result = this.Parent!.MatchesEntity(mainEntity);
                    if (result != true)
                        return result;

                    return mainEntity.Mixins.Contains(mixin);
                }
            case PropertyRouteType.MListItems:
                {
                    var parentEntity = entity.TryGetParentEntity<ModifiableEntity>();

                    if (parentEntity == null)
                        return null;

                    var result = this.Parent!.Parent!.MatchesEntity(parentEntity);
                    if (result != true)
                        return result;

                    var list = (IList?)this.PropertyInfo!.GetValue(parentEntity);

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

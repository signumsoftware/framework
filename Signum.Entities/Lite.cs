using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Diagnostics;
using Signum.Entities.Basics;
using System.Collections.Concurrent;
using System.ComponentModel;
using Signum.Entities.Internal;

namespace Signum.Entities;

#pragma warning disable IDE1006
public interface Lite<out T> : IComparable, IComparable<Lite<Entity>>
#pragma warning restore IDE1006
    where T : class, IEntity
{
    T Entity { get; }
    T? EntityOrNull { get; }

    PrimaryKey Id { get; }
    bool IsNew { get; }
    PrimaryKey? IdOrNull { get; }
    Type EntityType { get; }

    void ClearEntity();
    void SetEntity(Entity ei);
    void SetToString(string toStr);
    PrimaryKey RefreshId();

    string Key();
    string KeyLong();

    Lite<T> Clone();
}

public static class Lite
{
    public static Type BaseImplementationType = typeof(LiteImp);

    static GenericInvoker<Func<PrimaryKey, string?, Lite<Entity>>> giNewLite =
        new((id, str) => new LiteImp<Entity>(id, str));

    static GenericInvoker<Func<Entity, string?, Lite<Entity>>> giNewLiteFat =
        new((entity, str) => new LiteImp<Entity>(entity, str));

    public static Type Generate(Type identificableType)
    {
        return typeof(Lite<>).MakeGenericType(identificableType);
    }

    public static Type? Extract(Type liteType)
    {
        if (liteType.IsInstantiationOf(typeof(Lite<>)) || typeof(LiteImp).IsAssignableFrom(liteType))
            return liteType.GetGenericArguments()[0];

        return null;
    }

    public static readonly Regex ParseRegex = new Regex(@"(?<type>[^;]+);(?<id>[\d\w-]+)(;(?<toStr>.+))?");

    public static Lite<Entity> Parse(string liteKey)
    {
        string? error = TryParseLite(liteKey, out Lite<Entity>? result);
        if (error == null)
            return result!;
        else
            throw new FormatException(error);
    }

    public static Lite<T> Parse<T>(string liteKey) where T : class, IEntity
    {
        return (Lite<T>)Lite.Parse(liteKey);
    }

    public static string? TryParseLite(string liteKey, out Lite<Entity>? result)
    {
        result = null;
        if (string.IsNullOrEmpty(liteKey))
            return null;

        Match match = ParseRegex.Match(liteKey);
        if (!match.Success)
            return ValidationMessage.InvalidFormat.NiceToString();

        Type? type = TypeEntity.TryGetType(match.Groups["type"].Value);
        if (type == null)
            return LiteMessage.Type0NotFound.NiceToString().FormatWith(match.Groups["type"].Value);

        if (!PrimaryKey.TryParse(match.Groups["id"].Value, type, out PrimaryKey id))
            return LiteMessage.IdNotValid.NiceToString();

        string? toStr = match.Groups["toStr"].Value.DefaultText(null!); //maybe null

        result = giNewLite.GetInvoker(type)(id, toStr);
        return null;
    }

    public static string? TryParse<T>(string liteKey, out Lite<T>? lite) where T : class, IEntity
    {
        var result = Lite.TryParseLite(liteKey, out Lite<Entity>? untypedLite);
        lite = (Lite<T>?)untypedLite;
        return result;
    }

    public static Lite<Entity> Create(Type type, PrimaryKey id)
    {
        return giNewLite.GetInvoker(type)(id, null);
    }

    public static Lite<Entity> Create(Type type, PrimaryKey id, string? toStr)
    {
        return giNewLite.GetInvoker(type)(id, toStr);
    }

    [DebuggerStepThrough]
    public static Lite<T> ToLite<T>(this T entity)
      where T : class, IEntity
    {
        if (entity.IdOrNull == null)
            throw new InvalidOperationException("ToLite is not allowed for new entities, use ToLiteFat instead");

        return (Lite<T>)giNewLite.GetInvoker(entity.GetType())(entity.Id, entity.ToString());
    }

    [DebuggerStepThrough]
    public static Lite<T> ToLite<T>(this T entity, string? toStr)
        where T : class, IEntity
    {
        if (entity.IsNew)
            throw new InvalidOperationException("ToLite is not allowed for new entities, use ToLiteFat instead");

        return (Lite<T>)giNewLite.GetInvoker(entity.GetType())(entity.Id, toStr);
    }

    [DebuggerStepThrough]
    public static Lite<T> ToLiteFat<T>(this T entity)
     where T : class, IEntity
    {
        return (Lite<T>)giNewLiteFat.GetInvoker(entity.GetType())((Entity)(IEntity)entity, entity.ToString());
    }

    [DebuggerStepThrough]
    public static Lite<T> ToLiteFat<T>(this T entity, string? toStr)
      where T : class, IEntity
    {
        return (Lite<T>)giNewLiteFat.GetInvoker(entity.GetType())((Entity)(IEntity)entity, toStr ?? entity.ToString());
    }

    [DebuggerStepThrough]
    public static Lite<T> ToLite<T>(this T entity, bool fat) where T : class, IEntity
    {
        if (fat)
            return entity.ToLiteFat();
        else
            return entity.ToLite();
    }

    [DebuggerStepThrough]
    public static Lite<T> ToLite<T>(this T entity, bool fat, string toStr) where T : class, IEntity
    {
        if (fat)
            return entity.ToLiteFat(toStr);
        else
            return entity.ToLite(toStr);
    }

    class IsExpander : IMethodExpander
    {
        public Expression Expand(Expression? instance, Expression[] arguments, MethodInfo mi)
        {
            return Expression.Equal(arguments[0], arguments[1]);
        }
    }


    [MethodExpander(typeof(IsExpander))]
    public static bool Is<T>(this T? entity1, T? entity2)
         where T : class, IEntity
    {
        if (entity1 == null && entity2 == null)
            return true;

        if (entity1 == null || entity2 == null)
            return false;

        if (entity1.GetType() != entity2.GetType())
            return false;

        if (entity1.IdOrNull != null)
            return entity1.Id == entity2.IdOrNull;
        else
            return object.ReferenceEquals(entity1, entity2);
    }

    [MethodExpander(typeof(IsExpander))]
    public static bool Is<T>(this Lite<T>? lite1, Lite<T>? lite2)
      where T : class, IEntity
    {
        if (lite1 == null && lite2 == null)
            return true;

        if (lite1 == null || lite2 == null)
            return false;

        if (lite1.EntityType != lite2.EntityType)
            return false;

        if (lite1.IdOrNull != null)
            return lite1.Id == lite2.IdOrNull;
        else
            return object.ReferenceEquals(lite1.EntityOrNull, lite2.EntityOrNull);
    }

    class IsEntityLiteExpander : IMethodExpander
    {
        static MethodInfo miToLazy = ReflectionTools.GetMethodInfo((TypeEntity type) => type.ToLite()).GetGenericMethodDefinition();

        public Expression Expand(Expression? instance, Expression[] arguments, MethodInfo mi)
        {
            Expression entity = arguments[0];
            Expression lite = arguments[1];

            var evalEntity = ExpressionEvaluator.PartialEval(entity);

            var type = mi.GetGenericArguments()[0];

            var toLite = evalEntity is ConstantExpression c && c.Value == null ?
                (Expression)Expression.Constant(null, typeof(Lite<>).MakeGenericType(type)) :
                (Expression)Expression.Call(null, miToLazy.MakeGenericMethod(type), entity);

            return Expression.Equal(toLite, lite);
        }
    }

    [MethodExpander(typeof(IsEntityLiteExpander))]
    public static bool Is<T>(this T? entity1, Lite<T>? lite2)
         where T : class, IEntity
    {
        if (entity1 == null && lite2 == null)
            return true;

        if (entity1 == null || lite2 == null)
            return false;

        if (entity1.GetType() != lite2.EntityType)
            return false;

        if (entity1.IdOrNull != null)
            return entity1.Id == lite2.IdOrNull;
        else
            return object.ReferenceEquals(entity1, lite2.EntityOrNull);
    }

    class IsLiteEntityExpander : IMethodExpander
    {
        static readonly MethodInfo miToLazy = ReflectionTools.GetMethodInfo((TypeEntity type) => type.ToLite()).GetGenericMethodDefinition();

        public Expression Expand(Expression? instance, Expression[] arguments, MethodInfo mi)
        {
            Expression lite = arguments[0];
            Expression entity = arguments[1];

            var evalEntity = ExpressionEvaluator.PartialEval(entity);

            var type = mi.GetGenericArguments()[0];

            var toLite = evalEntity is ConstantExpression c && c.Value == null ?
                (Expression)Expression.Constant(null, typeof(Lite<>).MakeGenericType(type)) :
                (Expression)Expression.Call(null, miToLazy.MakeGenericMethod(type), entity);

            return Expression.Equal(lite, toLite);
        }
    }

    [MethodExpander(typeof(IsLiteEntityExpander))]
    public static bool Is<T>(this Lite<T>? lite1, T? entity2)
        where T : class, IEntity
    {
        if (lite1 == null && entity2 == null)
            return true;

        if (lite1 == null || entity2 == null)
            return false;

        if (lite1.EntityType != entity2.GetType())
            return false;

        if (lite1.IdOrNull != null)
            return lite1.Id == entity2.IdOrNull;
        else
            return object.ReferenceEquals(lite1.EntityOrNull, entity2);
    }

    public static XDocument EntityDGML(this Entity entity)
    {
        return GraphExplorer.FromRootVirtual(entity).EntityDGML();
    }


    public static bool IsLite(this Type t)
    {
        return typeof(Lite<IEntity>).IsAssignableFrom(t);
    }

    public static Type CleanType(this Type t)
    {
        return Lite.Extract(t) ?? t;
    }


    public static Lite<T> Create<T>(PrimaryKey id) where T : Entity
    {
        return new LiteImp<T>(id, null);
    }

    public static Lite<T> Create<T>(PrimaryKey id, string toStr) where T : Entity
    {
        return new LiteImp<T>(id, toStr);
    }

    static ConcurrentDictionary<Type, ConstructorInfo> ciLiteConstructorId = new ConcurrentDictionary<Type, ConstructorInfo>();
    public static ConstructorInfo LiteConstructorId(Type type)
    {
        return ciLiteConstructorId.GetOrAdd(type, CreateLiteConstructor);
    }

    static ConstructorInfo CreateLiteConstructor(Type t)
    {
        return typeof(LiteImp<>).MakeGenericType(t).GetConstructor(new[] { typeof(PrimaryKey), typeof(string) })!;
    }


    public static NewExpression NewExpression(Type type, Expression id, Expression toString)
    {
        return Expression.New(Lite.LiteConstructorId(type), id.UnNullify(), toString);
    }


    static Lite<T>? ToLiteFatInternal<T>(this T? entity, string? toStr)
        where T : class, IEntity
    {
        if (entity == null)
            return null;

        return entity.ToLiteFat(toStr);
    }

    static MethodInfo miToLiteFatInternal = ReflectionTools.GetMethodInfo(() => ToLiteFatInternal<Entity>(null, null)).GetGenericMethodDefinition();
    public static Expression ToLiteFatInternalExpression(Expression reference, Expression toString)
    {
        return Expression.Call(miToLiteFatInternal.MakeGenericMethod(reference.Type), reference, toString);
    }

    public static Lite<T> ParsePrimaryKey<T>(string id)
        where T : Entity
    {
        return Lite.Create<T>(PrimaryKey.Parse(id, typeof(T)));
    }

    public static Lite<Entity> ParsePrimaryKey(Type type, string id)
    {
        return Lite.Create(type, PrimaryKey.Parse(id, type));
    }
}

public enum LiteMessage
{
    IdNotValid,
    [Description("Invalid Format")]
    InvalidFormat,
    [Description("New")]
    New_G,
    [Description("Type {0} not found")]
    Type0NotFound,
    [Description("Text")]
    ToStr
}

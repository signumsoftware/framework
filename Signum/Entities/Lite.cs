using Signum.Utilities.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.ComponentModel;
using Signum.Entities.Internal;

namespace Signum.Entities;

#pragma warning disable IDE1006
public interface Lite<out T> : IComparable, IComparable<Lite<Entity>>
#pragma warning restore IDE1006
    where T : class, IEntity
{
    /// <summary>
    /// Returns Entity if the lite has been previously loaded, or in database queries, otherwise exception
    /// </summary>
    T Entity { get; }

    /// <summary>
    /// Returns Entity if the lite has been previously loaded, otherwise null
    /// </summary>
    T? EntityOrNull { get; }

    /// <summary>
    /// Returns the Id if the lite is not pointing to a new entity, otherwise exception
    /// </summary>
    PrimaryKey Id { get; }


    /// <summary>
    /// PartitionId to optimize queries
    /// </summary>
    int? PartitionId { get; }

    /// <summary>
    /// Determines whether the lite is pointing to a new Entity. 
    /// </summary>
    bool IsNew { get; }

    /// <summary>
    /// Returns the Id if the lite is not pointing to a new entity, otherwise null
    /// </summary>
    PrimaryKey? IdOrNull { get; }

    /// <summary>
    /// Returns the run-time type of the entity the lite is pointing to. 
    /// </summary>
    Type EntityType { get; }

    /// <summary>
    /// The type of the model (typically typeof(string))
    /// </summary>
    Type ModelType { get;  }

    /// <summary>
    /// The model of the lite (typically the ToString() evaluation)
    /// </summary>
    object? Model { get; }

    /// <summary>
    /// Removes the reference to the full entity, maiking the lite more lightweight
    /// </summary>
    void ClearEntity();

    /// <summary>
    /// Sets the reference to the full entity. Not generic to keep Lite<T> co-variant. Type and Id checked at run-time. 
    /// </summary>
    void SetEntity(Entity ei);

    /// <summary>
    /// Sets the model of the entity. Not checked in anyway. 
    /// </summary>
    void SetModel(object? model);

    /// <summary>
    /// Copies the Id from the entity to this lite instance. Typically used after saving by the framework. 
    /// </summary>
    PrimaryKey RefreshId();

    /// <summary>
    /// Unique identifier of this lite, containing type and id
    /// </summary>
    string Key();

    /// <summary>
    /// Unique identifier of this lite, containing type and id and toString
    /// </summary>
    string KeyLong();

    /// <summary>
    /// Creates a new instance of this lite without the entity. Only works for Lite with an ID. 
    /// Usefull to be defenisive about someone else loading the entity and producing serialization problems. 
    /// </summary>
    Lite<T> Clone();


    /// <summary>
    /// Get the Model Property and cast it to M
    /// </summary>
    /// <typeparam name="M"></typeparam>
    /// <returns></returns>
    M GetModel<M>() where M : ModelEntity; //LiteModel<T>
}


public interface ILiteModelConstructor
{
    public bool IsDefault { get; set; }
    public Type EntityType { get; }
    public Type ModelType { get; }

    ILiteModelConstructor Clone(bool isDefault);

    LambdaExpression GetConstructorExpression();

    object? NotFoundModel(Lite<Entity> lite);
}

public class LiteModelConstructor<T, M> : ILiteModelConstructor
    where T : Entity
{
    public LiteModelConstructor(bool isDefault, Expression<Func<T, M>> constructorExpression, Func<T, M> constructorFunction)
    {
        IsDefault = isDefault;
        ConstructorExpression = constructorExpression;
        ConstructorFunction = constructorFunction;
    }

    public LiteModelConstructor(bool isDefault, Expression<Func<T, M>> constructorExpression)
    {
        IsDefault = isDefault;
        ConstructorExpression = constructorExpression;
        ConstructorFunction = constructorExpression.Compile();
    }

    public bool IsDefault { get; set; }
    public Type EntityType => typeof(T);
    public Type ModelType => typeof(M);
    public Expression<Func<T, M>> ConstructorExpression { get; } 
    public Func<T, M> ConstructorFunction { get; }

    public Func<Lite<T>, M>? NotFoundModel { get; set; }

    public ILiteModelConstructor Clone(bool isDefault) => new LiteModelConstructor<T, M>(isDefault, ConstructorExpression, ConstructorFunction);

    object? ILiteModelConstructor.NotFoundModel(Lite<Entity> lite)
    {
        if (NotFoundModel == null)
            return null;

        return NotFoundModel.Invoke((Lite<T>)lite);
    }

    public LambdaExpression GetConstructorExpression() => this.ConstructorExpression;
}

public static class Lite
{
    public static Type BaseImplementationType = typeof(LiteImp);

    static GenericInvoker<Func<PrimaryKey, object?, int?, Lite<Entity>>> giNewLite =
        new((id, m, partitionId) => new LiteImp<Entity, string>(id, (string?)m, partitionId));

    static GenericInvoker<Func<Entity, object?, Lite<Entity>>> giNewLiteFat =
        new((entity, m) => new LiteImp<Entity, string>(entity, (string?)m));

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

        Type? type = TypeLogic.TryGetType(match.Groups["type"].Value);
        if (type == null)
            return LiteMessage.Type0NotFound.NiceToString().FormatWith(match.Groups["type"].Value);

        var idStr = match.Groups["id"].Value;
        int? partitionId = null;
        if (idStr.Contains("/"))
        {
            idStr = idStr.Before("/");
            partitionId = int.Parse(idStr.After("/"));
        }

        if (!PrimaryKey.TryParse(idStr, type, out PrimaryKey id))
            return LiteMessage.IdNotValid.NiceToString();

        string? toStr = match.Groups["toStr"].Value.DefaultText(null!); //maybe null

        result = giNewLite.GetInvoker(type, typeof(string))(id, toStr, partitionId);

        return null;
    }
       

    public static string? TryParse<T>(string liteKey, out Lite<T>? lite) where T : class, IEntity
    {
        var result = Lite.TryParseLite(liteKey, out Lite<Entity>? untypedLite);
        lite = (Lite<T>?)untypedLite;
        return result;
    }

    public static Lite<Entity> Create(Type type, PrimaryKey id) => Create(type, id, (int?)null);
    public static Lite<Entity> Create(Type type, PrimaryKey id, int? partitionId)
    {
        return giNewLite.GetInvoker(type, typeof(string))(id, null, partitionId);
    }

    public static Lite<Entity> Create(Type type, PrimaryKey id, object model) => Create(type, id, model, null);
    public static Lite<Entity> Create(Type type, PrimaryKey id, object model, int? partitionId)
    {
        return giNewLite.GetInvoker(type, model.GetType())(id, model, partitionId);
    }

    public static Lite<Entity> Create(Type type, PrimaryKey id, Type modelType) => Create(type, id, modelType, null);
    public static Lite<Entity> Create(Type type, PrimaryKey id, Type modelType, int? partitionId)
    {
        return giNewLite.GetInvoker(type, modelType)(id, null, partitionId);
    }

    [DebuggerStepThrough]
    public static Lite<T> ToLite<T>(this T entity)
      where T : class, IEntity
    {
        if (entity.IdOrNull == null)
            throw new InvalidOperationException("ToLite is not allowed for new entities, use ToLiteFat instead");

        var modelType = Lite.DefaultModelType(entity.GetType());

        var model = Lite.ConstructModel(entity, modelType);

        return (Lite<T>)giNewLite.GetInvoker(entity.GetType(), modelType)(entity.Id, model, entity.PartitionId);
    }

    [DebuggerStepThrough]
    public static Lite<T> ToLite<T>(this T entity, Type modelType)
     where T : class, IEntity
    {
        if (entity.IdOrNull == null)
            throw new InvalidOperationException("ToLite is not allowed for new entities, use ToLiteFat instead");

        var model = Lite.ConstructModel(entity, modelType);

        var lite = (Lite<T>)giNewLite.GetInvoker(entity.GetType(), modelType)(entity.Id, model, entity.PartitionId);

        return lite;
    }

    public static IEnumerable<Type> GetAllLiteModelTypes(Type entityType)
    {
        var dic = Lite.LiteModelConstructors.TryGetC(entityType);
        if (dic == null)
            return new[] { typeof(string) };

        return dic.Keys.PreAnd(typeof(string));
    }

    [DebuggerStepThrough]
    public static Lite<T> ToLite<T>(this T entity, object model)
        where T : class, IEntity
    {
        if (entity.IsNew)
            throw new InvalidOperationException("ToLite is not allowed for new entities, use ToLiteFat instead");

        return (Lite<T>)giNewLite.GetInvoker(entity.GetType(), model.GetType())(entity.Id, model, entity.PartitionId);
    }

    [DebuggerStepThrough]
    public static Lite<T> ToLiteFat<T>(this T entity)
     where T : class, IEntity
    {
        var modelType = Lite.DefaultModelType(entity.GetType());

        var model = Lite.ConstructModel(entity, modelType);

        return (Lite<T>)giNewLiteFat.GetInvoker(entity.GetType(), modelType)((Entity)(IEntity)entity, model);
    }

    [DebuggerStepThrough]
    public static Lite<T> ToLiteFat<T>(this T entity, Type modelType)
        where T : class, IEntity
    {
        var model = Lite.ConstructModel(entity, modelType);

        return (Lite<T>)giNewLiteFat.GetInvoker(entity.GetType(), modelType)((Entity)(IEntity)entity, model);
    }

    [DebuggerStepThrough]
    public static Lite<T> ToLiteFat<T>(this T entity, object model)
      where T : class, IEntity
    {
        return (Lite<T>)giNewLiteFat.GetInvoker(entity.GetType(), model.GetType())((Entity)(IEntity)entity, model);
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
    public static Lite<T> ToLite<T>(this T entity, bool fat, object model) where T : class, IEntity
    {
        if (fat)
            return entity.ToLiteFat(model);
        else
            return entity.ToLite(model);
    }

    class IsExpander : IMethodExpander
    {
        public Expression Expand(Expression? instance, Expression[] arguments, MethodInfo mi)
        {
            return Expression.Equal(arguments[0], arguments[1]);
        }
    }

    public static Dictionary<Type/*Entity*/, Dictionary<Type/*Model*/, ILiteModelConstructor>> LiteModelConstructors = new();

    public static void RegisterLiteModelConstructor<T, M>(Expression<Func<T, M>> constructorExpression, bool isDefault = true, bool isOverride = false) 
        where T : Entity
        => RegisterLiteModelConstructor(new LiteModelConstructor<T, M>(isDefault, constructorExpression), isOverride);

    public static void RegisterLiteModelConstructor<T, M>(LiteModelConstructor<T, M> liteModelConstructor, bool isOverride = false) 
        where T : Entity
    {
        if (typeof(M).IsModelEntity() && typeof(M).GetMethod("ToString", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)?.GetCustomAttribute<ExpressionFieldAttribute>(false) == null)
            throw new InvalidOperationException($"{typeof(M).TypeName()} needs to implement ToString using [AutoExpressionField] to allow ToString on Lite<{typeof(T).TypeName()}>");

        var dic = LiteModelConstructors.GetOrCreate(typeof(T));

        if (dic.ContainsKey(typeof(M)) && !isOverride)
            throw new InvalidOperationException($"'{typeof(T).TypeName()}' already has lite model constructor for Model '{typeof(M).TypeName()}'. Consider using isOverride = true");

        var currentDefault = dic.Values.SingleOrDefault(a => a.IsDefault);
        if (liteModelConstructor.IsDefault && currentDefault != null)
        {
            if (!isOverride)
                throw new InvalidOperationException($"'{typeof(T).TypeName()}' already has a default Lite Model Constructor ({currentDefault.ModelType.TypeName()})");

            dic[typeof(M)] = currentDefault.Clone(isDefault: false);
        }

        dic[typeof(M)] = liteModelConstructor;
    }

    public static Type DefaultModelType(Type type)
    {
        return LiteModelConstructors.TryGetC(type)?.Values.SingleOrDefaultEx(a => a.IsDefault)?.ModelType ?? typeof(string);
    }

    public static object? GetNotFoundModel(Lite<IEntity> lite)
    {
        var lmc = LiteModelConstructors.TryGetC(lite.EntityType)?.TryGetC(lite.ModelType);

        if (lmc == null)
        {
            if (lite.ModelType == typeof(string))
                return ("[" + EngineMessage.EntityWithType0AndId1NotFound.NiceToString().FormatWith(lite.EntityType.NiceName(), lite.Id) + "]");

            throw new InvalidOperationException($"Entity '{lite.EntityType}' has not registered LiteModelConstructor for '{lite.ModelType}'");
        }

        return lmc.NotFoundModel((Lite<Entity>)lite);
    }

    public static object ConstructModel(IEntity e, Type modelType)
    {
        return giGetModel.GetInvoker(e.GetType(), modelType)((Entity)e);
    }

    public static string ModelTypeToString(Type modelType)
    {
        if (modelType == typeof(string))
            return "string";

        return Reflector.CleanTypeName(modelType);
    }

    public static Type ParseModelType(Type entityType, string modelTypeStr)
    {
        if (modelTypeStr == "string" || 
            modelTypeStr == "String")
            return typeof(string);

        var dic = LiteModelConstructors.TryGetC(entityType);

        var single = dic?.Keys.SingleOrDefaultEx(a => a.Name == modelTypeStr || Reflector.CleanTypeName(a) == modelTypeStr);

        if (single == null)
            throw new InvalidOperationException($"No Lite Model with name '{modelTypeStr}' is registered for '{entityType.TypeName()}'");

        return single;
    }

    static GenericInvoker<Func<Entity, object>> giGetModel = new GenericInvoker<Func<Entity, object>>((e) => ConstructModel<Entity, string>(e));
    public static M ConstructModel<T, M>(T e)
        where T : Entity
    {
        var lmc = LiteModelConstructors.TryGetC(typeof(T))?.TryGetC(typeof(M));

        if (lmc == null)
        {
            if (typeof(M) == typeof(string))
                return (M)(object)e.ToString()!;

            throw new InvalidOperationException($"Entity '{typeof(T).TypeName()}' has not registered LiteModelConstructor for '{typeof(M).TypeName()}'");
        }

        return ((LiteModelConstructor<T, M>)lmc).ConstructorFunction(e);
    }

    public static LambdaExpression GetModelConstructorExpression(Type entityType, Type modelType) => giGetModelConstructorExpression.GetInvoker(entityType, modelType)();
    static readonly GenericInvoker<Func<LambdaExpression>> giGetModelConstructorExpression = new(() => GetModelConstructorExpression<Entity, string>());
    public static Expression<Func<T, M>> GetModelConstructorExpression<T, M>()
    where T : Entity
    {
        var lmc = LiteModelConstructors.TryGetC(typeof(T))?.TryGetC(typeof(M));

        if (lmc == null)
        {
            if (typeof(M) == typeof(string))
            {
                Expression<Func<T, string>> ex = e => e.ToString();

                return (Expression<Func<T, M>>)(LambdaExpression)ex;
            }

            throw new InvalidOperationException($"Entity '{typeof(T).TypeName()}' has not registered LiteModelConstructor for '{typeof(M).TypeName()}'");
        }

        return ((LiteModelConstructor<T, M>)lmc).ConstructorExpression;
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
        return new LiteImp<T, string>(id, null, null);
    }

    public static Lite<T> Create<T>(PrimaryKey id, int? partitionId) where T : Entity
    {
        return new LiteImp<T, string>(id, null, partitionId);
    }

    public static Lite<T> Create<T>(PrimaryKey id, object model) where T : Entity
        => Create<T>(id, model, null);

    public static Lite<T> Create<T>(PrimaryKey id, object model, int? partitionId) where T : Entity
    {
        if(model == null || model is string)
            return new LiteImp<T, string>(id, (string?)model, partitionId);

        return (Lite<T>)giNewLite.GetInvoker(typeof(T), model?.GetType() ?? typeof(string))(id, model, partitionId);       
    }

    static ConcurrentDictionary<(Type type, Type modelType), ConstructorInfo> liteConstructorCache = new();
    public static ConstructorInfo GetLiteConstructorFromCache(Type type, Type modelType)
    {
        return liteConstructorCache.GetOrAdd((type, modelType), t => CreateLiteConstructor(t.type, t.modelType));
    }

    static ConstructorInfo CreateLiteConstructor(Type t, Type modelType)
    {
        return typeof(LiteImp<,>).MakeGenericType(t, modelType).GetConstructor(new[] { typeof(PrimaryKey), modelType, typeof(int?) }) ??
            throw new InvalidOperationException("No Constructor found");
    }


    public static NewExpression NewExpression(Type type, Expression id, Expression model, Expression partitionId)
    {
        return Expression.New(Lite.GetLiteConstructorFromCache(type, model.Type), id.UnNullify(), model, partitionId);
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
    [Description("Type {0} not found")]
    Type0NotFound,
    [Description("Text")]
    ToStr
}

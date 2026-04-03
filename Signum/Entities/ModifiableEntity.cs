using System.ComponentModel;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Specialized;
using Signum.Utilities.Reflection;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Signum.Entities;


public interface IModifiableEntity : INotifyPropertyChanged
{
    M Mixin<M>() where M : MixinEntity;
    M? TryMixin<M>() where M : MixinEntity;
}


[DescriptionOptions(DescriptionOptions.Members | DescriptionOptions.Description), InTypeScript(false)]
public abstract class ModifiableEntity : Modifiable, IModifiableEntity, ICloneable, IDataErrorInfo
{
    static Func<bool>? isRetrievingFunc = null;
    static public bool IsRetrieving
    {
        get { return isRetrievingFunc != null && isRetrievingFunc(); }
    }

    internal static void SetIsRetrievingFunc(Func<bool> isRetrievingFunc)
    {
        ModifiableEntity.isRetrievingFunc = isRetrievingFunc;
    }

    protected internal const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    protected virtual T Get<T>(T fieldValue, [CallerMemberName]string? automaticPropertyName = null)
    {
        return fieldValue;
    }


    protected virtual bool Set<T>(ref T field, T value, [CallerMemberName]string? automaticPropertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        PropertyInfo? pi = GetPropertyInfo(automaticPropertyName!);

        if (pi == null)
            throw new ArgumentException("No PropertyInfo with name {0} found in {1} or any implemented interface".FormatWith(automaticPropertyName, this.GetType().TypeName()));

        if (value is IMListPrivate && !((IMListPrivate)value).IsNew && !object.ReferenceEquals(value, field))
            throw new InvalidOperationException("Only MList<T> with IsNew = true can be assigned to an entity");

        if (field is INotifyCollectionChanged colb)
        {
            if (AttributeManager<BindParentAttribute>.FieldContainsAttribute(GetType(), pi))
            {
                colb.CollectionChanged -= ChildCollectionChanged;
                if (colb is IEnumerable<IModifiableEntity> mlist)
                {
                    foreach (ModifiableEntity item in mlist)
                        item.ClearParentEntity(this);
                }
            }
        }
        else if (field is ModifiableEntity modb)
        {
            if (AttributeManager<BindParentAttribute>.FieldContainsAttribute(GetType(), pi))
                modb.ClearParentEntity(this);
        }

        SetSelfModified();
        field = value;

        if (field is INotifyCollectionChanged cola)
        {
            if (AttributeManager<BindParentAttribute>.FieldContainsAttribute(GetType(), pi))
            {
                cola.CollectionChanged += ChildCollectionChanged;
                if (cola is IEnumerable<IModifiableEntity> mlist)
                {
                    foreach (ModifiableEntity item in mlist)
                        item.SetParentEntity(this);
                }

            }
        }
        else if (field is ModifiableEntity moda)
        {
            if (AttributeManager<BindParentAttribute>.FieldContainsAttribute(GetType(), pi))
                moda.SetParentEntity(this);
        }

        NotifyPrivate(pi.Name);
        NotifyPrivate("Error");
        NotifyToString();

        ClearTemporalError(pi.Name);

        return true;
    }

    struct PropertyKey : IEquatable<PropertyKey>
    {
        public PropertyKey(Type type, string propertyName)
        {
            this.Type = type;
            this.PropertyName = propertyName;
        }

        public Type Type;
        public string PropertyName;

        public bool Equals(PropertyKey other) => other.Type == Type && other.PropertyName == PropertyName;
        public override bool Equals(object? obj) => obj is PropertyKey pk && Equals(pk);
        public override int GetHashCode() => Type.GetHashCode() ^ PropertyName.GetHashCode();
    }

    static readonly ConcurrentDictionary<PropertyKey, PropertyInfo?> PropertyCache = new ConcurrentDictionary<PropertyKey, PropertyInfo?>();

    protected PropertyInfo? GetPropertyInfo(string propertyName)
    {
        return PropertyCache.GetOrAdd(new PropertyKey(this.GetType(), propertyName), key =>
            key.Type.GetProperty(propertyName, flags) ??
             key.Type.GetInterfaces().Select(i => i.GetProperty(key.PropertyName, flags)).NotNull().FirstOrDefault());
    }

    static Expression<Func<ModifiableEntity, string>> ToStringPropertyExpression = m => m.ToString()!;
    [HiddenProperty, ExpressionField("ToStringPropertyExpression")]
    public string ToStringProperty
    {
        get
        {
            string? str = ToString();
            return str.HasText() ? str : this.GetType().NiceName();
        }
    }
    
    #region Collection Events

    protected internal override void PostRetrieving(PostRetrievingContext ctx)
    {
        BindParent();
    }

    protected virtual void BindParent()
    {
        foreach (object? field in AttributeManager<BindParentAttribute>.FieldsWithAttribute(this))
        {
            if (field == null)
                continue;

            if (field is ModifiableEntity entity)
            {
                entity.SetParentEntity(this);
            }
            else if (field is INotifyCollectionChanged col)
            {
                col.CollectionChanged += ChildCollectionChanged;

                if (col is IEnumerable<IModifiableEntity> mlist)
                {
                    foreach (ModifiableEntity item in mlist)
                    {
                        item.SetParentEntity(this);
                    }
                }
            }
        }
    }

    //[OnDeserialized]
    //private void OnDeserialized(StreamingContext context)
    //{
    //    BindParent();
    //}

    protected virtual void ChildCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (AttributeManager<BindParentAttribute>.FieldsWithAttribute(this).Contains(sender))
        {
            string? propertyName = AttributeManager<BindableAttribute>.FindPropertyName(this, sender!);
            if (propertyName != null)
                NotifyPrivate(propertyName);

            if (sender is IEnumerable<IModifiableEntity>)
            {
                if (args.NewItems != null)
                {
                    foreach (ModifiableEntity p in args.NewItems)
                        p.SetParentEntity(this);
                }

                if (args.OldItems != null)
                {
                    foreach (ModifiableEntity p in args.OldItems)
                        p.ClearParentEntity(this);
                }
            }
        }
    }

    protected virtual void ChildPropertyChanged(object sender, PropertyChangedEventArgs e)
    {

    }

    protected virtual string? ChildPropertyValidation(ModifiableEntity sender, PropertyInfo pi)
    {
        return null;
    }
    #endregion

    [field: NonSerialized, Ignore]
    public event PropertyChangedEventHandler? PropertyChanged;

    [NonSerialized, Ignore]
    ModifiableEntity? parentEntity;

    public virtual T? TryGetParentEntity<T>()
        where T: class, IModifiableEntity 
    {
        return ((IModifiableEntity?)parentEntity) as T;
    }

    public virtual T GetParentEntity<T>()
        where T : IModifiableEntity
    {
        if (parentEntity == null)
            throw new InvalidOperationException("parentEntity is null");

        return (T)(IModifiableEntity)parentEntity;
    }

    protected virtual void SetParentEntity(ModifiableEntity p)
    {
        if (p != null && this.parentEntity != null && this.parentEntity != p)
            throw new InvalidOperationException($"'{nameof(parentEntity)}' of '{this}'({this.GetType().TypeName()}) is still connected to '{parentEntity}'({parentEntity.GetType().TypeName()}), then can not be set to '{p}'({p.GetType().TypeName()})");

        this.parentEntity = p;
    }

    protected virtual void ClearParentEntity(ModifiableEntity p)
    {
        if (p == this.parentEntity)
            this.parentEntity = null;
    }

    internal string? OnParentChildPropertyValidation(PropertyInfo pi)
    {
        if (parentEntity == null)
            return null;

        return parentEntity.ChildPropertyValidation(this, pi);
    }

    public void Notify<T>(Expression<Func<T>> property)
    {
        NotifyPrivate(ReflectionTools.BasePropertyInfo(property).Name);
        NotifyError();
    }

    public void NotifyError()
    {
        NotifyPrivate("Error");
    }

 

    public void NotifyToString()
    {
        NotifyPrivate("ToStringProperty");
    }

    void NotifyPrivate(string propertyName)
    {
        var parent = this.parentEntity;
        if (parent != null)
            parent.ChildPropertyChanged(this, new PropertyChangedEventArgs(propertyName));

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    }


    #region Temporal ID
    [Ignore]
    internal Guid temporalId = Guid.NewGuid();

    public override int GetHashCode()
    {
        return GetType().FullName!.GetHashCode() ^ temporalId.GetHashCode();
    }
    #endregion

    #region IDataErrorInfo Members
    [HiddenProperty]
    public string? Error
    {
        get { return IntegrityCheck()?.Errors.Values.ToString("\n"); }
    }

    

    public IntegrityCheck? IntegrityCheck()
    {
        using (var log = HeavyProfiler.LogNoStackTrace("IntegrityCheck"))
        {
            var validators = Validator.GetPropertyValidators(GetType());

            Dictionary<string, string>? dic = null;

            foreach (var pv in validators.Values)
            {
                var error = pv.PropertyCheck(this);

                if (error != null)
                {
                    if (dic == null)
                        dic = new Dictionary<string, string>();

                    dic.Add(pv.PropertyInfo.Name, error);
                }
            }
            if (dic == null)
                return null;

            return new Entities.IntegrityCheck(this, dic);
        }
    }


    string IDataErrorInfo.Error => this.Error ?? "";

    //override for per-property checks
    [HiddenProperty]
    string IDataErrorInfo.this[string columnName]
    {
        get
        {
            if (columnName == null)
                return ((IDataErrorInfo)this).Error;
            else
                return PropertyCheck(columnName) ?? "";
        }
    }

    public string? PropertyCheck(Expression<Func<object?>> property)
    {
        return PropertyCheck(ReflectionTools.GetPropertyInfo(property).Name);
    }

    public string? PropertyCheck(string propertyName)
    {
        IPropertyValidator? pp = Validator.TryGetPropertyValidator(GetType(), propertyName);

        if (pp == null)
            return null; //Hidden properties

        return pp.PropertyCheck(this);
    }

    protected internal virtual string? PropertyValidation(PropertyInfo pi)
    {
        return null;
    }

    public bool IsPropertyReadonly(string propertyName)
    {
        IPropertyValidator? pp = Validator.TryGetPropertyValidator(GetType(), propertyName);

        if (pp == null)
            return false; //Hidden properties

        return pp.IsPropertyReadonly(this);
    }


    public PropertyRoute? TryGetPropertyRoute(MemberInfo mi) => TryGetPropertyRoute()?.Add(mi);
    public PropertyRoute? TryGetPropertyRoute()
    {
        if(this is IRootEntity)
            return PropertyRoute.Root(this.GetType());

        if(this.parentEntity is { } p &&  p.TryGetPropertyRoute() is PropertyRoute pr)
        {
            var validators = Validator.GetPropertyValidators(p.GetType());

            var type = this.GetType();

            foreach (var kvp in validators)
            {
                if (kvp.Value.PropertyInfo.PropertyType.IsAssignableFrom(type))
                {
                    if(kvp.Value.GetValueUntyped(this.parentEntity) == this)
                        return pr.Add(kvp.Value.PropertyInfo);
                }

                if (kvp.Value.PropertyInfo.PropertyType.ElementType()?.IsAssignableFrom(type) == true)
                {
                    if (kvp.Value.GetValueUntyped(this.parentEntity) is IEnumerable<IModifiableEntity> list)
                        if (list.Contains(this))
                            return pr.Add(kvp.Value.PropertyInfo).Add("Item");
                }
            }
        }

        return null;
    }


    protected internal virtual bool IsPropertyReadonly(PropertyInfo pi)
    {
        return false;
    }

    protected static void Validate<T>(Expression<Func<T, object?>> property, Func<T, PropertyInfo, string?> validate) where T : ModifiableEntity
    {
        Validator.PropertyValidator(property).StaticPropertyValidation += validate;
    }

    public Dictionary<Guid, IntegrityCheck>? FullIntegrityCheck()
    {
        var graph = GraphExplorer.FromRoot(this);
        return GraphExplorer.FullIntegrityCheck(graph);
    }

    [MethodExpander(typeof(NicePropertyNameExpander))]
    public static string NicePropertyName<R>(Expression<Func<R>> property)
    {
        return ReflectionTools.GetPropertyInfo(property).NiceName();
    }

    [MethodExpander(typeof(NicePropertyNameExpander))]
    public static string NicePropertyName<E, R>(Expression<Func<E, R>> property)
    {
        return ReflectionTools.GetPropertyInfo(property).NiceName();
    }

    #endregion

    #region ICloneable Members
    object ICloneable.Clone()
    {
#pragma warning disable SYSLIB0011 // Type or member is obsolete
        BinaryFormatter bf = new BinaryFormatter();
        using (MemoryStream stream = new MemoryStream())
        {
            bf.Serialize(stream, this);
            stream.Seek(0, SeekOrigin.Begin);
            return bf.Deserialize(stream);
        }
#pragma warning restore SYSLIB0011 // Type or member is obsolete
    }

    #endregion


    [Ignore]
    internal Dictionary<string, string>? temporalErrors;
    internal void SetTemporalErrors(Dictionary<string, string>? errors)
    {
        NotifyTemporalErrors();

        this.temporalErrors = errors;

        NotifyTemporalErrors();
    }

    void NotifyTemporalErrors()
    {
        if (temporalErrors != null)
        {
            foreach (var e in temporalErrors.Keys)
                NotifyPrivate(e);

            NotifyError();
        }
    }

    void ClearTemporalError(string propertyName)
    {
        if (this.temporalErrors == null)
            return;

        this.temporalErrors.Remove(propertyName);
        NotifyPrivate(propertyName);
        NotifyError();
    }

    public void SetTemporalError(PropertyInfo pi, string? error)
    {
        if (error == null)
        {
            if (this.temporalErrors != null)
            {
                this.temporalErrors.Remove(pi.Name);
                if (this.temporalErrors.Count == 0)
                    this.temporalErrors = null;
            }
        }
        else
        {
            if (this.temporalErrors == null)
                this.temporalErrors = new Dictionary<string, string>();


            this.temporalErrors.Add(pi.Name, error);
        }
    }

    internal ModifiableEntity()
    {
        mixin = MixinDeclarations.CreateMixins(this);
    }

    [Ignore, DebuggerBrowsable(DebuggerBrowsableState.Never)]
    readonly MixinEntity? mixin;
    public M Mixin<M>() where M : MixinEntity
    {
        var result = TryMixin<M>();
        if (result != null)
            return result;

        throw new InvalidOperationException("Mixin {0} not declared for {1} in MixinDeclarations"
            .FormatWith(typeof(M).TypeName(), GetType().TypeName()));
    }

    public M? TryMixin<M>() where M : MixinEntity
    {
        var current = mixin;
        while (current != null)
        {
            if (current is M)
                return (M)current;
            current = current.Next;
        }

        return null;
    }

    public MixinEntity? TryMixin(string mixinName)
    {
        var current = mixin;
        while (current != null)
        {
            if (current.GetType().Name == mixinName)
                return current;
            current = current.Next;
        }

        return null;
    }

    public MixinEntity GetMixin(Type mixinType)
    {
        var current = mixin;
        while (current != null)
        {
            if (current.GetType() == mixinType)
                return current;
            current = current.Next;
        }

        throw new InvalidOperationException("Mixin {0} not declared for {1} in MixinDeclarations"
            .FormatWith(mixinType.TypeName(), GetType().TypeName()));
    }

    [HiddenProperty]
    public MixinEntity this[string mixinName]
    {
        get
        {
            var current = mixin;
            while (current != null)
            {
                if (current.GetType().Name == mixinName)
                    return current;
                current = current.Next;
            }

            throw new InvalidOperationException("Mixin {0} not declared for {1} in MixinDeclarations"
                .FormatWith(mixinName, GetType().TypeName()));
        }
    }

    [HiddenProperty]
    public IEnumerable<MixinEntity> Mixins
    {
        get
        {
            var current = mixin;
            while (current != null)
            {
                yield return current;
                current = current.Next;
            }
        }
    }
}

public class IntegrityCheck
{
    public IntegrityCheck(ModifiableEntity me, Dictionary<string, string> errors)
    {
        this.TemporalId = me.temporalId;
        this.Type = me.GetType();
        this.Id = me is Entity e ? e.id : null;
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));
    }

    public Guid TemporalId { get; private set; }
    public Type Type { get; private set; }
    public PrimaryKey? Id { get; private set; }
    public Dictionary<string, string> Errors { get; private set; }

    public override string ToString()
    {
        return $"{Errors.Count} errors in {" ".Combine(Type.Name, Id)}\n"
              + Errors.ToString(kvp => "    {0}: {1}".FormatWith(kvp.Key, kvp.Value), "\n");
    }
}

public class IntegrityCheckWithEntity
{
    public IntegrityCheckWithEntity(IntegrityCheck integrityCheck, ModifiableEntity entity)
    {
        this.IntegrityCheck = integrityCheck;
        this.Entity = entity;
    }

    public IntegrityCheck IntegrityCheck {get; private set;}
    public ModifiableEntity Entity {get; set;}

    public override string ToString()
    {
        var validators = Validator.GetPropertyValidators(Entity.GetType());
        return $"{IntegrityCheck.Errors.Count} errors in {" ".Combine(IntegrityCheck.Type.Name, IntegrityCheck.Id)}\n"
              + IntegrityCheck.Errors.ToString(kvp => "    {0} ({1}): {2}".FormatWith(
                  kvp.Key,
                  validators.GetOrThrow(kvp.Key).GetValueUntyped(Entity) ?? "null", 
                  kvp.Value), 
                  "\n");
    }
}


public class NicePropertyNameExpander : IMethodExpander
{
    public Expression Expand(Expression? instance, Expression[] arguments, MethodInfo mi)
    {
        var lambda = (LambdaExpression)arguments[0].StripQuotes();

        var niceName = ReflectionTools.BasePropertyInfo(lambda).NiceName();

        return Expression.Constant(niceName, typeof(string));
    }
}

#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using Signum.Entities;
using Signum.Engine.Maps;
using Signum.Utilities.DataStructures;
using Signum.Utilities.ExpressionTrees;
using Signum.Web.Properties;
using Signum.Engine;
using Signum.Entities.Reflection;
using System.Reflection;
using System.Collections;
using System.Diagnostics;
using System.Web.Mvc;
using System.Collections.Specialized;
using System.Linq.Expressions;
using System.Globalization;
#endregion

namespace Signum.Web
{
    public abstract class Mapping
    {
        public abstract Type StaticType { get; }

        protected static readonly string[] specialProperties = new[] 
        { 
            TypeContext.Ticks,
            EntityBaseKeys.RuntimeInfo,
            EntityBaseKeys.ToStr, 
            EntityListBaseKeys.Index,
        };

        public static Mapping<T> Create<T>()
        {
            if (typeof(T).IsModifiableEntity() || typeof(T).IsIIdentifiable())
                return (Mapping<T>)Activator.CreateInstance(typeof(AutoEntityMapping<>).MakeGenericType(typeof(T)));

            if (typeof(T).IsLite())
                return (Mapping<T>)Activator.CreateInstance(typeof(LiteMapping<>).MakeGenericType(Reflector.ExtractLite(typeof(T))));

            if (Reflector.IsMList(typeof(T)))
                return (Mapping<T>)Activator.CreateInstance(typeof(MListMapping<>).MakeGenericType(typeof(T).ElementType()));

            return new ValueMapping<T>();
        }
    }

    public abstract class Mapping<T> : Mapping
    {
        public override Type StaticType
        {
            get { return typeof(T); }
        }

        public void OnGetValue(MappingContext<T> ctx)
        {
            if (GetValue != null)
            {
                ctx.Value = GetValue(ctx);
            }
            else
            {
                if (ctx.Empty())
                {
                    ctx.SupressChange = true;
                    return;
                }

                ctx.Value = DefaultGetValue(ctx);
            }
        }

        protected internal abstract T DefaultGetValue(MappingContext<T> ctx);

        public Func<MappingContext<T>, T> GetValue;

        internal void OnValidation(MappingContext<T> ctx)
        {
            Debug.Assert(!ctx.Empty());

            if (!AvoidRecursiveValidation)
                RecursiveValidation(ctx);

            if (Validated != null)
                Validated(ctx);
        }

        public abstract void RecursiveValidation(MappingContext<T> ctx);

        public bool AvoidRecursiveValidation { get; set; }

        public Action<MappingContext<T>> Validated;
    }

    public class ValueMapping
    {
        public static bool ParseHtmlBool(string input)
        {
            string[] vals = input.Split(',');
            return vals[0] == "true" || vals[0] == "True";
        }
    }

    public class ValueMapping<T> : Mapping<T>
    {
        protected internal override T DefaultGetValue(MappingContext<T> ctx)
        {

            Type type = typeof(T).UnNullify();

            if (type == typeof(bool))
            {
                return (T)(object)ValueMapping.ParseHtmlBool(ctx.Input);
            }
            else if (type == typeof(DateTime))
            {
                if (ctx.Input.HasText())
                    return (T)(object)DateTime.Parse(ctx.Input).FromUserInterface();
                return (T)(object)null;
            }
            else if (ReflectionTools.IsNumber(type) && ReflectionTools.IsPercentage(Reflector.FormatString(ctx.PropertyRoute), CultureInfo.CurrentCulture))
            {
                return (T)ReflectionTools.ParsePercentage(ctx.Input, type, CultureInfo.CurrentCulture);
            }
            
            return ReflectionTools.Parse<T>(ctx.Input);
        }

        public override void RecursiveValidation(MappingContext<T> ctx)
        {
            return;
        }
    }

    public class AutoEntityMapping<T> : Mapping<T> where T: class 
    {
        public Dictionary<Type, Mapping> AllowedMappings;

        public EntityMapping<R> RegisterMapping<R>(EntityMapping<R> mapping) where R : ModifiableEntity
        {
            if (AllowedMappings == null)
                AllowedMappings = new Dictionary<Type, Mapping>();

            AllowedMappings.Add(typeof(R), mapping);

            return mapping;
        }

        protected internal override T DefaultGetValue(MappingContext<T> ctx)
        {
            string strRuntimeInfo;
            if (!ctx.Inputs.TryGetValue(EntityBaseKeys.RuntimeInfo, out strRuntimeInfo))
                return ctx.Value; //I only have some ValueLines of an Entity (so no Runtime, Id or anything)

            RuntimeInfo runtimeInfo = RuntimeInfo.FromFormValue(strRuntimeInfo);

            if (runtimeInfo.RuntimeType == null)
            {
                return (T)(object)null;
            }

            if (typeof(T) == runtimeInfo.RuntimeType || typeof(T).IsEmbeddedEntity())
                return GetRuntimeValue<T>(ctx, ctx.PropertyRoute);

            return miGetRuntimeValue.GetInvoker(runtimeInfo.RuntimeType)(this, ctx, PropertyRoute.Root(runtimeInfo.RuntimeType));
        }

        static GenericInvoker<Func<AutoEntityMapping<T>, MappingContext<T>, PropertyRoute, T>> miGetRuntimeValue = 
           new GenericInvoker<Func<AutoEntityMapping<T>, MappingContext<T>, PropertyRoute, T>>((aem, mc, pr)=>aem.GetRuntimeValue<T>(mc, pr));
        public R GetRuntimeValue<R>(MappingContext<T> ctx, PropertyRoute route)
            where R : class, T 
        {
            if (AllowedMappings != null && !AllowedMappings.ContainsKey(typeof(R)))
            {
                return (R)(object)ctx.None(Resources.Type0NotAllowed.Formato(typeof(R)));
            }

            Mapping<R> mapping =  (Mapping<R>)(AllowedMappings.TryGetC(typeof(R)) ?? Navigator.EntitySettings(typeof(R)).UntypedMappingDefault);
            SubContext<R> sc = new SubContext<R>(ctx.ControlID, mapping, null, route, ctx) { Value = ctx.Value as R }; // If the type is different, the AutoEntityMapping has the current value but EntityMapping just null
            mapping.OnGetValue(sc);
            ctx.AddChild(sc);
            return sc.Value;
        }

        public override void RecursiveValidation(MappingContext<T> ctx)
        {
            if (ctx.FirstChild != null)
                ctx.FirstChild.ValidateInternal();
        }
    }

    public class EntityMapping<T> : Mapping<T> where T : ModifiableEntity
    {
        abstract class PropertyMapping
        {
            public static PropertyMapping Create(PropertyPack pp)
            {
                return (PropertyMapping)Activator.CreateInstance(typeof(PropertyMapping<>).MakeGenericType(typeof(T), pp.PropertyInfo.PropertyType), pp);
            }

            public abstract void SetProperty(MappingContext<T> parent);
        }

        class PropertyMapping<P> : PropertyMapping
        {
            public readonly Func<T, P> GetValue;
            public readonly Action<T, P> SetValue;
            public readonly PropertyPack PropertyPack;

            public Mapping<P> Mapping { get; set; }

            public PropertyMapping(PropertyPack pp)
            {
                GetValue = ReflectionTools.CreateGetter<T, P>(pp.PropertyInfo);
                SetValue = ReflectionTools.CreateSetter<T, P>(pp.PropertyInfo);
                PropertyPack = pp;
                Mapping = Create<P>();
            }

            public override void SetProperty(MappingContext<T> parent)
            {
                SubContext<P> ctx = CreateSubContext(parent);

                try
                {
                    Mapping.OnGetValue(ctx);

                    if (!ctx.SupressChange)
                    {
                        if (ctx.Ticks != null && ctx.Ticks != 0)
                            ctx.AddOnFinish(() => SetValue(parent.Value, ctx.Value));
                        else
                            SetValue(parent.Value, ctx.Value);
                    }
                }
                catch (Exception e)
                {
                    string error = e is FormatException ? Resources._0HasAnInvalidFormat : Resources.NotPossibleToaAssign0;

                    ctx.Error.Add(error.Formato(PropertyPack.PropertyInfo.NiceName()));
                }

                if (!ctx.Empty())
                    parent.AddChild(ctx);
            }

            public SubContext<P> CreateSubContext(MappingContext<T> parent)
            {
                string newControlId = TypeContextUtilities.Compose(parent.ControlID, PropertyPack.PropertyInfo.Name);
                PropertyRoute route = parent.PropertyRoute.Add(this.PropertyPack.PropertyInfo);

                SubContext<P> ctx = new SubContext<P>(newControlId, Mapping, PropertyPack, route, parent);
                if (parent.Value != null)
                    ctx.Value = GetValue(parent.Value);
                return ctx;
            }
        }

        Dictionary<string, PropertyMapping> Properties = new Dictionary<string, PropertyMapping>();

        public EntityMapping(bool fillProperties)
        {
            if (fillProperties)
            {
                Properties = Validator.GetPropertyPacks(typeof(T))
                    .Where(kvp => !kvp.Value.PropertyInfo.IsReadOnly())
                    .ToDictionary(kvp => kvp.Key, kvp => PropertyMapping.Create(kvp.Value));
            }
        }

        protected internal override T DefaultGetValue(MappingContext<T> ctx)
        {
            var val = OnGetEntity(ctx);

            if (val == ctx.Value)
                ctx.SupressChange = true;
            else
                ctx.Value = val;

            SetProperties(ctx);

            return val;
        }

        public void SetProperties(MappingContext<T> ctx)
        {
            foreach (PropertyMapping item in Properties.Values)
            {
                item.SetProperty(ctx);
            }
        }

        public override void RecursiveValidation(MappingContext<T> ctx)
        {
            ModifiableEntity entity = ctx.Value;
            foreach (MappingContext childCtx in ctx.Children())
            {
                string error = entity.PropertyCheck(childCtx.PropertyPack);
                if (error.HasText())
                    childCtx.Error.Add(error);


                childCtx.ValidateInternal();
            }
        }

        public Func<MappingContext<T>, T> GetEntity;

        public T OnGetEntity(MappingContext<T> ctx)
        {
            if (GetEntity != null)
                return GetEntity(ctx);
            else
                return DefaultGetEntity(ctx);
        }

        public T DefaultGetEntity(MappingContext<T> ctx)
        {
            string strRuntimeInfo;
            if (!ctx.Inputs.TryGetValue(EntityBaseKeys.RuntimeInfo, out strRuntimeInfo))
                return ctx.Value; //I only have some ValueLines of an Entity (so no Runtime, Id or anything)

            RuntimeInfo runtimeInfo = RuntimeInfo.FromFormValue(strRuntimeInfo);

            if (runtimeInfo.RuntimeType == null)
                return null;

            if (typeof(T).IsEmbeddedEntity())
            {
                if (runtimeInfo.IsNew || ctx.Value == null)
                    return Constructor.Construct<T>();

                return ctx.Value;
            }
            else
            {
                IdentifiableEntity identifiable = (IdentifiableEntity)(ModifiableEntity)ctx.Value;

                 if (runtimeInfo.IsNew)
                 {
                     if(identifiable != null && identifiable.IsNew)
                         return (T)(ModifiableEntity)identifiable;
                     else
                         return Constructor.Construct<T>();
                 }

                 if (identifiable != null && runtimeInfo.IdOrNull == identifiable.IdOrNull && runtimeInfo.RuntimeType == identifiable.GetType())
                     return (T)(ModifiableEntity)identifiable;
                 else
                     return (T)(ModifiableEntity)Database.Retrieve(runtimeInfo.RuntimeType, runtimeInfo.IdOrNull.Value);
            }
        }


        public EntityMapping<T> CreateProperty<P>(Expression<Func<T, P>> property)
        {
            PropertyInfo pi = ReflectionTools.GetPropertyInfo(property);

            PropertyMapping<P> propertyMapping = (PropertyMapping<P>)Properties.GetOrCreate(pi.Name,
                () => new PropertyMapping<P>(Validator.GetOrCreatePropertyPack(typeof(T), pi.Name)));

            propertyMapping.Mapping = Mapping.Create<P>();

            return this;
        }


        public EntityMapping<T> SetPropertyMapping<P>(Expression<Func<T, P>> property, Action<Mapping<P>> continuation)
        {
            PropertyInfo pi = ReflectionTools.GetPropertyInfo(property);
            continuation(((PropertyMapping<P>)Properties[pi.Name]).Mapping);
            return this;
        }

        public EntityMapping<T> SetPropertyMapping<P>(Expression<Func<T, P>> property, Mapping<P> newMapping)
        {
            PropertyInfo pi = ReflectionTools.GetPropertyInfo(property);

            PropertyMapping<P> propertyMapping = (PropertyMapping<P>)Properties.GetOrCreate(pi.Name,
                () => new PropertyMapping<P>(Validator.GetOrCreatePropertyPack(typeof(T), pi.Name)));

            propertyMapping.Mapping = newMapping;

            return this;
        }

        public EntityMapping<T> SetProperty<P>(Expression<Func<T, P>> property, Func<MappingContext<P>, P> getValue)
        {
            PropertyInfo pi = ReflectionTools.GetPropertyInfo(property);
            ((PropertyMapping<P>)Properties[pi.Name]).Mapping.GetValue = getValue;
            
            return this;
        }

        public EntityMapping<T> RemoveProperty<P>(Expression<Func<T, P>> property)
        {
            PropertyInfo pi = ReflectionTools.GetPropertyInfo(property);
            Properties.Remove(pi.Name);
            return this;
        }

        public EntityMapping<T> ClearProperties()
        {
            Properties.Clear();
            return this;
        }
    }




    public class LiteMapping<S> : Mapping<Lite<S>>
        where S : class, IIdentifiable
    {
        public Mapping<S> EntityMapping { get; set; }

        public LiteMapping()
        {
            EntityMapping = Create<S>();
        }

        protected internal override Lite<S> DefaultGetValue(MappingContext<Lite<S>> ctx)
        {
            var newLite = Change(ctx);
            if (newLite == ctx.Value)
                ctx.SupressChange = true;

            return newLite;
        }

        public Lite<S> Change(MappingContext<Lite<S>> ctx)
        {
            string strRuntimeInfo;
            if (!ctx.Inputs.TryGetValue(EntityBaseKeys.RuntimeInfo, out strRuntimeInfo)) //I only have some ValueLines of an Entity (so no Runtime, Id or anything)
                return TryModifyEntity(ctx, ctx.Value);

            RuntimeInfo runtimeInfo = RuntimeInfo.FromFormValue(strRuntimeInfo);

            Lite<S> lite = (Lite<S>)ctx.Value;

            if (runtimeInfo.RuntimeType == null)
                return null;

            if (runtimeInfo.IsNew)
            {
                if (lite != null && lite.EntityOrNull != null && lite.EntityOrNull.IsNew)
                    return TryModifyEntity(ctx, lite);

                return TryModifyEntity(ctx, new Lite<S>((S)(IIdentifiable)Constructor.Construct(runtimeInfo.RuntimeType)));
            }

            if (lite == null)
                return TryModifyEntity(ctx, Database.RetrieveLite<S>(runtimeInfo.RuntimeType, runtimeInfo.IdOrNull.Value));

            if (runtimeInfo.IdOrNull.Value == lite.IdOrNull && runtimeInfo.RuntimeType == lite.RuntimeType)
                return TryModifyEntity(ctx, lite);

            return TryModifyEntity(ctx, Database.RetrieveLite<S>(runtimeInfo.RuntimeType, runtimeInfo.IdOrNull.Value));
        }

        public Lite<S> TryModifyEntity(MappingContext<Lite<S>> ctx, Lite<S> newLite)
        {
            if (!ctx.Inputs.Keys.Except(Mapping.specialProperties).Any())
                return newLite; // If form does not contains changes to the entity

            if (EntityMapping == null)
                throw new InvalidOperationException("Changes to Entity {0} are not allowed because EntityMapping is null".Formato(newLite.TryToString()));

            var sc = new SubContext<S>(ctx.ControlID, EntityMapping, null, ctx.PropertyRoute.Add("Entity"), ctx) { Value = newLite.Retrieve() };
            EntityMapping.OnGetValue(sc);

            ctx.AddChild(sc);

            if (sc.SupressChange)
                return newLite;

            return sc.Value.ToLite(sc.Value.IsNew);
        }

        public override void RecursiveValidation(MappingContext<Lite<S>> ctx)
        {
            if (EntityMapping != null && ctx.Value != null && ctx.Value.EntityOrNull != null)
            {
                if (ctx.FirstChild != null)
                    ctx.FirstChild.ValidateInternal();
            }
        }
    }

    public class MListMapping<S> : Mapping<MList<S>>
    {
        public Mapping<S> ElementMapping { get; set; }

        public MListMapping()
        {
            ElementMapping = Create<S>();
        }

        public IEnumerable<MappingContext<S>> GenerateItemContexts(MappingContext<MList<S>> ctx)
        {
            IList<string> inputKeys = (IList<string>)ctx.Inputs.Keys;

            PropertyRoute route = ctx.PropertyRoute.Add("Item"); 

            for (int i = 0; i < inputKeys.Count; i++)
            {
                string subControlID = inputKeys[i];

                if (specialProperties.Contains(subControlID))
                    continue;

                string index = subControlID.Substring(0, subControlID.IndexOf(TypeContext.Separator));

                SubContext<S> itemCtx = new SubContext<S>(TypeContextUtilities.Compose(ctx.ControlID, index), ElementMapping, null, route, ctx);

                yield return itemCtx;

                i += itemCtx.Inputs.Count - 1;
            }

        }

        protected internal override MList<S> DefaultGetValue(MappingContext<MList<S>> ctx)
        {
            MList<S> oldList = ctx.Value;

            MList<S> newList = new MList<S>();

            foreach (MappingContext<S> itemCtx in GenerateItemContexts(ctx))
            {
                Debug.Assert(!itemCtx.Empty());

                int? oldIndex = itemCtx.Inputs.TryGetC(EntityListBaseKeys.Index).ToInt();

                if (oldIndex.HasValue && oldList.Count > oldIndex.Value)
                    itemCtx.Value = oldList[oldIndex.Value];

                ElementMapping.OnGetValue(itemCtx);

                ctx.AddChild(itemCtx);
                if (itemCtx.Value != null)
                    newList.Add(itemCtx.Value);
            }
            return newList;
        }

        public MList<S> CorrelatedGetValue(MappingContext<MList<S>> ctx)
        {
            MList<S> list = ctx.Value;
            int i = 0;

            foreach (MappingContext<S> itemCtx in GenerateItemContexts(ctx).OrderBy(mc => mc.ControlID.Substring(mc.ControlID.LastIndexOf("_") + 1).ToInt().Value))
            {
                Debug.Assert(!itemCtx.Empty());

                itemCtx.Value = list[i];
                ElementMapping.OnGetValue(itemCtx);

                ctx.AddChild(itemCtx);
                list[i] = itemCtx.Value;

                i++;
            }

            return list;
        }

        public override void RecursiveValidation(MappingContext<MList<S>> ctx)
        {
            foreach (var ct in ctx.Children())
            {
                ct.ValidateInternal();
            }
        }
    }

    public class MListDictionaryMapping<S, K> : MListMapping<S>
        where S : ModifiableEntity
    {
        Func<S, K> GetKey;

        public string Route { get; set; }

        Mapping<K> keyPropertyMapping;
        public Mapping<K> KeyPropertyMapping
        {
            get { return keyPropertyMapping; }
            set { keyPropertyMapping = value; }
        }


        public MListDictionaryMapping(Func<S, K> getKey, string route)
        {
            this.GetKey = getKey;

            this.keyPropertyMapping = Create<K>();

            this.Route = route;
        }

        protected internal override MList<S> DefaultGetValue(MappingContext<MList<S>> ctx)
        {
            MList<S> list = ctx.Value;
            var dic = list.ToDictionary(GetKey);

            PropertyRoute route = ctx.PropertyRoute.Add("Item"); 

            foreach (MappingContext<S> itemCtx in GenerateItemContexts(ctx))
            {
                Debug.Assert(!itemCtx.Empty());

                SubContext<K> subContext = new SubContext<K>(TypeContextUtilities.Compose(itemCtx.ControlID, Route), keyPropertyMapping, null, route, itemCtx);

                keyPropertyMapping.OnGetValue(subContext);

                itemCtx.Value = dic[subContext.Value];
                ElementMapping.OnGetValue(itemCtx);

                ctx.AddChild(itemCtx);
            }

            return list;
        }
    }
}

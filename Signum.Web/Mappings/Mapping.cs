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
using Signum.Web;
#endregion

namespace Signum.Web
{
    public delegate T Mapping<T>(MappingContext<T> ctx);

    public static class Mapping
    {
        static Mapping()
        {
            MappingRepository<bool>.Mapping = GetValue(ctx => ParseHtmlBool(ctx.Input));
            MappingRepository<byte>.Mapping = GetValue(ctx => byte.Parse(ctx.Input));
            MappingRepository<sbyte>.Mapping = GetValue(ctx => sbyte.Parse(ctx.Input));
            MappingRepository<short>.Mapping = GetValue(ctx => short.Parse(ctx.Input));
            MappingRepository<ushort>.Mapping = GetValue(ctx => ushort.Parse(ctx.Input));
            MappingRepository<int>.Mapping = GetValue(ctx => int.Parse(ctx.Input));
            MappingRepository<uint>.Mapping = GetValue(ctx => uint.Parse(ctx.Input));
            MappingRepository<long>.Mapping = GetValue(ctx => long.Parse(ctx.Input));
            MappingRepository<ulong>.Mapping = GetValue(ctx => ulong.Parse(ctx.Input));
            MappingRepository<float>.Mapping = GetValue(ctx => ctx.PropertyRoute != null && ReflectionTools.IsPercentage(Reflector.FormatString(ctx.PropertyRoute), CultureInfo.CurrentCulture) ? (float)ReflectionTools.ParsePercentage(ctx.Input, typeof(float), CultureInfo.CurrentCulture): float.Parse(ctx.Input));
            MappingRepository<double>.Mapping = GetValue(ctx => ctx.PropertyRoute != null && ReflectionTools.IsPercentage(Reflector.FormatString(ctx.PropertyRoute), CultureInfo.CurrentCulture) ? (double)ReflectionTools.ParsePercentage(ctx.Input, typeof(double), CultureInfo.CurrentCulture) : double.Parse(ctx.Input));
            MappingRepository<decimal>.Mapping = GetValue(ctx => ctx.PropertyRoute != null && ReflectionTools.IsPercentage(Reflector.FormatString(ctx.PropertyRoute), CultureInfo.CurrentCulture) ? (decimal)ReflectionTools.ParsePercentage(ctx.Input, typeof(decimal), CultureInfo.CurrentCulture) : decimal.Parse(ctx.Input));
            MappingRepository<DateTime>.Mapping = GetValue(ctx => DateTime.Parse(ctx.Input).FromUserInterface());
            MappingRepository<Guid>.Mapping = GetValue(ctx => Guid.Parse(ctx.Input));
            MappingRepository<TimeSpan>.Mapping = GetValue(ctx => TimeSpan.Parse(ctx.Input));

            MappingRepository<bool?>.Mapping = GetValueNullable(ctx => ParseHtmlBool(ctx.Input));
            MappingRepository<byte?>.Mapping = GetValueNullable(ctx => byte.Parse(ctx.Input));
            MappingRepository<sbyte?>.Mapping = GetValueNullable(ctx => sbyte.Parse(ctx.Input));
            MappingRepository<short?>.Mapping = GetValueNullable(ctx => short.Parse(ctx.Input));
            MappingRepository<ushort?>.Mapping = GetValueNullable(ctx => ushort.Parse(ctx.Input));
            MappingRepository<int?>.Mapping = GetValueNullable(ctx => int.Parse(ctx.Input));
            MappingRepository<uint?>.Mapping = GetValueNullable(ctx => uint.Parse(ctx.Input));
            MappingRepository<long?>.Mapping = GetValueNullable(ctx => long.Parse(ctx.Input));
            MappingRepository<ulong?>.Mapping = GetValueNullable(ctx => ulong.Parse(ctx.Input));
            MappingRepository<float?>.Mapping = GetValueNullable(ctx => ctx.PropertyRoute != null && ReflectionTools.IsPercentage(Reflector.FormatString(ctx.PropertyRoute), CultureInfo.CurrentCulture) ? (float)ReflectionTools.ParsePercentage(ctx.Input, typeof(float), CultureInfo.CurrentCulture) : float.Parse(ctx.Input));
            MappingRepository<double?>.Mapping = GetValueNullable(ctx => ctx.PropertyRoute != null && ReflectionTools.IsPercentage(Reflector.FormatString(ctx.PropertyRoute), CultureInfo.CurrentCulture) ? (double)ReflectionTools.ParsePercentage(ctx.Input, typeof(double), CultureInfo.CurrentCulture) : double.Parse(ctx.Input));
            MappingRepository<decimal?>.Mapping = GetValueNullable(ctx => ctx.PropertyRoute != null && ReflectionTools.IsPercentage(Reflector.FormatString(ctx.PropertyRoute), CultureInfo.CurrentCulture) ? (decimal)ReflectionTools.ParsePercentage(ctx.Input, typeof(decimal), CultureInfo.CurrentCulture) : decimal.Parse(ctx.Input));
            MappingRepository<DateTime?>.Mapping = GetValueNullable(ctx => DateTime.Parse(ctx.Input).FromUserInterface());
            MappingRepository<Guid?>.Mapping = GetValueNullable(ctx => Guid.Parse(ctx.Input));
            MappingRepository<TimeSpan?>.Mapping = GetValueNullable(ctx => TimeSpan.Parse(ctx.Input));


            MappingRepository<string>.Mapping = ctx =>
            {
                if (ctx.Empty())
                    return ctx.None();

                return ctx.Input;
            };
        }

        public static void RegisterValue<T>(Mapping<T> mapping)
        {
            MappingRepository<T>.Mapping = mapping;
        }

        public static EntityMapping<T> AsEntityMapping<T>(this Mapping<T> mapping) where T : ModifiableEntity
        {
            return (EntityMapping<T>)mapping.Target;
        }

        public static AutoEntityMapping<T> AsAutoEntityMapping<T>(this Mapping<T> mapping) where T : class
        {
            return (AutoEntityMapping<T>)mapping.Target;
        }

        public static MListMapping<T> AsMListMapping<T>(this Mapping<MList<T>> mapping) where T : class
        {
            return (MListMapping<T>)mapping.Target;
        }

        internal static readonly string[] specialProperties = new[] 
        { 
            EntityBaseKeys.RuntimeInfo,
            EntityBaseKeys.ToStr, 
            EntityListBaseKeys.Index,
        };

        static GenericInvoker<Func<Delegate>> giForAutoEntity = new GenericInvoker<Func<Delegate>>(() => ForAutoEntity<IIdentifiable>());
        static Mapping<T> ForAutoEntity<T>() where T : class
        {
            return new AutoEntityMapping<T>().GetValue;
        }

        static GenericInvoker<Func<Delegate>> giForLite = new GenericInvoker<Func<Delegate>>(() => ForLite<IIdentifiable>());
        static Mapping<Lite<S>> ForLite<S>() where S : class, IIdentifiable
        {
            return new LiteMapping<S>().GetValue;
        }

        static GenericInvoker<Func<Delegate>> giForMList = new GenericInvoker<Func<Delegate>>(() => ForMList<int>());
        static Mapping<MList<S>> ForMList<S>()
        {
            return new MListMapping<S>().GetValue;
        }

        static GenericInvoker<Func<Delegate>> giForEnum = new GenericInvoker<Func<Delegate>>(() => ForEnum<DayOfWeek>());
        static Mapping<T> ForEnum<T>() where T : struct
        {
            return MappingRepository<T>.Mapping = GetValue<T>(ctx => (T)Enum.Parse(typeof(T), ctx.Input));
        }

        static GenericInvoker<Func<Delegate>> giForEnumNullable = new GenericInvoker<Func<Delegate>>(() => ForEnumNullable<DayOfWeek>());
        static Mapping<T?> ForEnumNullable<T>() where T : struct
        {
            return MappingRepository<T?>.Mapping = GetValueNullable<T>(ctx => (T)Enum.Parse(typeof(T), ctx.Input));
        }


        public static Mapping<T> ForValue<T>()
        {
            var result = MappingRepository<T>.Mapping;

            if (result != null)
                return result;

            if (typeof(T).UnNullify().IsEnum)
            {
                MappingRepository<T>.Mapping = (Mapping<T>)(typeof(T).IsNullable() ? giForEnumNullable : giForEnum).GetInvoker(typeof(T).UnNullify())();

                return MappingRepository<T>.Mapping;
            }

            return null;
        }

        static class MappingRepository<T>
        {
            public static Mapping<T> Mapping; 
        }

        public static Mapping<T> New<T>()
        {
            var result = ForValue<T>();
            if (result != null)
                return result;

            if (typeof(T).IsModifiableEntity() || typeof(T).IsIIdentifiable())
                return (Mapping<T>)giForAutoEntity.GetInvoker(typeof(T))(); ;

            if (typeof(T).IsLite())
                return (Mapping<T>)giForLite.GetInvoker(Reflector.ExtractLite(typeof(T)))();

            if (Reflector.IsMList(typeof(T)))
                return (Mapping<T>)giForMList.GetInvoker(typeof(T).ElementType())();

            return ctx => { throw new InvalidOperationException("No mapping implemented for {0}".Formato(typeof(T).TypeName())); };
        }

        static Mapping<T> GetValue<T>(Func<MappingContext, T> parse) where T : struct
        {
            return ctx =>
            {
                if (ctx.Empty())
                    return ctx.None();

                try
                {
                    return parse(ctx);
                }
                catch (FormatException)
                {
                    return ctx.None(ctx.PropertyPack != null ? Resources._0HasAnInvalidFormat.Formato(ctx.PropertyPack.PropertyInfo.NiceName()) : Resources.InvalidFormat);
                }
            };
        }

        static Mapping<T?> GetValueNullable<T>(Func<MappingContext, T> parse) where T : struct
        {
            return ctx =>
            {
                if (ctx.Empty())
                    return ctx.None();

                string input = ctx.Input;
                if (string.IsNullOrWhiteSpace(input))
                    return null;

                try
                {
                    return parse(ctx);
                }
                catch (FormatException)
                {
                    return ctx.None(ctx.PropertyPack != null ? Resources._0HasAnInvalidFormat.Formato(ctx.PropertyPack.PropertyInfo.NiceName()) : Resources.InvalidFormat);
                }
            };
        }

        public static bool ParseHtmlBool(string input)
        {
            string[] vals = input.Split(',');
            return vals[0] == "true" || vals[0] == "True";
        }

      
    }

    public abstract class BaseMapping<T>
    {
        public abstract T GetValue(MappingContext<T> mapping);

        public static implicit operator Mapping<T>(BaseMapping<T> mapping)
        {
            return mapping.GetValue;
        }
    }

    public class AutoEntityMapping<T> : BaseMapping<T> where T : class 
    {
        public Dictionary<Type, Delegate> AllowedMappings;

        public Mapping<R> RegisterMapping<R>(Mapping<R> mapping) where R : ModifiableEntity
        {
            if (AllowedMappings == null)
                AllowedMappings = new Dictionary<Type, Delegate>();

            AllowedMappings.Add(typeof(R), mapping);

            return mapping;
        }

        public override T GetValue(MappingContext<T> ctx)
        {
            if (ctx.Empty())
                return ctx.None();
            
            string strRuntimeInfo;
            Type runtimeType = ctx.Inputs.TryGetValue(EntityBaseKeys.RuntimeInfo, out strRuntimeInfo) ? 
                RuntimeInfo.FromFormValue(strRuntimeInfo).RuntimeType: 
                ctx.Value.TryCC(t=>t.GetType());

            if (runtimeType == null)
                return (T)(object)null;

            if (typeof(T) == runtimeType || typeof(T).IsEmbeddedEntity())
                return GetRuntimeValue<T>(ctx, ctx.PropertyRoute);

            return miGetRuntimeValue.GetInvoker(runtimeType)(this, ctx, PropertyRoute.Root(runtimeType));
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
            SubContext<R> sc = new SubContext<R>(ctx.ControlID, null, route, ctx) { Value = ctx.Value as R }; // If the type is different, the AutoEntityMapping has the current value but EntityMapping just null
            sc.Value = mapping(sc);
            ctx.SupressChange = sc.SupressChange;
            ctx.AddChild(sc);
            return sc.Value;
        }
    }

    public class EntityMapping<T>: BaseMapping<T> where T : ModifiableEntity
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
                Mapping = Signum.Web.Mapping.New<P>();
            }

            public override void SetProperty(MappingContext<T> parent)
            {
                SubContext<P> ctx = CreateSubContext(parent);

                try
                {
                    ctx.Value = Mapping(ctx);

                    if (!ctx.SupressChange)
                        SetValue(parent.Value, ctx.Value);
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

                SubContext<P> ctx = new SubContext<P>(newControlId, PropertyPack, route, parent);
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

        public override T GetValue(MappingContext<T> ctx)
        {
            if (ctx.Empty())
                return ctx.None();

            var val = GetEntity(ctx);

            if (val == ctx.Value)
                ctx.SupressChange = true;
            else
                ctx.Value = val;

            SetProperties(ctx);

            RecursiveValidation(ctx);

            return val;
        }

        public virtual void SetProperties(MappingContext<T> ctx)
        {
            foreach (PropertyMapping item in Properties.Values)
            {
                item.SetProperty(ctx);
            }
        }

        public virtual void RecursiveValidation(MappingContext<T> ctx)
        {
            ModifiableEntity entity = ctx.Value;
            foreach (MappingContext childCtx in ctx.Children())
            {
                string error = entity.PropertyCheck(childCtx.PropertyPack);
                if (error.HasText())
                    childCtx.Error.Add(error);
            }
        }

        public virtual T GetEntity(MappingContext<T> ctx)
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

            propertyMapping.Mapping = Mapping.New<P>();

            return this;
        }

        public EntityMapping<T> ReplaceProperty<P>(Expression<Func<T, P>> property, Func<Mapping<P>, Mapping<P>> replacer)
        {
            PropertyInfo pi = ReflectionTools.GetPropertyInfo(property);
            var pm = (PropertyMapping<P>)Properties[pi.Name];
            pm.Mapping = replacer(pm.Mapping);
            return this;
        }


        public EntityMapping<T> GetProperty<P>(Expression<Func<T, P>> property, Action<Mapping<P>> continuation)
        {
            PropertyInfo pi = ReflectionTools.GetPropertyInfo(property);
            continuation(((PropertyMapping<P>)Properties[pi.Name]).Mapping);
            return this;
        }

        public EntityMapping<T> SetProperty<P>(Expression<Func<T, P>> property, Mapping<P> mapping)
        {
            PropertyInfo pi = ReflectionTools.GetPropertyInfo(property);

            PropertyMapping<P> propertyMapping = (PropertyMapping<P>)Properties.GetOrCreate(pi.Name,
                () => new PropertyMapping<P>(Validator.GetOrCreatePropertyPack(typeof(T), pi.Name)));

            propertyMapping.Mapping = mapping;
            
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


    public class LiteMapping<S> where S : class, IIdentifiable
    {
        public Mapping<S> EntityMapping { get; set; }

        public LiteMapping()
        {
            EntityMapping = Mapping.New<S>();
        }

        public Lite<S> GetValue(MappingContext<Lite<S>> ctx)
        {
            if (ctx.Empty())
                return ctx.None();

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

            var sc = new SubContext<S>(ctx.ControlID, null, ctx.PropertyRoute.Add("Entity"), ctx) { Value = newLite.Retrieve() };
            sc.Value = EntityMapping(sc);

            ctx.AddChild(sc);

            if (sc.SupressChange)
                return newLite;

            return sc.Value.ToLite(sc.Value.IsNew);
        }
    }

    public abstract class BaseMListMapping<S> : BaseMapping<MList<S>>
    {
        public Mapping<S> ElementMapping { get; set; }

        public BaseMListMapping()
        {
            ElementMapping = Mapping.New<S>();
        }

        public IEnumerable<MappingContext<S>> GenerateItemContexts(MappingContext<MList<S>> ctx)
        {
            IList<string> inputKeys = (IList<string>)ctx.Inputs.Keys;

            PropertyRoute route = ctx.PropertyRoute.Add("Item");

            for (int i = 0; i < inputKeys.Count; i++)
            {
                string subControlID = inputKeys[i];

                if (Mapping.specialProperties.Contains(subControlID))
                    continue;

                string index = subControlID.Substring(0, subControlID.IndexOf(TypeContext.Separator));

                SubContext<S> itemCtx = new SubContext<S>(TypeContextUtilities.Compose(ctx.ControlID, index), null, route, ctx);

                yield return itemCtx;

                i += itemCtx.Inputs.Count - 1;
            }
        }

    }

    public class MListMapping<S> : BaseMListMapping<S>
    {
        public override MList<S> GetValue(MappingContext<MList<S>> ctx)
        {
            if (ctx.Empty())
                return ctx.None();

            MList<S> oldList = ctx.Value;

            MList<S> newList = new MList<S>();

            foreach (MappingContext<S> itemCtx in GenerateItemContexts(ctx))
            {
                Debug.Assert(!itemCtx.Empty());

                int? oldIndex = itemCtx.Inputs.TryGetC(EntityListBaseKeys.Index).ToInt();

                if (oldIndex.HasValue && oldList.Count > oldIndex.Value)
                    itemCtx.Value = oldList[oldIndex.Value];

                itemCtx.Value = ElementMapping(itemCtx);

                ctx.AddChild(itemCtx);
                if (itemCtx.Value != null)
                    newList.Add(itemCtx.Value);
            }
            return newList;
        }

    }

    public class MListCorrelatedMapping<S> : MListMapping<S>
    {
        public override MList<S> GetValue(MappingContext<MList<S>> ctx)
        {
            MList<S> list = ctx.Value;
            int i = 0;

            foreach (MappingContext<S> itemCtx in GenerateItemContexts(ctx).OrderBy(mc => mc.ControlID.Substring(mc.ControlID.LastIndexOf("_") + 1).ToInt().Value))
            {
                Debug.Assert(!itemCtx.Empty());

                itemCtx.Value = list[i];
                itemCtx.Value = ElementMapping(itemCtx);

                ctx.AddChild(itemCtx);
                list[i] = itemCtx.Value;

                i++;
            }

            return list;
        }
    }

    public class MListDictionaryMapping<S, K> : BaseMListMapping<S>
        where S : ModifiableEntity
    {
        Func<S, K> GetKey;

        public string Route { get; set; }

        public Mapping<K> KeyPropertyMapping{get;set;}
        
        public MListDictionaryMapping(Func<S, K> getKey, string route)
        {
            this.GetKey = getKey;

            this.KeyPropertyMapping = Mapping.New<K>();

            this.Route = route;
        }

        public override MList<S> GetValue(MappingContext<MList<S>> ctx)
        {
            if (ctx.Empty())
                return ctx.None();

            MList<S> list = ctx.Value;
            var dic = list.ToDictionary(GetKey);

            PropertyRoute route = ctx.PropertyRoute.Add("Item");

            foreach (MappingContext<S> itemCtx in GenerateItemContexts(ctx))
            {
                Debug.Assert(!itemCtx.Empty());

                SubContext<K> subContext = new SubContext<K>(TypeContextUtilities.Compose(itemCtx.ControlID, Route), null, route, itemCtx);

                subContext.Value = KeyPropertyMapping(subContext);

                itemCtx.Value = dic[subContext.Value];
                itemCtx.Value = ElementMapping(itemCtx);

                ctx.AddChild(itemCtx);
            }

            return list;
        }
    }
}

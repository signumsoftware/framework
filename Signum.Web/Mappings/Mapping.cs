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
using Microsoft.SqlServer.Types;
using Signum.Entities.Basics;
using System.Drawing;
using static Signum.Entities.UserAssets.SmartDateTimeFilterValueConverter;
using Signum.Entities.UserAssets;

namespace Signum.Web
{
    public delegate T Mapping<T>(MappingContext<T> ctx);

    public static class Mapping
    {
        public static Func<PropertyRoute, string> CanChange;

        public static void AssertCanChange(PropertyRoute route)
        {
            if (CanChange == null)
                return;

            string error = CanChange(route);

            if (error.HasText())
                throw new UnauthorizedAccessException(error);
        }

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
            MappingRepository<float>.Mapping = GetValue(ctx => ctx.PropertyRoute != null && ReflectionTools.IsPercentage(Reflector.FormatString(ctx.PropertyRoute), CultureInfo.CurrentCulture) ? (float)ReflectionTools.ParsePercentage(ctx.Input, typeof(float), CultureInfo.CurrentCulture) : float.Parse(ctx.Input));
            MappingRepository<double>.Mapping = GetValue(ctx => ctx.PropertyRoute != null && ReflectionTools.IsPercentage(Reflector.FormatString(ctx.PropertyRoute), CultureInfo.CurrentCulture) ? (double)ReflectionTools.ParsePercentage(ctx.Input, typeof(double), CultureInfo.CurrentCulture) : double.Parse(ctx.Input));
            MappingRepository<decimal>.Mapping = GetValue(ctx => ctx.PropertyRoute != null && ReflectionTools.IsPercentage(Reflector.FormatString(ctx.PropertyRoute), CultureInfo.CurrentCulture) ? (decimal)ReflectionTools.ParsePercentage(ctx.Input, typeof(decimal), CultureInfo.CurrentCulture) : decimal.Parse(ctx.Input));
            MappingRepository<DateTime>.Mapping = GetValue(ctx => DateTime.Parse(ctx.HasInput ? ctx.Input : ctx.Inputs["Date"] + " " + ctx.Inputs["Time"]).FromUserInterface());
            MappingRepository<Guid>.Mapping = GetValue(ctx => Guid.Parse(ctx.Input));
            MappingRepository<TimeSpan>.Mapping = GetValue(ctx =>
            {
                var dateFormatAttr = ctx.PropertyRoute.PropertyInfo.GetCustomAttribute<TimeSpanDateFormatAttribute>();
                if (dateFormatAttr != null)
                    return DateTime.ParseExact(ctx.Input, dateFormatAttr.Format, CultureInfo.CurrentCulture).TimeOfDay;
                else
                    return TimeSpan.Parse(ctx.Input);
            });
            MappingRepository<SqlHierarchyId>.Mapping = GetValue(ctx => SqlHierarchyId.Parse(ctx.Input));
            MappingRepository<ColorEmbedded>.Mapping = GetValue(ctx => ctx.Input.HasText() ? ColorEmbedded.FromRGBHex(ctx.Input) : null);

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
            MappingRepository<DateTime?>.Mapping = GetValue(ctx =>
            {
                var input = ctx.HasInput ? ctx.Input : " ".CombineIfNotEmpty(ctx.Inputs["Date"], ctx.Inputs["Time"]);



                if (input.HasText())
                {
                    DateTime dt;
                    if (DateTime.TryParse(input, out dt))
                        return dt.FromUserInterface();
                    else
                    {
                        var res = SmartDateTimeSpan.TryParse(input, allowSmart: true);
                        if (res is Result<SmartDateTimeSpan>.Success s)
                        {
                            dt = s.Value.ToDateTime();
                            return dt.FromUserInterface();

                        }
                    }
                }

                return (DateTime?)null;

                //return input.HasText() ? DateTime.Parse(input).FromUserInterface() : (DateTime?)null;
            });
            MappingRepository<Guid?>.Mapping = GetValueNullable(ctx => Guid.Parse(ctx.Input));
            MappingRepository<TimeSpan?>.Mapping = GetValue(ctx => 
            {
                if (ctx.Input.IsNullOrEmpty())
                    return (TimeSpan?)null;

                var dateFormatAttr = ctx.PropertyRoute.PropertyInfo.GetCustomAttribute<TimeSpanDateFormatAttribute>();
                if (dateFormatAttr != null)
                    return DateTime.ParseExact(ctx.Input, dateFormatAttr.Format, CultureInfo.CurrentCulture).TimeOfDay;
                else
                    return TimeSpan.Parse(ctx.Input);
            });

            MappingRepository<string>.Mapping = StringTrim;
        }

        public static Mapping<string> String = ctx =>
        {
            if (ctx.Empty())
                return ctx.None();

            return ctx.Input;
        };

        public static Mapping<string> StringTrim = ctx =>
        {
            if (ctx.Empty())
                return ctx.None();

            if (ctx.Input.HasText())
                return ctx.Input.Trim();

            return ctx.Input;
        };

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
            EntityComboKeys.Combo,
            EntityListBaseKeys.Index,
            EntityListBaseKeys.RowId,
            EntityListBaseKeys.List
        };

        static GenericInvoker<Func<Delegate>> giForAutoEntity = new GenericInvoker<Func<Delegate>>(() => ForAutoEntity<IEntity>());
        static Mapping<T> ForAutoEntity<T>() where T : class
        {
            return new AutoEntityMapping<T>().GetValue;
        }

        static GenericInvoker<Func<Delegate>> giForLite = new GenericInvoker<Func<Delegate>>(() => ForLite<IEntity>());
        static Mapping<Lite<S>> ForLite<S>() where S : class, IEntity
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

            if (typeof(T).IsModifiableEntity() || typeof(T).IsIEntity())
                return (Mapping<T>)giForAutoEntity.GetInvoker(typeof(T))(); ;

            if (typeof(T).IsLite())
                return (Mapping<T>)giForLite.GetInvoker(Lite.Extract(typeof(T)))();

            if (Reflector.IsMList(typeof(T)))
                return (Mapping<T>)giForMList.GetInvoker(typeof(T).ElementType())();

            return ctx => { throw new InvalidOperationException("No mapping implemented for {0}".FormatWith(typeof(T).TypeName())); };
        }

        static Mapping<T> GetValue<T>(Func<MappingContext, T> parse)
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
                    return ctx.None(ctx.PropertyValidator != null ? ValidationMessage._0HasAnInvalidFormat.NiceToString().FormatWith(ctx.PropertyValidator.PropertyInfo.NiceName()) : ValidationMessage.InvalidFormat.NiceToString());
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
                    return ctx.None(ctx.PropertyValidator != null ? ValidationMessage._0HasAnInvalidFormat.NiceToString().FormatWith(ctx.PropertyValidator.PropertyInfo.NiceName()) : ValidationMessage.InvalidFormat.NiceToString());
                }
            };
        }

        public static bool ParseHtmlBool(string input)
        {
            string[] vals = input.Split(',');
            return vals[0] == "true" || vals[0] == "True";
        }

        public static List<string> IndexPrefixes(this IDictionary<string, string> inputs)
        {
            return inputs.Keys
                .Where(k => k != EntityListBaseKeys.ListPresent && k != EntityListBaseKeys.List && k != EntityBaseKeys.ToStr)
                .Select(str => str.Substring(0, str.IndexOf(TypeContext.Separator)))
                .Distinct()
                .OrderBy(a => int.Parse(a))
                .ToList();
        }

        public static List<IDictionary<string, string>> IndexSubDictionaries(this IDictionary<string, string> inputs)
        {
            return inputs.IndexPrefixes().Select(pf => inputs.SubDictionary(pf)).ToList();
        }

        public static IDictionary<string, string> SubDictionary(this IDictionary<string, string> sortedList, string nameToAppend)
        {
            var csl = sortedList as ContextualSortedList<string>;

            string prefix = csl == null ? nameToAppend : csl.Prefix + nameToAppend;

            return new ContextualSortedList<string>(sortedList, prefix + TypeContext.Separator);
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

        public AutoEntityMapping<T> RegisterMapping<R>(EntityMapping<R> mapping) where R : ModifiableEntity
        {
            return RegisterMapping((Mapping<R>)mapping);
        }

        public AutoEntityMapping<T> RegisterMapping<R>(Mapping<R> mapping) where R : ModifiableEntity
        {
            if (AllowedMappings == null)
                AllowedMappings = new Dictionary<Type, Delegate>();

            AllowedMappings.Add(typeof(R), mapping);

            return this;
        }

        public bool DisambiguateRuntimeInfo { get; set; }

        public override T GetValue(MappingContext<T> ctx)
        {
            using (HeavyProfiler.LogNoStackTrace("GetValue", () => "AutoEntityMapping<{0}>".FormatWith(typeof(T).TypeName())))
            {
                if (ctx.Empty())
                    return ctx.None();

                Type entityType;
                entityType = GetRuntimeType(ctx);


                if (entityType == null)
                    return (T)(object)null;

                if (typeof(T) == entityType || typeof(T).IsEmbeddedEntity())
                    return GetRuntimeValue<T>(ctx, ctx.PropertyRoute);

                return miGetRuntimeValue.GetInvoker(entityType)(this, ctx, PropertyRoute.Root(entityType));
            }
        }

        private Type GetRuntimeType(MappingContext<T> ctx)
        {
            string strRuntimeInfo;
            if (ctx.Inputs.TryGetValue(EntityBaseKeys.RuntimeInfo, out strRuntimeInfo))
            {
                if (!DisambiguateRuntimeInfo)
                    return RuntimeInfo.FromFormValue(strRuntimeInfo)?.EntityType;
                else
                {
                    RuntimeInfo runtimeInfo = strRuntimeInfo.Split(',')
                        .Select(r => RuntimeInfo.FromFormValue(r))
                        .OrderBy(a => !a.ToLite().RefersTo((Entity)(object)ctx.Value))
                        .FirstEx();

                    return runtimeInfo?.EntityType;
                }
            }
            else
                return ctx.Value?.GetType();
        }

        static GenericInvoker<Func<AutoEntityMapping<T>, MappingContext<T>, PropertyRoute, T>> miGetRuntimeValue =
           new GenericInvoker<Func<AutoEntityMapping<T>, MappingContext<T>, PropertyRoute, T>>((aem, mc, pr) => aem.GetRuntimeValue<T>(mc, pr));
        public R GetRuntimeValue<R>(MappingContext<T> ctx, PropertyRoute route)
            where R : class, T
        {
            if (AllowedMappings != null && !AllowedMappings.ContainsKey(typeof(R)))
            {
                return (R)(object)ctx.None(ValidationMessage.Type0NotAllowed.NiceToString().FormatWith(typeof(R)));
            }

            Mapping<R> mapping = (Mapping<R>)(AllowedMappings?.TryGetC(typeof(R)) ?? Navigator.EntitySettings(typeof(R)).UntypedMappingLine);
            SubContext<R> sc = new SubContext<R>(ctx.Prefix, null, route, ctx) { Value = ctx.Value as R }; // If the type is different, the AutoEntityMapping has the current value but EntityMapping just null
            sc.Value = mapping(sc);
            ctx.SupressChange = sc.SupressChange;
            ctx.AddChild(sc);
            return sc.Value;
        }
    }

    interface IPropertyMapping<T>
    {
        IPropertyValidator PropertyValidator { get; }

        void SetProperty(MappingContext<T> parent);

        Type MixinType { get; }
    }

    interface IPropertyMapping<T, P> : IPropertyMapping<T>
    {
        Mapping<P> Mapping { get; set; }
    }

    class PropertyMapping<T, P> : IPropertyMapping<T, P>
        where T : ModifiableEntity
    {
        public int Order { get; internal set; }
        public IPropertyValidator PropertyValidator { get; private set; }

        readonly Func<T, P> GetValue;
        readonly Action<T, P> SetValue;

        public Mapping<P> Mapping { get; set; }

        public PropertyMapping(IPropertyValidator pv)
        {
            this.PropertyValidator = pv;
            GetValue = ReflectionTools.CreateGetter<T, P>(pv.PropertyInfo);
            SetValue = ReflectionTools.CreateSetter<T, P>(pv.PropertyInfo);
            Mapping = Signum.Web.Mapping.New<P>();
        }

        public void SetProperty(MappingContext<T> parent)
        {
            SubContext<P> ctx = CreateSubContext(parent);

            try
            {
                var old = ctx.Value;
                ctx.Value = Mapping(ctx);

                if (!ctx.SupressChange)
                {
                    if (!object.Equals(old, ctx.Value))
                        Signum.Web.Mapping.AssertCanChange(ctx.PropertyRoute);

                    SetValue(parent.Value, ctx.Value);
                }
            }
            catch (Exception e)
            {
                string error = e is FormatException ? ValidationMessage._0HasAnInvalidFormat.NiceToString() :
                    e is UnauthorizedAccessException ? e.Message :
                    ValidationMessage.NotPossibleToaAssign0.NiceToString();

                ctx.Error.Add(error.FormatWith(PropertyValidator.PropertyInfo.NiceName()));
            }

            if (!ctx.Empty())
                parent.AddChild(ctx);
        }

        public SubContext<P> CreateSubContext(MappingContext<T> parent)
        {
            string newPrefix = TypeContextUtilities.Compose(parent.Prefix, PropertyValidator.PropertyInfo.Name);
            PropertyRoute route = parent.PropertyRoute.Add(this.PropertyValidator.PropertyInfo);

            SubContext<P> ctx = new SubContext<P>(newPrefix, PropertyValidator, route, parent);
            if (parent.Value != null)
                ctx.Value = GetValue(parent.Value);
            return ctx;
        }


        public Type MixinType
        {
            get { return null; }
        }

        public override string ToString()
        {
            return this.PropertyValidator.PropertyInfo.PropertyName();
        }
    }

    class MixinPropertyMapping<T, M, P> : IPropertyMapping<T, P>
        where T : Entity
        where M : MixinEntity
    {
        public PropertyMapping<M, P> PropertyMapping;

        public MixinPropertyMapping(IPropertyValidator pv)
        {
            PropertyMapping = new PropertyMapping<M, P>(pv);
        }

        public void SetProperty(MappingContext<T> parent)
        {
            MixinContext<M> ctx = new MixinContext<M>(parent.PropertyRoute.Add(typeof(M)), parent);
            if (parent.Value != null)
                ctx.Value = parent.Value.Mixin<M>();

            PropertyMapping.SetProperty(ctx);
        }

        public IPropertyValidator PropertyValidator
        {
            get { return PropertyMapping.PropertyValidator; }
        }

        public Mapping<P> Mapping
        {
            get { return PropertyMapping.Mapping; }
            set { PropertyMapping.Mapping = value; }
        }

        public Type MixinType
        {
            get { return typeof(M); }
        }

        public override string ToString()
        {
            return "[" + typeof(M).TypeName() + "] " + this.PropertyValidator.PropertyInfo.PropertyName();
        }
    }

    public class EntityMapping<T> : BaseMapping<T> where T : ModifiableEntity
    {
        List<IPropertyMapping<T>> properties = new List<IPropertyMapping<T>>();

        public EntityMapping(bool fillProperties)
        {
            if (fillProperties)
            {
                properties.AddRange(Validator.GetPropertyValidators(typeof(T)).Values
                    .Where(pv => !pv.PropertyInfo.IsReadOnly())
                    .Select(pv => NewProperty(pv, null)));

                if (typeof(Entity).IsAssignableFrom(typeof(T)))
                {
                    foreach (var t in MixinDeclarations.GetMixinDeclarations(typeof(T)))
                    {
                        properties.AddRange(Validator.GetPropertyValidators(t).Values
                            .Where(pv => !pv.PropertyInfo.IsReadOnly())
                            .Select(pv => NewProperty(pv, t)));

                    }
                }
            }
        }

        static IPropertyMapping<T> NewProperty(PropertyInfo pi, Type mixinType)
        {
            return NewProperty(Validator.TryGetPropertyValidator(mixinType ?? typeof(T), pi.Name) ?? new PropertyValidator<T>(pi), mixinType);
        }

        static IPropertyMapping<T> NewProperty(IPropertyValidator pv, Type mixinType)
        {
            if (mixinType == null)
                return (IPropertyMapping<T>)Activator.CreateInstance(typeof(PropertyMapping<,>).MakeGenericType(typeof(T), pv.PropertyInfo.PropertyType), pv);
            else
                return (IPropertyMapping<T>)Activator.CreateInstance(typeof(MixinPropertyMapping<,,>).MakeGenericType(typeof(T), mixinType, pv.PropertyInfo.PropertyType), pv);
        }

        public override T GetValue(MappingContext<T> ctx)
        {
            using (HeavyProfiler.LogNoStackTrace("GetValue", () => "EntityMapping<{0}>".FormatWith(typeof(T).TypeName())))
            {
                if (ctx.Empty())
                    return ctx.None();

                var val = GetEntity(ctx);

                if (val == ctx.Value)
                    ctx.SupressChange = true;
                else
                    ctx.Value = val;

                SetValueProperties(ctx);

                return val;
            }
        }

        public virtual void SetValueProperties(MappingContext<T> ctx)
        {
            foreach (IPropertyMapping<T> item in properties)
            {
                item.SetProperty(ctx);
            }
        }

        public bool DesambiguateRuntimeInfo { get; set; }

        public virtual T GetEntity(MappingContext<T> ctx)
        {
            string strRuntimeInfo;
            if (!ctx.Inputs.TryGetValue(EntityBaseKeys.RuntimeInfo, out strRuntimeInfo))
                return ctx.Value; //I only have some ValueLines of an Entity (so no Runtime, Id or anything)

            RuntimeInfo runtimeInfo;
            if (!DesambiguateRuntimeInfo)
                runtimeInfo = RuntimeInfo.FromFormValue(strRuntimeInfo);
            else
            {
                runtimeInfo = strRuntimeInfo.Split(',')
                    .Select(r => RuntimeInfo.FromFormValue(r))
                    .OrderBy(a => !a?.ToLite().RefersTo((Entity)(object)ctx.Value))
                    .FirstEx();
            }

            if (runtimeInfo == null)
                return null;

            if (typeof(T).IsEmbeddedEntity())
            {
                if (runtimeInfo.IsNew || ctx.Value == null)
                    return new ConstructorContext(ctx.Controller).Construct<T>();

                return ctx.Value;
            }
            else if (typeof(T).IsModelEntity())
            {
                if (runtimeInfo.IsNew || ctx.Value == null)
                    return new ConstructorContext(ctx.Controller).Construct<T>();

                return ctx.Value;
            }
            else
            {
                Entity identifiable = (Entity)(ModifiableEntity)ctx.Value;

                var result = GetEntity(ctx.Controller, runtimeInfo, identifiable);

                if (result is Entity && runtimeInfo.Ticks != null)
                    ((Entity)(ModifiableEntity)result).ticks = runtimeInfo.Ticks.Value;

                return result;
            }
        }

        private static T GetEntity(ControllerBase controller,  RuntimeInfo runtimeInfo, Entity identifiable)
        {
            if (runtimeInfo.IsNew)
            {
                if (identifiable != null && identifiable.IsNew)
                    return (T)(ModifiableEntity)identifiable;
                else
                    return new ConstructorContext(controller).Construct<T>();
            }

            if (identifiable != null && runtimeInfo.IdOrNull == identifiable.IdOrNull && runtimeInfo.EntityType == identifiable.GetType())
                return (T)(ModifiableEntity)identifiable;
            else
                return (T)(ModifiableEntity)Database.Retrieve(runtimeInfo.EntityType, runtimeInfo.IdOrNull.Value);
        }


        public EntityMapping<T> CreateProperty<P>(Expression<Func<T, P>> property)
        {
            Type mixin;
            PropertyInfo pi = GetPropertyInfo(property, out mixin);

            var prop = (IPropertyMapping<T, P>)properties.SingleOrDefaultEx(p => ReflectionTools.PropertyEquals(p.PropertyValidator.PropertyInfo, pi) && p.MixinType == mixin);

            if (prop != null)
                throw new InvalidOperationException("{0} already registered".FormatWith(pi.Name));

            properties.Add(NewProperty(pi, mixin));

            return this;
        }

        public EntityMapping<T> ReplaceProperty<P>(Expression<Func<T, P>> property, Func<Mapping<P>, Mapping<P>> replacer)
        {
            Type mixin;
            PropertyInfo pi = GetPropertyInfo(property, out mixin);

            var prop = (IPropertyMapping<T, P>)properties.SingleEx(p => ReflectionTools.PropertyEquals(p.PropertyValidator.PropertyInfo, pi) && p.MixinType == mixin);

            prop.Mapping = replacer(prop.Mapping);

            return this;
        }

        public EntityMapping<T> GetProperty<P>(Expression<Func<T, P>> property, Action<Mapping<P>> continuation)
        {
            Type mixin;
            PropertyInfo pi = GetPropertyInfo(property, out mixin);

            var prop = (IPropertyMapping<T, P>)properties.SingleEx(p => ReflectionTools.PropertyEquals(p.PropertyValidator.PropertyInfo, pi) && p.MixinType == mixin);

            continuation(prop.Mapping);

            return this;
        }

        public EntityMapping<T> SetProperty<P>(Expression<Func<T, P>> property, Mapping<P> mapping)
        {
            Type mixin;
            PropertyInfo pi = GetPropertyInfo(property, out mixin);

            var prop = (IPropertyMapping<T, P>)properties.SingleOrDefaultEx(p => ReflectionTools.PropertyEquals(p.PropertyValidator.PropertyInfo, pi) && p.MixinType == mixin);

            if (prop == null)
            {
                prop = (IPropertyMapping<T, P>)NewProperty(pi, mixin);
                properties.Add(prop);
            }

            prop.Mapping = mapping;

            return this;
        }

        public EntityMapping<T> RemoveProperty<P>(Expression<Func<T, P>> property)
        {
            Type mixin;
            PropertyInfo pi = GetPropertyInfo(property, out mixin);

            var prop = properties.SingleEx(p => ReflectionTools.PropertyEquals(p.PropertyValidator.PropertyInfo, pi) && p.MixinType == mixin);

            properties.Remove(prop);

            return this;
        }

        public EntityMapping<T> ClearProperties()
        {
            properties.Clear();

            return this;
        }


        PropertyInfo GetPropertyInfo(LambdaExpression property, out Type mixin)
        {
            if (property == null)
                throw new ArgumentNullException("property");

            Expression body = property.Body;
            if (body.NodeType == ExpressionType.Convert)
                body = ((UnaryExpression)body).Operand;

            MemberExpression ex = body as MemberExpression;
            if (ex == null)
                throw new ArgumentException("The lambda 'property' should be an expression accessing a property");

            PropertyInfo pi = ex.Member as PropertyInfo;
            if (pi == null)
                throw new ArgumentException("The lambda 'property' should be an expression accessing a property");

            var mce = ex.Expression as MethodCallExpression;
            if (ex.Expression == property.Parameters.Only())
            {
                mixin = null;

                return pi;
            }
            else if (mce != null && mce.Method.IsInstantiationOf(MixinDeclarations.miMixin))
            {
                mixin = mce.Method.GetGenericArguments()[0];

                if (!MixinDeclarations.GetMixinDeclarations(typeof(T)).Contains(mixin))
                    throw new ArgumentException("The mixin {0} used in lambda 'property' is not registered".FormatWith(mixin.TypeName()));

                return pi;
            }
            else
                throw new ArgumentException("The lambda 'property' should be an expression accessing a property");
        }
    }

    public interface ILiteMapping
    {
        bool AvoidEntityMapping { get; set; }
    }

    public class LiteMapping<S>: ILiteMapping where S : class, IEntity
    {
        public bool AvoidEntityMapping { get; set; }
        public Mapping<S> EntityMapping { get; set; }

        public LiteMapping()
        {
            EntityMapping = Mapping.New<S>();

            EntityHasChanges = AnyNonSpecialImput;
        }

        public Lite<S> GetValue(MappingContext<Lite<S>> ctx)
        {
            using (HeavyProfiler.LogNoStackTrace("GetValue", () => "LiteMapping<{0}>".FormatWith(typeof(S).TypeName())))
            {
                if (ctx.Empty())
                    return ctx.None();

                var newLite = Change(ctx);
                if (newLite == ctx.Value)
                    ctx.SupressChange = true;

                return newLite;
            }
        }

        public Lite<S> Change(MappingContext<Lite<S>> ctx)
        {
            string strRuntimeInfo;
            if (!ctx.Inputs.TryGetValue(EntityBaseKeys.RuntimeInfo, out strRuntimeInfo)) //I only have some ValueLines of an Entity (so no Runtime, Id or anything)
                return TryModifyEntity(ctx, ctx.Value);

            RuntimeInfo runtimeInfo = RuntimeInfo.FromFormValue(strRuntimeInfo);

            Lite<S> lite = (Lite<S>)ctx.Value;

            if (runtimeInfo == null)
                return null;

            if (runtimeInfo.IsNew)
            {
                if (lite != null && lite.EntityOrNull != null && lite.EntityOrNull.IsNew)
                    return TryModifyEntity(ctx, lite);

                return TryModifyEntity(ctx, (Lite<S>)((Entity)new ConstructorContext(ctx.Controller).ConstructUntyped(runtimeInfo.EntityType)).ToLiteFat());
            }

            if (lite == null)
                return TryModifyEntity(ctx, (Lite<S>)Database.RetrieveLite(runtimeInfo.EntityType, runtimeInfo.IdOrNull.Value));

            if (runtimeInfo.IdOrNull.Value == lite.IdOrNull && runtimeInfo.EntityType == lite.EntityType)
                return TryModifyEntity(ctx, lite);

            return TryModifyEntity(ctx, (Lite<S>)Database.RetrieveLite(runtimeInfo.EntityType, runtimeInfo.IdOrNull.Value));
        }

        public Lite<S> TryModifyEntity(MappingContext<Lite<S>> ctx, Lite<S> lite)
        {
            //commented out because of Lite<FileEntity/FilePathEntity>
            if (AvoidEntityMapping || !EntityHasChanges(ctx))
                return lite; // If form does not contains changes to the entity

            if (EntityMapping == null)
                throw new InvalidOperationException("Changes to Entity {0} are not allowed because EntityMapping is null".FormatWith(lite?.ToString()));

            var sc = new SubContext<S>(ctx.Prefix, null, ctx.PropertyRoute.Add("Entity"), ctx) { Value = lite.Retrieve() };
            sc.Value = EntityMapping(sc);

            ctx.AddChild(sc);

            if (sc.SupressChange)
                return lite;

            return sc.Value.ToLite(sc.Value.IsNew);
        }

        public Func<MappingContext<Lite<S>>, bool> EntityHasChanges;

        private static bool AnyNonSpecialImput(MappingContext<Lite<S>> ctx)
        {
            return ctx.Inputs.Keys.Except(Mapping.specialProperties).Any();
        }
    }

    public abstract class BaseMListMapping<S> : BaseMapping<MList<S>>
    {
        public Mapping<S> ElementMapping { get; set; }

        public BaseMListMapping()
        {
            ElementMapping = Mapping.New<S>();
        }

        public BaseMListMapping(Mapping<S> elementMapping)
        {
            this.ElementMapping = elementMapping;
        }

        public BaseMListMapping<S> SetElementMapping(Mapping<S> elementMapping)
        {
            this.ElementMapping = elementMapping;
            return this;
        }

        public IEnumerable<MappingContext<S>> GenerateItemContexts(MappingContext<MList<S>> ctx)
        {
            PropertyRoute route = ctx.PropertyRoute.Add("Item");

            var indexPrefixes = ctx.Inputs.IndexPrefixes();

            foreach (var index in indexPrefixes.OrderBy(ip => (ctx.GlobalInputs.TryGetC(TypeContextUtilities.Compose(ctx.Prefix, ip, EntityListBaseKeys.Index)) ?? ip).ToInt()))
            {
                SubContext<S> itemCtx = new SubContext<S>(TypeContextUtilities.Compose(ctx.Prefix, index), null, route, ctx);

                yield return itemCtx;
            }
        }
    }

    public class MListMapping<S> : BaseMListMapping<S>
    {
        public Type RowIdType;

        public MListMapping()
            : base()
        {
        }

        public MListMapping(Mapping<S> elementMapping)
            : base(elementMapping)
        {
        }

        public override MList<S> GetValue(MappingContext<MList<S>> ctx)
        {
            using (HeavyProfiler.LogNoStackTrace("GetValue", () => "MListMapping<{0}>".FormatWith(typeof(S).TypeName())))
            {
                if (ctx.Empty())
                    return ctx.None();

                IMListPrivate<S> mlistPriv = ctx.Value;

                var dic = mlistPriv == null ? new Dictionary<PrimaryKey, MList<S>.RowIdElement>() :
                    mlistPriv.InnerList.Where(a => a.RowId.HasValue).ToDictionary(a => a.RowId.Value, a => a);

                var newList = new List<MList<S>.RowIdElement>();
                foreach (MappingContext<S> itemCtx in GenerateItemContexts(ctx))
                {
                    Debug.Assert(!itemCtx.Empty());

                    string rowIdString = (itemCtx.Inputs.ContainsKey(EntityListBaseKeys.RowId)? itemCtx.Inputs[EntityListBaseKeys.RowId]:null);  

                    if(rowIdString.HasText())
                    {
                        var rowId = new PrimaryKey((IComparable)ReflectionTools.Parse(rowIdString, GetRowIdType(ctx)));

                        var oldValue = dic.GetOrThrow(rowId, "No RowID {0} found");

                        itemCtx.Value = oldValue.Element;
                        itemCtx.Value = ElementMapping(itemCtx);

                        ctx.AddChild(itemCtx);

                        if (itemCtx.Value != null)
                        {
                            var val = itemCtx.SupressChange ? oldValue.Element : itemCtx.Value;

                            if (oldValue.Element.Equals(val))
                                newList.Add(new MList<S>.RowIdElement(val, rowId, oldValue.OldIndex));
                            else
                                newList.Add(new MList<S>.RowIdElement(val));
                        }
                    }
                    else
                    {
                        itemCtx.Value = ElementMapping(itemCtx);
                        ctx.AddChild(itemCtx);
                        if (itemCtx.Value != null && !itemCtx.SupressChange)
                            newList.Add(new MList<S>.RowIdElement(itemCtx.Value));
                    }
                }

                if (!AreEqual(newList, mlistPriv == null ? null : mlistPriv.InnerList))
                {
                    Signum.Web.Mapping.AssertCanChange(ctx.PropertyRoute);

                    if (ctx.Value == null)
                        mlistPriv = ctx.Value = new MList<S>();

                    var added = newList.Select(a=>a.Element).Except(mlistPriv.InnerList.Select(a=>a.Element)).ToList();
                    var removed = mlistPriv.InnerList.Select(a=>a.Element).Except(newList.Select(a=>a.Element)).ToList();

                    mlistPriv.InnerList.Clear();
                    mlistPriv.InnerList.AddRange(newList);
                    mlistPriv.InnerListModified(added, removed);
                }

                return ctx.Value;
            }
        }

        private Type GetRowIdType(MappingContext<MList<S>> ctx)
        {
            if(RowIdType != null)
                return RowIdType;

            return RowIdType = GetRowIdTypeFromSchema(ctx);
        }

        private static Type GetRowIdTypeFromSchema(MappingContext<MList<S>> ctx)
        {
            var tryField = ctx.PropertyRoute == null ? null : Schema.Current.TryField(ctx.PropertyRoute) as FieldMList;

            if (tryField == null)
                throw new InvalidOperationException("Impossible to determine RowIdType for {0}. Set it manually".FormatWith(typeof(MListMapping<S>).TypeName()));

            return tryField.TableMList.PrimaryKey.Type;
        }

        private bool AreEqual(List<MList<S>.RowIdElement> newList, List<MList<S>.RowIdElement> oldList)
        {
            if (newList.IsNullOrEmpty() && oldList.IsNullOrEmpty())
                return true;

            if (newList == null || oldList == null)
                return false;

            if (newList.Count != oldList.Count)
                return false;

            //Ordering the elements by RowId could remove some false modifications due to database indeterminism
            //but we can not be sure if order matters, and at the end the order from HTML should be respected
            for (int i = 0; i < newList.Count; i++)
            {
                if (newList[i].RowId != oldList[i].RowId ||
                   !object.Equals(newList[i].Element, oldList[i].Element))
                    return false;
            }

            return true;
        }

    }

    public class MListCorrelatedMapping<S> : MListMapping<S>
    {
        public MListCorrelatedMapping()
            : base()
        {
        }

        public MListCorrelatedMapping(Mapping<S> elementMapping)
            : base(elementMapping)
        {
        }

        public override MList<S> GetValue(MappingContext<MList<S>> ctx)
        {
            using (HeavyProfiler.LogNoStackTrace("GetValue", () => "MListCorrelatedMapping<{0}>".FormatWith(typeof(S).TypeName())))
            {
                MList<S> list = ctx.Value;
                int i = 0;

                foreach (MappingContext<S> itemCtx in GenerateItemContexts(ctx).OrderBy(mc => mc.Prefix.Substring(mc.Prefix.LastIndexOf("_") + 1).ToInt().Value))
                {
                    Debug.Assert(!itemCtx.Empty());

                    itemCtx.Value = list[i];
                    itemCtx.Value = ElementMapping(itemCtx);

                    ctx.AddChild(itemCtx);

                    if (!itemCtx.SupressChange && !object.Equals(list[i], itemCtx.Value))
                        Signum.Web.Mapping.AssertCanChange(ctx.PropertyRoute);

                    if (!list[i].Equals(itemCtx.Value))
                        list[i] = itemCtx.Value;

                    i++;
                }

                return list;
            }
        }
    }

    public class MListDictionaryMapping<S, K> : BaseMListMapping<S>
        where S : ModifiableEntity
    {
        MemberInfo[] MemberList;
        Func<S, K> GetKey;
        public Mapping<K> KeyMapping { get; set; }

        public Func<S, bool> FilterElements;

        public bool OnlyIfPossible;

        public MListDictionaryMapping(Expression<Func<S, K>> getKeyExpression)
            : this(getKeyExpression, Mapping.New<S>())
        {
          
        }

        public MListDictionaryMapping(Expression<Func<S, K>> getKeyExpression, Mapping<S> elementMapping)
            : base(elementMapping)
        {
            this.GetKey = getKeyExpression.Compile();

            var body = RemoveToLite(getKeyExpression.Body);

            this.MemberList = Reflector.GetMemberListBase(body);

            this.KeyMapping =  Mapping.New<K>();

            if (body != getKeyExpression.Body)
                ((ILiteMapping)this.KeyMapping.Target).AvoidEntityMapping = true;
        }

        private Expression RemoveToLite(Expression body)
        {
            if (body.Type.IsLite() && body.NodeType == ExpressionType.Call)
            {
                MethodCallExpression mce = body as MethodCallExpression;

                if (mce != null && mce.Method.Name.StartsWith("ToLite") & mce.Method.DeclaringType == typeof(Lite))
                    return mce.Arguments[0];
            }

            return body;
        }

        public override MList<S> GetValue(MappingContext<MList<S>> ctx)
        {
            using (HeavyProfiler.LogNoStackTrace("GetValue", () => "MListDictionaryMapping<{0}>".FormatWith(typeof(S).TypeName())))
            {
                if (ctx.Empty())
                    return ctx.None();

                MList<S> list = ctx.Value;

                var dic = (FilterElements == null ? list : list.Where(FilterElements)).ToDictionary(GetKey);

                PropertyRoute route = ctx.PropertyRoute.Add("Item").Continue(MemberList);

                string[] namesToAppend = MemberList.Select(MemberAccessGatherer.GetName).NotNull().ToArray();

                foreach (MappingContext<S> itemCtx in GenerateItemContexts(ctx))
                {
                    var tce = new TypeContextExpression(new PropertyInfo[0], typeof(S), itemCtx.PropertyRoute, itemCtx.Value);

                    SubContext<K> subContext = new SubContext<K>(TypeContextUtilities.Compose(itemCtx.Prefix, namesToAppend), null, route, itemCtx);

                    subContext.Value = KeyMapping(subContext);

                    if (!dic.ContainsKey(subContext.Value) && OnlyIfPossible)
                        continue;

                    itemCtx.Value = dic.GetOrThrow(subContext.Value);

                    itemCtx.Value = ElementMapping(itemCtx);

                    ctx.AddChild(itemCtx);
                }

                return list;
            }
        }
    }
}

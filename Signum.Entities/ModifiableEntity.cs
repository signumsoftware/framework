using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using System.ComponentModel;
using System.Reflection;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Specialized;
using Signum.Utilities.Reflection;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;
using Signum.Entities.Reflection;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using Signum.Utilities.ExpressionTrees;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;

namespace Signum.Entities
{
    [Serializable, DescriptionOptions(DescriptionOptions.Members | DescriptionOptions.Description), InTypeScript(false)]
    public abstract class ModifiableEntity : Modifiable, INotifyPropertyChanged, IDataErrorInfo, ICloneable
    {
        static Func<bool> isRetrievingFunc = null;
        static public bool IsRetrieving
        {
            get { return isRetrievingFunc != null && isRetrievingFunc(); }
        }

        internal static void SetIsRetrievingFunc(Func<bool> isRetrievingFunc)
        {
            ModifiableEntity.isRetrievingFunc = isRetrievingFunc;
        }

        protected internal const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        protected virtual T Get<T>(T fieldValue, [CallerMemberName]string automaticPropertyName = null)
        {
            return fieldValue;
        }

      
        protected virtual bool Set<T>(ref T field, T value, [CallerMemberName]string automaticPropertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            PropertyInfo pi = GetPropertyInfo(automaticPropertyName);

            if (pi == null)
                throw new ArgumentException("No PropertyInfo with name {0} found in {1} or any implemented interface".FormatWith(automaticPropertyName, this.GetType().TypeName()));

            if (value is IMListPrivate && !((IMListPrivate)value).IsNew && !object.ReferenceEquals(value, field))
                throw new InvalidOperationException("Only MList<T> with IsNew = true can be assigned to an entity");

            if (field is INotifyCollectionChanged col)
            {
                if (AttributeManager<NotifyCollectionChangedAttribute>.FieldContainsAttribute(GetType(), pi))
                    col.CollectionChanged -= ChildCollectionChanged;

                if (AttributeManager<NotifyChildPropertyAttribute>.FieldContainsAttribute(GetType(), pi))
                    foreach (ModifiableEntity item in (IEnumerable)col)
                        item.SetParentEntity(null);
            }

            if ((object)field is ModifiableEntity mod)
            {
                if (AttributeManager<NotifyChildPropertyAttribute>.FieldContainsAttribute(GetType(), pi))
                    mod.SetParentEntity(null);
            }

            SetSelfModified();
            field = value;

            col = field as INotifyCollectionChanged;
            if (col != null)
            {
                if (AttributeManager<NotifyCollectionChangedAttribute>.FieldContainsAttribute(GetType(), pi))
                    col.CollectionChanged += ChildCollectionChanged;

                if (AttributeManager<NotifyChildPropertyAttribute>.FieldContainsAttribute(GetType(), pi))
                    foreach (ModifiableEntity item in (IEnumerable)col)
                        item.SetParentEntity(this);
            }

            mod = field as ModifiableEntity;
            if (mod != null)
            {
                if (AttributeManager<NotifyChildPropertyAttribute>.FieldContainsAttribute(GetType(), pi))
                    mod.SetParentEntity(this);
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
            public override bool Equals(object obj) => obj is PropertyKey && Equals((PropertyKey)obj);
            public override int GetHashCode() => Type.GetHashCode() ^ PropertyName.GetHashCode();
        }

        static ConcurrentDictionary<PropertyKey, PropertyInfo> PropertyCache = new ConcurrentDictionary<PropertyKey, PropertyInfo>();

        protected PropertyInfo GetPropertyInfo(string propertyName)
        {
            return PropertyCache.GetOrAdd(new PropertyKey(this.GetType(), propertyName), key =>
                key.Type.GetProperty(propertyName, flags) ??
                 key.Type.GetInterfaces().Select(i => i.GetProperty(key.PropertyName, flags)).NotNull().FirstOrDefault());
        }

        static Expression<Func<ModifiableEntity, string>> ToStringPropertyExpression = m => m.ToString();
        [HiddenProperty, ExpressionField("ToStringPropertyExpression")]
        public string ToStringProperty
        {
            get
            {
                string str = ToString();
                return str.HasText() ? str : this.GetType().NiceName();
            }
        }

        #region Collection Events

        protected internal override void PostRetrieving()
        {
            RebindEvents();
        }

        protected virtual void RebindEvents()
        {
            foreach (INotifyCollectionChanged notify in AttributeManager<NotifyCollectionChangedAttribute>.FieldsWithAttribute(this))
            {
                if (notify == null)
                    continue;
             
                notify.CollectionChanged += ChildCollectionChanged;
            }

            foreach (object field in AttributeManager<NotifyChildPropertyAttribute>.FieldsWithAttribute(this))
            {
                if (field == null)
                    continue;

                if (field is ModifiableEntity entity)
                    entity.SetParentEntity(this);
                else
                {
                    foreach (ModifiableEntity item in (IEnumerable)field)
                        item.SetParentEntity(this);
                }
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            RebindEvents();
        }

        protected virtual void ChildCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            string propertyName = AttributeManager<NotifyCollectionChangedAttribute>.FindPropertyName(this, sender);
            if (propertyName != null)
                NotifyPrivate(propertyName); 

            if (AttributeManager<NotifyChildPropertyAttribute>.FieldsWithAttribute(this).Contains(sender))
            {
                if (args.NewItems != null)
                {
                    foreach (var p in args.NewItems.Cast<ModifiableEntity>())
                        p.SetParentEntity(this);
                }

                if (args.OldItems != null)
                {
                    foreach (var p in args.OldItems.Cast<ModifiableEntity>())
                        p.SetParentEntity(this);
                }
            }
        }

        protected virtual void ChildPropertyChanged(object sender, PropertyChangedEventArgs e)
        {

        }

        protected virtual string ChildPropertyValidation(ModifiableEntity sender, PropertyInfo pi)
        {
            return null;
        }
        #endregion

        [field: NonSerialized, Ignore]
        public event PropertyChangedEventHandler PropertyChanged;

        [NonSerialized, Ignore]
        ModifiableEntity parentEntity;

        public ModifiableEntity GetParentEntity()
        {
            return parentEntity;
        }

        private void SetParentEntity(ModifiableEntity p)
        {
            if (p != null && this.parentEntity != null && this.parentEntity != p)
                throw new InvalidOperationException($"'{nameof(parentEntity)}' is still connected to '{parentEntity}'");

            this.parentEntity = p;
        }

        internal string OnParentChildPropertyValidation(PropertyInfo pi)
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
            var parent = this.GetParentEntity();
            if (parent != null)
                parent.ChildPropertyChanged(this, new PropertyChangedEventArgs(propertyName));

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        }

        
        #region Temporal ID
        [Ignore]
        internal Guid temporalId = Guid.NewGuid();

        internal ModifiableEntity()
        {
        }

        public override int GetHashCode()
        {
            return GetType().FullName.GetHashCode() ^ temporalId.GetHashCode();
        }
        #endregion

        #region IDataErrorInfo Members
        [HiddenProperty]
        public string Error
        {
            get { return IntegrityCheck()?.Errors.Values.ToString("\r\n"); }
        }

        public IntegrityCheck IntegrityCheck()
        {
            using (var log = HeavyProfiler.LogNoStackTrace("IntegrityCheck"))
            {
                var validators = Validator.GetPropertyValidators(GetType());

                Dictionary<string, string> dic = null;

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

        //override for per-property checks
        [HiddenProperty]
        string IDataErrorInfo.this[string columnName]
        {
            get
            {
                if (columnName == null)
                    return ((IDataErrorInfo)this).Error;
                else
                    return PropertyCheck(columnName);
            }
        }

        public string PropertyCheck(Expression<Func<object>> property)
        {
            return PropertyCheck(ReflectionTools.GetPropertyInfo(property).Name);
        }

        public string PropertyCheck(string propertyName) 
        {
            IPropertyValidator pp = Validator.TryGetPropertyValidator(GetType(), propertyName);

            if (pp == null)
                return null; //Hidden properties

            return pp.PropertyCheck(this);
        }

        protected internal virtual string PropertyValidation(PropertyInfo pi)
        {
            return null;
        }

        protected static void Validate<T>(Expression<Func<T, object>> property, Func<T, PropertyInfo, string> validate) where T : ModifiableEntity
        {
            Validator.PropertyValidator(property).StaticPropertyValidation += validate;
        }

        public Dictionary<Guid, IntegrityCheck> FullIntegrityCheck()
        {
            var graph = GraphExplorer.FromRoot(this);
            return GraphExplorer.FullIntegrityCheck(graph);
        }

        protected static string NicePropertyName<R>(Expression<Func<R>> property)
        {
            return ReflectionTools.GetPropertyInfo(property).NiceName();
        }

        [ForceEagerEvaluation]
        protected static string NicePropertyName<E, R>(Expression<Func<E, R>> property)
        {
            return ReflectionTools.GetPropertyInfo(property).NiceName();
        }

        #endregion

        #region ICloneable Members
        object ICloneable.Clone()
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream())
            {
                bf.Serialize(stream, this);
                stream.Seek(0, SeekOrigin.Begin);
                return bf.Deserialize(stream);
            }
        }

        #endregion


        [Ignore]
        internal Dictionary<string, string> temporalErrors;
        internal void SetTemporalErrors(Dictionary<string, string> errors)
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
    }

    [Serializable]
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
            return $"{Errors.Count} errors in {" ".CombineIfNotEmpty(Type.Name, Id)}\r\n"
                  + Errors.ToString(kvp => "    {0}: {1}".FormatWith(kvp.Key, kvp.Value), "\r\n");
        }
    }
}

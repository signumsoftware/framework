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

namespace Signum.Entities
{
    [Serializable, DebuggerTypeProxy(typeof(FlattenHierarchyProxy)), DescriptionOptions(DescriptionOptions.Members | DescriptionOptions.Description)]
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

        protected virtual T Get<T>(T fieldValue, [CallerMemberNameAttribute]string automaticPropertyName = null)
        {
            return fieldValue;
        }

        protected virtual bool Set<T>(ref T field, T value, [CallerMemberNameAttribute]string automaticPropertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            PropertyInfo pi = this.GetType().GetProperty(automaticPropertyName, flags) ?? this.GetType().GetInterfaces().Select(i => i.GetProperty(automaticPropertyName, flags)).NotNull().FirstOrDefault();

            if (pi == null)
                throw new ArgumentException("No PropertyInfo with name {0} found in {1} or any implemented interface".FormatWith(automaticPropertyName, this.GetType().TypeName()));

            if (value is IMListPrivate && !((IMListPrivate)value).IsNew && !object.ReferenceEquals(value, field))
                throw new InvalidOperationException("Only MList<T> with IsNew = true can be assigned to an entity");

            INotifyCollectionChanged col = field as INotifyCollectionChanged;
            if (col != null)
            {
                if (AttributeManager<NotifyCollectionChangedAttribute>.FieldContainsAttribute(GetType(), pi))
                    col.CollectionChanged -= ChildCollectionChanged;

                if (AttributeManager<NotifyChildPropertyAttribute>.FieldContainsAttribute(GetType(), pi))
                    foreach (INotifyPropertyChanged item in (IEnumerable)col)
                        item.PropertyChanged -= ChildPropertyChanged;

                if (AttributeManager<ValidateChildPropertyAttribute>.FieldContainsAttribute(GetType(), pi))
                    foreach (ModifiableEntity item in (IEnumerable)col)
                        item.ExternalPropertyValidation -= ChildPropertyValidation;
            }

            ModifiableEntity mod = field as ModifiableEntity;
            if (mod != null)
            {
                if (AttributeManager<NotifyChildPropertyAttribute>.FieldContainsAttribute(GetType(), pi))
                    mod.PropertyChanged -= ChildPropertyChanged;

                if (AttributeManager<ValidateChildPropertyAttribute>.FieldContainsAttribute(GetType(), pi))
                    mod.ExternalPropertyValidation -= ChildPropertyValidation;
            }

            SetSelfModified();
            field = value;

            col = field as INotifyCollectionChanged;
            if (col != null)
            {
                if (AttributeManager<NotifyCollectionChangedAttribute>.FieldContainsAttribute(GetType(), pi))
                    col.CollectionChanged += ChildCollectionChanged;

                if (AttributeManager<NotifyChildPropertyAttribute>.FieldContainsAttribute(GetType(), pi))
                    foreach (INotifyPropertyChanged item in (IEnumerable)col)
                        item.PropertyChanged += ChildPropertyChanged;

                if (AttributeManager<ValidateChildPropertyAttribute>.FieldContainsAttribute(GetType(), pi))
                    foreach (ModifiableEntity item in (IEnumerable)col)
                        item.ExternalPropertyValidation += ChildPropertyValidation;
            }

            mod = field as ModifiableEntity;
            if (mod != null)
            {
                if (AttributeManager<NotifyChildPropertyAttribute>.FieldContainsAttribute(GetType(), pi))
                    mod.PropertyChanged += ChildPropertyChanged;

                if (AttributeManager<ValidateChildPropertyAttribute>.FieldContainsAttribute(GetType(), pi))
                    mod.ExternalPropertyValidation += ChildPropertyValidation;
            }

            NotifyPrivate(pi.Name);
            NotifyPrivate("Error");
            ClearTemporalError(pi.Name);

            return true;
        }

        static readonly Expression<Func<ModifiableEntity, string>> ToStringPropertyExpression = m => m.ToString();
        [HiddenProperty]
        public string ToStringProperty
        {
            get
            {
                string str = ToString();
                return str.HasText() ? str : this.GetType().NiceName();
            }
        }

        protected bool SetToStr<T>(ref T field, T value, [CallerMemberNameAttribute]string automaticPropertyName = null)
        {
            if (this.Set(ref field, value, automaticPropertyName))
            {
                NotifyToString();
                return true;
            }
            return false;
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

                var entity = field as ModifiableEntity;
                if (entity != null)
                    entity.PropertyChanged += ChildPropertyChanged;
                else
                {
                    foreach (INotifyPropertyChanged item in (IEnumerable)field)
                        item.PropertyChanged += ChildPropertyChanged;
                }
            }

            foreach (object field in AttributeManager<ValidateChildPropertyAttribute>.FieldsWithAttribute(this))
            {
                if (field == null)
                    continue;

                var entity = field as ModifiableEntity;
                if (entity != null)
                    entity.ExternalPropertyValidation += ChildPropertyValidation;
                else
                {
                    foreach (ModifiableEntity item in (IEnumerable)field)
                        item.ExternalPropertyValidation += ChildPropertyValidation;
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
                    foreach (var p in args.NewItems.Cast<INotifyPropertyChanged>()) p.PropertyChanged += ChildPropertyChanged;
                if (args.OldItems != null)
                    foreach (var p in args.OldItems.Cast<INotifyPropertyChanged>()) p.PropertyChanged -= ChildPropertyChanged;
            }

            if (AttributeManager<ValidateChildPropertyAttribute>.FieldsWithAttribute(this).Contains(sender))
            {
                if (args.NewItems != null)
                    foreach (var p in args.NewItems.Cast<ModifiableEntity>()) p.ExternalPropertyValidation += ChildPropertyValidation;
                if (args.OldItems != null)
                    foreach (var p in args.OldItems.Cast<ModifiableEntity>()) p.ExternalPropertyValidation -= ChildPropertyValidation;
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

        [field: NonSerialized, Ignore]
        public event Func<ModifiableEntity, PropertyInfo, string> ExternalPropertyValidation;

        internal string OnExternalPropertyValidation(PropertyInfo pi)
        {
            if (ExternalPropertyValidation == null)
                return null;

            return ExternalPropertyValidation(this, pi);
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
            var handler = PropertyChanged;

            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
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
            get { return IntegrityCheck()?.Values.ToString("\r\n"); }
        }

        public Dictionary<string, string> IntegrityCheck()
        {
            using (var log = HeavyProfiler.LogNoStackTrace("IntegrityCheck"))
            {
                var validators = Validator.GetPropertyValidators(GetType());

                Dictionary<string, string> result = null;

                foreach (var pv in validators.Values)
                {
                    var error = pv.PropertyCheck(this);

                    if (error != null)
                    {
                        if (result == null)
                            result = new Dictionary<string, string>();

                        result.Add(pv.PropertyInfo.Name, error);
                    }
                }

                return result;
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

        public Dictionary<Guid, Dictionary<string, string>> FullIntegrityCheck()
        {
            var graph = GraphExplorer.FromRoot(this);
            return GraphExplorer.FullIntegrityCheck(graph);
        }

        protected static string NicePropertyName<R>(Expression<Func<R>> property)
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

    //Based on: http://blogs.msdn.com/b/jaredpar/archive/2010/02/19/flattening-class-hierarchies-when-debugging-c.aspx
    internal sealed class FlattenHierarchyProxy
    {
        [DebuggerDisplay("{Value}", Name = "{Name,nq}", Type = "{TypeName,nq}")]
        internal struct Member
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            internal string Name;
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            internal object Value;
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            internal Type Type;
            internal Member(string name, object value, Type type)
            {
                Name = name;
                Value = value;
                Type = type;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            internal string TypeName
            {
                get { return Type.TypeName(); }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly object target;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Member[] memberList;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        internal Member[] Items
        {
            get
            {
                if (memberList == null)
                {
                    memberList = BuildMemberList().ToArray();
                }
                return memberList;
            }
        }

        public FlattenHierarchyProxy(object target)
        {
            this.target = target;
        }

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private List<Member> BuildMemberList()
        {
            var list = new List<Member>();
            if (target == null)
                return list;
         
            var type = target.GetType();
            list.Add(new Member("Type", type, typeof(Type)));

            foreach (var t in type.Follow(t => t.BaseType).TakeWhile(t => t != typeof(ModifiableEntity) && t != typeof(Modifiable) && t != typeof(MixinEntity)).Reverse())
            {
                foreach (var fi in t.GetFields(flags).Where(fi => fi.Name != "mixin").OrderBy(f => f.MetadataToken))
                {
                    object value = fi.GetValue(target);
                    list.Add(new Member(fi.Name, value, fi.FieldType));
                }
            }

            Entity ident = target as Entity;

            if (ident != null)
            {
                foreach (var m in ident.Mixins)
                {
                    list.Add(new Member(m.GetType().Name, m, m.GetType()));
                }
            }

            return list;
        }
    }
}

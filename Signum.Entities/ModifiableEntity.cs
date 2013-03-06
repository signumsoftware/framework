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

namespace Signum.Entities
{
    [Serializable, DebuggerTypeProxy(typeof(FlattenHierarchyProxy))]
    public abstract class ModifiableEntity : Modifiable, INotifyPropertyChanged, IDataErrorInfo, ICloneable
    {
        protected virtual bool Set<T>(ref T field, T value, Expression<Func<T>> property)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            PropertyInfo pi = ReflectionTools.BasePropertyInfo(property);

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

        protected bool SetToStr<T>(ref T field, T value, Expression<Func<T>> property)
        {
            if (this.Set(ref field, value, property))
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

        protected void Notify<T>(Expression<Func<T>> property)
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

        static long temporalIdCounter = 0;

        #region Temporal ID
        [Ignore]
        internal int temporalId;

        internal ModifiableEntity()
        {
            temporalId = unchecked((int)Interlocked.Increment(ref temporalIdCounter));
        }

        public override int GetHashCode()
        {
            return GetType().FullName.GetHashCode() ^ temporalId;
        }
        #endregion

        #region IDataErrorInfo Members
        [HiddenProperty]
        public string Error
        {
            get { return IntegrityCheck(); }
        }

        public string IntegrityCheck()
        {
            using (var log = HeavyProfiler.LogNoStackTrace("IntegrityCheck"))
            {
                var validators = Validator.GetPropertyValidators(GetType());

                StringBuilder sb = null;

                foreach (IPropertyValidator pv in validators.Values)
                {
                    log.Switch("PropertyCheck", pv.PropertyInfo.Name);
                    string error = pv.PropertyCheck(this);

                    if (error != null)
                    {
                        if (sb == null)
                            sb = new StringBuilder();
                        else
                            sb.Append("\r\n");
                        
                        sb.Append(error);
                    }
                }

                if (sb == null)
                    return null;

                return sb.ToString();
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

        public string FullIntegrityCheck()
        {
            var graph = GraphExplorer.FromRoot(this);
            return GraphExplorer.FullIntegrityCheck(graph, withIndependentEmbeddedEntities: !(this is IdentifiableEntity));
        }

        public Dictionary<ModifiableEntity, string> FullIntegrityCheckDictionary()
        {
            var graph = GraphExplorer.FromRoot(this);
            return GraphExplorer.IntegrityDictionary(graph);
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

        private List<Member> BuildMemberList()
        {
            var list = new List<Member>();
            if (target == null)
                return list;

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var type = target.GetType();
            list.Add(new Member("Type", type, typeof(Type)));

            foreach (var t in type.FollowC(t => t.BaseType).TakeWhile(t=> t!= typeof(ModifiableEntity) && t!= typeof(Modifiable)).Reverse())
            {
                foreach (var fi in t.GetFields(flags).OrderBy(f => f.MetadataToken))
                {
                    object value = fi.GetValue(target);
                    list.Add(new Member(fi.Name, value, fi.FieldType));
                }
            }

            return list;
        }
    }
}

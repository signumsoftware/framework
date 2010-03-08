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

namespace Signum.Entities
{
    [Serializable]
    public abstract class ModifiableEntity : Modifiable, INotifyPropertyChanged, IDataErrorInfo, ICloneable
    {
        [Ignore]
        protected bool selfModified = true;

        [HiddenProperty]
        public override bool SelfModified
        {
            get { return selfModified; }
            internal set { selfModified = value; }
        }

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

            selfModified = true;
            NotifyPrivate(pi.Name);
            NotifyPrivate("Error");

            return true;
        }

        [HiddenProperty]
        public string ToStringMethod
        {
            get
            {
                string str = ToString();
                return str.HasText() ? str : this.GetType().NiceName();
            }
        }

        public bool SetToStr<T>(ref T field, T value, Expression<Func<T>> property)
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

        protected virtual string ChildPropertyValidation(ModifiableEntity sender, PropertyInfo pi, object propertyValue)
        {
            return null;
        }
        #endregion

        [field: NonSerialized, Ignore]
        public event PropertyChangedEventHandler PropertyChanged;

        [field: NonSerialized, Ignore]
        public event PropertyValidationEventHandler ExternalPropertyValidation;

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
            NotifyPrivate("ToStringMethod");
        }

        void NotifyPrivate(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
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

        //override for full entitity integrity check. Remember to call base. 
        public string IntegrityCheck()
        {
            return Validator.GetPropertyPacks(GetType()).Select(k => PropertyCheck(k.Value)).NotNull().ToString("\r\n");
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
                {
                    PropertyPack pp = Validator.GetOrCreatePropertyPack(GetType(), columnName);
                    if (pp == null)
                        return null; //Hidden properties

                    return PropertyCheck(pp);
                }
            }
        }

        public string PropertyCheck<T, S>(Expression<Func<T, S>> property) where T : ModifiableEntity
        {
            return PropertyCheck(Validator.GetOrCreatePropertyPack(property));
        }

        public string PropertyCheck(string propertyName) 
        {
            return PropertyCheck(Validator.GetOrCreatePropertyPack(GetType(), propertyName));
        }
       
        public string PropertyCheck(PropertyPack pp)
        {
            if (pp.DoNotValidate)
                return null;

            object propertyValue = pp.GetValue(this);

            //ValidatorAttributes
            foreach (var validator in pp.Validators)
            {
                string result = validator.Error(pp.PropertyInfo, propertyValue);
                if (result != null)
                    return result.Formato(pp.PropertyInfo.NiceName());
            }

            //Internal Validation
            if (!pp.SkipPropertyValidation)
            {
                string result = PropertyValidation(pp.PropertyInfo);
                if (result != null)
                    return result;
            }

            //External Validation
            if (!pp.SkipExternalPropertyValidation && ExternalPropertyValidation != null)
            {
                string result = ExternalPropertyValidation(this, pp.PropertyInfo, propertyValue);
                if (result != null)
                    return result;
            }

            //Static validation
            if (pp.StaticPropertyValidation != null)
            {
                string result = pp.StaticPropertyValidation(this, pp.PropertyInfo, propertyValue);
                if (result != null)
                    return result;
            }

            return null;
        }

        protected virtual string PropertyValidation(PropertyInfo pi)
        {
            return null;
        }

        public string FullIntegrityCheck()
        {
            return GraphExplorer.Integrity(GraphExplorer.FromRoot(this));
        }

        public Dictionary<ModifiableEntity, string> FullIntegrityCheckDictionary()
        {
            return GraphExplorer.IntegrityDictionary(GraphExplorer.FromRoot(this));
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
}

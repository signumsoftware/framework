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

        protected virtual bool Set<T>(ref T variable, T value, Expression<Func<T>> property)
        {
            if (EqualityComparer<T>.Default.Equals(variable, value))
                return false;

            PropertyInfo pi = ReflectionTools.BasePropertyInfo(property); 

            if (variable is INotifyCollectionChanged)
            {
                if(AttributeManager<NotifyCollectionChangedAttribute>.HasToNotify(GetType(), pi))
                    ((INotifyCollectionChanged)variable).CollectionChanged -= ChildCollectionChanged;

                if (AttributeManager<NotifyChildPropertyAttribute>.HasToNotify(GetType(), pi))
                    foreach (INotifyPropertyChanged item in (IEnumerable)variable)
                        item.PropertyChanged -= ChildPropertyChanged;

                if (AttributeManager<ValidateChildPropertyAttribute>.HasToNotify(GetType(), pi))
                    foreach (ModifiableEntity item in (IEnumerable)variable)
                        item.PropertyValidation -= ChildPropertyValidation;
            }

            if (variable is ModifiableEntity)
            {
                if (AttributeManager<NotifyChildPropertyAttribute>.HasToNotify(GetType(), pi))
                    ((INotifyPropertyChanged)variable).PropertyChanged -= ChildPropertyChanged;

                if (AttributeManager<ValidateChildPropertyAttribute>.HasToNotify(GetType(), pi))
                    ((ModifiableEntity)(object)variable).PropertyValidation -= ChildPropertyValidation;
            }

            variable = value;

            if (variable is INotifyCollectionChanged)
            {
                if (AttributeManager<NotifyCollectionChangedAttribute>.HasToNotify(GetType(), pi))
                    ((INotifyCollectionChanged)variable).CollectionChanged += ChildCollectionChanged;

                if (AttributeManager<NotifyChildPropertyAttribute>.HasToNotify(GetType(), pi))
                    foreach (INotifyPropertyChanged item in (IEnumerable)variable)
                        item.PropertyChanged += ChildPropertyChanged;

                if (AttributeManager<ValidateChildPropertyAttribute>.HasToNotify(GetType(), pi))
                    foreach (ModifiableEntity item in (IEnumerable)variable)
                        item.PropertyValidation += ChildPropertyValidation;
            }

            if (variable is ModifiableEntity)
            {
                if (AttributeManager<NotifyChildPropertyAttribute>.HasToNotify(GetType(), pi))
                    ((INotifyPropertyChanged)variable).PropertyChanged += ChildPropertyChanged;

                if (AttributeManager<ValidateChildPropertyAttribute>.HasToNotify(GetType(), pi))
                    ((ModifiableEntity)(object)variable).PropertyValidation += ChildPropertyValidation;
            }

            selfModified = true;
            NotifyPrivate(pi.Name);
            NotifyPrivate("Error");

            return true;
        }

        [HiddenProperty]
        public string ToStringMethod
        {
            get { return ToString().HasText() ? ToString() : this.GetType().Name; }
        }

        public bool SetToStr<T>(ref T variable, T valor, Expression<Func<T>> property)
        {
            if (this.Set(ref variable, valor, property))
            {
                NotifyPrivate("ToStringMethod");
                return true;
            }
            return false;
        }

        #region Collection Events

        protected internal override void PostRetrieving()
        {
            RebindEvents();

            base.PostRetrieving();
        }

        protected virtual void RebindEvents()
        {
            foreach (Func<object, object> getter in AttributeManager<NotifyCollectionChangedAttribute>.FieldsToNotify(GetType()))
            {
                INotifyCollectionChanged notify = (INotifyCollectionChanged)getter(this);
                if (notify != null)
                    notify.CollectionChanged += ChildCollectionChanged;
            }

            foreach (Func<object, object> getter in AttributeManager<NotifyChildPropertyAttribute>.FieldsToNotify(GetType()))
            {
                object obj = getter(this); 
 
                if(obj == null)
                    continue; 

                var entity = obj as INotifyPropertyChanged;
                if(entity != null)
                    entity.PropertyChanged += ChildPropertyChanged;
                else
                {
                    foreach (INotifyPropertyChanged item in (IEnumerable)obj)
                        item.PropertyChanged += ChildPropertyChanged;
                }
            }

            foreach (Func<object, object> getter in AttributeManager<ValidateChildPropertyAttribute>.FieldsToNotify(GetType()))
            {
                object obj = getter(this);

                if (obj == null)
                    continue;

                var entity = obj as ModifiableEntity;
                if (entity != null)
                    entity.PropertyValidation += ChildPropertyValidation;
                else
                {
                    foreach (ModifiableEntity item in (IEnumerable)obj)
                        item.PropertyValidation += ChildPropertyValidation;
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
            if (AttributeManager<NotifyChildPropertyAttribute>.FieldsToNotify(GetType()).Any(f => f(this) == sender))
            {
                if (args.NewItems != null)
                    foreach (var p in args.NewItems.Cast<INotifyPropertyChanged>())p.PropertyChanged += ChildPropertyChanged;
                if (args.OldItems != null)
                    foreach (var p in args.OldItems.Cast<INotifyPropertyChanged>())p.PropertyChanged -= ChildPropertyChanged;
            }

            if (AttributeManager<ValidateChildPropertyAttribute>.FieldsToNotify(GetType()).Any(f => f(this) == sender))
            {
                if (args.NewItems != null)
                    foreach (var p in args.NewItems.Cast<ModifiableEntity>()) p.PropertyValidation += ChildPropertyValidation;
                if (args.OldItems != null)
                    foreach (var p in args.OldItems.Cast<ModifiableEntity>()) p.PropertyValidation -= ChildPropertyValidation;
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
        public event PropertyValidationEventHandler PropertyValidation;

        protected void Notify<T>(Expression<Func<T>> property)
        {
            NotifyPrivate(ReflectionTools.BasePropertyInfo(property).Name);
            NotifyPrivate("Error");
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
        public override string IntegrityCheck()
        {
            return Reflector.GetPropertyPacks(GetType()).Select(k => this[k.Key]).NotNull().ToString("\r\n");
        }

        //override for per-property checks
        [HiddenProperty]
        public string this[string columnName]
        {
            get
            {
                if (columnName == null)
                    return Error;
                else
                {
                    PropertyPack pp = Reflector.GetPropertyPacks(GetType()).TryGetC(columnName);
                    if (pp == null)
                        return null; //Hidden properties

                    object val = pp.GetValue(this);
                    string result = pp.Validators.Select(v => v.Error(val)).NotNull().Select(e => e.Formato(pp.PropertyInfo.NiceName())).FirstOrDefault();

                    if (result != null)
                        return result;

                    result = PropertyCheck(pp.PropertyInfo);

                    if (result != null)
                        return result;

                    if (PropertyValidation != null)
                        result = PropertyValidation(this, pp.PropertyInfo, val);

                    return result;
                }
            }
        }

        protected virtual string PropertyCheck(PropertyInfo pi)
        {
            return null;
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

    public delegate string PropertyValidationEventHandler(ModifiableEntity sender, PropertyInfo pi, object propertyValue);

    public static class ModifiableEntityExtensions
    {
        public static PropertyValidationEventHandler AddValidation<T, P>(this T entity, Expression<Func<T, P>> property, Func<P, string> error)
            where T : ModifiableEntity
        {
            PropertyInfo pi2 = (PropertyInfo)ReflectionTools.BaseMemberInfo(property);

            PropertyValidationEventHandler val = (sender, pi, propertyValue) =>
                sender == entity && ReflectionTools.PropertyEquals(pi, pi2) ? 
                    error((P)propertyValue) : 
                    null;

            entity.PropertyValidation += val;

            return val;
        }

        public static PropertyValidationEventHandler AddValidation<T, P>(this T entity, Expression<Func<T, P>> property, ValidatorAttribute validator)
           where T : ModifiableEntity
        {
            PropertyInfo pi2 = (PropertyInfo)ReflectionTools.BaseMemberInfo(property);

            PropertyValidationEventHandler val = (sender, pi, propertyValue) =>
                sender == entity && ReflectionTools.PropertyEquals(pi, pi2) ? 
                    validator.Error(propertyValue).TryCC(str=>str.Formato(pi.NiceName())) : 
                    null;

            entity.PropertyValidation += val;

            return val;
        }
    }
}

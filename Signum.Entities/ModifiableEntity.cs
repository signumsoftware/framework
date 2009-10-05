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

        protected virtual bool Set<T>(ref T variable, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(variable, value))
                return false;

            if (variable is INotifyCollectionChanged)
            {
                if(NotifyCollectionChangedAttribute.HasToNotify(GetType(), propertyName))
                    ((INotifyCollectionChanged)variable).CollectionChanged -= ChildCollectionChanged;

                if(NotifyPropertyChangedAttribute.HasToNotify(GetType(), propertyName))
                    foreach (INotifyPropertyChanged item in (IEnumerable)variable)
                        item.PropertyChanged -= ChildItemPropertyChanged;
            }

            if (variable is INotifyPropertyChanged && NotifyPropertyChangedAttribute.HasToNotify(GetType(), propertyName))
                ((INotifyPropertyChanged)variable).PropertyChanged -= ChildItemPropertyChanged; 

            variable = value;

            if (variable is INotifyCollectionChanged)
            {
                if (NotifyCollectionChangedAttribute.HasToNotify(GetType(), propertyName))
                    ((INotifyCollectionChanged)variable).CollectionChanged += ChildCollectionChanged;

                if (NotifyPropertyChangedAttribute.HasToNotify(GetType(), propertyName))
                    foreach (INotifyPropertyChanged item in (IEnumerable)variable)
                        item.PropertyChanged += ChildItemPropertyChanged;
            }

            if (variable is INotifyPropertyChanged && NotifyPropertyChangedAttribute.HasToNotify(GetType(), propertyName))
                ((INotifyPropertyChanged)variable).PropertyChanged += ChildItemPropertyChanged; 

            selfModified = true;
            Notify(propertyName);
            NotifyError();

            return true;
        }

        protected void NotifyToString()
        {
            Notify("ToStringMethod");
        }

        protected void NotifyError()
        {
            Notify("Error");
        }

        protected void NotifyError(string propertyName)
        {
            Notify(propertyName);
            Notify("Error");
        }

        [HiddenProperty]
        public string ToStringMethod
        {
            get { return ToString(); }
        }

        public bool SetToStr<T>(ref T variable, T valor, string propertyName)
        {
            if (this.Set(ref variable, valor, propertyName))
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

            base.PostRetrieving();
        }

        protected virtual void RebindEvents()
        {
            foreach (Func<object, object> getter in NotifyCollectionChangedAttribute.FieldsToNotify(GetType()))
            {
                INotifyCollectionChanged notify = (INotifyCollectionChanged)getter(this);
                if (notify != null)
                    notify.CollectionChanged += ChildCollectionChanged;
            }

            foreach (Func<object, object> getter in NotifyPropertyChangedAttribute.FieldsToNotify(GetType()))
            {
                object obj = getter(this); 
 
                if(obj == null)
                    continue; 

                var entity = obj as INotifyPropertyChanged;
                if(entity != null)
                {
                    entity.PropertyChanged += ChildItemPropertyChanged;
                }
                else
                {
                    foreach (INotifyPropertyChanged item in (IEnumerable)obj)
                        item.PropertyChanged  += ChildItemPropertyChanged;
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
            if (NotifyPropertyChangedAttribute.FieldsToNotify(GetType()).Any(f => f(this) == sender))
            {
                if (args.NewItems != null)
                    foreach (var p in args.NewItems.Cast<INotifyPropertyChanged>())p.PropertyChanged += ChildItemPropertyChanged;
                if (args.OldItems != null)
                    foreach (var p in args.OldItems.Cast<INotifyPropertyChanged>())p.PropertyChanged -= ChildItemPropertyChanged;
            }
        }

        protected virtual void ChildItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }
        #endregion

        protected void Notify(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void Notify(object sender, string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(sender, new PropertyChangedEventArgs(propertyName));
        }

        static long temporalIdCounter = 0;

        [Ignore]
        internal int temporalId;

        internal ModifiableEntity()
        {
            temporalId = unchecked((int)Interlocked.Increment(ref temporalIdCounter));
        }

        //static int currentId = 0; 

        public override int GetHashCode()
        {
            return GetType().FullName.GetHashCode() ^ temporalId;
        }
        
        [field:NonSerialized, Ignore]
        public event PropertyChangedEventHandler PropertyChanged;

        #region IDataErrorInfo Members

   
        [HiddenProperty]
        public string Error
        {
            get { return IntegrityCheck(); }
        }

        //override for full entitity integrity check. Remember to call base. 
        public override string IntegrityCheck()
        {
            return Reflector.GetPropertyValidators(GetType()).Select(k => this[k.Key]).NotNull().ToString("\r\n");
        }

        //override for per-property checks
        [HiddenProperty]
        public virtual string this[string columnName]
        {
            get
            {
                if (columnName == null)
                    return Error;
                else
                {
                    PropertyPack pp = Reflector.GetPropertyValidators(GetType()).TryGetC(columnName);
                    if (pp == null)
                        return null; 
                    object val = pp.GetValue(this);
                    return pp.Validators.Select(v => v.Error(val)).NotNull().Select(e => e.Formato(pp.PropertyInfo.NiceName())).FirstOrDefault();
                }
            }
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

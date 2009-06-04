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

namespace Signum.Entities
{
    [Serializable]
    public abstract class ModifiableEntity : Modifiable, INotifyPropertyChanged, IDataErrorInfo, ICloneable
    {
        [Ignore]
        protected bool selfModified = true;

        internal ModifiableEntity() { }

        [DoNotValidate]
        public override bool SelfModified
        {
            get { return selfModified; }
            internal set { selfModified = value; }
        }

        protected bool Set<T>(ref T variable, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(variable, value))
                return false;

            if (variable is INotifyCollectionChanged && NotifyCollectionChangedAttribute.HasToNotify(GetType(), propertyName))
                ((INotifyCollectionChanged)variable).CollectionChanged -= ChildCollectionChanged;

            if (variable is INotifyPropertyChanged && NotifyPropertyChangedAttribute.HasToNotify(GetType(), propertyName))
                ((INotifyPropertyChanged)variable).PropertyChanged -= ChildItemPropertyChanged; 

            variable = value;

            if (variable is INotifyCollectionChanged && NotifyCollectionChangedAttribute.HasToNotify(GetType(), propertyName))
                ((INotifyCollectionChanged)variable).CollectionChanged += ChildCollectionChanged;

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

        [DoNotValidate]
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
                INotifyPropertyChanged notify = (INotifyPropertyChanged)getter(this);
                if (notify != null)
                    notify.PropertyChanged += ChildItemPropertyChanged;
            }
        }

   

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            RebindEvents();
        }

        protected virtual void ChildCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {

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

        [Ignore]
        internal int temporalId = MyRandom.Current.Next();

        public override int GetHashCode()
        {
            return GetType().FullName.GetHashCode() ^ temporalId;
        }
        
        [field:NonSerialized, Ignore]
        public event PropertyChangedEventHandler PropertyChanged;

        #region IDataErrorInfo Members

        internal static Dictionary<string, PropertyPack> GetPropertyValidators(Type type)
        {
            lock (validators)
            {
                return validators.GetOrCreate(type, () =>
                    MemberEntryFactory.GenerateIList(type, MemberOptions.Properties | MemberOptions.Getter| MemberOptions.Setters| MemberOptions.Untyped)
                    .Cast<IMemberEntry>()
                    .Where(p=>!Attribute.IsDefined(p.MemberInfo, typeof(DoNotValidateAttribute)))
                    .ToDictionary(p => p.Name, p => new PropertyPack((PropertyInfo)p.MemberInfo, p.UntypedGetter, p.UntypedSetter)));
            }
        }

        static Dictionary<Type, Dictionary<string, PropertyPack>> validators = new Dictionary<Type, Dictionary<string, PropertyPack>>();

        [DoNotValidate]
        public string Error
        {
            get { return IntegrityCheck(); }
        }

        //override for full entitity integrity check. Remember to call base. 
        public override string IntegrityCheck()
        {
            return GetPropertyValidators(GetType()).Select(k => this[k.Key]).NotNull().ToString("\r\n");
        }

        //override for per-property checks
        [DoNotValidate]
        public virtual string this[string columnName]
        {
            get
            {
                if (columnName == null)
                    return Error;
                else
                {
                    PropertyPack pp = GetPropertyValidators(GetType())[columnName];
                    object val = pp.GetValue(this);
                    return pp.Validators.Select(v => v.Error(val)).NotNull().Select(e => e.Formato(columnName)).FirstOrDefault();
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

    public class PropertyPack
    {
        public PropertyPack(PropertyInfo pi, Func<object, object> getValue, Action<object, object> setValue)
        {
            this.PropertyInfo = pi;
            Validators = pi.GetCustomAttributes(typeof(ValidatorAttribute), true).OfType<ValidatorAttribute>().ToReadOnly();
            this.GetValue = getValue;
            this.SetValue = setValue;
        }

        public readonly Func<object, object> GetValue;
        public readonly Action<object, object> SetValue;
        public readonly PropertyInfo PropertyInfo;
        public readonly ReadOnlyCollection<ValidatorAttribute> Validators;
    }

   
}

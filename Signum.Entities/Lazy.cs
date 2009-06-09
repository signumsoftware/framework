using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Reflection;
using Signum.Entities.Properties; 

namespace Signum.Entities
{
    [Serializable]
    public class Lazy<T> : Lazy
        where T : class, IIdentifiable
    {
        T entityOrNull;

        // Methods
        protected Lazy()
        {
        }

        public Lazy(int id)
            : base(typeof(T), id)
        {
        }

        public Lazy(Type runtimeType, int id)
            : base(runtimeType, id)
        {
            if (!typeof(T).IsAssignableFrom(runtimeType))
                throw new ApplicationException(Resources.TypeIsNotSmallerThan.Formato(runtimeType, typeof(T)));
        }

        public Lazy(T entidad)
            : base((IdentifiableEntity)(IIdentifiable)entidad)
        {   
        }

        public static explicit operator Lazy<T>(Lazy lazy)
        {
            if (lazy.UntypedEntityOrNull != null)
                return new Lazy<T>((T)(object)lazy.UntypedEntityOrNull);
            else
                return new Lazy<T>(lazy.RuntimeType, lazy.Id);
        }

        public override IdentifiableEntity UntypedEntityOrNull
        {
            get { return (IdentifiableEntity)(object)EntityOrNull; }
            internal set { EntityOrNull = (T)(object)value; }
        }

        public T EntityOrNull
        {
            get { return entityOrNull; }
            internal set { entityOrNull = value; }
        }

        const int MagicMask = 123456853; 

        public override int GetHashCode()
        {
            if (this.EntityOrNull != null)
                return EntityOrNull.GetHashCode() ^ MagicMask;
            return base.Id.GetHashCode() ^ base.RuntimeType.Name.GetHashCode() ^ MagicMask;
        }
    }

    [Serializable]
    public abstract class Lazy : Modifiable
    {
        Type runtimeType;
        int? id;
        string toStr;

        protected Lazy()
        {
        }

        public Lazy(Type runtimeType, int id)
        {
            if (runtimeType == null || !typeof(IdentifiableEntity).IsAssignableFrom(runtimeType))
                throw new ApplicationException(Resources.TypeIsNotSmallerThan.Formato(runtimeType, typeof(IIdentifiable)));

            this.runtimeType = runtimeType;
            this.id = id;
        }

        public Lazy(IdentifiableEntity entidad)
        {
            if (entidad == null)
                throw new ArgumentNullException("entidad");

            this.runtimeType = entidad.GetType();
            this.ToStr = entidad.ToString();
            this.UntypedEntityOrNull = entidad;
            this.id = entidad.IdOrNull;
        }
      
        public int RefreshId()
        {
            if (UntypedEntityOrNull != null)
                id = UntypedEntityOrNull.Id;
            return id.Value; 
        }

        public Type RuntimeType
        {
            get { return runtimeType; }
        }

        public int Id
        {
            get
            {
                if (id == null)
                    throw new ApplicationException(Resources.TheLazyIsPointingToANewEntityAndHasNoIdYet);
                return id.Value;
            }
        }

        public int? IdOrNull
        {
            get { return id; }
        }

        public abstract IdentifiableEntity UntypedEntityOrNull
        {
            get;
            internal set;
        }

        public void SetEntity(IdentifiableEntity ei)
        {
            if (id == null)
                throw new ApplicationException(Resources.NewEntitiesAreNotAllowed); 

            if (id != ei.id || RuntimeType != ei.GetType())
                throw new ApplicationException(Resources.EntitiesDoNotMatch);

            this.UntypedEntityOrNull = ei;
        }

        public void ClearEntity()
        {
            if (id == null)
                throw new ApplicationException(Resources.RemovingEntityNotAllowedInNewLazies);

            this.UntypedEntityOrNull = null;
        }

        protected internal override void PreSaving()
        {
            UntypedEntityOrNull.TryDoC(e => e.PreSaving());
            if (UntypedEntityOrNull != null)
                toStr = UntypedEntityOrNull.ToStr;
            //Is better to have an old string than having nothing
        }

        public override bool SelfModified
        {
            get { return false; }
            internal set { }
        }

        public override string ToString()
        {
            if (this.UntypedEntityOrNull != null)
                return this.UntypedEntityOrNull.ToString();
            if (this.toStr != null)
                return this.toStr;
            return "{0}({1})".Formato(this.RuntimeType, this.id);
        }

        public string ToStringLong()
        {
            if (this.UntypedEntityOrNull == null)
                return "[({0}:{1}) ToStr:{2}]".Formato(this.runtimeType.Name, this.id, this.toStr);
            return "[{0}]".Formato(this.UntypedEntityOrNull);
        }

        public static Lazy Create(Type type, int id)
        {
            return (Lazy)Activator.CreateInstance(Reflector.GenerateLazy(type), type, id);
        }

        public static Lazy Create(Type type, int id, Type runtimeType)
        {
            return (Lazy)Activator.CreateInstance(Reflector.GenerateLazy(type), runtimeType, id);
        }

        public static Lazy Create(Type type, int id, Type runtimeType, string toStr)
        {
            Lazy result = (Lazy)Activator.CreateInstance(Reflector.GenerateLazy(type), runtimeType, id);
            result.ToStr = toStr;
            return result;
        }

        public static Lazy Create(Type type, IdentifiableEntity entidad)
        {
            return (Lazy)Activator.CreateInstance(Reflector.GenerateLazy(type), entidad);
        }

        public string ToStr
        {
            get { return toStr; }
            internal set { toStr = value;  }
        }

        internal bool EqualsIdent(IdentifiableEntity entity)
        {
            if (entity == null)
                return false;

            if (RuntimeType != entity.GetType())
                return false;

            if (UntypedEntityOrNull == null)
                return Id == entity.IdOrNull;
            else
                return object.ReferenceEquals(this.UntypedEntityOrNull, entity);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
      
            if (this == obj)
                return true;

            Lazy lazy = obj as Lazy;
            if (lazy != null)
            {
                if (RuntimeType != lazy.RuntimeType)
                    return false;

                if (UntypedEntityOrNull == null)
                    return Id == lazy.IdOrNull;
                else
                    return object.ReferenceEquals(this.UntypedEntityOrNull, lazy.UntypedEntityOrNull);
            }
           
            return false;
        }
    }


    public static class LazyUtils
    {
        public static Lazy<T> ToLazy<T>(this T entity) where T : class, IIdentifiable
        {
            if (entity.IsNew)
                throw new ApplicationException(Resources.ToLazyLightNotAllowedForNewEntities);

            var milazy = new Lazy<T>(entity);
            milazy.EntityOrNull = null;
            return milazy;
        }

        public static Lazy<T> ToLazy<T>(this T entidad, bool fat) where T : class, IIdentifiable
        {
            if (fat)
                return entidad.ToLazyFat();
            else
                return entidad.ToLazy(); 
        }

        public static Lazy<T> ToLazyFat<T>(this T entidad) where T : class, IIdentifiable
        {
            return new Lazy<T>(entidad);
        }      
    }
}

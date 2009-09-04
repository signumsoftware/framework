using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Reflection;
using Signum.Entities.Properties;
using System.Reflection; 

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

        internal Lazy(T entidad)
            : base((IdentifiableEntity)(IIdentifiable)entidad)
        {   
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

        public bool RefersTo(T entity)
        {
            if (entity == null)
                return false;

            if (RuntimeType != entity.GetType())
                return false;

            if (IdOrNull != null)
                return Id == entity.IdOrNull;
            else
                return object.ReferenceEquals(this.EntityOrNull, entity);
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

        protected Lazy(Type runtimeType, int id)
        {
            if (runtimeType == null || !typeof(IdentifiableEntity).IsAssignableFrom(runtimeType))
                throw new ApplicationException(Resources.TypeIsNotSmallerThan.Formato(runtimeType, typeof(IIdentifiable)));

            this.runtimeType = runtimeType;
            this.id = id;
        }

        protected Lazy(IdentifiableEntity entidad)
        {
            if (entidad == null)
                throw new ArgumentNullException("entidad");

            this.runtimeType = entidad.GetType();
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
            if (entidad == null)
                throw new ArgumentNullException("entidad");

            BindingFlags bf = BindingFlags.Default | BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.NonPublic;

            ConstructorInfo ci = Reflector.GenerateLazy(type).GetConstructor(bf, null, new[] { type }, null);

            Lazy result = (Lazy)ci.Invoke(new[] { entidad }); 
            result.ToStr = entidad.TryToString();
            return result;
        }

        public string ToStr
        {
            get { return toStr; }
            internal set { toStr = value;  }
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

        const int MagicMask = 123456853; 
        public override int GetHashCode()
        {
            if (this.UntypedEntityOrNull != null)
                return UntypedEntityOrNull.GetHashCode() ^ MagicMask;
            return this.Id.GetHashCode() ^ this.RuntimeType.Name.GetHashCode() ^ MagicMask;
        }
    }


    public static class LazyUtils
    {
        public static Lazy<T> ToLazy<T>(this Lazy lazy)
            where T : class, IIdentifiable
        {
            if (lazy == null)
                return null;

            if (lazy is Lazy<T>)
                return (Lazy<T>)lazy;

            if (lazy.UntypedEntityOrNull != null)
                return new Lazy<T>((T)(object)lazy.UntypedEntityOrNull) { ToStr = lazy.ToStr };
            else
                return new Lazy<T>(lazy.RuntimeType, lazy.Id) { ToStr = lazy.ToStr };
        }

        public static Lazy<T> ToLazy<T>(this Lazy lazy, string toStr) 
            where T : class, IIdentifiable
        {
            if (lazy == null)
                return null;

            if (lazy is Lazy<T>)
                return (Lazy<T>)lazy;

            if (lazy.UntypedEntityOrNull != null)
                return new Lazy<T>((T)(object)lazy.UntypedEntityOrNull) { ToStr = toStr };
            else
                return new Lazy<T>(lazy.RuntimeType, lazy.Id) { ToStr = toStr };
        }

        public static Lazy<T> ToLazy<T>(this T entity)
          where T : class, IIdentifiable
        {
            if (entity == null)
                return null;


            if (entity.IsNew)
                throw new ApplicationException(Resources.ToLazyLightNotAllowedForNewEntities);

            return new Lazy<T>(entity.GetType(), entity.Id) { ToStr = entity.ToString() };
        }

        public static Lazy<T> ToLazy<T>(this T entity, string toStr) 
            where T : class, IIdentifiable
        {
            if (entity == null)
                return null;

            if (entity.IsNew)
                throw new ApplicationException(Resources.ToLazyLightNotAllowedForNewEntities);

            return new Lazy<T>(entity.GetType(), entity.Id){ ToStr = toStr};
        }

        public static Lazy<T> ToLazy<T>(this T entity, bool fat) where T : class, IIdentifiable
        {
            if (fat)
                return entity.ToLazyFat();
            else
                return entity.ToLazy(); 
        }

        public static Lazy<T> ToLazy<T>(this T entity, bool fat, string toStr) where T : class, IIdentifiable
        {
            if (fat)
                return entity.ToLazyFat(toStr);
            else
                return entity.ToLazy(toStr);
        }

        public static Lazy<T> ToLazyFat<T>(this T entity) where T : class, IIdentifiable
        {
            if (entity == null)
                return null;

            return new Lazy<T>(entity) { ToStr = entity.ToString() };
        }

        public static Lazy<T> ToLazyFat<T>(this T entity, string toStr) where T : class, IIdentifiable
        {
            if (entity == null)
                return null;

            return new Lazy<T>(entity) { ToStr = toStr };
        } 
    }
}

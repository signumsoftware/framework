using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Utilities;
using System.Reflection;
using Signum.Entities.Reflection;
using System.ComponentModel;
using System.Linq.Expressions;
using Signum.Utilities.Reflection;
using Signum.Utilities.ExpressionTrees;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.CodeDom.Compiler;

namespace Signum.Entities
{
    [Serializable, DescriptionOptions(DescriptionOptions.All), InTypeScript(false)]
    public abstract class Entity : ModifiableEntity, IEntity
    {
        [Ignore, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal PrimaryKey? id;


        [Ignore, DebuggerBrowsable(DebuggerBrowsableState.Never), ColumnName("ToStr")]
        protected internal string? toStr; //for queries and lites on entities with non-expression ToString

        [HiddenProperty, Description("Id")]
        public PrimaryKey Id
        {
            get
            {
                if (id == null)
                    throw new InvalidOperationException("{0} is new and has no Id".FormatWith(this.GetType().Name));
                return id.Value;
            }
            internal set { id = value; } //Use SetId method to change the Id
        }

        [HiddenProperty]
        public PrimaryKey? IdOrNull
        {
            get { return id; }
        }

        [Ignore, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        bool isNew = true;
        [HiddenProperty]
        public bool IsNew
        {
            get { return isNew; }
            internal set { isNew = value; }
        }

        [Ignore, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal long ticks;
        [HiddenProperty]
        public long Ticks
        {
            get { return ticks; }
            set { ticks = value; }
        }

        protected bool SetIfNew<T>(ref T field, T value, [CallerMemberName]string? automaticPropertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            if (!IsNew)
            {
                throw new InvalidOperationException("Attempt to modify '{0}' when the entity is not new".FormatWith(automaticPropertyName));
            }

            return base.Set<T>(ref field, value, automaticPropertyName!);
        }

        public override string ToString()
        {
            return BaseToString();
        }

        public string BaseToString()
        {
            return "{0} ({1})".FormatWith(GetType().NiceName(), id.HasValue ? id.ToString() : LiteMessage.New_G.NiceToString().ForGenderAndNumber(this.GetType().GetGender()));
        }

        public override bool Equals(object? obj)
        {
            if(obj == this)
                return true;

            if(obj == null)
                return false;

            if (obj is Entity ident && ident.GetType() == this.GetType() && this.id != null && this.id == ident.id)
                return true;

            return false;
        }

        public virtual Dictionary<Guid, IntegrityCheck>? EntityIntegrityCheck()
        {
            using (Mixins.OfType<CorruptMixin>().Any(c => c.Corrupt) ? Corruption.AllowScope() : null)
            {
                return EntityIntegrityCheckBase();
            }
        }

        internal virtual Dictionary<Guid, IntegrityCheck>? EntityIntegrityCheckBase()
        {
            using (HeavyProfiler.LogNoStackTrace("EntityIntegrityCheckBase", () => GetType().Name))
                return GraphExplorer.EntityIntegrityCheck(GraphExplorer.FromRootEntity(this));
        }

        public override int GetHashCode()
        {
            return id == null ?
                base.GetHashCode() :
                StringHashEncoder.GetHashCode32(GetType().FullName!) ^ id.Value.GetHashCode();
        }

        public void SetGraphErrors(IntegrityCheckException ex)
        {
            GraphExplorer.SetValidationErrors(GraphExplorer.FromRoot(this), ex);
        }

    }

    [InTypeScript(false)]
    public interface IEntity : IModifiableEntity, IRootEntity
    {
        PrimaryKey Id { get; }

        [HiddenProperty]
        PrimaryKey? IdOrNull { get; }

        [HiddenProperty]
        bool IsNew { get; }

        [HiddenProperty]
        string ToStringProperty { get; }
    }

    public interface IRootEntity
    {

    }

    public static class UnsafeEntityExtensions
    {
        public static T SetId<T>(this T entity, PrimaryKey? id)
        where T : Entity
        {
            entity.id = id;
            return entity;
        }

        public static T SetReadonly<T, V>(this T entity, Expression<Func<T, V>> readonlyProperty, V value)
             where T : ModifiableEntity
        {
            return SetReadonly(entity, readonlyProperty, value, true);
        }

        public static T SetReadonly<T, V>(this T entity, Expression<Func<T, V>> readonlyProperty, V value, bool setSelfModified)
             where T : ModifiableEntity
        {
            var pi = ReflectionTools.BasePropertyInfo(readonlyProperty);

            Action<T, V> setter = ReadonlySetterCache<T>.Setter<V>(pi);

            setter(entity, value);
            if (setSelfModified)
                entity.SetSelfModified();

            return entity;
        }

        static class ReadonlySetterCache<T> where T : ModifiableEntity
        {
            static ConcurrentDictionary<string, Delegate> cache = new ConcurrentDictionary<string, Delegate>();

            internal static Action<T, V> Setter<V>(PropertyInfo pi)
            {
                return (Action<T, V>)cache.GetOrAdd(pi.Name, s => ReflectionTools.CreateSetter<T, V>(Reflector.FindFieldInfo(typeof(T), pi))!);
            }
        }

        public static T SetIsNew<T>(this T entity, bool isNew = true)
            where T : Entity
        {
            entity.IsNew = isNew;
            entity.SetSelfModified();
            return entity;
        }

        public static T SetNotModified<T>(this T mod)
            where T : Modifiable
        {
            if (mod is Entity)
                ((Entity)(Modifiable)mod).IsNew = false;
            mod.Modified = ModifiedState.Clean; /*Compiler bug*/
            return mod;
        }

        public static T SetModified<T>(this T entity)
            where T : Modifiable
        {
            entity.Modified = ModifiedState.Modified;
            return entity;
        }

        public static T SetNotModifiedGraph<T>(this T entity, PrimaryKey id)
            where T : Entity
        {
            foreach (var item in GraphExplorer.FromRoot(entity).Where(a => a.Modified != ModifiedState.Sealed))
            {
                item.SetNotModified();
                if (item is Entity e && e.IdOrNull == null)
                    e.SetId(new PrimaryKey("invalidId"));
            }

            entity.SetId(id);

            return entity;
        }
    }

    public static class EntityContext
    {
        public static PrimaryKey EntityId(object? obj)
        {
            throw new InvalidOperationException("EntityContext.EntityId can only be called inside LINQ queries");
        }

        public static PrimaryKey? MListRowId(object? obj)
        {
            throw new NotImplementedException("EntityContext.MListRowId can only be called inside LINQ queries");
        }
    }
}

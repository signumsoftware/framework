using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Runtime.Serialization;
using System.Reflection;
using System.Collections.Specialized;
using Signum.Utilities.DataStructures;
using Signum.Entities.Reflection;
using System.ComponentModel;
using System.Linq.Expressions;
using Signum.Utilities.Reflection;
using Signum.Utilities.ExpressionTrees;
using Signum.Services;
using System.Runtime.CompilerServices;

namespace Signum.Entities
{
    [Serializable, DescriptionOptions(DescriptionOptions.All)]
    public abstract class IdentifiableEntity : ModifiableEntity, IIdentifiable
    {
        internal int? id = null; //primary key
        [Ignore]
        protected internal string toStr; //for queries and lites on entities with non-expression ToString 

        [HiddenProperty, Description("Id")]
        public int Id
        {
            get
            {
                if (id == null)
                    throw new InvalidOperationException("{0} is new and has no Id".Formato(this.GetType().Name));
                return id.Value;
            }
            internal set { id = value; }
        }

        [HiddenProperty]
        public int? IdOrNull
        {
            get { return id; }
        }

        [Ignore]
        bool isNew = true;
        [HiddenProperty]
        public bool IsNew
        {
            get { return isNew; }
            internal set { isNew = value; }
        }

        protected bool SetIfNew<T>(ref T field, T value, [CallerMemberNameAttribute]string automaticPropertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            if (!IsNew)
            {
                throw new InvalidOperationException("Attempt to modify '{0}' when the entity is not new".Formato(automaticPropertyName));
            }

            return base.Set<T>(ref field, value, automaticPropertyName);
        }

        public override string ToString()
        {
            return BaseToString();
        }

        public string BaseToString()
        {
            return "{0} ({1})".Formato(GetType().Name, id.HasValue ? id.ToString() : LiteMessage.New.NiceToString());
        }

        public override bool Equals(object obj)
        {
            if(obj == this)
                return true;

            if(obj == null)
                return false;

            IdentifiableEntity ident = obj as IdentifiableEntity;
            if (ident != null && ident.GetType() == this.GetType() && this.id != null && this.id == ident.id)
                return true;

            return false;
        }

        public virtual string IdentifiableIntegrityCheck()
        {
            using (Mixins.OfType<CorruptMixin>().Any(c => c.Corrupt) ? Corruption.AllowScope() : null)
            {
                return IdentifiableIntegrityCheckBase();
            }
        }

        internal virtual string IdentifiableIntegrityCheckBase()
        {
            using (HeavyProfiler.LogNoStackTrace("IdentifiableIntegrityCheck", () => GetType().Name))
                return GraphExplorer.IdentifiableIntegrityCheck(GraphExplorer.FromRootIdentifiable(this));
        }

        public override int GetHashCode()
        {
            return id == null ?
                base.GetHashCode() :
                StringHashEncoder.GetHashCode32(GetType().FullName) ^ id.Value;
        }

        public IdentifiableEntity()
        {
            mixin = MixinDeclarations.CreateMixins(this);
        }

        [Ignore]
        readonly MixinEntity mixin;
        public M Mixin<M>() where M : MixinEntity
        {
            var current = mixin;
            while (current != null)
            {
                if (current is M)
                    return (M)current;
                current = current.Next;
            }

            throw new InvalidOperationException("Mixin {0} not declared for {1} in MixinDeclarations"
                .Formato(typeof(M).TypeName(), GetType().TypeName()));
        }

        public MixinEntity GetMixin(Type mixinType)
        {
            var current = mixin;
            while (current != null)
            {
                if (current.GetType() == mixinType)
                    return current;
                current = current.Next;
            }

            throw new InvalidOperationException("Mixin {0} not declared for {1} in MixinDeclarations"
                .Formato(mixinType.TypeName(), GetType().TypeName()));
        }

        [HiddenProperty]
        public MixinEntity this[string mixinName]
        {
            get
            {
                var current = mixin;
                while (current != null)
                {
                    if (current.GetType().Name == mixinName)
                        return current;
                    current = current.Next;
                }

                throw new InvalidOperationException("Mixin {0} not declared for {1} in MixinDeclarations"
                    .Formato(mixinName, GetType().TypeName()));
            }
        }

        [HiddenProperty]
        public IEnumerable<MixinEntity> Mixins
        {
            get
            {
                var current = mixin;
                while (current != null)
                {
                    yield return current;
                    current = current.Next;
                }
            }
        }

       
    }

    public interface IIdentifiable : INotifyPropertyChanged, IDataErrorInfo, IRootEntity
    {
        int Id { get; }

        [HiddenProperty]
        int? IdOrNull { get; }

        [HiddenProperty]
        bool IsNew { get; }

        [HiddenProperty]
        string ToStringProperty { get; }
    }

    public interface IRootEntity
    {

    }

    /// <summary>
    /// Represents a PrimaryKey of type int, long, Guid or string, for example.
    /// Its a struct to avoid another object in heap
    /// The default value represents an invalid state.  
    /// </summary>
    public struct PrimaryKey : IEquatable<PrimaryKey>
    {
        public readonly object Object;

        public PrimaryKey(object obj)
        {
            if(obj == null)
                throw new InvalidOperationException(""); 

            this.Object = obj;
        }

        public override string ToString()
        {
            return Object.ToString();
        }

        public override bool Equals(object obj)
        {
            return obj is PrimaryKey && this.Equals((PrimaryKey)obj);
        }

        public override int GetHashCode()
        {
            return Object.GetHashCode();
        }

        public bool Equals(PrimaryKey other)
        {
            if (other.Object.GetType() != this.Object.GetType())
                throw new InvalidOperationException("Comparing PrimaryKey of types {0} with anotherone of the {1}".Formato(other.Object.GetType(), this.Object.GetType()));

            return other.Object.Equals(this.Object);
        }

        public static implicit operator PrimaryKey(int id)
        {
            return new PrimaryKey(id);
        }

        public static implicit operator PrimaryKey?(int? id)
        {
            if (id == null)
                return null;

            return new PrimaryKey(id.Value);
        }

        public static implicit operator PrimaryKey(long id)
        {
            return new PrimaryKey(id);
        }

        public static implicit operator PrimaryKey?(long? id)
        {
            if (id == null)
                return null;

            return new PrimaryKey(id.Value);
        }

        public static implicit operator PrimaryKey(Guid id)
        {
            return new PrimaryKey(id);
        }

        public static implicit operator PrimaryKey?(Guid? id)
        {
            if (id == null)
                return null;

            return new PrimaryKey(id.Value);
        }

        public static implicit operator PrimaryKey?(string id)
        {
            if (id == null)
                return null;

            return new PrimaryKey(id);
        }

        public static explicit operator int(PrimaryKey key)
        {
            return (int)key.Object;
        }

        public static explicit operator long(PrimaryKey key)
        {
            return (long)key.Object;
        }

        public static explicit operator Guid(PrimaryKey key)
        {
            return (Guid)key.Object;
        }

        public static explicit operator string(PrimaryKey key)
        {
            return (string)key.Object;
        }

        public static bool operator ==(PrimaryKey a, PrimaryKey b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(PrimaryKey a, PrimaryKey b)
        {
            return !a.Equals(b);
        }
    }
}

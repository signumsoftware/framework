﻿using System;
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
using System.Data;
using System.Globalization;

namespace Signum.Entities
{
    [Serializable, DescriptionOptions(DescriptionOptions.All)]
    public abstract class Entity : ModifiableEntity, IEntity
    {
        [Ignore]
        internal PrimaryKey? id = null;
        [Ignore]
        protected internal string toStr; //for queries and lites on entities with non-expression ToString 

        [HiddenProperty, Description("Id")]
        public PrimaryKey Id
        {
            get
            {
                if (id == null)
                    throw new InvalidOperationException("{0} is new and has no Id".FormatWith(this.GetType().Name));
                return id.Value;
            }
            internal set { id = value; }
        }


        public void SetPrimaryKey(PrimaryKey value)
        {
            id = value;
        }

        [HiddenProperty]
        public PrimaryKey? IdOrNull
        {
            get { return id; }
        }

        public void SetIsNew(bool value)
        {
            IsNew = value;
        }


        [Ignore]
        bool isNew = true;
        [HiddenProperty]
        public bool IsNew
        {
            get { return isNew; }
            internal set { isNew = value; }
        }

        [Ignore]
        internal long ticks = 0;
        [HiddenProperty]
        public long Ticks
        {
            get { return ticks; }
            set { ticks = value; }
        }

        protected bool SetIfNew<T>(ref T field, T value, [CallerMemberNameAttribute]string automaticPropertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            if (!IsNew)
            {
                throw new InvalidOperationException("Attempt to modify '{0}' when the entity is not new".FormatWith(automaticPropertyName));
            }

            return base.Set<T>(ref field, value, automaticPropertyName);
        }

        public override string ToString()
        {
            return BaseToString();
        }

        public string BaseToString()
        {
            return "{0} ({1})".FormatWith(GetType().NiceName(), id.HasValue ? id.ToString() : LiteMessage.New_G.NiceToString().ForGenderAndNumber(this.GetType().GetGender()));
        }

        public override bool Equals(object obj)
        {
            if(obj == this)
                return true;

            if(obj == null)
                return false;

            Entity ident = obj as Entity;
            if (ident != null && ident.GetType() == this.GetType() && this.id != null && this.id == ident.id)
                return true;

            return false;
        }

        public virtual Dictionary<Guid, Dictionary<string, string>> EntityIntegrityCheck()
        {
            using (Mixins.OfType<CorruptMixin>().Any(c => c.Corrupt) ? Corruption.AllowScope() : null)
            {
                return IdentifiableIntegrityCheckBase();
            }
        }

        internal virtual Dictionary<Guid, Dictionary<string, string>> IdentifiableIntegrityCheckBase()
        {
            using (HeavyProfiler.LogNoStackTrace("IdentifiableIntegrityCheck", () => GetType().Name))
                return GraphExplorer.IdentifiableIntegrityCheck(GraphExplorer.FromRootIdentifiable(this));
        }

        public override int GetHashCode()
        {
            return id == null ?
                base.GetHashCode() :
                StringHashEncoder.GetHashCode32(GetType().FullName) ^ id.Value.GetHashCode();
        }

        public Entity()
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
                .FormatWith(typeof(M).TypeName(), GetType().TypeName()));
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
                .FormatWith(mixinType.TypeName(), GetType().TypeName()));
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
                    .FormatWith(mixinName, GetType().TypeName()));
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

        public void SetGraphErrors(IntegrityCheckException ex)
        {
            GraphExplorer.SetValidationErrors(GraphExplorer.FromRoot(this), ex);
        }
       
    }

    public interface IEntity : INotifyPropertyChanged, IDataErrorInfo, IRootEntity
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

   

}

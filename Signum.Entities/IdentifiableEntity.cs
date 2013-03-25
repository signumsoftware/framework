using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Runtime.Serialization;
using System.Reflection;
using System.Collections.Specialized;
using Signum.Entities.Properties;
using Signum.Utilities.DataStructures;
using Signum.Entities.Reflection;
using System.ComponentModel;
using System.Linq.Expressions;
using Signum.Utilities.Reflection;
using Signum.Utilities.ExpressionTrees;
using Signum.Services;

namespace Signum.Entities
{
    [Serializable]
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
        [Description("Id")]
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

        protected bool SetIfNew<T>(ref T field, T value, Expression<Func<T>> property)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            if (!IsNew)
            {
                PropertyInfo pi = ReflectionTools.BasePropertyInfo(property);
                throw new InvalidOperationException("Attempt to modify '{0}' when the entity is not new".Formato(pi.Name));
            }

            return base.Set<T>(ref field, value, property);
        }

        public override string ToString()
        {
            return BaseToString();
        }

        internal string BaseToString()
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
            using (HeavyProfiler.LogNoStackTrace("IdentifiableIntegrityCheck", () => GetType().Name))
                return GraphExplorer.IdentifiableIntegrityCheck(GraphExplorer.FromRootIdentifiable(this));
        }

        public override int GetHashCode()
        {
            return id == null ?
                base.GetHashCode() :
                StringHashEncoder.GetHashCode32(GetType().FullName) ^ id.Value;
        }

    }

    [DescriptionOptions(DescriptionOptions.None)]
    public interface IIdentifiable : INotifyPropertyChanged, IDataErrorInfo, IRootEntity
    {
        int Id { get; }
        int? IdOrNull { get; }
        bool IsNew { get; }
        string ToStringProperty { get; }
    }

    public interface IRootEntity
    {

    }
}

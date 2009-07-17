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

namespace Signum.Entities
{
    [Serializable]
    public abstract class IdentifiableEntity : ModifiableEntity, IIdentifiable
    {
        internal int? id = null; //primary key
        internal string toStr; //no value for new entities

        [HiddenProperty]
        public int Id
        {
            get
            {
                if (id == null)
                    throw new InvalidOperationException(Resources._0IsNewAndHasNoId.Formato(this.GetType().Name));
                return id.Value;
            }
            internal set { id = value; }
        }

        [HiddenProperty]
        public int? IdOrNull
        {
            get { return id; }
        }

        [HiddenProperty]
        public string ToStr
        {
            get { return toStr; }
            private set { Set(ref toStr, value, "ToStr"); }
        }

        [Ignore]
        bool isNew = true; 
        [HiddenProperty]
        public bool IsNew 
        {
            get { return isNew; }
            set { isNew = value; }
        }

        protected bool SetIfNew<T>(ref T variable, T value, string propertyName)
        {
            if (!IsNew)
                throw new ApplicationException("Attempt to modify {0} when the entity is not new".Formato(propertyName));

            return base.Set<T>(ref variable, value, propertyName);
        }

        protected internal override void PreSaving()
        {
            base.PreSaving();

            ToStr = ToString();
        }

        public override string ToString()
        {
            return "{0} ({1})".Formato(GetType().Name, id.HasValue ? id.ToString() : Resources.New);
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
            return GraphExplorer.GraphIntegrityCheck(this, ModifyInspector.IdentifiableExplore);
        }

        public virtual Dictionary<Modifiable,string> IdentifiableIntegrityCheckDictionary()
        {
            return GraphExplorer.GraphIntegrityCheckDictionary(this, ModifyInspector.IdentifiableExplore);
        }

        public override int GetHashCode()
        {
            return id == null ?
                base.GetHashCode() :
                GetType().FullName.GetHashCode() ^ id.Value;
        }
    }

    public interface IIdentifiable
    {
        int Id { get; }
        int? IdOrNull { get; }
        string ToStr { get; }
        bool IsNew { get; }
    }
}

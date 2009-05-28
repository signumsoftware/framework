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

        [DoNotValidate]
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

        public int? IdOrNull
        {
            get { return id; }
        }

        public string ToStr
        {
            get { return toStr; }
            private set { Set(ref toStr, value, "ToStr"); }
        }

        public bool IsNew { get { return id == null; } }

        protected internal override void PreSaving()
        {
            base.PreSaving();

            toStr = ToString();

            if (this is ICorrupt)
            {
                ICorrupt corrupt = (ICorrupt)this;
                if (corrupt.Corrupt)
                {
                    using (Corruption.Deny())
                    {
                        string integrity = IdentifiableIntegrityCheck();
                        if (string.IsNullOrEmpty(integrity))
                            corrupt.Corrupt = false;
                    }
                }
            }
        }

        public override string ToString()
        {
            return "{0} ({1})".Formato(GetType().Name, IsNew ? Resources.New : id.ToString());
        }

        public bool EqualsIdent(IdentifiableEntity ident)
        {
            if (this == ident)
                return true; 

            if (ident.GetType() == this.GetType() && !this.IsNew && this.id == ident.id)
                return true;

            return false; 
        }

        public override bool Equals(object obj)
        {
            if(obj == this)
                return true;

            if(obj == null)
                return false;

            if (obj is IdentifiableEntity)
                return EqualsIdent((IdentifiableEntity)obj);

            if (obj is Lazy)
                return ((Lazy)obj).EqualsIdent(this); 

            return false;
        }

        public string IdentifiableIntegrityCheck()
        {
            return GraphExplorer.GraphIntegrityCheck(this, ModifyInspector.IdentifiableExplore); 
        }

        public Dictionary<Modifiable,string> IdentifiableIntegrityCheckDictionary()
        {
            return GraphExplorer.GraphIntegrityCheckDictionary(this, ModifyInspector.IdentifiableExplore);
        }

        public override int GetHashCode()
        {
            return IsNew ?
                base.GetHashCode() :
                GetType().FullName.GetHashCode() ^ id.Value;
        }
    }

    public interface ICorrupt : IIdentifiable
    {
        bool Corrupt { get; set; }
    }

    public static class Corruption
    {
        [ThreadStatic]
        static bool allowed = false;

        public static bool Denied { get { return !allowed; } }

        public static IDisposable Allow()
        {
            if (allowed) return null;
            allowed = true;
            return new Disposable(() => allowed = false);
        }

        public static IDisposable Deny()
        {
            if (!allowed) return null; 
            allowed = false;
            return new Disposable(() => allowed = true);
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

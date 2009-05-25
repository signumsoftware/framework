using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities.Authorization
{
    [Serializable]
    public class RuleTypeDN : IdentifiableEntity
    {
        RoleDN role;
        [NotNullValidator]
        public RoleDN Role
        {
            get { return role; }
            set { Set(ref role, value, "Role"); }
        }

        TypeDN type;
        [NotNullValidator]
        public TypeDN Type
        {
            get { return type; }
            set { Set(ref type, value, "Type"); }
        }

        TypeAccess access;
        public TypeAccess Access
        {
            get { return access; }
            set { Set(ref access, value, "TypeAccess"); }
        }
    }

    //Only for client-side communication
    [Serializable]
    public class TypeAccessRule : EmbeddedEntity
    {
        public TypeAccessRule(TypeAccess accessBase)
        {
            this.accessBase = accessBase;
        }

        TypeAccess accessBase;
        public TypeAccess AccessBase
        {
            get { return accessBase; }
        }

        TypeAccess? accessOverride;
        public TypeAccess Access
        {
            get { return accessOverride ?? accessBase; }
            set
            {
                TypeAccess? val = value == accessBase ? (TypeAccess?)null : value;

                if (Set(ref accessOverride, val, "Access"))
                {
                    Notify("Overriden");
                    Notify("Create");
                    Notify("Modify");
                    Notify("Read");
                    Notify("None");
                }
            }
        }

        public bool Create
        {
            get { return Access == TypeAccess.Create; }
            set { if (value) Access = TypeAccess.Create; }
        }

        public bool Modify
        {
            get { return Access == TypeAccess.Modify; }
            set { if (value) Access = TypeAccess.Modify; }
        }

        public bool Read
        {
            get { return Access == TypeAccess.Read; }
            set { if (value) Access = TypeAccess.Read; }
        }

        public bool None
        {
            get { return Access == TypeAccess.None; }
            set { if (value) Access = TypeAccess.None; }
        }

        public bool Overriden
        {
            get { return accessOverride.HasValue; }
        }

        IdentifiableEntity resource;
        public IdentifiableEntity Resource
        {
            get { return resource; }
            set { Set(ref resource, value, "Resource"); }
        }
    }

    public enum TypeAccess
    {
        None,
        Read,
        Modify,
        Create,
    }
}

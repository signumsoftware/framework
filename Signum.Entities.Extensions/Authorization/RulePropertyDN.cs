using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;

namespace Signum.Entities.Authorization
{
    //Only for client-side communication
    [Serializable]
    public class AccessRule : EmbeddedEntity
    {
        public AccessRule(Access accessBase)
        {
            this.accessBase = accessBase;
        }

        Access accessBase;
        public Access AccessBase
        {
            get { return accessBase; }
        }

        Access? accessOverride;
        public Access Access
        {
            get { return accessOverride ?? accessBase; }
            set
            {
                Access? val = value == accessBase ? (Access?)null : value;

                if (Set(ref accessOverride, val, "Access"))
                {
                    Notify("Overriden");
                    Notify("Modify");
                    Notify("Read");
                    Notify("None");
                }
            }
        }

        public bool Modify
        {
            get { return Access == Access.Modify; }
            set { if (value) Access = Access.Modify; }
        }

        public bool Read
        {
            get { return Access == Access.Read; }
            set { if (value) Access = Access.Read; }
        }

        public bool None
        {
            get { return Access == Access.None; }
            set { if (value) Access = Access.None; }
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

    public enum Access
    {
        None,
        Read,
        Modify,
    }

    [Serializable]
    public class RulePropertyDN : IdentifiableEntity
    {
        RoleDN role;
        [NotNullValidator]
        public RoleDN Role
        {
            get { return role; }
            set { Set(ref role, value, "Role"); }
        }

        PropertyDN property;
        [NotNullValidator]
        public PropertyDN Property
        {
            get { return property; }
            set { Set(ref property, value, "Property"); }
        }

        Access access;
        public Access Access
        {
            get { return access; }
            set { Set(ref access, value, "Access"); }
        }
    }
}

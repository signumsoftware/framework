using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using Signum.Entities;
using Signum.Utilities;

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

                if (Set(ref accessOverride, val, () => Access))
                {
                    Notify(()=>Overriden);
                    Notify(()=>Modify);
                    Notify(()=>Read);
                    Notify(()=>None);
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
            set { Set(ref resource, value, () => Resource); }
        }
    }

    [Serializable]
    public class EntityGroupRule : EmbeddedEntity
    {
        public EntityGroupRule(bool allowedInBase, bool allowedOutBase)
        {
            this.allowedInBase = allowedInBase;
            this.allowedOutBase = allowedOutBase; 
        }

        bool allowedInBase;
        public bool AllowedInBase
        {
            get { return allowedInBase; }
        }

        bool allowedOutBase;
        public bool AllowedOutBase
        {
            get { return allowedOutBase; }
        }

        bool? allowedInOverride;
        public bool AllowedIn
        {
            get { return allowedInOverride ?? allowedInBase; }
            set
            {
                bool? val = value == allowedInBase ? (bool?)null : value;

                if (Set(ref allowedInOverride, val, () => AllowedIn))
                {
                    Notify(() => Overriden);
                }
            }
        }

        bool? allowedOutOverride;
        public bool AllowedOut
        {
            get { return allowedOutOverride ?? allowedOutBase; }
            set
            {
                bool? val = value == allowedOutBase ? (bool?)null : value;

                if (Set(ref allowedOutOverride, val, () => AllowedOut))
                {
                    Notify(() => Overriden);
                }
            }
        }

        public bool Overriden
        {
            get { return allowedInOverride.HasValue || allowedOutOverride.HasValue; }
        }

        EntityGroupDN group;
        public EntityGroupDN Group
        {
            get { return group; }
            set { Set(ref group, value, () => Group); }
        }
    }


    //Only for client-side communication
    [Serializable]
    public class AllowedRule : EmbeddedEntity
    {
        public AllowedRule(bool allowedBase)
        {
            this.allowedBase = allowedBase;
        }

        bool allowedBase;
        public bool AllowedBase
        {
            get { return allowedBase; }
        }

        bool? allowedOverride;
        public bool Allowed
        {
            get { return allowedOverride ?? allowedBase; }
            set
            {
                bool? val = value == allowedBase ? (bool?)null : value;

                if (Set(ref allowedOverride, val, () => Allowed))
                {
                    Notify(()=>Overriden);
                }
            }
        }

        public bool Overriden
        {
            get { return allowedOverride.HasValue; }
        }

        IdentifiableEntity resource;
        public IdentifiableEntity Resource
        {
            get { return resource; }
            set { Set(ref resource, value, () => Resource); }
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

                if (Set(ref accessOverride, val, () => Access))
                {
                    Notify(()=>Overriden);
                    Notify(()=>Create);
                    Notify(()=>Modify);
                    Notify(()=>Read);
                    Notify(()=>None);
                }
            }
        }

        public static readonly TypeAccess CreateKey = (TypeAccess)4; 
        public bool Create
        {
            get { return Access.HasFlag(CreateKey); }
            set
            {
                if (value)
                    Access = Access | TypeAccess.CreateOnly;
                else
                    Access = Access & ~CreateKey;
            }
        }

        public static readonly TypeAccess ModifyKey = (TypeAccess)2; 
        public bool Modify
        {
            get { return Access.HasFlag(ModifyKey); }
            set
            {
                if (value)
                    Access = Access | TypeAccess.ModifyOnly;
                else
                    Access = Access & ~ModifyKey;
            }
        }

        public bool Read
        {
            get { return Access.HasFlag(TypeAccess.Read); }
            set { if (value) Access = Access | TypeAccess.Read; }
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

        TypeDN type;
        public TypeDN Type
        {
            get { return type; }
            set { Set(ref type, value, () => Type); }
        }
    }
}

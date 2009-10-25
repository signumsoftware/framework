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

    //[Serializable]
    //public class EntityGroupAccessRule : EmbeddedEntity
    //{
    //    public EntityGroupAccessRule(Access accessInBase, Access accessOutBase)
    //    {
    //        this.accessInBase = accessInBase;
    //    }

    //    Access accessInBase;
    //    public Access AccessInBase
    //    {
    //        get { return accessInBase; }
    //    }

    //    Access accessOutBase;
    //    public Access AccessOutBase
    //    {
    //        get { return accessOutBase; }
    //    }

    //    Access? accessInOverride;
    //    public Access AccessIn
    //    {
    //        get { return accessInOverride ?? accessInBase; }
    //        set
    //        {
    //            Access? val = value == accessInBase ? (Access?)null : value;

    //            if (Set(ref accessInOverride, val, "AccessIn"))
    //            {
    //                Notify("Overriden");
    //                Notify("ModifyIn");
    //                Notify("ReadIn");
    //                Notify("NoneIn");
    //            }
    //        }
    //    }

    //    Access? accessOutOverride;
    //    public Access AccessOut
    //    {
    //        get { return accessOutOverride ?? accessOutBase; }
    //        set
    //        {
    //            Access? val = value == accessOutBase ? (Access?)null : value;

    //            if (Set(ref accessOutOverride, val, "AccessOut"))
    //            {
    //                Notify("Overriden");
    //                Notify("ModifyOut");
    //                Notify("ReadOut");
    //                Notify("NoneOut");
    //            }
    //        }
    //    }

    //    public bool ModifyIn
    //    {
    //        get { return AccessIn == Access.Modify; }
    //        set { if (value) AccessIn = Access.Modify; }
    //    }

    //    public bool ReadIn
    //    {
    //        get { return AccessIn == Access.Read; }
    //        set { if (value) AccessIn = Access.Read; }
    //    }

    //    public bool NoneIn
    //    {
    //        get { return AccessIn == Access.None; }
    //        set { if (value) AccessIn = Access.None; }
    //    }

    //    public bool ModifyOut
    //    {
    //        get { return AccessOut == Access.Modify; }
    //        set { if (value) AccessOut = Access.Modify; }
    //    }

    //    public bool ReadOut
    //    {
    //        get { return AccessOut == Access.Read; }
    //        set { if (value) AccessOut = Access.Read; }
    //    }

    //    public bool NoneOut
    //    {
    //        get { return AccessOut == Access.None; }
    //        set { if (value) AccessOut = Access.None; }
    //    }

    //    public bool Overriden
    //    {
    //        get { return accessInOverride.HasValue || accessOutOverride.HasValue; }
    //    }

    //    EntityGroupDN group;
    //    public EntityGroupDN Group
    //    {
    //        get { return group; }
    //        set { Set(ref group, value, "Group"); }
    //    }
    //}


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

                if (Set(ref allowedOverride, val, "Allowed"))
                {
                    Notify("Overriden");
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
            set { Set(ref resource, value, "Resource"); }
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
}

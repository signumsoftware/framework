using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;

namespace Signum.Web
{
    public abstract class EntitySettings
    {
        public abstract Type StaticType { get; }

     
        public string UrlName { get; set; }
        public string TypeName { get; set; }

        public abstract Mapping UntypedMappingDefault { get; }
        public abstract Mapping UntypedMappingAdmin { get; }

        public abstract bool OnIsReadOnly(ModifiableEntity entity, bool isAdmin);
        public abstract bool OnIsViewable(ModifiableEntity entity, bool isAdmin);
        public abstract bool OnIsNavigable(ModifiableEntity entity, bool isAdmin);
        public abstract bool OnIsCreable(bool isAdmin);
        public abstract bool OnShowSave(); 

        public abstract string OnPartialViewName(ModifiableEntity entity);
    }

    public class EntitySettings<T> : EntitySettings where T : ModifiableEntity
    {
        public override Type StaticType
        {
            get { return typeof(T); }
        }

        public EntityMapping<T> MappingDefault { get; set; }
        public EntityMapping<T> MappingAdmin { get; set; }

        public override Mapping UntypedMappingDefault { get { return MappingDefault; } }
        public override Mapping UntypedMappingAdmin { get { return MappingAdmin; } }

        public override bool OnIsReadOnly(ModifiableEntity entity, bool isAdmin)
        {
            if (IsReadOnly != null)
                foreach (Func<T, bool, bool> item in IsReadOnly.GetInvocationList())
                {
                    if (item((T)entity, isAdmin))
                        return true;
                }

            return false;
        }

        public override bool OnIsViewable(ModifiableEntity entity, bool isAdmin)
        {
            if (PartialViewName == null)
                return false;

            if (IsViewable != null)
                foreach (Func<T, bool, bool> item in IsViewable.GetInvocationList())
                {
                    if (!item((T)entity, isAdmin))
                        return false;
                }

            return true;
        }

        public override bool OnIsNavigable(ModifiableEntity entity, bool isAdmin)
        {
            if (PartialViewName == null)
                return false;

            if (IsNavigable != null)
                foreach (Func<T, bool, bool> item in IsNavigable.GetInvocationList())
                {
                    if (!item((T)entity, isAdmin))
                        return false;
                }

            return true;
        }

        public override bool OnIsCreable(bool isAdmin)
        {
            if (IsCreable != null)
                foreach (Func<bool, bool> item in IsCreable.GetInvocationList())
                {
                    if (!item(isAdmin))
                        return false;
                }

            return true;
        }

        public override bool OnShowSave()
        {
            return ShowSave;
        }

        public Func<bool, bool> IsCreable { get; set; }
        public Func<T, bool, bool> IsReadOnly { get; set; }
        public Func<T, bool, bool> IsViewable { get; set; }
        public Func<T, bool, bool> IsNavigable{ get; set; }      

        public bool ShowSave { get; set; }
   
        public Func<T, string> PartialViewName { get; set; }
        
        public override string OnPartialViewName(ModifiableEntity entity)
        {
            return PartialViewName((T)entity);
        }
        
        public EntitySettings(EntityType entityType)
        {
            switch (entityType)
            {
                case EntityType.Default:
                    ShowSave = true;
                    MappingAdmin = MappingDefault = new EntityMapping<T>(true);
                    break;
                case EntityType.Admin:
                    ShowSave = true;
                    //IsReadOnly = admin => !admin;
                    IsCreable = admin => admin;
                    IsViewable = (_, admin) => admin;
                    IsNavigable = (_, admin) => admin;
                    MappingAdmin = new EntityMapping<T>(true);
                    MappingDefault = new EntityMapping<T>(false);
                    break;
                case EntityType.NotSaving:
                    ShowSave = false;
                    MappingAdmin = MappingDefault = new EntityMapping<T>(true);
                    break;
                case EntityType.ServerOnly:
                    ShowSave = false;
                    IsReadOnly = (_, admin) => true;
                    IsCreable = admin => false;
                    MappingAdmin = MappingDefault = new EntityMapping<T>(false);
                    break;
                case EntityType.Content:
                    ShowSave = false;
                    IsCreable = admin => false;
                    IsViewable = (_, admin) => false;
                    IsNavigable = (_, admin) => false;
                    MappingAdmin = null;
                    MappingDefault = new EntityMapping<T>(true);
                    break;
                default:
                    break;
            }
        }
    }

    public enum WindowType
    {
        View,
        Find,
        Admin
    }

    public enum EntityType
    {
        Admin,
        Default,
        NotSaving,
        ServerOnly,
        Content,
    }
}

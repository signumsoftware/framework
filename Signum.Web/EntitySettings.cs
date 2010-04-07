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
        
        public Func<bool, bool> IsCreable { get; set; }
        public Func<bool, bool> IsViewable { get; set; }
        public Func<bool, bool> IsNavigable { get; set; }
        public bool IsReadOnly { get; set; }
        public Func<bool, bool> ShowOkSave { get; set; }

        public abstract bool HasPartialViewName { get; }

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
   
        public Func<T, string> PartialViewName { get; set; }
        public override bool HasPartialViewName
        {
            get { return PartialViewName == null; }
        }

        public override string OnPartialViewName(ModifiableEntity entity)
        {
            return PartialViewName((T)entity);
        }
        
        public EntitySettings(EntityType entityType)
        {
            switch (entityType)
            {
                case EntityType.Default:
                    ShowOkSave = _ => true;
                    MappingAdmin = MappingDefault = new EntityMapping<T>(true);
                    break;
                case EntityType.Admin:
                    ShowOkSave = _ => true;
                    //IsReadOnly = admin => !admin;
                    IsCreable = admin => admin;
                    IsViewable = admin => admin;
                    IsNavigable = admin => admin;
                    MappingAdmin = new EntityMapping<T>(true);
                    MappingDefault = new EntityMapping<T>(false);
                    break;
                case EntityType.NotSaving:
                    ShowOkSave = _ => false;
                    MappingAdmin = MappingDefault = new EntityMapping<T>(true);
                    break;
                case EntityType.ServerOnly:
                    ShowOkSave = _ => false;
                    IsReadOnly = true;
                    IsCreable = admin => false;
                    MappingAdmin = MappingDefault = new EntityMapping<T>(false);
                    break;
                case EntityType.Content:
                    ShowOkSave = _ => false;
                    IsCreable = admin => false;
                    IsViewable = admin => false;
                    IsNavigable = admin => false;
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

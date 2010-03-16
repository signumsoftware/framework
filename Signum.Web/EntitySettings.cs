using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;

namespace Signum.Web
{
    public class EntitySettings
    {
        public Func<ModifiableEntity, string> PartialViewName { get; set; }
        public string UrlName { get; set; }
        public string TypeName { get; set; }

        public Func<bool, bool> IsCreable { get; set; }
        public Func<bool, bool> IsViewable { get; set; }
        public Func<bool, bool> IsNavigable { get; set; }
        public bool IsReadOnly { get; set; }
        public Func<bool, bool> ShowOkSave { get; set; }

        public EntitySettings(EntityType entityType)
        {
            switch (entityType)
            {
                case EntityType.Default:
                    ShowOkSave = _ => true;
                    break;
                case EntityType.Admin:
                    ShowOkSave = _ => true;
                    //IsReadOnly = admin => !admin;
                    IsCreable = admin => admin;
                    IsViewable = admin => admin;
                    IsNavigable = admin => admin;
                    break;
                case EntityType.NotSaving:
                    ShowOkSave = _ => false;
                    break;
                case EntityType.ServerOnly:
                    ShowOkSave = _ => false;
                    IsReadOnly = true;
                    IsCreable = admin => false;
                    break;
                case EntityType.Content:
                    ShowOkSave = _ => false;
                    IsCreable = admin => false;
                    IsViewable = admin => false;
                    IsNavigable = admin => false;
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

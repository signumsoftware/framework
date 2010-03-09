using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;

namespace Signum.Web
{
    public class EntitySettings
    {
        public Func<ModifiableEntity, string> PartialViewName;
        public string UrlName;

        public Func<bool, bool> IsCreable { get; set; }
        public Func<bool, bool> IsViewable { get; set; }
        public Func<bool, bool> IsNavigable { get; set; }
        public Func<bool, bool> IsReadOnly { get; set; }
        public Func<bool, bool> ShowOkSave { get; set; }

        public EntitySettings(bool isSimpleType)
        {
            if (isSimpleType)
            {
                IsCreable = admin => admin;
                IsNavigable = admin => admin;
                IsViewable = admin => admin;
                IsReadOnly = admin => !admin;
            }
            else
            {
                IsCreable = admin => true;
                IsNavigable = admin => true;
                IsViewable = admin => true;
                IsReadOnly = admin => false;
            }
        }
    }
}

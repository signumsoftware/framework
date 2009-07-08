using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Web
{
    public class EntitySettings
    {
        public string PartialViewName;
        public string UrlName;

        public Func<bool, bool> IsCreable;
        public Func<bool, bool> IsViewable;
        public Func<bool, bool> IsReadOnly;
        
        public EntitySettings(bool isSimpleType)
        {
            if (isSimpleType)
            {
                IsCreable = admin => admin;
                IsViewable = admin => admin;
                IsReadOnly = admin => !admin;
            }
            else
            {
                IsCreable = admin => true;
                IsViewable = admin => true;
                IsReadOnly = admin => false;
            }
        }
    }
}

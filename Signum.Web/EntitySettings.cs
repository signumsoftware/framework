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

        public Func<bool, bool> IsCreable { get; set; }
        public Func<bool, bool> IsPopupViewable { get; set; }
        public Func<bool, bool> IsViewable { get; set; }
        public Func<bool, bool> IsReadOnly { get; set; }
        public Func<bool, bool> ShowOkSave { get; set; }

        public EntitySettings(bool isSimpleType)
        {
            if (isSimpleType)
            {
                IsCreable = admin => admin;
                IsViewable = admin => admin;
                IsPopupViewable = admin => admin;
                IsReadOnly = admin => !admin;
            }
            else
            {
                IsCreable = admin => true;
                IsViewable = admin => true;
                IsPopupViewable = admin => true;
                IsReadOnly = admin => false;
            }
        }
    }
}

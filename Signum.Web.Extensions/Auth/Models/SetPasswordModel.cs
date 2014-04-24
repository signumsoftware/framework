#region usings
using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Threading;
using Signum.Entities.Authorization;
using Signum.Engine;
using Signum.Engine.Authorization;
using Signum.Services;
using Signum.Utilities;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using Signum.Entities;
using Signum.Engine.Mailing;
using System.Collections.Generic;
using Signum.Engine.Operations;
using Signum.Web.Operations;
using System.ComponentModel;
#endregion


namespace Signum.Web.Auth
{
    [Serializable, Description("")]
    public class SetPasswordModel : ModelEntity
    {
        string password;
        [NotNullValidator]
        public string Password
        {
            get { return password; }
            set { Set(ref password, value); }
        }
    }
}
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
using Signum.Web.Extensions.Properties;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using Signum.Entities;
using Signum.Engine.Mailing;
using System.Collections.Generic;
using Signum.Engine.Operations;
using Signum.Entities.Extensions.Authorization;
using Signum.Web.Operations;
using System.ComponentModel;
#endregion


namespace Signum.Web.Auth
{
    [Serializable, Description("")]
    public class SetPasswordModel : ModelEntity
    {
        Lite<UserDN> user;
        [NotNullValidator]
        public Lite<UserDN> User
        {
            get { return user; }
            set { Set(ref user, value, () => User); }
        }

        string password;
        [NotNullValidator]
        public string Password
        {
            get { return password; }
            set { Set(ref password, value, () => Password); }
        }

        string repeatPassword;
        [NotNullValidator]
        public string RepeatPassword
        {
            get { return repeatPassword; }
            set { Set(ref repeatPassword, value, () => RepeatPassword); }
        }

        protected override string PropertyValidation(System.Reflection.PropertyInfo pi)
        {
            if (pi.Is(() => RepeatPassword) && RepeatPassword != Password)
                return Resources.PasswordsAreDifferent;

            return null;
        }    
    
    }
}
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities.Basics
{
    public interface IUserDN : IIdentifiable
    {
    }

    public static class UserHolder
    {
        public static readonly string UserSessionKey = "user";

        public static readonly SessionVariable<IUserDN> CurrentUserVariable = Statics.SessionVariable<IUserDN>(UserSessionKey);
        public static IUserDN Current
        {
            get { return CurrentUserVariable.Value; }
            set { CurrentUserVariable.Value = value; }
        }
    }
}

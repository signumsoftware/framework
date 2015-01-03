using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities.Basics
{
    public interface IUserEntity : IEntity
    {
    }

    public static class UserHolder
    {
        public static readonly string UserSessionKey = "user";

        public static readonly SessionVariable<IUserEntity> CurrentUserVariable = Statics.SessionVariable<IUserEntity>(UserSessionKey);
        public static IUserEntity Current
        {
            get { return CurrentUserVariable.Value; }
            set { CurrentUserVariable.Value = value; }
        }
    }
}

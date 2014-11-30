using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Authorization;
using System.Reflection;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Engine.Mailing;
using Signum.Engine.Operations;
using Signum.Utilities;

namespace Signum.Engine.Authorization
{
    public static class PasswordExpirationLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<PasswordExpiresIntervalEntity>();

                dqm.RegisterQuery(typeof(PasswordExpiresIntervalEntity), ()=>
                    from e in Database.Query<PasswordExpiresIntervalEntity>()
                     select new
                     {
                         Entity = e,
                         e.Id,
                         e.Enabled,
                         e.Days,
                         e.DaysWarning
                     });

                new Graph<PasswordExpiresIntervalEntity>.Execute(PasswordExpiresIntervalOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (pei, _) => { },
                }.Register();

                AuthLogic.UserLogingIn += (u =>
                {
                    if (u.PasswordNeverExpires)
                        return;

                    var ivp = Database.Query<PasswordExpiresIntervalEntity>().Where(p => p.Enabled).FirstOrDefault();
                    if (ivp == null)
                        return;
                    
                    if (TimeZoneManager.Now > u.PasswordSetDate.AddDays((double)ivp.Days))
                        throw new PasswordExpiredException(AuthMessage.ExpiredPassword.NiceToString());
                });

                AuthLogic.LoginMessage += (() =>
                {
                    UserEntity u = UserEntity.Current;

                    if (u.PasswordNeverExpires)
                        return null;

                    PasswordExpiresIntervalEntity ivp = null;
                    using (AuthLogic.Disable())
                        ivp = Database.Query<PasswordExpiresIntervalEntity>().Where(p => p.Enabled).FirstOrDefault();
                    
                    if (ivp == null)
                        return null;

                    if (TimeZoneManager.Now > u.PasswordSetDate.AddDays((double)ivp.Days).AddDays((double)-ivp.DaysWarning))
                        return AuthMessage.YourPasswordIsNearExpiration.NiceToString();

                    return null;
                });
            }
        }
    }
}

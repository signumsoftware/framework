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
using Signum.Engine.Extensions.Properties;

namespace Signum.Engine.Authorization
{
    public static class PasswordExpiresLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<PasswordExpiresIntervalDN>();

                dqm[typeof(PasswordExpiresIntervalDN)] =
                    (from e in Database.Query<PasswordExpiresIntervalDN>()
                     select new
                     {
                         Entity = e.ToLite(),
                         e.Id,
                         e.Enabled,
                         e.Days,
                         e.DaysWarning
                     }).ToDynamic();

                AuthLogic.UserLogingIn += (u =>
                {
                    if (u.PasswordNeverExpires)
                        return;

                    var ivp = Database.Query<PasswordExpiresIntervalDN>().Where(p => p.Enabled).FirstOrDefault();
                    if (ivp == null)
                        return;

                    if (TimeZoneManager.Now > u.PasswordSetDate.AddDays((double)ivp.Days))
                        throw new PasswordExpiredException(Signum.Engine.Extensions.Properties.Resources.ExpiredPassword);

                });

                AuthLogic.LoginMessage += (() =>
                {
                    UserDN u = UserDN.Current;

                    if (u.PasswordNeverExpires)
                        return null;

                    var ivp = Database.Query<PasswordExpiresIntervalDN>().Where(p => p.Enabled).FirstOrDefault();
                    if (ivp == null)
                        return null;

                    if (TimeZoneManager.Now > u.PasswordSetDate.AddDays((double)ivp.Days).AddDays((double)-ivp.DaysWarning))
                        return Resources.PasswordNearExpired;

                    return null;
                });
            }
        }
    }
}

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
using Signum.Utilities;
using Signum.Entities.Mailing;
using Signum.Engine.Basics;
using System.Globalization;
using Signum.Engine.Translation;


namespace Signum.Engine.Authorization
{
    public static class ResetPasswordRequestLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<ResetPasswordRequestEntity>();

                dqm.RegisterQuery(typeof(ResetPasswordRequestEntity), () =>
                    from e in Database.Query<ResetPasswordRequestEntity>()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.RequestDate,
                        e.Code,
                        e.User,
                        e.User.Email
                    });

                EmailLogic.AssertStarted(sb);

                SystemEmailLogic.RegisterSystemEmail<ResetPasswordRequestMail>(() => new EmailTemplateEntity
                {
                    Messages = CultureInfoLogic.ForEachCulture(culture => new EmailTemplateMessageEntity(culture)
                    {
                        Text = "<p>{0}</p>".FormatWith(AuthEmailMessage.YouRecentlyRequestedANewPassword.NiceToString()) +
                            "<p>{0} @[User.UserName]</p>".FormatWith(AuthEmailMessage.YourUsernameIs.NiceToString()) +
                            "<p>{0}</p>".FormatWith(AuthEmailMessage.YouCanResetYourPasswordByFollowingTheLinkBelow.NiceToString()) +
                            "<p><a href=\"@[m:Url]\">@[m:Url]</a></p>",
                        Subject = AuthEmailMessage.ResetPasswordRequestSubject.NiceToString()
                    }).ToMList()
                });
            }
        }

        public static ResetPasswordRequestEntity ResetPasswordRequest(UserEntity user)
        {
            using (AuthLogic.Disable())
            {
                //Remove old previous requests
                Database.Query<ResetPasswordRequestEntity>()
                    .Where(r => r.User.Is(user) && r.RequestDate < TimeZoneManager.Now.AddMonths(1))
                    .UnsafeUpdate()
                    .Set(e => e.Lapsed, e => true)
                    .Execute();

                return new ResetPasswordRequestEntity()
                {
                    Code = MyRandom.Current.NextString(5),
                    User = user,
                    RequestDate = TimeZoneManager.Now,
                }.Save();
            }
        }

        public static Func<string, UserEntity> GetUserByEmail = (email) => Database.Query<UserEntity>().Where(u => u.Email == email).SingleOrDefaultEx();
    }

    public class ResetPasswordRequestMail : SystemEmail<ResetPasswordRequestEntity>
    {
        public string Url;

        public ResetPasswordRequestMail(ResetPasswordRequestEntity entity) : this(entity, "http://wwww.tesurl.com") 
        { }

        public ResetPasswordRequestMail(ResetPasswordRequestEntity entity, string url) :base(entity)
        {
            this.Url = url;
        }

        public override List<EmailOwnerRecipientData> GetRecipients()
        {
            return SendTo(Entity.User.EmailOwnerData);
        }
    }
}

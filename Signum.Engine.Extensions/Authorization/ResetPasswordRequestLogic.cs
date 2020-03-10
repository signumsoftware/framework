using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Mailing;
using Signum.Engine.Maps;
using Signum.Engine.Scheduler;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Mailing;
using Signum.Entities.Scheduler;
using Signum.Utilities;

namespace Signum.Engine.Authorization
{
    public static class ResetPasswordRequestLogic
    {
        public static void Start(SchemaBuilder sb)
        {
            if (!sb.NotDefined(MethodInfo.GetCurrentMethod()))
                return;

            sb.Include<ResetPasswordRequestEntity>()
                .WithQuery(() => e => new
                {
                    Entity = e,
                    e.Id,
                    e.RequestDate,
                    e.Code,
                    e.User,
                    e.User.Email
                });

            EmailLogic.AssertStarted(sb);

            SimpleTaskLogic.Register(ResetPasswordRequestTask.Timeout,
                (ScheduledTaskContext ctx) =>
                {
                    Database.Query<ResetPasswordRequestEntity>()
                        .Where(r => r.RequestDate < TimeZoneManager.Now.AddHours(24))
                        .UnsafeUpdate()
                        .Set(e => e.Lapsed, e => true)
                        .Execute();

                    return null;
                }
            );

            SystemEmailLogic.RegisterSystemEmail<ResetPasswordRequestMail>(() => new EmailTemplateEntity
            {
                Messages = CultureInfoLogic.ForEachCulture(culture => new EmailTemplateMessageEmbedded(culture)
                {
                    Text =
                        "<p>{0}</p>".FormatWith(AuthEmailMessage.YouRecentlyRequestedANewPassword.NiceToString()) +
                        "<p>{0} @[User.UserName]</p>".FormatWith(AuthEmailMessage.YourUsernameIs.NiceToString()) +
                        "<p>{0}</p>".FormatWith(AuthEmailMessage.YouCanResetYourPasswordByFollowingTheLinkBelow
                            .NiceToString()) +
                        "<p><a href=\"@[m:Url]\">@[m:Url]</a></p>",
                    Subject = AuthEmailMessage.ResetPasswordRequestSubject.NiceToString()
                }).ToMList()
            });

            SystemEmailLogic.RegisterSystemEmail<PasswordChangedMail>(() => new EmailTemplateEntity
            {
                Messages = CultureInfoLogic.ForEachCulture(culture => new EmailTemplateMessageEmbedded(culture)
                {
                    Text = $@"<p>{AuthEmailMessage.YourPasswordHasRecentlyBeenChanged.NiceToString()}</p>
                                  <p>{AuthEmailMessage.IfYouHaveNotChangedYourPasswordPleaseGetInContactWithUs.NiceToString()}</p>",
                    Subject = AuthEmailMessage.PasswordChangedSubject.NiceToString()
                }).ToMList()
            });
        }

        public static ResetPasswordRequestEntity ResetPasswordRequest(UserEntity user)
        {
            using (AuthLogic.Disable())
            {
                //Remove old previous requests
                Database.Query<ResetPasswordRequestEntity>()
                    .Where(r => r.User.Is(user) && !r.Lapsed)
                    .UnsafeUpdate()
                    .Set(e => e.Lapsed, e => true)
                    .Execute();

                return new ResetPasswordRequestEntity()
                {
                    Code = MyRandom.Current.NextString(32),
                    User = user,
                    RequestDate = TimeZoneManager.Now,
                }.Save();
            }
        }
    }
    
    [AutoInit]
    public static class ResetPasswordRequestTask
    {
        public static readonly SimpleTaskSymbol Timeout;
    }

    public class ResetPasswordRequestMail : SystemEmail<ResetPasswordRequestEntity>
    {
        public string Url;

        public ResetPasswordRequestMail(ResetPasswordRequestEntity entity) : this(entity, "http://wwww.tesurl.com") { }

        public ResetPasswordRequestMail(ResetPasswordRequestEntity entity, string url) : base(entity)
        {
            this.Url = url;
        }

        public override List<EmailOwnerRecipientData> GetRecipients()
        {
            return SendTo(Entity.User.EmailOwnerData);
        }
    }

    public class PasswordChangedMail : SystemEmail<ResetPasswordRequestEntity>
    {
        public PasswordChangedMail(ResetPasswordRequestEntity entity) : base(entity) { }

        public override List<EmailOwnerRecipientData> GetRecipients()
        {
            return SendTo(Entity.User.EmailOwnerData);
        }
    }
}

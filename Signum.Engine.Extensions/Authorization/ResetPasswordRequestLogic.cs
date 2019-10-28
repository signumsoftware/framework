using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Entities.Authorization;
using System.Reflection;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Engine.Mailing;
using Signum.Utilities;
using Signum.Entities.Mailing;
using Signum.Engine.Basics;
using Signum.Engine.Operations;
using Signum.Services;
using Signum.Entities.Basics;

namespace Signum.Engine.Authorization
{
    public static class ResetPasswordRequestLogic
    {
        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
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

                EmailModelLogic.RegisterEmailModel<ResetPasswordRequestEmail>(() => new EmailTemplateEntity
                {
                    DisableAuthorization = true,
                    Messages = CultureInfoLogic.ForEachCulture(culture => new EmailTemplateMessageEmbedded(culture)
                    {
                        Text = "<p>{0}</p>".FormatWith(AuthEmailMessage.YouRecentlyRequestedANewPassword.NiceToString()) +
                            "<p>{0} @[User.UserName]</p>".FormatWith(AuthEmailMessage.YourUsernameIs.NiceToString()) +
                            "<p>{0}</p>".FormatWith(AuthEmailMessage.YouCanResetYourPasswordByFollowingTheLinkBelow.NiceToString()) +
                            "<p><a href=\"@[m:Url]\">@[m:Url]</a></p>",
                        Subject = AuthEmailMessage.ResetPasswordRequestSubject.NiceToString()
                    }).ToMList()
                });



                new Graph<ResetPasswordRequestEntity>.Execute(ResetPasswordRequestOperation.Execute)
                {
                    CanBeNew = false,
                    CanBeModified = false,
                    CanExecute = (e) => e.Lapsed == false ? null : AuthEmailMessage.YourResetPasswordRequestHasExpired.NiceToString(),
                    Execute = (e, args) =>
                    {
                        string password = args.GetArg<string>();
                        e.Lapsed = true;
                        var user = e.User;

                        user.PasswordHash = Security.EncodePassword(password);
                        using (AuthLogic.Disable())
                            user.Execute(UserOperation.Save);
                    }
                }.Register();

            }
        }


        public static ResetPasswordRequestEntity ResetPasswordRequestExecute(string code, string password)
        {
            using (AuthLogic.Disable())
            {
                //Remove old previous requests
                var rpr = Database.Query<ResetPasswordRequestEntity>()
                     .Where(r => r.Code == code && !r.Lapsed)
                     .SingleOrDefault();

                using (UserHolder.UserSession(rpr.User))
                {
                    rpr.Execute(ResetPasswordRequestOperation.Execute, password);
                }
                return rpr;
            }
        }

        public static ResetPasswordRequestEntity SendResetPasswordRequestEmail(string email)
        {
            UserEntity user;
            using (AuthLogic.Disable())
            {
                user = Database.Query<UserEntity>()
                  .Where(u => u.Email == email && u.State != UserState.Disabled)
                .SingleOrDefault();

                if (user == null)
                    throw new ApplicationException(AuthEmailMessage.EmailNotFound.NiceToString());
            }
            var request= ResetPasswordRequest(user);

            string url = EmailLogic.Configuration.UrlLeft+ @"/auth/ResetPassword?code={0}".FormatWith(request.Code);

            using (AuthLogic.Disable())
                new ResetPasswordRequestEmail(request, url).SendMail();

            return request;
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

        public static Func<string, UserEntity?> GetUserByEmail = (email) => Database.Query<UserEntity>().Where(u => u.Email == email).SingleOrDefaultEx();
    }

    public class ResetPasswordRequestEmail : EmailModel<ResetPasswordRequestEntity>
    {
        public string Url;

        public ResetPasswordRequestEmail(ResetPasswordRequestEntity entity) : this(entity, "http://wwww.tesurl.com")
        { }

        public ResetPasswordRequestEmail(ResetPasswordRequestEntity entity, string url) : base(entity)
        {
            this.Url = url;
        }

        public override List<EmailOwnerRecipientData> GetRecipients()
        {
            return SendTo(Entity.User.EmailOwnerData);
        }
    }
}

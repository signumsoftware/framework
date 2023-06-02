using Signum.Authorization;
using Signum.Basics;
using Signum.Mailing;
using Signum.Mailing.Templates;

namespace Signum.Authorization.ResetPassword;

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


        AuthLogic.OnDeactivateUser = u =>
        {
            var config = EmailLogic.Configuration;
            var request = ResetPasswordRequestLogic.ResetPasswordRequest(u);
            var url = $"{config.UrlLeft}/auth/resetPassword?code={request.Code}";

            var mail = new UserLockedMail(u, url);
            mail.SendMailAsync();
        };

        EmailLogic.AssertStarted(sb);

        EmailModelLogic.RegisterEmailModel<ResetPasswordRequestEmail>(() => new EmailTemplateEntity
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

        EmailModelLogic.RegisterEmailModel<UserLockedMail>(() => new EmailTemplateEntity
        {
            Messages = CultureInfoLogic.ForEachCulture(culture => new EmailTemplateMessageEmbedded(culture)
            {
                Text =
                    "<p>{0}</p>".FormatWith(AuthEmailMessage.YourAccountHasBeenLockedDueToSeveralFailedLogins.NiceToString()) +
                    "<p>{0}</p>".FormatWith(AuthEmailMessage.YouCanResetYourPasswordByFollowingTheLinkBelow.NiceToString()) +
                    "<p><a href=\"@[m:Url]\">@[m:Url]</a></p>",
                Subject = AuthEmailMessage.YourAccountHasBeenLocked.NiceToString()
            }).ToMList()
        });

        new Graph<ResetPasswordRequestEntity>.Execute(ResetPasswordRequestOperation.Execute)
            {
                CanBeNew = false,
                CanBeModified = false,
                CanExecute = (e) => e.IsValid ? null : AuthEmailMessage.YourResetPasswordRequestHasExpired.NiceToString(),
                Execute = (e, args) =>
                {
                    string password = args.GetArg<string>();
                    e.Used = true;
                    var user = e.User;

                    var error = UserEntity.OnValidatePassword(password);
                    if (error != null)
                        throw new ApplicationException(error);

                    if (user.State == UserState.Deactivated)
                    {
                        user.Execute(UserOperation.Reactivate);
                    }
                    
                    user.PasswordHash = PasswordEncoding.EncodePassword(password);
                    user.LoginFailedCounter = 0;
                    using (AuthLogic.Disable())
                    {
                        user.Execute(UserOperation.Save);
                    }
                }
            }.Register();
    }

    public static ResetPasswordRequestEntity ResetPasswordRequestExecute(string code, string password)
    {
        using (AuthLogic.Disable())
        {
            var rpr = Database.Query<ResetPasswordRequestEntity>()
                 .Where(r => r.Code == code && r.IsValid)
                 .SingleEx();

            using (UserHolder.UserSession(rpr.User))
            {
                rpr.Execute(ResetPasswordRequestOperation.Execute, password);
            }
            return rpr;
        }
    }

    public static ResetPasswordRequestEntity SendResetPasswordRequestEmail(string email)
    {
        UserEntity? user;
        using (AuthLogic.Disable())
        {
            user = Database
                .Query<UserEntity>()
                .SingleOrDefault(u => u.Email == email && u.State != UserState.Deactivated);

            if (user == null)
                throw new ApplicationException(AuthEmailMessage.EmailNotFound.NiceToString());
        }

        try
        {
            var request = ResetPasswordRequest(user);

            string url = EmailLogic.Configuration.UrlLeft + @"/auth/ResetPassword?code={0}".FormatWith(request.Code);

            using (AuthLogic.Disable())
                new ResetPasswordRequestEmail(request, url).SendMail();

            return request;
        }
        catch (Exception ex)
        {
            ex.LogException();
            throw new ApplicationException(LoginAuthMessage.AnErrorOccurredRequestNotProcessed.NiceToString());
        }

    }

    public static ResetPasswordRequestEntity ResetPasswordRequest(UserEntity user)
    {
        using (OperationLogic.AllowSave<UserEntity>())
        using (AuthLogic.Disable())
        {
            //Remove old previous requests
            Database.Query<ResetPasswordRequestEntity>()
                .Where(r => r.User.Is(user) && r.IsValid)
                .UnsafeUpdate()
                .Set(e => e.Used, e => true)
                .Execute();

            return new ResetPasswordRequestEntity
            {
                Code = MyRandom.Current.NextString(32),
                User = user,
                RequestDate = Clock.Now,
            }.Save();
        }
    }
}
    
public class ResetPasswordRequestEmail : EmailModel<ResetPasswordRequestEntity>
{
    public string Url;

    public ResetPasswordRequestEmail(ResetPasswordRequestEntity entity) : this(entity, "http://wwww.tesurl.com") { }

    public ResetPasswordRequestEmail(ResetPasswordRequestEntity entity, string url) : base(entity)
    {
        this.Url = url;
    }

    public override List<EmailOwnerRecipientData> GetRecipients()
    {
        return SendTo(Entity.User.EmailOwnerData);
    }
}

public class UserLockedMail : EmailModel<UserEntity>
{
    public string Url;

    public UserLockedMail(UserEntity entity) : this(entity, "http://testurl.com") { }

    public UserLockedMail(UserEntity entity, string url) : base(entity)
    {
        this.Url = url;
    }

    public override List<EmailOwnerRecipientData> GetRecipients()
    {
        return SendTo(Entity.EmailOwnerData);
    }
}

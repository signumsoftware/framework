using Signum.Authorization;
using Signum.Basics;
using Signum.Mailing;
using Signum.Mailing.Templates;

namespace Signum.Authorization.ResetPassword;

public static class ResetPasswordRequestLogic
{
    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
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
                    "<p>{0}</p>".FormatWith(ResetPasswordMessage.YouRecentlyRequestedANewPassword.NiceToString()) +
                    "<p>{0} @[User.UserName]</p>".FormatWith(ResetPasswordMessage.YourUsernameIs.NiceToString()) +
                    "<p>{0}</p>".FormatWith(ResetPasswordMessage.YouCanResetYourPasswordByFollowingTheLinkBelow
                        .NiceToString()) +
                    "<p><a href=\"@[m:Url]\">@[m:Url]</a></p>",
                Subject = ResetPasswordMessage.ResetPasswordRequestSubject.NiceToString()
            }).ToMList()
        });

        EmailModelLogic.RegisterEmailModel<UserLockedMail>(() => new EmailTemplateEntity
        {
            Messages = CultureInfoLogic.ForEachCulture(culture => new EmailTemplateMessageEmbedded(culture)
            {
                Text =
                    "<p>{0}</p>".FormatWith(ResetPasswordMessage.YourAccountHasBeenLockedDueToSeveralFailedLogins.NiceToString()) +
                    "<p>{0}</p>".FormatWith(ResetPasswordMessage.YouCanResetYourPasswordByFollowingTheLinkBelow.NiceToString()) +
                    "<p><a href=\"@[m:Url]\">@[m:Url]</a></p>",
                Subject = ResetPasswordMessage.YourAccountHasBeenLocked.NiceToString()
            }).ToMList()
        });

        new Graph<ResetPasswordRequestEntity>.Execute(ResetPasswordRequestOperation.Execute)
        {
            CanBeNew = false,
            CanBeModified = false,
            CanExecute = (e) => e.Validate(),
            Execute = (e, args) =>
            {
                string password = args.GetArg<string>();
                e.Used = true;
                var user = e.User;

                var error = UserEntity.OnValidatePassword(password);
                if (error != null)
                    throw new ResetPasswordException(error);

                if (user.State == UserState.Deactivated)
                {
                    user.Execute(UserOperation.Reactivate);
                }

                user.PasswordHash = PasswordEncoding.EncodePassword(user.UserName, password);
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
                 .Where(r => r.Code == code)
                 .SingleOrDefaultEx();

            if (rpr == null)
                throw new ResetPasswordException(ResetPasswordMessage.TheCodeOfYourLinkIsIncorrect.NiceToString());

            var error = rpr.Validate();
            if (error.HasText())
                throw new ResetPasswordException(error);

			RemoveOtherRequests(rpr);
            
			using (UserHolder.UserSession(rpr.User))
            {
                rpr.Execute(ResetPasswordRequestOperation.Execute, password);
            }
            return rpr;
        }
    }

    public static void RequestNewLink(string code)
    {
        using (AuthLogic.Disable())
        {
            var rpr = Database.Query<ResetPasswordRequestEntity>()
                 .Where(r => r.Code == code)
                 .SingleOrDefaultEx();

            if (rpr == null)
                throw new ResetPasswordException(ResetPasswordMessage.TheCodeOfYourLinkIsIncorrect.NiceToString());

            SendResetPasswordRequestEmail(rpr.User.Email!);
        }
    }

    public static void SendResetPasswordRequestEmail(string email)
    {
        try
        {
            List<UserEntity> users;
            try
            {
                using (AuthLogic.Disable())
                {
                    users = Database
                        .Query<UserEntity>()
                        .Where(u => u.Email == email && u.State != UserState.Deactivated)
                        .ToList();

                    if (users.IsEmpty())
                        throw new ApplicationException(ResetPasswordMessage.EmailNotFound.NiceToString());
                }
            }
            catch (Exception ex)
            {
                ex.LogException();
                throw;
            }

            try
            {
                foreach (var user in users)
                {
                    var request = ResetPasswordRequest(user);

                    string url = EmailLogic.Configuration.UrlLeft + @"/auth/ResetPassword?code={0}".FormatWith(request.Code);

                    using (AuthLogic.Disable())
                        new ResetPasswordRequestEmail(request, url).SendMail();
                }
            }
            catch (Exception ex)
            {
                ex.LogException();
                throw new ApplicationException(LoginAuthMessage.AnErrorOccurredRequestNotProcessed.NiceToString());
            }
        }
        catch
        {
            if (!AuthServer.AvoidExplicitErrorMessages)
                throw;
        }
    }

    public static ResetPasswordRequestEntity ResetPasswordRequest(UserEntity user, int maxValidCodes = 5)
    {
        using (OperationLogic.AllowSave<UserEntity>())
        using (AuthLogic.Disable())
        {

            CancelExcess(user, maxValidCodes-1);

            var rpr = new ResetPasswordRequestEntity
            {
                Code = Random.Shared.NextString(32),
                User = user,
                RequestDate = Clock.Now,
            }.Save();


            //RemoveOtherRequests(rpr);
            return rpr; ;
        }
    }



    private static void RemoveOtherRequests(ResetPasswordRequestEntity rpr)
    {
        Database.Query<ResetPasswordRequestEntity>()
            .Where(r => r.User.Is(rpr.User) && r.IsValid && !r.Is(rpr))
            .UnsafeUpdate()
            .Set(e => e.Used, e => true)
            .Execute();
    }

    private static void CancelExcess(UserEntity user, int maxValidCodes)
    {
        var valid = Database.Query<ResetPasswordRequestEntity>()
             .Where(r => r.User.Is(user) && r.IsValid)
             .OrderByDescending(r => r.RequestDate)
             .Select(r => r.ToLite()).Take(maxValidCodes).ToList();



        Database.Query<ResetPasswordRequestEntity>()
      .Where(r => r.User.Is(user) && r.IsValid && !valid.Any(c => c.Is(r)))
      .UnsafeUpdate()
      .Set(e => e.Used, e => true)
      .Execute();
    }

    private static void CancelResetPasswordRequests(UserEntity user)
    {
        Database.Query<ResetPasswordRequestEntity>()
            .Where(r => r.User.Is(user) && r.IsValid)
            .UnsafeUpdate()
            .Set(e => e.Used, e => true)
            .Execute();
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

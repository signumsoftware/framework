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
using Signum.Utilities;
using Signum.Entities.Mailing;
using Signum.Engine.Basics;

namespace Signum.Engine.Authorization
{
    public class ResetPasswordRequestMail : EmailModel<UserDN>
    {
        public string Link;
    }

    public static class ResetPasswordRequestLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<ResetPasswordRequestDN>();

                dqm[typeof(ResetPasswordRequestDN)] = (from e in Database.Query<ResetPasswordRequestDN>()
                                                       select new
                                                       {
                                                           Entity = e,
                                                           e.Id,
                                                           e.RequestDate,
                                                           e.Code,
                                                           e.User,
                                                           e.User.Email
                                                       }).ToDynamic();

                EmailLogic.AssertStarted(sb);

                EmailLogic.RegisterSystemTemplate(AuthorizationMail.ResetPasswordRequest, tc => new EmailTemplateDN
                {
                    Active = true,
                    IsBodyHtml = true,
                    AssociatedType = typeof(ResetPasswordRequestDN).ToTypeDN(),
                    Recipient = new TemplateQueryTokenDN { TokenString = "User" },
                    Messages = tc.CreateMessages(() => new EmailTemplateMessageDN
                    { 
                        Text = Resources.ResetPasswordRequestMail, 
                        Subject = Resources.ResetPasswordRequestSubject
                    })
                });
            }
        }

        public enum AuthorizationMail
        {
            ResetPasswordRequest
        }

        public static ResetPasswordRequestDN ResetPasswordRequest(UserDN user)
        {
            //Remove old previous requests
            Database.Query<ResetPasswordRequestDN>()
                .Where(r => r.User.Is(user) && r.RequestDate < TimeZoneManager.Now.AddMonths(1))
                .UnsafeDelete();

            return new ResetPasswordRequestDN()
            {
                Code = MyRandom.Current.NextString(5),
                User = user,
                RequestDate = TimeZoneManager.Now,
            }.Save();
        }

        public static void ResetPasswordRequestAndSendEmail(UserDN user, Func<ResetPasswordRequestDN, string> urlGenerator)
        {
            var rpr = ResetPasswordRequest(user);

            rpr.SendMail(AuthorizationMail.ResetPasswordRequest);

            //new ResetPasswordRequestMail
            //{
            //    To = user,
            //    Link = urlGenerator(rpr),
            //}.Send();
        }

        public static Func<string, UserDN> GetUserByEmail = (email) =>
        {
            UserDN user = Database.Query<UserDN>().Where(u => u.Email == email).SingleOrDefaultEx();

            if (user == null)
                throw new ApplicationException(Resources.ThereSNotARegisteredUserWithThatEmailAddress);

            return user;
        };

    }
}

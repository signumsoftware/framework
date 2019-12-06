using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Signum.Engine.Authorization;
using Signum.Entities;
using Signum.React.Facades;
using Signum.Engine;
using System.Threading;
using Signum.Engine.Basics;
using Signum.Entities.Mailing;
using Signum.Engine.Mailing;
using Signum.React.Filters;
using System.Linq;
using Signum.Utilities;

namespace Signum.React.Mailing
{
    [ValidateModelFilter]
    public class MailingController : ControllerBase
    {
        [HttpGet("api/asyncEmailSender/view")]
        public AsyncEmailSenderState View()
        {
            AsyncEmailSenderPermission.ViewAsyncEmailSenderPanel.AssertAuthorized();

            AsyncEmailSenderState state = AsyncEmailSenderLogic.ExecutionState();

            return state;
        }

        [HttpPost("api/asyncEmailSender/start")]
        public void Start()
        {
            AsyncEmailSenderPermission.ViewAsyncEmailSenderPanel.AssertAuthorized();

            AsyncEmailSenderLogic.StartRunningEmailSenderAsync(0);

            Thread.Sleep(1000);
        }

        [HttpPost("api/asyncEmailSender/stop")]
        public void Stop()
        {
            AsyncEmailSenderPermission.ViewAsyncEmailSenderPanel.AssertAuthorized();

            AsyncEmailSenderLogic.Stop();

            Thread.Sleep(1000);
        }



#pragma warning disable IDE1006 // Naming Styles
        public class CreateEmailRequest
        {
            public Lite<EmailTemplateEntity> template { get; set; }
            public Lite<Entity> lite { get; set; }
            public ModifiableEntity entity { get; set; }
        }
#pragma warning restore IDE1006 // Naming Styles

        [HttpPost("api/email/constructorType")]
        public string GetConstructorType([Required, FromBody]EmailModelEntity model)
        {
            var type = EmailModelLogic.GetEntityType(model.ToType());

            return ReflectionServer.GetTypeName(type);
        }

        [HttpPost("api/email/emailTemplates")]
        public List<Lite<EmailTemplateEntity>> GetEmailTemplates(string queryKey, EmailTemplateVisibleOn visibleOn, [Required, FromBody]GetEmailTemplatesRequest request)
        {
            object queryName = QueryLogic.ToQueryName(queryKey);

            var entity = request.lite?.RetrieveAndForget();

            return EmailTemplateLogic.GetApplicableEmailTemplates(queryName, entity, visibleOn);
        }

        [HttpGet("api/email/getAllTypes")]
        public List<string> GetAllTypes()
        {
            return EmailLogic.GetAllTypes()
                      .Select(type => TypeLogic.TypeToEntity.GetOrThrow(type).CleanName)
                      .ToList();
        }

        public class GetEmailTemplatesRequest
        {
            public Lite<Entity> lite { get; set; }
        }
    }
}

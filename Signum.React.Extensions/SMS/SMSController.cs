using Microsoft.AspNetCore.Mvc;
using Signum.Engine.Authorization;
using System.Threading;
using Signum.Entities.SMS;

namespace Signum.React.SMS
{
    public class SMSController : ControllerBase
    {
        [HttpPost("api/sms/remainingCharacters")]
        public int RemainingCharacters([FromBody] RemainingCharactersRequest request)
        {
            var message = request.RemoveNoSMSCharacters ? SMSCharacters.RemoveNoSMSCharacters(request.Message) : request.Message;

            return SMSCharacters.RemainingLength(message);
        }

        public class RemainingCharactersRequest
        {
            public string Message;
            public bool RemoveNoSMSCharacters;
        }
    }
}

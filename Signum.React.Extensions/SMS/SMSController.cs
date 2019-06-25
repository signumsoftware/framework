using Microsoft.AspNetCore.Mvc;
using Signum.Engine.Authorization;
using System.Threading;
using Signum.Entities.SMS;
using System.Collections.Generic;
using System;
using Signum.Engine.SMS;
using System.Linq;
using Signum.Entities.Basics;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Utilities;

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

        [HttpGet("api/sms/getAllTypes")]
        public List<string> GetAllTypes()
        {
            return SMSLogic.GetAllTypes()
                      .Select(type => TypeLogic.TypeToEntity.GetOrThrow(type).CleanName)
                      .ToList();
        }

        public class RemainingCharactersRequest
        {
            public string Message;
            public bool RemoveNoSMSCharacters;
        }
    }
}

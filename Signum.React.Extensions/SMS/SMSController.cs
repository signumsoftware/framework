using Microsoft.AspNetCore.Mvc;
using Signum.Engine.Authorization;
using Signum.Entities.SMS;
using Signum.Engine.SMS;

namespace Signum.React.SMS;

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

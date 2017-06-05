using Signum.Entities;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Authorization
{
    [Serializable]
    public class AuthTokenConfigurationEmbedded : EmbeddedEntity
    {
        [Unit("mins")]
        public int RefreshTokenEvery { get; set; } = 30;

        [DateInPastValidator]
        public DateTime? RefreshAnyTokenPreviousTo { get; set; }
    }
}

using Signum.Engine;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Signum.Engine.Authorization
{
    public static class WebAuthnLogic
    {
        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodBase.GetCurrentMethod()))
            {
                sb.Include<WebAuthnCredentialEntity>()
                    .WithQuery(() => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.User,
                        e.CreationDate,
                        e.CredentialId,
                        e.CredType,
                        e.Counter,
                    });

                sb.Include<WebAuthnCredentialsCreateOptionsEntity>()
                    .WithQuery(() => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.User,
                        e.CreationDate,
                        e.Json,
                    });
            }
        }
    }
}

using Signum.Engine.Authorization;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Services;
using Signum.Utilities;
using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Signum.React.Filters;
using System.ComponentModel.DataAnnotations;
using Signum.Engine.Basics;
using Signum.Engine;
using Fido2NetLib;
using Fido2NetLib.Objects;
using System.Text;
using static Fido2NetLib.Fido2;
using Signum.Utilities.ExpressionTrees;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Signum.React.Authorization
{
    [ValidateModelFilter]
    public class WebAuthnController : ControllerBase
    {


        Fido2 fido2; 
        public WebAuthnController()
        {
            fido2 = new Fido2(new Fido2Configuration
            {
                ServerDomain = "localhost",
                ServerName = "FIDO2 Test",
                Origin = "http://localhost",
                TimestampDriftTolerance = 300000,
            });
        }

        public class MakeCredentialOptionsRequest
        {
            public Lite<UserEntity> User { get; set; }
        }

        public class MakeCredentialOptionsResponse
        {
            public Guid CreateOptionsId;
            public CredentialCreateOptions CredentialCreateOptions;
        }

        [HttpPost("api/webauthn/makeCredentialOptions")]
        public MakeCredentialOptionsResponse MakeCredentialOptions([Required, FromBody] MakeCredentialOptionsRequest request)
        {
            var existingKeys = Database.Query<WebAuthnCredentialEntity>()
                .Where(a => a.User.Is(request.User))
                .Select(a => new PublicKeyCredentialDescriptor(a.CredentialId))
                .ToList();

            var authenticatorSelection = new AuthenticatorSelection
            {
                RequireResidentKey = false, //For usernameless will be true
                UserVerification = UserVerificationRequirement.Preferred
            };

            var exts = new AuthenticationExtensionsClientInputs()
            {
                Extensions = true,
                UserVerificationIndex = true,
                Location = true,
                UserVerificationMethod = true,
                BiometricAuthenticatorPerformanceBounds = new AuthenticatorBiometricPerfBounds
                {
                    FAR = float.MaxValue,
                    FRR = float.MaxValue
                }
            };

            var user = request.User.RetrieveAndForget();

            var fido2User = new Fido2User
            {
                DisplayName = user.UserName,
                Name = user.UserName,
                Id = Encoding.UTF8.GetBytes(user.Id.ToString())
            };

            var options = fido2.RequestNewCredential(fido2User, existingKeys, authenticatorSelection, AttestationConveyancePreference.None, exts);

            if (options.Status != "ok")
                throw new InvalidOperationException(options.ErrorMessage);

            Database.Query<WebAuthnCredentialsCreateOptionsEntity>().Where(a => a.CreationDate < DateTime.Now.AddMonths(-1)).UnsafeDelete();

            var optionsEntity = new WebAuthnCredentialsCreateOptionsEntity
            {
                User = user.ToLite(),
                Json = options.ToJson()
            }.Save();

            return new MakeCredentialOptionsResponse 
            {
                CredentialCreateOptions = options,
                CreateOptionsId = (Guid)optionsEntity.Id,
            };
        }


        public class  MakeCredentialsRequest
        {
            public Guid CreateOptionsId;
            public AuthenticatorAttestationRawResponse AttestationRawResponse;
        }

        [HttpPost("api/webauthn/makeCredential")]
        public async Task<CredentialMakeResult> MakeCredential([Required, FromBody] MakeCredentialsRequest request)
        {
            var optionsEntity = Database.Retrieve<WebAuthnCredentialsCreateOptionsEntity>(request.CreateOptionsId);

            var options = CredentialCreateOptions.FromJson(optionsEntity.Json);

            var result = await fido2.MakeNewCredentialAsync(request.AttestationRawResponse, options, async (args) =>
            {
                return !(await Database.Query<WebAuthnCredentialEntity>().AnyAsync(c => c.CredentialId == args.CredentialId));
            });

            if (result.Status != "ok")
                throw new InvalidOperationException(options.ErrorMessage);

            new WebAuthnCredentialEntity
            {
                CredentialId = result.Result.CredentialId,
                PublicKey = result.Result.PublicKey,
                User = Lite.ParsePrimaryKey<UserEntity>(Encoding.UTF8.GetString(result.Result.User.Id)),
                Counter = (int)result.Result.Counter,
                CredType = result.Result.CredType,
                Aaguid = result.Result.Aaguid,
            }.Save();

            return result;
        }


        public class AssertionOptionsRequest
        {
            public string UserName; 
        }

        [HttpPost]
        [Route("api/webauthn/assertionOptions")]
        public AssertionOptions AssertionOptionsPost([FromBody][Required] AssertionOptionsRequest request)
        {


            var existingCredentials = new List<PublicKeyCredentialDescriptor>();

            if (!string.IsNullOrEmpty(request.UserName))
            {
                existingCredentials = Database.Query<WebAuthnCredentialEntity>()
                    .Where(a => a.User.Entity.UserName == request.UserName)
                    .Select(a => new PublicKeyCredentialDescriptor(a.CredentialId))
                    .ToList();
            }

            var exts = new AuthenticationExtensionsClientInputs()
            {
                SimpleTransactionAuthorization = "FIDO",
                GenericTransactionAuthorization = new TxAuthGenericArg
                {
                    ContentType = "text/plain",
                    Content = new byte[] { 0x46, 0x49, 0x44, 0x4F }
                },
                UserVerificationIndex = true,
                Location = true,
                UserVerificationMethod = true
            };

            // 3. Create options
            var uv = string.IsNullOrEmpty(userVerification) ? UserVerificationRequirement.Discouraged : userVerification.ToEnum<UserVerificationRequirement>();
            var options = fido2.GetAssertionOptions(
                existingCredentials,
                uv,
                exts
            );

            // 4. Temporarily store options, session/in-memory cache/redis/db
            HttpContext.Session.SetString("fido2.assertionOptions", options.ToJson());

            // 5. Return options to client
            return options;

        }

    }
}

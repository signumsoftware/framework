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
using static Signum.React.Authorization.AuthController;

namespace Signum.React.Authorization
{
    [ValidateModelFilter]
    public class WebAuthnController : ControllerBase
    {


        Fido2 fido2;
        public WebAuthnController()
        {
            var config = WebAuthnLogic.GetConfig();

            fido2 = new Fido2(new Fido2Configuration
            {
                ServerDomain = config.ServerDomain,
                ServerName = config.ServerName,
                Origin = config.Origin,
                TimestampDriftTolerance = 300000,
            });
        }

        public class MakeCredentialOptionsResponse
        {
            public Guid CreateOptionsId;
            public CredentialCreateOptions CredentialCreateOptions;
        }

        [HttpPost("api/webauthn/makeCredentialOptions")]
        public MakeCredentialOptionsResponse MakeCredentialOptions()
        {
            using (AuthLogic.Disable())
            {
                var existingKeys = Database.Query<WebAuthnCredentialEntity>()
                    .Where(a => a.User.Is(UserEntity.Current))
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

                var user = UserEntity.Current;

                var fido2User = new Fido2User
                {
                    DisplayName = user.UserName,
                    Name = user.UserName,
                    Id = Encoding.UTF8.GetBytes(user.Id.ToString())
                };

                var options = fido2.RequestNewCredential(fido2User, existingKeys, authenticatorSelection, AttestationConveyancePreference.None, exts);

                if (options.Status != "ok")
                    throw new InvalidOperationException(options.ErrorMessage);

                Database.Query<WebAuthnMakeCredentialsOptionsEntity>().Where(a => a.CreationDate < DateTime.Now.AddMonths(-1)).UnsafeDelete();

                var optionsEntity = new WebAuthnMakeCredentialsOptionsEntity
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
        }


        public class MakeCredentialsRequest
        {
            public Guid CreateOptionsId;
            public AuthenticatorAttestationRawResponse AttestationRawResponse;
        }

        [HttpPost("api/webauthn/makeCredential")]
        public async Task<CredentialMakeResult> MakeCredential([Required, FromBody] MakeCredentialsRequest request)
        {
            using (AuthLogic.Disable())
            {
                var optionsEntity = Database.Retrieve<WebAuthnMakeCredentialsOptionsEntity>(request.CreateOptionsId);

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
        }


        public class AssertionOptionsRequest
        {
            public string UserName;
        }

        public class AssertionOptionsResponse
        {
            public AssertionOptions AssertionOptions;
            public Guid AssertionOptionsId;
        }

        [HttpPost("api/webauthn/assertionOptions"), SignumAllowAnonymous]
        public AssertionOptionsResponse GetAssertionOptions([FromBody][Required] AssertionOptionsRequest request)
        {
            using (AuthLogic.Disable())
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
                var uv = UserVerificationRequirement.Discouraged;
                var options = fido2.GetAssertionOptions(
                    existingCredentials,
                    uv,
                    exts
                );

                if (options.Status != "ok")
                    throw new InvalidOperationException(options.ErrorMessage);

                Database.Query<WebAuthnAssertionOptionsEntity>().Where(a => a.CreationDate < DateTime.Now.AddMonths(-1)).UnsafeDelete();

                var optionsEntity = new WebAuthnAssertionOptionsEntity
                {
                    Json = options.ToJson()
                }.Save();

                return new AssertionOptionsResponse
                {
                    AssertionOptions = options,
                    AssertionOptionsId = (Guid)optionsEntity.Id
                };
            }
        }

        public class MakeAssertionRequest
        {
            public Guid AssertionOptionsId;
            public AuthenticatorAssertionRawResponse AssertionRawResponse;
        }

        [HttpPost("api/webauthn/makeAssertion"), SignumAllowAnonymous]
        public async Task<LoginResponse> MakeAssertion([FromBody][Required] MakeAssertionRequest request)
        {
            using (AuthLogic.Disable())
            using (Transaction tr = new Transaction())
            {
                var assertionOptions = Database.Retrieve<WebAuthnAssertionOptionsEntity>(request.AssertionOptionsId);
                var options = AssertionOptions.FromJson(assertionOptions.Json);

                var cred = Database.Query<WebAuthnCredentialEntity>().SingleEx(cred => cred.CredentialId == request.AssertionRawResponse.Id);

                var res = await fido2.MakeAssertionAsync(request.AssertionRawResponse, options, cred.PublicKey, (uint)cred.Counter, (args) =>
                {
                    if (!MemoryExtensions.SequenceEqual<byte>(cred.CredentialId, args.CredentialId))
                        return Task.FromResult(false);

                    var userId = Encoding.UTF8.GetBytes(cred.User.Id.ToString());
                    if (!MemoryExtensions.SequenceEqual<byte>(userId, args.UserHandle))
                        return Task.FromResult(false);

                    return Task.FromResult(true);
                });

                cred.Counter++;
                cred.Save();

                var user = cred.User.RetrieveAndForget();

                AuthServer.OnUserPreLogin(ControllerContext, user);

                AuthServer.AddUserSession(ControllerContext, user);

                var token = AuthTokenServer.CreateToken(user);

                return tr.Commit(new LoginResponse { userEntity = user, token = token, authenticationType = "webauthn" });
            }
        }
    }
}

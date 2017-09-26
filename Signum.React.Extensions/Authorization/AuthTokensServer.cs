using Signum.Engine;
using Signum.Engine.Authorization;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.React.Filters;
using Signum.Utilities;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http.Formatting;

namespace Signum.React.Authorization
{
    public static class AuthTokenServer
    {
        static Func<AuthTokenConfigurationEmbedded> Configuration;

        public static void Start(Func<AuthTokenConfigurationEmbedded> tokenConfig, string hashableEncryptionKey)
        {
            Configuration = tokenConfig;
            CryptoKey = new MD5CryptoServiceProvider().Using(p => p.ComputeHash(UTF8Encoding.UTF8.GetBytes(hashableEncryptionKey)));

            SignumAuthenticationFilterAttribute.Authenticators.Add(TokenAuthenticator);
            SignumAuthenticationFilterAttribute.Authenticators.Add(AnonymousAuthenticator);
            SignumAuthenticationFilterAttribute.Authenticators.Add(InvalidAuthenticator);
        }

        public static SignumAuthenticationResult InvalidAuthenticator(HttpActionContext actionContext)
        {
            throw new AuthenticationException("No authentication information found!");
        }

        public static SignumAuthenticationResult AnonymousAuthenticator(HttpActionContext actionContext)
        {
            var r = actionContext.ActionDescriptor as ReflectedHttpActionDescriptor;
            if (r.GetCustomAttributes<AllowAnonymousAttribute>().Any() || r.ControllerDescriptor.ControllerType.HasAttribute<AllowAnonymousAttribute>())
                return new SignumAuthenticationResult();
            
            return null;
        }

        static SignumAuthenticationResult TokenAuthenticator(HttpActionContext ctx)
        {
            var tokenString = ctx.Request.Headers.Authorization?.Parameter;
            if (tokenString == null)
            {
                return null;
            }

            var token = DeserializeToken(tokenString);

            var c = Configuration();

            bool requiresRefresh = token.CreationDate.AddMinutes(c.RefreshTokenEvery) < TimeZoneManager.Now ||
                c.RefreshAnyTokenPreviousTo.HasValue && token.CreationDate < c.RefreshAnyTokenPreviousTo;

            if (requiresRefresh)
            {
                return new SignumAuthenticationResult { ErrorResult = new UpgradeTokenResult(ctx.RequestContext.Configuration.Formatters.JsonFormatter) };
            }

            return new SignumAuthenticationResult { User = token.User };
        }

        internal class UpgradeTokenResult : IHttpActionResult
        {
            private JsonMediaTypeFormatter jsonFormatter;

            public UpgradeTokenResult(JsonMediaTypeFormatter jsonFormatter)
            {
                this.jsonFormatter = jsonFormatter;
            }

            public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
            {
                var error = new HttpError(new NewTokenRequiredException("Please upgrade the token to continue using the service"), includeErrorDetail: true); //Avoid annoying exception
                
                var message = new HttpResponseMessage(HttpStatusCode.UpgradeRequired)
                {
                    Content = new ObjectContent<HttpError>(error, jsonFormatter),
                };

                return Task.FromResult(message); 
            }
        }

        public static string RefreshToken(string oldToken, out UserEntity newUser)
        {
            AuthToken token = DeserializeToken(oldToken);

            newUser = AuthLogic.Disable().Using(_ => Database.Query<UserEntity>().SingleOrDefaultEx(u => u.Id == token.User.Id));

            if (newUser == null)
                throw new AuthenticationException(AuthMessage.TheUserIsNotLongerInTheDatabase.NiceToString());

            if (newUser.State == UserState.Disabled)
                throw new AuthenticationException(AuthMessage.User0IsDisabled.NiceToString(newUser));

            if (newUser.UserName != token.User.UserName)
                throw new AuthenticationException(AuthMessage.InvalidUsername.NiceToString());

            if (!newUser.PasswordHash.SequenceEqual(token.User.PasswordHash))
                throw new AuthenticationException(AuthMessage.InvalidPassword.NiceToString());

            AuthToken newToken = new AuthToken
            {
                User = newUser,
                CreationDate = TimeZoneManager.Now,
            };

            var result = SerializeToken(newToken);

            return result;
        }

        static NetDataContractSerializer formatter = new NetDataContractSerializer();

        static AuthToken DeserializeToken(string authHeader)
        {
            try
            {

                //using (HeavyProfiler.LogNoStackTrace("DeserializeToken"))
                {
                    var array = Convert.FromBase64String(authHeader);

                    array = Decrypt(array);

                    using (var ms = new MemoryStream(array))
                    using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
                        return (AuthToken)formatter.Deserialize(ds);
                }
            }
            catch (Exception)
            {
                throw new AuthenticationException("Invalid token");
            }
        }

        public static string CreateToken(UserEntity user)
        {
            AuthToken newToken = new AuthToken
            {
                User = user,
                CreationDate = TimeZoneManager.Now,
            };

            return SerializeToken(newToken);
        }

        static string SerializeToken(AuthToken entity)
        {
            using (HeavyProfiler.LogNoStackTrace("SerializeToken"))
            {
                var array = new MemoryStream().Using(ms =>
                {
                    using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Compress))
                        formatter.Serialize(ds, entity);

                    return ms.ToArray();
                });

                array = Encrypt(array);

                return Convert.ToBase64String(array);
            }
        }

        static byte[] CryptoKey;

        //http://stackoverflow.com/questions/8041451/good-aes-initialization-vector-practice
        static byte[] Encrypt(byte[] toEncryptBytes)
        {
            using (var provider = new AesCryptoServiceProvider())
            {
                provider.Key = CryptoKey;
                provider.Mode = CipherMode.CBC;
                provider.Padding = PaddingMode.PKCS7;
                using (var encryptor = provider.CreateEncryptor(provider.Key, provider.IV))
                {
                    using (var ms = new MemoryStream())
                    {
                        ms.Write(provider.IV, 0, provider.IV.Length);
                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            cs.Write(toEncryptBytes, 0, toEncryptBytes.Length);
                            cs.FlushFinalBlock();
                        }
                        return ms.ToArray();
                    }
                }
            }
        }

        static byte[] Decrypt(byte[] encryptedString)
        {
            using (var provider = new AesCryptoServiceProvider())
            {
                provider.Key = CryptoKey;
                using (var ms = new MemoryStream(encryptedString))
                {
                    // Read the first 16 bytes which is the IV.
                    byte[] iv = new byte[16];
                    ms.Read(iv, 0, 16);
                    provider.IV = iv;

                    using (var decryptor = provider.CreateDecryptor())
                    {
                        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            return cs.ReadAllBytes();
                        }
                    }
                }
            }
        }

    }

    public class AuthToken
    {
        public UserEntity User { get; set; }

        public DateTime CreationDate { get; set; }
    }


    [Serializable]
    public class NewTokenRequiredException : Exception
    {
        public NewTokenRequiredException(string message) : base(message) { }
    }
}
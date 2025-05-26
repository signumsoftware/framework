using System.IO;
using System.IO.Compression;
using System.Security.Authentication;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Text.Json;
using Signum.API.Filters;
using Signum.API.Json;
using Signum.API.Controllers;
using Signum.API;

namespace Signum.Authorization.AuthToken;

public static class AuthTokenServer
{
    public static Func<AuthTokenConfigurationEmbedded> Configuration;
    public static Action<UserWithClaims, AuthToken?, AuthToken?> OnAuthToken;
    public static Func<string, bool> AuthenticateHeader = (authHeader) => true;

    public static void Start(Func<AuthTokenConfigurationEmbedded> tokenConfig, string hashableEncryptionKey)
    {
        Configuration = tokenConfig;
        using var md5 = MD5.Create();
        CryptoKey = md5.ComputeHash(Encoding.UTF8.GetBytes(hashableEncryptionKey));
        ReflectionServer.RegisterLike(typeof(AuthTokenConfigurationEmbedded), () => false);
        SignumAuthenticationFilter.Authenticators.Add(TokenAuthenticator);
        SignumAuthenticationFilter.Authenticators.Add(AnonymousUserAuthenticator);
        SignumAuthenticationFilter.Authenticators.Add(AllowAnonymousAuthenticator);
        SignumAuthenticationFilter.Authenticators.Add(InvalidAuthenticator);
    }

    public static SignumAuthenticationResult? InvalidAuthenticator(FilterContext actionContext)
    {
        throw new AuthenticationException("No authentication information found!");
    }

    public static SignumAuthenticationResult? AnonymousUserAuthenticator(FilterContext actionContext)
    {
        if (AuthLogic.AnonymousUser != null)
            return new SignumAuthenticationResult { UserWithClaims = new UserWithClaims(AuthLogic.AnonymousUser) };

        return null;
    }


    public static SignumAuthenticationResult? AllowAnonymousAuthenticator(FilterContext actionContext)
    {
        if (actionContext.ActionDescriptor is ControllerActionDescriptor cad &&
            (cad.MethodInfo.HasAttribute<SignumAllowAnonymousAttribute>() || cad.ControllerTypeInfo.HasAttribute<SignumAllowAnonymousAttribute>()))
            return new SignumAuthenticationResult();

        return null;
    }

    public static string AuthHeader = "Authorization";

    public static void PrepareForWindowsAuthentication()
    {
        AuthHeader = "Signum_Authorization";
    }

    public static SignumAuthenticationResult? TokenAuthenticator(FilterContext ctx)
    {
        var authHeader = ctx.HttpContext.Request.Headers[AuthHeader].FirstOrDefault();
        if (authHeader == null || !AuthenticateHeader(authHeader))
            return null;


        var token = DeserializeAuthHeaderToken(authHeader);
        if (token?.User == null)
            return null;

        var c = Configuration();

        bool requiresRefresh = token.CreationDate.AddMinutes(c.RefreshTokenEvery) < Clock.Now ||
            c.RefreshAnyTokenPreviousTo.HasValue && token.CreationDate < c.RefreshAnyTokenPreviousTo ||
            ctx.HttpContext.Request.Query.ContainsKey("refreshToken");

        if (requiresRefresh)
        {
            ctx.HttpContext.Response.Headers["New_Token"] = RefreshToken(token, out var newUserWithClaims);
            return new SignumAuthenticationResult { UserWithClaims = newUserWithClaims };
        }
        else
        {
            var userWithClaims = token.ToUserWithClaims();
            OnAuthToken?.Invoke(userWithClaims, token, null);
            return new SignumAuthenticationResult { UserWithClaims = userWithClaims };
        }

    }

    public static string RefreshToken(AuthToken oldToken, out UserWithClaims newUser)
    {
        var user = AuthLogic.Disable().Using(_ => Database.Query<UserEntity>().SingleOrDefaultEx(u => u.Id == oldToken.User.Id));

        if (user == null)
            throw new AuthenticationException(LoginAuthMessage.TheUserIsNotLongerInTheDatabase.NiceToString());

        if (user.State == UserState.Deactivated)
            throw new AuthenticationException(LoginAuthMessage.User0IsDeactivated.NiceToString(user));

        if (user.ToString() != oldToken.User.ToString())
            throw new AuthenticationException(LoginAuthMessage.InvalidUsername.NiceToString());

        if (!(user.PasswordHash.EmptyIfNull()).SequenceEqual((oldToken.PasswordHash.EmptyIfNull()).EmptyIfNull()))
            throw new AuthenticationException(LoginAuthMessage.InvalidPassword.NiceToString());

        newUser = new UserWithClaims(user);

        AuthToken newToken = new AuthToken
        {
            User = newUser.User,
            Claims = newUser.Claims,
            PasswordHash = user.PasswordHash,
            CreationDate = Clock.Now,
        };

        OnAuthToken?.Invoke(newUser, oldToken, newToken);

        var result = SerializeToken(newToken);
        return result;
    }


    public static Func<string, AuthToken?> DeserializeAuthHeaderToken = (string authHeader) => 
    {
        try
        {
            return DeserializeToken(authHeader.After("Bearer "));

        }
        catch (AuthenticationException)
        {
            return null;
        }
    };

    public static AuthToken DeserializeToken(string token)
    {
        try
        {
            var array = Convert.FromBase64String(token);

            array = Decrypt(array);

            using (var ms = new MemoryStream(array))
            using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
            {
                var bytes = ds.ReadAllBytes();
                var authToken = JsonExtensions.FromJsonBytes<AuthToken>(bytes, EntityJsonContext.FullJsonSerializerOptions);

                authToken.Claims = authToken.Claims.SelectDictionary(key => key, obj => obj is JsonElement elem ? OperationController.BaseOperationRequest.ConvertObject(elem, EntityJsonContext.FullJsonSerializerOptions, null) : obj);

                return authToken;
            }
        }
        catch (Exception)
        {
            throw new AuthenticationException("Invalid token");
        }
    }

    public static string CreateToken(UserEntity user)
    {
        var userWithClaims = new UserWithClaims(user);

        AuthToken newToken = new AuthToken
        {
            User = userWithClaims.User,
            Claims = userWithClaims.Claims,
            CreationDate = Clock.Now,
            PasswordHash = user.PasswordHash,
        };

        OnAuthToken?.Invoke(userWithClaims, null, newToken);

        return SerializeToken(newToken);
    }

    static string SerializeToken(AuthToken token)
    {
        using (HeavyProfiler.LogNoStackTrace("SerializeToken"))
        {
            var array = new MemoryStream().Using(ms =>
            {
                using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Compress))
                {
                    using (Utf8JsonWriter writer = new Utf8JsonWriter(ds))
                    {
                        JsonSerializer.Serialize(writer, token, EntityJsonContext.FullJsonSerializerOptions);
                    }
                }

                return ms.ToArray();
            });

            //var str = Encoding.UTF8.GetString(array);

            array = Encrypt(array);

            return Convert.ToBase64String(array);
        }
    }

    static byte[] CryptoKey;

    //http://stackoverflow.com/questions/8041451/good-aes-initialization-vector-practice
    static byte[] Encrypt(byte[] toEncryptBytes)
    {
        using var provider = Aes.Create();
        provider.Key = CryptoKey;
        provider.Mode = CipherMode.CBC;
        provider.Padding = PaddingMode.PKCS7;
        using var encryptor = provider.CreateEncryptor(provider.Key, provider.IV);
        using var ms = new MemoryStream();
        ms.Write(provider.IV, 0, provider.IV.Length);
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        cs.Write(toEncryptBytes, 0, toEncryptBytes.Length);
        cs.FlushFinalBlock();
        return ms.ToArray();
    }

    static byte[] Decrypt(byte[] encryptedString)
    {
        using var provider = Aes.Create();
        provider.Key = CryptoKey;
        using var ms = new MemoryStream(encryptedString);
        // Read the first 16 bytes which is the IV.
        byte[] iv = new byte[16];
        ms.Read(iv, 0, 16);
        provider.IV = iv;

        using var decryptor = provider.CreateDecryptor();
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        return cs.ReadAllBytes();
    }
}

public class AuthToken
{
    public Lite<IUserEntity> User { get; set; }
    public Dictionary<string, object?> Claims { get; set; }
    public byte[]? PasswordHash { get; set; } //To check if the password has changed
    public DateTime CreationDate { get; set; }

    public UserWithClaims ToUserWithClaims() => new UserWithClaims(this.User, this.Claims);
}

public class NewTokenRequiredException : Exception
{
    public NewTokenRequiredException(string message) : base(message) { }
}

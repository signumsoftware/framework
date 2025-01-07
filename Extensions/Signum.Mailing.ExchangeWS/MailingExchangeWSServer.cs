using Microsoft.AspNetCore.Builder;
using Signum.API.Json;
using Signum.API;
using Signum.Utilities.Reflection;
using System.Text.Json;
using Microsoft.Exchange.WebServices.Data;
using Signum.Authorization;
using Signum.Mailing;
using Signum.Authorization.Rules;

namespace Signum.Mailing.ExchangeWS;

internal class MailingExchangeWSServer
{
    public static void Start(IApplicationBuilder app)
    {
        ReflectionServer.OverrideIsNamespaceAllowed.Add(typeof(ExchangeVersion).Namespace!, () => TypeAuthLogic.GetAllowed(typeof(EmailSenderConfigurationEntity)).MaxUI() > TypeAllowedBasic.None);

        if (Schema.Current.Tables.ContainsKey(typeof(ExchangeWebServiceEmailServiceEntity)))
        {
            var piPassword = ReflectionTools.GetPropertyInfo((ExchangeWebServiceEmailServiceEntity e) => e.Password);
            var pcs = SignumServer.WebEntityJsonConverterFactory.GetPropertyConverters(typeof(ExchangeWebServiceEmailServiceEntity));
            pcs.GetOrThrow("password").CustomWriteJsonProperty = (Utf8JsonWriter writer, WriteJsonPropertyContext ctx) => { };
            pcs.Add("newPassword", new PropertyConverter
            {
                AvoidValidate = true,
                CustomWriteJsonProperty = (Utf8JsonWriter writer, WriteJsonPropertyContext ctx) => { },
                CustomReadJsonProperty = (ref Utf8JsonReader reader, ReadJsonPropertyContext ctx) =>
                {
                    ctx.Factory.AssertCanWrite(ctx.ParentPropertyRoute.Add(piPassword), ctx.Entity, null);

                    var password = reader.GetString()!;

                    ((ExchangeWebServiceEmailServiceEntity)ctx.Entity).Password = EmailSenderConfigurationLogic.EncryptPassword(password);
                }
            });
        }
    }
}

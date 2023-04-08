using Microsoft.AspNetCore.Builder;
using Signum.API;
using Signum.API.Json;
using Signum.MailingPop3;
using Signum.Utilities.Reflection;
using System.Text.Json;

namespace Signum.MailingReception;

internal class Pop3Server
{
    public static void Start(IApplicationBuilder app)
    {
        if (Schema.Current.Tables.ContainsKey(typeof(Pop3ConfigurationEntity)))
        {
            var piPassword = ReflectionTools.GetPropertyInfo((Pop3ConfigurationEntity e) => e.Password);
            var pcs = SignumServer.WebEntityJsonConverterFactory.GetPropertyConverters(typeof(Pop3ConfigurationEntity));
            pcs.GetOrThrow("password").CustomWriteJsonProperty = (Utf8JsonWriter writer, WriteJsonPropertyContext ctx) => { };
            pcs.Add("newPassword", new PropertyConverter
            {
                AvoidValidate = true,
                CustomWriteJsonProperty = (Utf8JsonWriter writer, WriteJsonPropertyContext ctx) => { },
                CustomReadJsonProperty = (ref Utf8JsonReader reader, ReadJsonPropertyContext ctx) =>
                {
                    ctx.Factory.AssertCanWrite(ctx.ParentPropertyRoute.Add(piPassword), ctx.Entity);

                    var password = reader.GetString()!;

                    ((Pop3ConfigurationEntity)ctx.Entity).Password = Pop3ConfigurationLogic.EncryptPassword(password);
                }
            });
        }
    }
}

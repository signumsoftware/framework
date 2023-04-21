using Signum.API;

namespace Signum.Files;

public static class FileLogic
{
    public static void Start(SchemaBuilder sb, WebServerBuilder? wsb)
    {
        if (wsb != null)
            FilesServer.Start(wsb.ApplicationBuilder);

        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            sb.Include<FileEntity>()
                .WithQuery(() => a => new
                {
                    Entity = a,
                    a.Id,
                    a.FileName,
                });
        }
    }
}

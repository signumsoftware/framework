using Signum.API;

namespace Signum.Files;

public static class FileLogic
{
    public static void Start(SchemaBuilder sb, WebServerBuilder? wsb)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            sb.Include<FileEntity>()
                .WithQuery(() => a => new
                {
                    Entity = a,
                    a.Id,
                    a.FileName,
                });

            if (wsb != null)
                FilesServer.Start(wsb.WebApplication);
        }
    }
}

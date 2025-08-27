using Signum.API;

namespace Signum.Files;

public static class FileLogic
{
    public static void Start(SchemaBuilder sb)
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

            if (sb.WebServerBuilder != null)
                FilesServer.Start(sb.WebServerBuilder);
        }
    }
}

using Signum.Entities.Files;

namespace Signum.Engine.Files;

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
        }
    }
}

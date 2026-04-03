
namespace Signum.Dynamic.Client;

public static class DynamicClientLogic
{
    public static ResetLazy<List<DynamicClientEntity>> Clients = null!;

    public static bool IsStarted = false;
    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.Include<DynamicClientEntity>()
            .WithSave(DynamicClientOperation.Save)
            .WithDelete(DynamicClientOperation.Delete)
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.Name,
                Code = e.Code.Etc(50),
            });

        new Graph<DynamicClientEntity>.ConstructFrom<DynamicClientEntity>(DynamicClientOperation.Clone)
        {
            Construct = (e, _) =>
            {
                return new DynamicClientEntity
                {
                    Name = e.Name + "_2",
                    Code = e.Code,
                };
            }
        }.Register();

        Clients = sb.GlobalLazy(() => Database.Query<DynamicClientEntity>().Where(a => !a.Mixin<DisabledMixin>().IsDisabled).ToList(),
            new InvalidateWith(typeof(DynamicClientEntity)));

        IsStarted = true;
    }
}

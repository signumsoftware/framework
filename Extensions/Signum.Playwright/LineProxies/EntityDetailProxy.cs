using Signum.Playwright.Frames;

namespace Signum.Playwright.LineProxies;

/// <summary>
/// Proxy for EntityDetail.tsx
/// </summary>
public class EntityDetailProxy : EntityBaseProxy
{
    public EntityDetailProxy(ILocator element, PropertyRoute route)
        : base(element, route)
    {
    }

    public override async Task<object?> GetValueUntypedAsync()
        => (await EntityInfoInternalAsync(null))?.ToLite();

    public override async Task SetValueUntypedAsync(object? value)
        => await SetLiteAsync(value is Entity e ? e.ToLite() : (Lite<Entity>?)value);

    public override Task<bool> IsReadonlyAsync()
        => throw new NotImplementedException();

    public async Task<Lite<IEntity>?> GetLiteAsync()
        => (await EntityInfoInternalAsync(null))?.ToLite();

    public async Task<EntityInfoProxy?> EntityInfoAsync()
    {
        return await EntityInfoInternalAsync(null);
    }

    public async Task SetLiteAsync(Lite<Entity>? value)
    {
        if (value == null)
        {
            if (await EntityInfoInternalAsync(null) != null)
                await RemoveAsync();
        }
        else
        {
            var modal = await FindAsync();
            await modal.SelectLiteAsync(value);
        }
    }

    public LineContainer<T> Details<T>() where T : ModifiableEntity
    {
        var subRoute = Route.Type == typeof(T) ? Route : PropertyRoute.Root(typeof(T));
        return new LineContainer<T>(this.Element.Locator("div[data-property-path]"), subRoute);
    }
}

using Microsoft.Playwright;
using Signum.Basics;
using Signum.Playwright.ModalProxies;

namespace Signum.Playwright.LineProxies;

/// <summary>
/// Proxy for EntityLine.tsx
/// </summary>
public class EntityLineProxy : EntityBaseProxy
{
    public EntityLineProxy(ILocator element, PropertyRoute route)
        : base(element, route)
    {
    }

    public override async Task<object?> GetValueUntypedAsync()
        => (await EntityInfoInternalAsync(null))?.ToLite();

    public override async Task SetValueUntypedAsync(object? value)
        => await SetLiteAsync(value is Entity e ? e.ToLite() : (Lite<Entity>?)value);

    public async Task<Lite<Entity>?> GetLiteAsync()
    {
        return (await EntityInfoInternalAsync(null))?.ToLite();
    }

    public ILocator AutoCompleteElement => this.Element.Locator(".sf-entity-autocomplete");

    public async Task SetLiteAsync(Lite<IEntity>? value)
    {
        if (await EntityInfoInternalAsync(null) != null)
            await RemoveAsync();

        if (value != null)
        {
            if (await AutoCompleteElement.IsVisibleAsync())
                await AutoCompleteAsync(value);
            else
            {
                var modal = await FindAsync();
                await modal.SelectLiteAsync(value);
            }
        }
    }

    public async Task AutoCompleteAsync(Lite<IEntity> lite)
        => await AutoCompleteWaitChangesAsync(AutoCompleteElement, Element, lite);

    public override async Task<bool> IsReadonlyAsync()
        => await Element.Locator(".form-control[readonly]").CountAsync() > 0
           || await Element.IsDisabledAsync();
}

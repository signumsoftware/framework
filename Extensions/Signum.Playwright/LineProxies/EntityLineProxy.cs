using Microsoft.Playwright;
using Signum.Basics;
using Signum.Playwright.ModalProxies;

namespace Signum.Playwright.LineProxies;

/// <summary>
/// Proxy for EntityLine control (autocomplete + find)
/// Equivalent to Selenium's EntityLineProxy
/// </summary>
public class EntityLineProxy : EntityBaseProxy
{
    public EntityLineProxy(ILocator element, PropertyRoute route, IPage page)
        : base(element, route, page)
    {
    }

    public override async Task SetValueUntypedAsync(object? value)
    {
        await SetLiteAsync(value is Entity e ? e.ToLite() : (Lite<Entity>?)value);
    }

    public override async Task<object?> GetValueUntypedAsync()
    {
        return await GetLiteAsync();
    }

    public async Task SetLiteAsync(Lite<IEntity>? value)
    {
        // Remove current value if any
        var currentInfo = await GetEntityInfoAsync();
        if (currentInfo != null)
        {
            await RemoveAsync();
        }

        if (value != null)
        {
            // Check if autocomplete is visible
            if (await AutoCompleteElement.IsVisibleAsync())
            {
                await AutoCompleteAsync(value);
            }
            else if (await FindButton.IsPresentAsync())
            {
                var findModal = await FindModalAsync();
                // Select the entity in the search modal
                await findModal.Modal.Locator($"tr[data-entity-key='{value.Key()}']").ClickAsync();
                await findModal.OkAsync();
            }
            else
            {
                throw new NotImplementedException("Neither autocomplete nor find button available");
            }
        }
    }

    public async Task<Lite<Entity>?> GetLiteAsync()
    {
        var info = await GetEntityInfoAsync();
        return info?.ToLite();
    }

    public async Task<ModalProxy> ViewAsync<T>() where T : ModifiableEntity
    {
        return await ViewModalAsync<T>();
    }

    public override async Task<bool> IsReadonlyAsync()
    {
        var readonlyInput = await Element.Locator(".form-control[readonly]").CountAsync();
        if (readonlyInput > 0) return true;

        return await Element.IsDomDisabledAsync() || await Element.IsDomReadonlyAsync();
    }
}

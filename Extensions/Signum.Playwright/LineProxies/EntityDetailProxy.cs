namespace Signum.Playwright.LineProxies;

/// <summary>
/// Proxy for EntityDetail control (embedded entity form)
/// Equivalent to Selenium's EntityDetailProxy
/// </summary>
public class EntityDetailProxy : EntityBaseProxy
{
    public EntityDetailProxy(ILocator element, PropertyRoute route, IPage page)
        : base(element, route, page)
    {
    }

    public override async Task SetValueUntypedAsync(object? value)
    {
        if (value == null)
        {
            if (await CreateButton.IsPresentAsync())
            {
                // Already has a value, remove it
                var removeBtn = Element.Locator(".sf-remove");
                if (await removeBtn.IsPresentAsync())
                {
                    await RemoveAsync();
                }
            }
        }
        else
        {
            // Create if doesn't exist
            if (await CreateButton.IsPresentAsync())
            {
                await CreateButton.ClickAsync();
            }

            // Now set the embedded entity fields
            // This would require field-by-field setting based on the entity type
            // Implementation depends on your specific needs
        }
    }

    public override async Task<object?> GetValueUntypedAsync()
    {
        var info = await GetEntityInfoAsync();
        return info?.ToLite();
    }

    public override async Task<bool> IsReadonlyAsync()
    {
        return await Element.IsDomDisabledAsync();
    }

    /// <summary>
    /// Get a field proxy within this embedded entity
    /// </summary>
    public BaseLineProxy GetField(string fieldName)
    {
        var fieldRoute = ItemRoute.Add(fieldName);
        var fieldElement = Element.Locator($"[data-member='{fieldName}']");
        
        return BaseLineProxy.AutoLine(fieldElement, fieldRoute, Page);
    }
}

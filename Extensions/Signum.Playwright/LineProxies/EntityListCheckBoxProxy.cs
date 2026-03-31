namespace Signum.Playwright.LineProxies;

/// <summary>
/// Proxy for EntityCheckBoxList.tsx
/// </summar>
public class EntityCheckBoxListProxy : EntityBaseProxy
{
    public EntityCheckBoxListProxy(ILocator element, PropertyRoute route)
        : base(element, route)
    {
    }

    public override Task<object?> GetValueUntypedAsync() => throw new NotImplementedException();
    public override Task SetValueUntypedAsync(object? value) => throw new NotImplementedException();
    public override Task<bool> IsReadonlyAsync() => throw new NotImplementedException();

    public ILocator CheckBoxElement(Lite<Entity> lite)
        => this.Element.Locator($"input[name='{lite.Key()}']");

    public async Task SetCheckedAsync(Lite<Entity> lite, bool isChecked)
    {
        if (isChecked)
            await CheckBoxElement(lite).CheckAsync();
        else
            await CheckBoxElement(lite).UncheckAsync();
    }
}

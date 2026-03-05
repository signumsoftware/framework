namespace Signum.Playwright.LineProxies;

/// <summary>
/// Proxy for EntityCombo control (dropdown of entities)
/// Equivalent to Selenium's EntityComboProxy
/// </summary>
public class EntityComboProxy : EntityBaseProxy
{
    public EntityComboProxy(ILocator element, PropertyRoute route, IPage page)
        : base(element, route, page)
    {
    }

    protected override ILocator InputLocator => Element.Locator("select.form-control");

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
        if (value == null)
        {
            await SelectByIndexAsync(0); // Select empty option
            return;
        }

        await SelectByValueAsync(value.Key());
    }

    public async Task<Lite<Entity>?> GetLiteAsync()
    {
        var select = InputLocator.First;
        var selectedValue = await select.EvaluateAsync<string?>("el => el.value");

        if (string.IsNullOrEmpty(selectedValue))
            return null;

        var text = await select.EvaluateAsync<string>("el => el.options[el.selectedIndex].text");
        
        // Parse the entity key (format: "Type;Id")
        var parts = selectedValue.Split(';');
        if (parts.Length < 2)
            return null;

        var type = Type.GetType(parts[0]);
        var id = PrimaryKey.Parse(parts[1], type!);

        return Lite.Create(type!, id, text);
    }

    public async Task SelectByValueAsync(string value)
    {
        var select = InputLocator.First;
        await select.SelectOptionAsync(value);
    }

    public async Task SelectByTextAsync(string text)
    {
        var select = InputLocator.First;
        await select.SelectOptionAsync(new SelectOptionValue { Label = text });
    }

    public async Task SelectByIndexAsync(int index)
    {
        var select = InputLocator.First;
        await select.SelectOptionAsync(new SelectOptionValue { Index = index });
    }

    public override async Task<bool> IsReadonlyAsync()
    {
        var select = InputLocator.First;
        return !await select.IsEnabledAsync();
    }
}

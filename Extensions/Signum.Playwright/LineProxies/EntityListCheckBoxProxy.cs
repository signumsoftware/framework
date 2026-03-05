namespace Signum.Playwright.LineProxies;

/// <summary>
/// Proxy for EntityListCheckBox control (checkbox list of entities)
/// Equivalent to Selenium's EntityListCheckBoxProxy
/// </summary>
public class EntityListCheckBoxProxy : EntityBaseProxy
{
    public EntityListCheckBoxProxy(ILocator element, PropertyRoute route, IPage page)
        : base(element, route, page)
    {
    }

    public ILocator Checkboxes => Element.Locator("input[type='checkbox']");

    public override async Task SetValueUntypedAsync(object? value)
    {
        throw new NotImplementedException("EntityListCheckBox SetValue not yet implemented");
    }

    public override async Task<object?> GetValueUntypedAsync()
    {
        var selectedLites = new List<Lite<Entity>>();
        var count = await Checkboxes.CountAsync();

        for (int i = 0; i < count; i++)
        {
            var checkbox = Checkboxes.Nth(i);
            if (await checkbox.IsCheckedAsync())
            {
                var value = await checkbox.GetAttributeAsync("value");
                // Parse entity from value
                if (!string.IsNullOrEmpty(value))
                {
                    var parts = value.Split(';');
                    if (parts.Length >= 2)
                    {
                        var type = Type.GetType(parts[0]);
                        var id = PrimaryKey.Parse(parts[1], type!);
                        var label = await checkbox.Locator("+ label").TextContentAsync();
                        selectedLites.Add(Lite.Create(type!, id, label));
                    }
                }
            }
        }

        return selectedLites;
    }

    public override async Task<bool> IsReadonlyAsync()
    {
        return await Element.IsDomDisabledAsync();
    }

    public async Task SetCheckedAsync(int index, bool isChecked)
    {
        var checkbox = Checkboxes.Nth(index);
        await checkbox.SetCheckedAsync(isChecked);
    }
}

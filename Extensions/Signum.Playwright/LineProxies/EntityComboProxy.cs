using Signum.Playwright.Frames;

namespace Signum.Playwright.LineProxies;

public class EntityComboProxy : EntityBaseProxy
{
    public EntityComboProxy(ILocator element, PropertyRoute route, IPage page)
        : base(element, route, page)
    {
    }

    public ILocator ComboElement => this.Element.Locator("select");
    public ILocator DropdownListInput => this.Element.Locator(".rw-dropdown-list-input");

    public override async Task<object?> GetValueUntypedAsync() => await GetLiteValueAsync();
    public override async Task SetValueUntypedAsync(object? value)
        => await SetLiteValueAsync(value is Entity e ? e.ToLite() : (Lite<Entity>?)value);

    public override async Task<bool> IsReadonlyAsync()
        => await this.Element.Locator("input[readonly]").CountAsync() > 0;

    public async Task<Lite<IEntity>?> GetLiteValueAsync()
    {
        var ei = await EntityInfoInternalAsync(null);
        if (ei == null)
            return null;

        var selected = ComboElement.Locator("option:checked");
        var text = await selected.TextContentAsync();

        return ei.ToLite(text);
    }

    public async Task SetLiteValueAsync(Lite<IEntity>? value)
    {
        var val = value == null ? "" : value.Key();
        await ComboElement.SelectOptionAsync(new SelectOptionValue { Value = val });
    }

    public async Task<List<Lite<Entity>?>> OptionsAsync()
    {
        var options = await ComboElement.Locator("option").AllAsync();

        var result = new List<Lite<Entity>?>();
        foreach (var o in options)
        {
            var val = await o.GetAttributeAsync("value");
            var text = await o.TextContentAsync();
            var lite = Lite.Parse(val)?.Do(l => l.SetModel(text));
            result.Add(lite);
        }

        return result;
    }

    public Task<FrameModalProxy<T>> ViewAsync<T>() where T : ModifiableEntity
        => base.ViewInternalAsync<T>();

    public async Task SelectLabelAsync(string label)
    {
        await WaitChangesAsync(async () =>
        {
            await ComboElement.SelectOptionAsync(new SelectOptionValue { Label = label });
        }, "ComboBox selected");
    }

    public async Task SelectIndexAsync(int index)
    {
        await WaitChangesAsync(async () =>
        {
            await ComboElement.SelectOptionAsync(new SelectOptionValue
            {
                Index = index + 1
            });
        }, "ComboBox selected");
    }

    public async Task<EntityInfoProxy?> EntityInfoAsync()
        => await EntityInfoInternalAsync(null);
}

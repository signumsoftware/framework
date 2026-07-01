using Signum.Playwright.Frames;
using Signum.Playwright.ModalProxies;
using Signum.Playwright.Search;

namespace Signum.Playwright.LineProxies;

/// <summary>
/// Abstract proxy for EntityBase.tsx
/// </summary>
public abstract class EntityBaseProxy : BaseLineProxy
{
    protected EntityBaseProxy(ILocator element, PropertyRoute route)
        : base(element, route)
    {
    }

    internal virtual ILocator ButtonBar => Element;

    public virtual PropertyRoute ItemRoute => this.Route;

    public ILocator CreateButton => ButtonBar.Locator("a.sf-create");

    protected async Task CreateEmbeddedAsync<T>()
    {
        await WaitChangesAsync(async () =>
        {
            var imp = this.ItemRoute.TryGetImplementations();

            if (imp != null && imp.Value.Types.Count() != 1)
            {
                var popup = await CreateButton.CaptureOnClickAsync();
                await ChooseTypeAsync(popup, typeof(T));
            }
            else
            {
                await CreateButton.ClickAsync();
            }

        });
    }

    public async Task<FrameModalProxy<T>> CreateModalAsync<T>() where T : ModifiableEntity
    {
        string changes = await GetChangesAsync();

        var popup = await CreateButton.CaptureOnClickAsync();

        popup = await ChooseTypeCaptureAsync(popup, typeof(T));

        var itemRoute = this.ItemRoute.Type == typeof(T) ? this.ItemRoute : PropertyRoute.Root(typeof(T));

        var modal = await FrameModalProxy<T>.NewAsync(popup, itemRoute);
        modal.Disposing = async okPressed => await WaitNewChangesAsync(changes);
        return modal;
    }

    public ILocator ViewButton => ButtonBar.Locator("a.sf-view");

    protected async Task<FrameModalProxy<T>> ViewInternalAsync<T>() where T : ModifiableEntity
    {
        var popup = await ViewButton.CaptureOnClickAsync();
        string changes = await GetChangesAsync();

        var result = await FrameModalProxy<T>.NewAsync(popup, this.ItemRoute);
        result.Disposing = async okPressed => await WaitNewChangesAsync(changes);
        return result;
    }


    public ILocator FindButton => ButtonBar.Locator("a.sf-find");

    public ILocator RemoveButton => ButtonBar.Locator("a.sf-remove");

    public async Task RemoveAsync()
    {
        await WaitChangesAsync(async () => await RemoveButton.ClickAsync());
    }

    public async Task<SearchModalProxy> FindAsync(Type? selectType = null)
    {
        string changes = await GetChangesAsync();
        var modal = await FindButton.CaptureOnClickAsync();

        modal = await ChooseTypeCaptureAsync(modal, selectType);

        var result = await SearchModalProxy.NewAsync(modal);
        result.Disposing = async okPressed => await WaitNewChangesAsync(changes);
        return result;
    }

    private async Task ChooseTypeAsync(ILocator element, Type selectType)
    {
        if (!await SelectorModalProxy.IsSelectorAsync(element))
            return;

        if (selectType == null)
            throw new InvalidOperationException("No type to choose from selected");

        await element.Locator($"text={TypeLogic.GetCleanName(selectType)}").ClickAsync();
    }

    private async Task<ILocator> ChooseTypeCaptureAsync(ILocator element, Type? selectType)
    {
        if (!await SelectorModalProxy.IsSelectorAsync(element))
            return element;

        if (selectType == null)
            throw new InvalidOperationException("No type to choose from selected");

        await element.Locator($"text={TypeLogic.GetCleanName(selectType)}").ClickAsync();

        return Page.Locator(".modal:visible").Last;
    }

    public async Task WaitChangesAsync(Func<Task> action)
    {
        var changes = await GetChangesAsync();

        await action();

        await WaitNewChangesAsync(changes);
    }

    public async Task WaitNewChangesAsync(string changes)
    {
        await Element.WaitAttributeAsync("data-changes", changes, "!==");
    }

    public async Task<string> GetChangesAsync()
    {
        var attr = await Element.GetAttributeAsync("data-changes");
        if (attr == null)
            throw new InvalidOperationException("data-changes attribute not found");

        return attr;
    }

    public async Task WaitEntityInfoChangesAsync(Func<Task> action, int? index = null)
    {
        var entityInfo = await EntityInfoStringAsync(index);

        await action();

        await DataEntityLocator(index).WaitAttributeAsync("data-entity", entityInfo, "!==");
    }

    protected async Task<string> EntityInfoStringAsync(int? index)
    {
        ILocator element = DataEntityLocator(index);

        var attr = await element.GetAttributeAsync("data-entity");

        if (attr == null)
            throw new InvalidOperationException("data-entity attribute not found");

        return attr;
    }

    private ILocator DataEntityLocator(int? index)
    {
        return index == null ?
            Element :
            Element.Locator("[data-entity]").Nth(index.Value);
    }

    protected async Task<EntityInfoProxy?> EntityInfoInternalAsync(int? index)
        => EntityInfoProxy.Parse(await EntityInfoStringAsync(index));

    protected async Task AutoCompleteWaitChangesAsync(ILocator input, ILocator container, Lite<IEntity> lite)
    {
        await WaitChangesAsync(async () =>
        {
            await AutoCompleteBasicAsync(input, container, lite);
        });
    }

    protected async Task AutoCompleteWaitChangesAsync(ILocator input, ILocator container, string beginning, bool resultContainsText = true)
    {
        await WaitChangesAsync(async () =>
        {
            await AutoCompleteBasicAsync(input, container, beginning, resultContainsText);
        });
    }

    protected static async Task AutoCompleteBasicAsync(ILocator input, ILocator container, Lite<IEntity> lite)
    {
        await input.ClickAsync();
        await input.ClearAsync();
        //await input.PressSequentiallyAsync("id:" + lite.Id.ToString(), new() { Delay = 50 });
        await input.TypeAsync("id:" + lite.Id.ToString());

        var list = container.Locator(".typeahead.dropdown-menu");
        await list.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        var item = list.Locator($"[data-entity-key='{lite.Key()}']");
        await item.ClickAsync();
    }

    protected static async Task AutoCompleteBasicAsync(ILocator input, ILocator container, string beginning, bool resultContainsText = true)
    {
        await input.ClickAsync();
        await input.ClearAsync();
        //await input.PressSequentiallyAsync(beginning, new() { Delay = 50 });
        await input.TypeAsync(beginning);

        var list = container.Locator(".typeahead.dropdown-menu");
        await list.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        var item = list.Locator("[data-entity-key]");
        if (resultContainsText)
            item = item.Filter(new() { HasTextString = beginning });

        await item.First.ClickAsync();
    }
}


public class EntityInfoProxy
{
    public bool IsNew { get; set; }
    public string TypeName { get; set; }

    public Type EntityType;
    public PrimaryKey? IdOrNull { get; set; }

    public EntityInfoProxy(string dataEntity)
    {
        var parts = dataEntity.Split(';');

        var typeName = parts[0];
        var id = parts[1];
        var isNew = parts[2];

        var type = TypeLogic.GetType(typeName);

        this.TypeName = typeName;
        this.EntityType = type;
        this.IdOrNull = id.HasText() ? PrimaryKey.Parse(id, type) : (PrimaryKey?)null;
        this.IsNew = isNew.HasText() && bool.Parse(isNew);
    }

    public Lite<Entity> ToLite(object? liteModel = null)
    {
        if (liteModel == null)
            return Lite.Create(this.EntityType, this.IdOrNull!.Value);
        else
            return Lite.Create(this.EntityType, this.IdOrNull!.Value, liteModel);
    }

    public static EntityInfoProxy? Parse(string dataEntity)
    {
        if (dataEntity == "null" || dataEntity == "undefined")
            return null;

        return new EntityInfoProxy(dataEntity);
    }

    public static async Task<EntityInfoProxy> GetFromMainEntityAsync(ILocator locator, string attribute = "data-main-entity")
    {
        await locator.WaitAttributeAsync(attribute, null, "!==");
        var attr = await locator.GetAttributeAsync(attribute);
        if (attr == null)
            throw new InvalidOperationException($"{attribute} attribute not found");

        return Parse(attr)!;
    }
}

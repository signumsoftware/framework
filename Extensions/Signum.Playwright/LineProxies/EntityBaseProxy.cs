using Signum.Playwright.Frames;
using Signum.Playwright.ModalProxies;
using Signum.Playwright.Search;

namespace Signum.Playwright.LineProxies;

public abstract class EntityBaseProxy : BaseLineProxy
{
    protected EntityBaseProxy(ILocator element, PropertyRoute route, IPage page)
        : base(element, route, page)
    {
    }

    public virtual PropertyRoute ItemRoute => this.Route;

    public ILocator CreateButton => Element.Locator("a.sf-create");

    protected async Task CreateEmbeddedAsync<T>()
    {
        await WaitChangesAsync(async () =>
        {
            var imp = this.ItemRoute.TryGetImplementations();

            if (imp != null && imp.Value.Types.Count() != 1)
            {
                var popup = await CaptureOnClickAsync(CreateButton);
                await ChooseTypeAsync(popup, typeof(T));
            }
            else
            {
                await CreateButton.ClickAsync();
            }

        }, "create clicked");
    }

    public async Task<FrameModalProxy<T>> CreateModalAsync<T>() where T : ModifiableEntity
    {
        string changes = await GetChangesAsync();

        var popup = await CaptureOnClickAsync(await CreateButton.WaitForAsync(new() { State = WaitForSelectorState.Visible }).ContinueWith(_ => CreateButton));

        popup = await ChooseTypeCaptureAsync(popup, typeof(T));

        var itemRoute = this.ItemRoute.Type == typeof(T) ? this.ItemRoute : PropertyRoute.Root(typeof(T));

        return new FrameModalProxy<T>(Page, popup, itemRoute)
        {
            Disposing = async okPressed => await WaitNewChangesAsync(changes, "create dialog closed")
        };
    }

    public ILocator ViewButton => Element.Locator("a.sf-view");

    protected async Task<FrameModalProxy<T>> ViewInternalAsync<T>() where T : ModifiableEntity
    {
        var popup = await CaptureOnClickAsync(ViewButton);
        string changes = await GetChangesAsync();

        return new FrameModalProxy<T>(Page, popup, this.ItemRoute)
        {
            Disposing = async okPressed => await WaitNewChangesAsync(changes, "create dialog closed")
        };
    }

    public ILocator FindButton => Element.Locator("a.sf-find");

    public ILocator RemoveButton => Element.Locator("a.sf-remove");

    public async Task RemoveAsync()
    {
        await WaitChangesAsync(async () => await RemoveButton.ClickAsync(), "removing");
    }

    public async Task<SearchModalProxy> FindAsync(Type? selectType = null)
    {
        string changes = await GetChangesAsync();
        var popup = await CaptureOnClickAsync(FindButton);

        popup = await ChooseTypeCaptureAsync(popup, selectType);

        return new SearchModalProxy(popup, Page)
        {
            Disposing = async okPressed => await WaitNewChangesAsync(changes, "create dialog closed")
        };
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

    public async Task WaitChangesAsync(Func<Task> action, string actionDescription)
    {
        var changes = await GetChangesAsync();

        await action();

        await WaitNewChangesAsync(changes, actionDescription);
    }

    public async Task WaitNewChangesAsync(string changes, string actionDescription)
    {
        await Page.WaitForFunctionAsync(
            @"(element, oldVal) => element.getAttribute('data-changes') !== oldVal",
            new object[] { Element, changes });
    }

    public async Task<string> GetChangesAsync()
    {
        var attr = await Element.GetAttributeAsync("data-changes");
        if (attr == null)
            throw new InvalidOperationException("data-changes attribute not found");

        return attr;
    }

    public async Task WaitEntityInfoChangesAsync(Func<Task> action, string actionDescription, int? index = null)
    {
        var entityInfo = await EntityInfoStringAsync(index);

        await action();

        await Page.WaitForFunctionAsync(
            @"(element, oldVal, index) => {
                let target = index == null ? element :
                    element.querySelectorAll('[data-entity]')[index];
                return target.getAttribute('data-entity') !== oldVal;
            }",
            new object[] { Element, entityInfo, index });
    }

    protected async Task<string> EntityInfoStringAsync(int? index)
    {
        var element = index == null
            ? Element
            : Element.Locator("[data-entity]").Nth(index.Value);

        var attr = await element.GetAttributeAsync("data-entity");

        if (attr == null)
            throw new InvalidOperationException("data-entity attribute not found");

        return attr;
    }

    protected async Task<EntityInfoProxy?> EntityInfoInternalAsync(int? index)
        => EntityInfoProxy.Parse(await EntityInfoStringAsync(index));

    public async Task AutoCompleteWaitChangesAsync(ILocator input, ILocator container, Lite<IEntity> lite)
    {
        await WaitChangesAsync(async () =>
        {
            await AutoCompleteBasicAsync(input, container, lite);
        }, "autocomplete selection");
    }

    public async Task AutoCompleteWaitChangesAsync(ILocator input, ILocator container, string beginning)
    {
        await WaitChangesAsync(async () =>
        {
            await AutoCompleteBasicAsync(input, container, beginning);
        }, "autocomplete selection");
    }

    public static async Task AutoCompleteBasicAsync(ILocator input, ILocator container, Lite<IEntity> lite)
    {
        await input.FillAsync("id:" + lite.Id.ToString());

        var list = container.Locator(".typeahead.dropdown-menu");
        await list.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        var item = list.Locator($"[data-entity-key='{lite.Key()}']");
        await item.ClickAsync();
    }

    public static async Task AutoCompleteBasicAsync(ILocator input, ILocator container, string beginning)
    {
        await input.FillAsync(beginning);

        var list = container.Locator(".typeahead.dropdown-menu");
        await list.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        var item = list.Locator("[data-entity-key]").Filter(new() { HasTextString = beginning });
        await item.First.ClickAsync();
    }

    protected async Task<ILocator> CaptureOnClickAsync(ILocator locator)
    {
        await locator.ClickAsync();
        return Page.Locator(".modal:visible").Last;
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
}

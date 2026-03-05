using Microsoft.Playwright;
using Signum.Basics;

namespace Signum.Playwright.LineProxies;

/// <summary>
/// Base class for all entity-related line proxies
/// Equivalent to Selenium's EntityBaseProxy
/// </summary>
public abstract class EntityBaseProxy : BaseLineProxy
{
    protected EntityBaseProxy(ILocator element, PropertyRoute route, IPage page)
        : base(element, route, page)
    {
    }

    public virtual PropertyRoute ItemRoute => Route;

    #region Buttons

    public ILocator CreateButton => Element.Locator("a.sf-create");
    public ILocator ViewButton => Element.Locator("a.sf-view");
    public ILocator FindButton => Element.Locator("a.sf-find");
    public ILocator RemoveButton => Element.Locator("a.sf-remove");

    #endregion

    #region Actions

    public async Task RemoveAsync()
    {
        await WaitChangesAsync(async () =>
        {
            await RemoveButton.ClickAsync();
        }, "removing");
    }

    public async Task<ModalProxy> CreateModalAsync<T>() where T : ModifiableEntity
    {
        var changes = await GetChangesAsync();

        var popup = await ModalProxy.CaptureAsync(Page, async () =>
        {
            await CreateButton.ClickAsync();
        });

        // Check if need to choose type
        await ChooseTypeIfNeededAsync(popup, typeof(T));

        popup.AfterClose = async () => await WaitNewChangesAsync(changes, "create dialog closed");

        return popup;
    }

    public async Task<ModalProxy> ViewModalAsync<T>() where T : ModifiableEntity
    {
        var changes = await GetChangesAsync();

        var popup = await ModalProxy.CaptureAsync(Page, async () =>
        {
            await ViewButton.ClickAsync();
        });

        popup.AfterClose = async () => await WaitNewChangesAsync(changes, "view dialog closed");

        return popup;
    }

    public async Task<ModalProxy> FindModalAsync(Type? selectType = null)
    {
        var changes = await GetChangesAsync();

        var popup = await ModalProxy.CaptureAsync(Page, async () =>
        {
            await FindButton.ClickAsync();
        });

        if (selectType != null)
        {
            await ChooseTypeIfNeededAsync(popup, selectType);
        }

        popup.AfterClose = async () => await WaitNewChangesAsync(changes, "find dialog closed");

        return popup;
    }

    #endregion

    #region AutoComplete

    public ILocator AutoCompleteElement => Element.Locator(".sf-entity-autocomplete");

    public async Task AutoCompleteAsync(Lite<IEntity> lite)
    {
        await WaitChangesAsync(async () =>
        {
            await AutoCompleteBasicAsync(lite);
        }, "autocomplete selection");
    }

    public async Task AutoCompleteAsync(string beginning)
    {
        await WaitChangesAsync(async () =>
        {
            await AutoCompleteBasicAsync(beginning);
        }, "autocomplete selection");
    }

    protected async Task AutoCompleteBasicAsync(Lite<IEntity> lite)
    {
        var input = AutoCompleteElement.First;
        await input.FillAsync($"id:{lite.Id}");

        var dropdown = Element.Locator(".typeahead.dropdown-menu");
        await dropdown.WaitVisibleAsync();

        var item = dropdown.Locator($"[data-entity-key='{lite.Key()}']");
        await item.ClickAsync();
    }

    protected async Task AutoCompleteBasicAsync(string beginning)
    {
        var input = AutoCompleteElement.First;
        await input.FillAsync(beginning);

        var dropdown = Element.Locator(".typeahead.dropdown-menu");
        await dropdown.WaitVisibleAsync();

        // Wait for items to load
        await Task.Delay(300);

        var items = dropdown.Locator("[data-entity-key]");
        var count = await items.CountAsync();

        for (int i = 0; i < count; i++)
        {
            var item = items.Nth(i);
            var text = await item.TextContentAsync();
            if (text?.Contains(beginning) == true)
            {
                await item.ClickAsync();
                return;
            }
        }

        throw new InvalidOperationException($"No item found containing '{beginning}'");
    }

    #endregion

    #region Entity Info

    public async Task<EntityInfoProxy?> GetEntityInfoAsync(int? index = null)
    {
        var entityString = await GetEntityInfoStringAsync(index);
        return EntityInfoProxy.Parse(entityString);
    }

    protected async Task<string> GetEntityInfoStringAsync(int? index = null)
    {
        ILocator element;
        if (index == null)
        {
            element = Element;
        }
        else
        {
            var entities = Element.Locator("[data-entity]");
            element = entities.Nth(index.Value);
        }

        return await element.GetAttributeAsync("data-entity") ?? "";
    }

    #endregion

    #region Change Tracking

    public async Task WaitChangesAsync(Func<Task> action, string actionDescription)
    {
        var changes = await GetChangesAsync();
        await action();
        await WaitNewChangesAsync(changes, actionDescription);
    }

    public async Task WaitNewChangesAsync(string oldChanges, string actionDescription)
    {
        await Page.WaitAsync(async () =>
        {
            var newChanges = await GetChangesAsync();
            return newChanges != oldChanges;
        }, $"Waiting for changes after {actionDescription} in {Route}", TimeSpan.FromSeconds(10));
    }

    public async Task<string> GetChangesAsync()
    {
        return await Element.GetAttributeAsync("data-changes") ?? "0";
    }

    #endregion

    #region Type Selection

    protected async Task ChooseTypeIfNeededAsync(ModalProxy modal, Type selectType)
    {
        // Check if this is a type selector modal
        var isSelector = await modal.Modal.Locator(".sf-type-selector").CountAsync() > 0;
        
        if (!isSelector)
            return;

        var typeName = selectType.Name;
        var typeButton = modal.Modal.Locator($"button:has-text('{typeName}')");
        await typeButton.ClickAsync();
    }

    #endregion
}

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

/// <summary>
/// Simple entity info holder
/// </summary>
public class EntityInfoProxy
{
    public string? EntityType { get; set; }
    public PrimaryKey? Id { get; set; }
    public string? ToString { get; set; }

    public static EntityInfoProxy? Parse(string entityString)
    {
        if (string.IsNullOrEmpty(entityString))
            return null;

        // Parse format: "TypeName;Id;ToString"
        var parts = entityString.Split(';');
        if (parts.Length < 2)
            return null;

        return new EntityInfoProxy
        {
            EntityType = parts[0],
            Id = PrimaryKey.Parse(parts[1], Type.GetType(parts[0])!),
            ToString = parts.Length > 2 ? parts[2] : null
        };
    }

    public Lite<Entity>? ToLite()
    {
        if (EntityType == null || Id == null)
            return null;

        var type = Type.GetType(EntityType);
        if (type == null)
            return null;

        return Lite.Create(type, Id.Value, ToString);
    }
}

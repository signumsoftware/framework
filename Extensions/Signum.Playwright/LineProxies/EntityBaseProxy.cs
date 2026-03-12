using Microsoft.Playwright;
using Signum.Basics;
using Signum.Playwright.ModalProxies;

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
/// Represents entity information parsed from data attributes
/// </summary>
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

    public static EntityInfoProxy? Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // Format: TypeName;Id;ToString or TypeName;(new);ToString
        var parts = value.Split(new[] { ';' }, StringSplitOptions.None);

        if (parts.Length < 2)
            return null;

        var typeName = parts[0];
        var isNew = parts[1] == "(new)";
        PrimaryKey? id = null;

        if (!isNew && !string.IsNullOrEmpty(parts[1]))
        {
            var type = TypeLogic.NameToType.TryGetC(typeName);
            if (type != null)
            {
                id = PrimaryKey.Parse(parts[1], type);
            }
        }

        var toStringValue = parts.Length > 2 ? string.Join(";", parts.Skip(2)) : null;

        return new EntityInfoProxy(typeName, id, toStringValue, isNew);
    }

    public Lite<Entity> ToLite()
    {
        if (IsNew || !Id.HasValue)
            throw new InvalidOperationException("Cannot create Lite from new entity");

        var type = TypeLogic.NameToType.TryGetC(TypeName);
        if (type == null)
            throw new InvalidOperationException($"Type {TypeName} not found in TypeLogic");

        return Lite.Create(type, Id.Value, ToStringValue);
    }

    public Lite<T> ToLite<T>() where T : Entity
    {
        return (Lite<T>)(object)ToLite();
    }

    public string ToJsString()
    {
        if (IsNew)
            return $"{TypeName};(new);{ToStringValue}";

        return $"{TypeName};{Id};{ToStringValue}";
    }

    public override string ToString()
    {
        return ToJsString();
    }
}

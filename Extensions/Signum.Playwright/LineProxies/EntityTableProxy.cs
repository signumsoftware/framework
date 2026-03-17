using Signum.Playwright.Frames;
using System.Xml.Linq;

namespace Signum.Playwright.LineProxies;

public class EntityTableProxy : EntityBaseProxy
{
    public override PropertyRoute ItemRoute => base.ItemRoute.Add("Item");

    public EntityTableProxy(ILocator element, PropertyRoute route, IPage page)
        : base(element, route, page)
    {
    }

    public override Task<object?> GetValueUntypedAsync() => throw new NotImplementedException();
    public override Task SetValueUntypedAsync(object? value) => throw new NotImplementedException();
    public override Task<bool> IsReadonlyAsync() => throw new NotImplementedException();

    public ILocator TableElement => this.Element.Locator(".sf-table");

    public ILocator RowElement(int index)
        => TableElement.Locator($"> tbody > tr:nth-child({index + 1})");

    public async Task<int> RowsCountAsync()
        => await TableElement.Locator("> tbody > tr").CountAsync();

    public async Task RemoveAsync(int index)
        => await RowElement(index).Locator(".sf-remove").ClickAsync();

    public async Task<EntityTableRow<T>> RowAsync<T>(int index) where T : ModifiableEntity
        => new EntityTableRow<T>(this, RowElement(index), this.ItemRoute, Page);
}

public class EntityTableRow<T> : LineContainer<T>
    where T : ModifiableEntity
{
    public EntityTableProxy EntityTable { get; }
    private IPage Page;

    public EntityTableRow(EntityTableProxy entityTable, ILocator element, PropertyRoute? route, IPage page)
        : base(element, page, route)
    {
        EntityTable = entityTable;
        Page = page;
    }

    public async Task<int> IndexAsync()
    {
        var rows = await EntityTable.TableElement.Locator("> tbody > tr").AllAsync();
        for (int i = 0; i < rows.Count; i++)
            if (rows[i].Equals(Element))
                return i;

        return -1;
    }
}

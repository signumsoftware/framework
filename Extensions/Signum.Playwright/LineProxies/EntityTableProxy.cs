using Signum.Playwright.Frames;
using System.Xml.Linq;

namespace Signum.Playwright.LineProxies;

public class EntityTableProxy : EntityBaseProxy
{
    public override PropertyRoute ItemRoute => base.ItemRoute.Add("Item");

    public EntityTableProxy(ILocator element, PropertyRoute route)
        : base(element, route)
    {
    }

    public override Task<object?> GetValueUntypedAsync() => throw new NotImplementedException();
    public override Task SetValueUntypedAsync(object? value) => throw new NotImplementedException();
    public override Task<bool> IsReadonlyAsync() => throw new NotImplementedException();

    public virtual ILocator TableElement => Element.Locator(".sf-table");

    public virtual ILocator RowElement(int index)
    {
        return TableElement.Locator($"> tbody > tr:nth-child({index + 1})");
    }

    public async Task WaitItemLoadedAsync(int index)
    {
        await RowElement(index).WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }

    public virtual async Task<int> RowsCountAsync()
    {
        var rows = await TableElement.Locator("> tbody > tr").AllAsync();
        return rows.Count;
    }

    public EntityTableRow<T> Row<T>(int index) where T : ModifiableEntity
    {
        return new EntityTableRow<T>(this, RowElement(index), this.ItemRoute);
    }

    internal async Task<int> IndexOfAsync(ILocator row)
    {
        var rows = await TableElement.Locator("> tbody > tr").AllAsync();
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i] == row) // Achtung: Referenzvergleich auf ILocator; ggf. CompareHandles verwenden
                return i;
        }
        return -1;
    }

    public ILocator RemoveRowButton(int index)
    {
        return RowElement(index).Locator(".sf-remove");
    }

    public async Task RemoveAsync(int index)
    {
        await RemoveRowButton(index).ClickAsync();
    }

    public async Task<EntityInfoProxy?> EntityInfoAsync(int index)
    {
        return await EntityInfoInternalAsync(index);
    }

    public async Task<EntityTableRow<T>> CreateRowAsync<T>() where T : ModifiableEntity
    {
        await CreateEmbeddedAsync<T>();
        return await LastRowAsync<T>();
    }

    public async Task<EntityTableRow<T>> LastRowAsync<T>() where T : ModifiableEntity
    {
        var count = await RowsCountAsync();
        return Row<T>(count - 1);
    }
}

public class EntityTableRow<T> : LineContainer<T>
    where T : ModifiableEntity
{
    public EntityTableProxy EntityTable { get; }

    public EntityTableRow(EntityTableProxy entityTable, ILocator element, PropertyRoute? route)
        : base(element, route)
    {
        EntityTable = entityTable;
    }

    public async Task<int> IndexAsync()
    {
        var rows = await EntityTable.TableElement.Locator("> tbody > tr").AllAsync();
        for (int i = 0; i < rows.Count; i++)
            if (rows[i].Equals(Element))
                return i;

        return -1;
    }

    public async Task<EntityTableRow<T>> WaitRefreshAsync(Func<Task> action)
    {
        var index = await IndexAsync();
        var originalHandle = await Element.ElementHandleAsync();

        await action();

        EntityTableRow<T> result = null!;
        await Element.Page.WaitForFunctionAsync(
            @"([table, index, handle]) => {
                const rows = table.querySelectorAll(':scope > tbody > tr');
                return rows[index] && rows[index] !== handle;
            }",
            new object[] { await EntityTable.TableElement.ElementHandleAsync(), index, originalHandle }
        );

        result = EntityTable.Row<T>(index);
        return result;
    }
}

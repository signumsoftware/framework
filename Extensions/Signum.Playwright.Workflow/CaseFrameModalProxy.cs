using Microsoft.Playwright;
using Signum.Playwright.Frames;
using Signum.Playwright.LineProxies;
using Signum.Playwright.ModalProxies;
using Signum.Workflow;

namespace Signum.Playwright.Workflow;

/// <summary>
/// Proxy of CaseFrameModal.tsx
/// </summary>
public class CaseFrameModalProxy<T> : ModalProxy, ILineContainer<T>, IEntityButtonContainer<T>, IValidationSummaryContainer 
    where T : ICaseMainEntity
{
    public PropertyRoute Route { get; }
    CaseFrameModalProxy(ILocator locator, PropertyRoute? route = null) : base(locator)
    {
        Route = route ?? PropertyRoute.Root(typeof(T));
    }

    public static async Task<CaseFrameModalProxy<T>> NewAsync(ILocator modal, PropertyRoute? route = null)
    {
        var result = new CaseFrameModalProxy<T>(modal, route);
        await result.MainControl.WaitVisibleAsync();
        return result;
    }

    ILocator IEntityButtonContainer.Container => Modal;
    ILocator ILineContainer.Element => Modal;
    ILocator IValidationSummaryContainer.Element => Modal;

    public override async ValueTask DisposeAsync()
    {
        if (!AvoidClose)
        {
            await this.Modal.Page.WaitAsync(async () =>
            {
                if (!await Modal.IsVisibleAsync())
                    return true;

                if (await TryToCloseAsync())
                    return true;

                var message = await Modal.Page.GetMessageModalAsync();
                if (message != null)
                {
                    await message.ClickAsync(MessageModalButton.Yes);
                }

                return false;
            });
        }

        Disposing?.Invoke(this.OkPressed);
    }
    private async Task<bool> TryToCloseAsync()
    {
        if (await this.CloseButton.IsVisibleAsync())
        {
            await this.CloseButton.ClickAsync();
            return false;
        }
        else
        {
            return true;
        }
    }

    private ILocator MainControl => Modal.Locator("div.sf-main-control");

    ILocator IEntityButtonContainer.MainControl => MainControl;

    public Task<EntityInfoProxy> GetEntityInfoAsync() => EntityInfoProxy.GetFromMainEntityAsync(MainControl);
}

public static class CaseFrameModalProxyExtension
{
    public static async Task<CaseFrameModalProxy<T>> Await_AsCaseFrameModal<T>(this Task<ILocator> modal)
    where T : ICaseMainEntity
    {
        return await CaseFrameModalProxy<T>.NewAsync(await modal);
    }

    public static Task<CaseFrameModalProxy<T>> AsCaseFrameModal<T>(this ILocator modal)
        where T : ICaseMainEntity   
    {
        return CaseFrameModalProxy<T>.NewAsync(modal);
    }
}

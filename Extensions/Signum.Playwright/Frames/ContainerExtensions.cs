using Microsoft.Playwright;

namespace Signum.Playwright.Frames;

public static class EntityButtonContainerExtensions
{
    public static ILocator SaveButton<T>(this IEntityButtonContainer<T> container) where T : ModifiableEntity
    {
        return container.Element.Locator("button.sf-entity-button-save, button:has-text('Save')");
    }

    public static async Task SaveAsync<T>(this IEntityButtonContainer<T> container) where T : ModifiableEntity
    {
        await container.SaveButton().ClickAsync();
        await container.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public static ILocator OperationButton<T>(this IEntityButtonContainer<T> container, string operationKey) where T : ModifiableEntity
    {
        return container.Element.Locator($"button[data-operation='{operationKey}'], button.sf-operation-button:has-text('{operationKey}')");
    }

    public static async Task ExecuteOperationAsync<T>(this IEntityButtonContainer<T> container, string operationKey) where T : ModifiableEntity
    {
        var button = container.OperationButton(operationKey);
        await button.ClickAsync();
        await container.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public static async Task<ModalProxy> ExecuteOperationCaptureAsync<T>(this IEntityButtonContainer<T> container, string operationKey) where T : ModifiableEntity
    {
        var button = container.OperationButton(operationKey);
        return await ModalProxy.CaptureAsync(container.Page, async () =>
        {
            await button.ClickAsync();
        });
    }

    public static async Task<bool> OperationPresentAsync<T>(this IEntityButtonContainer<T> container, string operationKey) where T : ModifiableEntity
    {
        return await container.OperationButton(operationKey).IsPresentAsync();
    }

    public static async Task<bool> OperationEnabledAsync<T>(this IEntityButtonContainer<T> container, string operationKey) where T : ModifiableEntity
    {
        var button = container.OperationButton(operationKey);
        if (!await button.IsPresentAsync())
            return false;

        return await button.IsEnabledAsync();
    }
}

public static class WidgetContainerExtensions
{
    public static ILocator Widget(this IWidgetContainer container, string widgetId)
    {
        return container.Element.Locator($"[data-widget-id='{widgetId}'], .sf-widget[data-id='{widgetId}']");
    }

    public static async Task<bool> HasWidgetAsync(this IWidgetContainer container, string widgetId)
    {
        return await container.Widget(widgetId).IsPresentAsync();
    }

    public static async Task ClickWidgetAsync(this IWidgetContainer container, string widgetId)
    {
        await container.Widget(widgetId).ClickAsync();
    }
}

public static class ValidationSummaryContainerExtensions
{
    public static ILocator ValidationSummary(this IValidationSummaryContainer container)
    {
        return container.Element.Locator(".alert-danger, .validation-summary-errors, .sf-validation-summary");
    }

    public static async Task<bool> HasValidationErrorsAsync(this IValidationSummaryContainer container)
    {
        return await container.ValidationSummary().IsPresentAsync();
    }

    public static async Task<string?> GetValidationErrorsTextAsync(this IValidationSummaryContainer container)
    {
        if (!await container.HasValidationErrorsAsync())
            return null;

        return await container.ValidationSummary().TextContentAsync();
    }

    public static async Task<List<string>> GetValidationErrorsAsync(this IValidationSummaryContainer container)
    {
        if (!await container.HasValidationErrorsAsync())
            return new List<string>();

        var errors = new List<string>();
        var items = container.ValidationSummary().Locator("li, .validation-error");
        var count = await items.CountAsync();

        for (int i = 0; i < count; i++)
        {
            var text = await items.Nth(i).TextContentAsync();
            if (!string.IsNullOrWhiteSpace(text))
                errors.Add(text.Trim());
        }

        return errors;
    }
}

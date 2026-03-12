namespace Signum.Playwright.Frames;

public interface IValidationSummaryContainer
{
    ILocator Element { get; }
}

public static class ValidationSummaryContainerExtensions
{
    public static ILocator ValidationSummary(this IValidationSummaryContainer container)
    {
        return container.Element.Locator("ul.validation-summary");
    }

    public static async Task<IReadOnlyList<string>> ValidationErrorsAsync(this IValidationSummaryContainer container)
    {
        var errors = container.ValidationSummary().Locator("> li");

        return await errors.AllTextContentsAsync();
    }
}


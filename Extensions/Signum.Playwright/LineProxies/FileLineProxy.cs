using Microsoft.Playwright;
using Signum.Basics;
using Signum.Entities.Reflection;

namespace Signum.Playwright.LineProxies;

/// <summary>
/// Proxy for file upload controls
/// Equivalent to Selenium's FileLineProxy
/// </summary>
public class FileLineProxy : BaseLineProxy
{
    public FileLineProxy(ILocator element, PropertyRoute route, IPage page)
        : base(element, route, page)
    {
    }

    protected override ILocator InputLocator => Element.Locator("input[type='file']");

    public override async Task SetValueUntypedAsync(object? value)
    {
        if (value == null)
        {
            await RemoveAsync();
        }
        else if (value is string filePath)
        {
            await UploadFileAsync(filePath);
        }
        else
        {
            // Try to get file path from object
            var filePathProp = value.GetType().GetProperty("FullPhysicalPath");
            if (filePathProp != null)
            {
                var path = filePathProp.GetValue(value) as string;
                if (path != null)
                {
                    await UploadFileAsync(path);
                    return;
                }
            }
            
            throw new ArgumentException($"Unsupported value type: {value.GetType()}. Expected string file path.");
        }
    }

    public override async Task<object?> GetValueUntypedAsync()
    {
        return await GetFileNameAsync();
    }

    public override async Task<bool> IsReadonlyAsync()
    {
        var input = InputLocator.First;
        return !await input.IsEnabledAsync();
    }

    /// <summary>
    /// Upload a file
    /// </summary>
    public async Task UploadFileAsync(string filePath)
    {
        var input = InputLocator.First;
        
        // Make sure file exists
        if (!System.IO.File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        await input.SetInputFilesAsync(filePath);
        
        // Wait for upload to complete
        await Task.Delay(500);
    }

    /// <summary>
    /// Upload multiple files
    /// </summary>
    public async Task UploadFilesAsync(params string[] filePaths)
    {
        var input = InputLocator.First;
        
        foreach (var path in filePaths)
        {
            if (!System.IO.File.Exists(path))
                throw new FileNotFoundException($"File not found: {path}");
        }

        await input.SetInputFilesAsync(filePaths);
        
        // Wait for upload to complete
        await Task.Delay(500);
    }

    /// <summary>
    /// Get the uploaded file name
    /// </summary>
    public async Task<string?> GetFileNameAsync()
    {
        var fileNameElement = Element.Locator(".sf-file-name, .file-name");
        if (await fileNameElement.CountAsync() > 0)
        {
            return await fileNameElement.First.TextContentAsync();
        }

        return null;
    }

    /// <summary>
    /// Remove the uploaded file
    /// </summary>
    public async Task RemoveAsync()
    {
        var removeButton = Element.Locator(".sf-file-remove, .file-remove");
        if (await removeButton.CountAsync() > 0)
        {
            await removeButton.First.ClickAsync();
        }
    }

    /// <summary>
    /// Download the file
    /// </summary>
    public async Task<string> DownloadFileAsync(string downloadPath)
    {
        var downloadLink = Element.Locator("a.sf-file-download, a.file-download");
        
        var download = await Page.RunAndWaitForDownloadAsync(async () =>
        {
            await downloadLink.ClickAsync();
        });

        var fileName = download.SuggestedFilename;
        var fullPath = System.IO.Path.Combine(downloadPath, fileName);
        
        await download.SaveAsAsync(fullPath);
        
        return fullPath;
    }
}

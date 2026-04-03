using Microsoft.AspNetCore.Mvc;
using Signum.API;
using Signum.API.Filters;

namespace Signum.Agent;

public class LanguageModelController : Controller
{
    [HttpGet("api/chatbot/provider/{providerKey}/models")]
    public async Task<List<string>> GetModels(string providerKey, CancellationToken token)
    {
        var symbol = SymbolLogic<LanguageModelProviderSymbol>.ToSymbol(providerKey);
        return (await LanguageModelLogic.GetModelNamesAsync(symbol, token)).Order().ToList();
    }

    [HttpGet("api/chatbot/provider/{providerKey}/embeddingModels")]
    public async Task<List<string>> GetEmbeddingModels(string providerKey, CancellationToken token)
    {
        var symbol = SymbolLogic<LanguageModelProviderSymbol>.ToSymbol(providerKey);
        return (await LanguageModelLogic.GetEmbeddingModelNamesAsync(symbol, token)).Order().ToList();
    }
}

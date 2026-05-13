import { ajaxGet } from '@framework/Services';
import { Navigator, EntitySettings } from '@framework/Navigator';
import { ChatbotLanguageModelEntity, EmbeddingsLanguageModelEntity, LanguageModelProviderSymbol } from './Signum.Agent';

export namespace LanguageModelClient {

  export function start(options: { routes: unknown[] }): void {
    Navigator.addSettings(new EntitySettings(ChatbotLanguageModelEntity, e => import('./Templates/ChatbotLanguageModel')));
    Navigator.addSettings(new EntitySettings(EmbeddingsLanguageModelEntity, e => import('./Templates/EmbeddingsLanguageModel')));
  }

  export namespace API {
    export function getModels(provider: LanguageModelProviderSymbol): Promise<string[]> {
      return ajaxGet({ url: `/api/chatbot/provider/${provider.key}/models` });
    }

    export function getEmbeddingModels(provider: LanguageModelProviderSymbol): Promise<string[]> {
      return ajaxGet({ url: `/api/chatbot/provider/${provider.key}/embeddingModels` });
    }
  }
}

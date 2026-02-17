import * as React from 'react'
import { Link, RouteObject } from 'react-router'
import { ajaxGet, ajaxPost, wrapRequest, AjaxOptions } from '@framework/Services';
import { Navigator, EntitySettings } from '@framework/Navigator'
import { ChatbotLanguageModelEntity, ChatSessionEntity, ChatMessageEntity, LanguageModelProviderSymbol, EmbeddingsLanguageModelEntity, UserFeedback } from './Signum.Agent'
import { toAbsoluteUrl } from '../../Signum/React/AppContext';
import { registerToString } from '../../Signum/React/Signum.Entities';
import { FontAwesomeIcon } from '@framework/Lines';

const ChatMarkdown = React.lazy(() => import("./Templates/ChatMarkdown"));

export namespace ChatbotClient {

  export let renderMarkdown = (markdown: string): React.JSX.Element => <ChatMarkdown content={markdown}/>;
  //export let renderMarkdown = (markdown: string): React.JSX.Element => <ReactMarkdownWithFormulas>{markdown}</ReactMarkdownWithFormulas>;
  export function start(options: { routes: RouteObject[] }): void {

    Navigator.addSettings(new EntitySettings(ChatbotLanguageModelEntity, e => import('./Templates/ChatbotLanguageModel')));
    Navigator.addSettings(new EntitySettings(EmbeddingsLanguageModelEntity, e => import('./Templates/EmbeddingsLanguageModel')));
    Navigator.addSettings(new EntitySettings(ChatSessionEntity, a => import('./Templates/ChatSession')));
    Navigator.addSettings(new EntitySettings(ChatMessageEntity, a => import('./Templates/ChatMessage')));
  }

  export namespace API {

    export function ask(question: string, sessionId?: string | number, signal?: AbortSignal): Promise<Response> {

      const options: AjaxOptions = { url: "/api/chatbot/ask", };

      return wrapRequest(options, () => {

        const headers = {
          'Accept': 'text/plain',
          'Content-Type': 'text/plain',
          'X-Chatbot-Session-Id': sessionId,
          ...options.headers
        } as any;

        return fetch(toAbsoluteUrl(options.url), {
          method: "POST",
          credentials: "same-origin",
          headers: headers,
          cache: 'no-store',
          body: question,
          signal: signal
        } as RequestInit);
      });
    }


    export function getModels(provider: LanguageModelProviderSymbol): Promise<Array<string>> {
      return ajaxGet({ url: `/api/chatbot/provider/${provider.key}/models` });
    }

    export function getEmbeddingModels(provider: LanguageModelProviderSymbol): Promise<Array<string>> {
      return ajaxGet({ url: `/api/chatbot/provider/${provider.key}/embeddingModels` });
    }

    export function getMessagesBySessionId(sessionId: string | number | undefined): Promise<Array<ChatMessageEntity>> {
      return ajaxGet({ url: "/api/chatbot/messages/" + sessionId });
    }

    export function setFeedback(messageId: string | number, feedback: UserFeedback | null, message?: string): Promise<void> {
      return ajaxPost({ url: "/api/chatbot/feedback/" + messageId }, { feedback, message });
    }
  }

  interface AgentInfo {
    resources: string[];
  }
}

import * as React  from 'react'
import { RouteObject } from 'react-router'
import { ajaxGet, wrapRequest, AjaxOptions } from '@framework/Services';
import { Navigator, EntitySettings } from '@framework/Navigator'
import { ChatbotLanguageModelEntity, ChatSessionEntity, ChatMessageEntity, ChatbotProviderSymbol } from './Signum.Chatbot'
import { toAbsoluteUrl } from '../../Signum/React/AppContext';
import { registerToString } from '../../Signum/React/Signum.Entities';

const ReactMarkdown = React.lazy(() => import("react-markdown"));
//const ReactMarkdownWithFormulas = React.lazy(() => import("./ReactMarkdownWithFormulas"));

export namespace ChatbotClient {

  export let renderMarkdown = (markdown: string): React.JSX.Element => <ReactMarkdown>{markdown}</ReactMarkdown>;
  //export let renderMarkdown = (markdown: string): React.JSX.Element => <ReactMarkdownWithFormulas>{markdown}</ReactMarkdownWithFormulas>;
  export function start(options: { routes: RouteObject[] }): void {

    Navigator.addSettings(new EntitySettings(ChatbotLanguageModelEntity, e => import('./Templates/ChatbotLanguageModel')));
    Navigator.addSettings(new EntitySettings(ChatSessionEntity, a => import('./Templates/ChatSession')));
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


    export function getModels(provider: ChatbotProviderSymbol): Promise<Array<string>> {
      return ajaxGet({ url: `/api/chatbot/provider/${provider.key}/models` });
    }

    export function getMessagesBySessionId(sessionId: string | number | undefined): Promise<Array<ChatMessageEntity>> {
      return ajaxGet({ url: "/api/chatbot/messages/" + sessionId });
    }
  }

  interface AgentInfo {
    resources: string[];
  }
}

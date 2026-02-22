import * as React from 'react'
import { RouteObject } from 'react-router'
import { ajaxGet, ajaxPost, wrapRequest, AjaxOptions } from '@framework/Services';
import { Navigator, EntitySettings } from '@framework/Navigator'
import * as AppContext from '@framework/AppContext'
import { ChatbotLanguageModelEntity, ChatSessionEntity, ChatMessageEntity, LanguageModelProviderSymbol, EmbeddingsLanguageModelEntity, ToolCallEmbedded, UserFeedback } from './Signum.Agent'
import { toAbsoluteUrl } from '../../Signum/React/AppContext';
import { Dic } from '@framework/Globals';
import { Finder } from '@framework/Finder';
import { MarkdownOrJson } from './Message';

const ChatMarkdown = React.lazy(() => import("./Templates/ChatMarkdown"));

export namespace ChatbotClient {

  export let renderMarkdown = (markdown: string): React.JSX.Element => <ChatMarkdown content={markdown} />;
  //export let renderMarkdown = (markdown: string): React.JSX.Element => <ReactMarkdownWithFormulas>{markdown}</ReactMarkdownWithFormulas>;
  export function start(options: { routes: RouteObject[] }): void {

    Navigator.addSettings(new EntitySettings(ChatbotLanguageModelEntity, e => import('./Templates/ChatbotLanguageModel')));
    Navigator.addSettings(new EntitySettings(EmbeddingsLanguageModelEntity, e => import('./Templates/EmbeddingsLanguageModel')));
    Navigator.addSettings(new EntitySettings(ChatSessionEntity, a => import('./Templates/ChatSession')));
    Navigator.addSettings(new EntitySettings(ChatMessageEntity, a => import('./Templates/ChatMessage')));
    Finder.registerPropertyFormatter(ChatMessageEntity.tryPropertyRoute(a => a.content), new Finder.CellFormatter((cell, ctx, column) => cell && <MarkdownOrJson content={cell} />, true));
  
    AppContext.clearSettingsActions.push(() => uiToolRegistry.clear());
  }

  const uiToolRegistry = new Map<string, UITool>();

  export function registerUITool(tool: UITool, override = false): void {
    if (uiToolRegistry.has(tool.uiToolName) && !override)
      throw new Error(`UITool '${tool.uiToolName}' is already registered.`);
    uiToolRegistry.set(tool.uiToolName, tool);
  }

  export function getUITool(uiToolName: string): UITool | undefined {
    return uiToolRegistry.get(uiToolName);
  }

  export namespace API {

    export function ask(question: string, optiions: { sessionId?: string | number, callId?: string, toolId?: string }, signal?: AbortSignal): Promise<Response> {

      const options: AjaxOptions = { url: "/api/chatbot/ask", };

      return wrapRequest(options, () => {

        const headers = {
          'Accept': 'text/plain',
          'Content-Type': 'text/plain',
          'X-Chatbot-Session-Id': optiions.sessionId,
          'X-Chatbot-UIReply-CallId': optiions.callId,
          'X-Chatbot-UIReply-ToolId': optiions.toolId,
          ...options.headers
        } as any;

        return fetch(toAbsoluteUrl(options.url), {
          method: "POST",
          credentials: "same-origin",
          headers: Dic.simplify(headers),
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

}

// UI Tool registry
//
// Subclass UITool and implement either:
//  • handleDirectly() — resolves automatically without showing anything in the chat
//                       (e.g. GetUIContext just reads browser state and calls sendToolResponse)
//  • renderWidget()   — renders a React widget inline in the conversation;
//                       call sendToolResponse() from the widget when the user responds.
//
export abstract class UITool {
  abstract uiToolName: string;

  handleDirectly?(call: ToolCallEmbedded, sendToolResponse: (call: ToolCallEmbedded, response: unknown) => void): Promise<void>;

  renderWidget?(call: ToolCallEmbedded, sendToolResponse: (call: ToolCallEmbedded, response: unknown) => void): React.ReactElement;
}

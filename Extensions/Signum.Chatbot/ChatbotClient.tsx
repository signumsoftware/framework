import { RouteObject } from 'react-router'
import { ajaxPost, ajaxGet, ajaxPostRaw, wrapRequest, AjaxOptions } from '@framework/Services';
import { Navigator, EntitySettings } from '@framework/Navigator'
import { ChatbotLanguageModelEntity, ChatSessionEntity, ChatMessageEntity } from './Signum.Chatbot'
import { ChatbotAgentEntity } from './Signum.Chatbot.Agents';
import { toAbsoluteUrl } from '../../Signum/React/AppContext';
import { Lite, MList } from '../../Signum/React/Signum.Entities';


export namespace ChatbotClient {

  export function start(options: { routes: RouteObject[] }): void {

    Navigator.addSettings(new EntitySettings(ChatbotLanguageModelEntity, e => import('./Templates/ChatbotLanguageModel')));
    Navigator.addSettings(new EntitySettings(ChatbotAgentEntity, a => import('./Templates/ChatbotAgent')));
  }

  export module API {

    export function askQuestionAsync(question: string, sessionId?: string | number, signal?: AbortSignal): Promise<Response> {

      const options: AjaxOptions = { url: "/api/askQuestionAsync", };

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

    export function getChatSessionById(id: string | number | undefined): Promise<Lite<ChatSessionEntity>> {
      return ajaxGet<Lite<ChatSessionEntity>>({ url: "/api/session/" + id });
    }

    export function getUserSessions(): Promise<Array<ChatSessionEntity>> {
      return ajaxGet<Array<ChatSessionEntity>>({ url: "/api/userSessions"});
    }

    export function getMessagesBySessionId(id: string | number | undefined): Promise<Array<ChatMessageEntity>> {
      return ajaxGet<Array<ChatMessageEntity>>({ url: "/api/messages/session/" + id });
    }

  }
}

import { ErrorBoundary } from "@framework/Components";
import * as React from "react";
import { ChatbotMessage, ChatMessageEntity, ToolCallEmbedded } from "./Signum.Chatbot";
import { FontAwesomeIcon } from "@framework/Lines";
import { ChatbotClient } from "./ChatbotClient";
import { getToString } from "@framework/Signum.Entities";

export const Message: React.NamedExoticComponent<{ msg: ChatMessageEntity; toolResponses: number; }>
  = React.memo(function Message(p: { msg: ChatMessageEntity; toolResponses: number; }): React.ReactElement {

    const role =
      p.msg.role == "System" ? <SystemMessage msg={p.msg} /> :
        p.msg.role == "User" ? <UserMessage msg={p.msg} /> :
          p.msg.role == "Assistant" ? <AssistantMessage msg={p.msg} /> :
            p.msg.role == "Tool" ? <ToolMessage msg={p.msg} /> :
              null;

    return (
      <ErrorBoundary>
        {role}
      </ErrorBoundary>
    );
  }, (a, b) => a.msg.id != null && a.toolResponses != b.toolResponses);


function looksLikeJson(text: string) {
  return text && (text.startsWith("{") || text.startsWith("["));
}

export function SystemMessage(p: { msg: ChatMessageEntity }): React.ReactElement {
  const [isOpen, setIsOpen] = React.useState(false);

  return (
    <div className={`mb-2 justify-content-start`}>
      <a className="chat-internal" href="#" onClick={e => { e.preventDefault(); setIsOpen(!isOpen); }}>
        <FontAwesomeIcon icon={"book"} /> {ChatbotMessage.InitialInstruction.niceToString()}
      </a>
      {isOpen &&
        <div className={`chat-bubble system`}>
          <React.Suspense fallback={null}>
            {ChatbotClient.renderMarkdown(p.msg.content!)}
          </React.Suspense>
        </div>
      }
    </div>
  );
}


export function AssistantMessage(p: { msg: ChatMessageEntity }): React.ReactElement {

  return (
    <div className={`mb-2 justify-content-start`}>
      {
        p.msg.content && <React.Suspense fallback={null}>
          {ChatbotClient.renderMarkdown(p.msg.content)}
        </React.Suspense>
      }
      {p.msg.toolCalls.map(tc => <ToolCall toolCall={tc.element} />)}
    </div>
  );
}

function ToolCall(p: { toolCall: ToolCallEmbedded }): React.ReactElement {
  const [isOpen, setIsOpen] = React.useState(false);

  const response = p.toolCall._response;
  return (
    <div className={`mb-2 justify-content-start`}>
      <a className="chat-internal" href="#" onClick={e => { e.preventDefault(); setIsOpen(!isOpen); }}>
        <FontAwesomeIcon icon={"hammer"} /> {p.toolCall.toolId}
      </a>
      {isOpen &&
        <div>
          <h4 className="chatbot-request">Request</h4>
          <div className={`chat-bubble tool-request`}>
            <pre className="mb-0">
              {p.toolCall.arguments}
            </pre>
          </div>
          {response && <ToolResponseBlock msg={response} />}
        </div>
      }
    </div>
  );
}

export function ToolResponseBlock(p: { msg: ChatMessageEntity }): React.ReactElement {
  return (
    <div>
      <h4 className="chatbot-response">Response</h4>
      <div className={`chat-bubble tool-response`}>
        {p.msg.exception ?
          <pre className="text-danger">
            {getToString(p.msg.exception)}
          </pre>
          :
          looksLikeJson(p.msg.content!) ?
            <pre className="mb-0">
              {p.msg.content}
            </pre>
            :
            <React.Suspense fallback={null}>
              {ChatbotClient.renderMarkdown(p.msg.content!)}
            </React.Suspense>
        }
      </div>
    </div>
  );
}


export function ToolMessage(p: { msg: ChatMessageEntity }): React.ReactElement {
  const [isOpen, setIsOpen] = React.useState(false);

  return (
    <div className={`mb-2 justify-content-start`}>
      <a className="chat-internal" href="#" onClick={e => { e.preventDefault(); setIsOpen(!isOpen); }}>
        <FontAwesomeIcon icon={"hammer"} className="red" /> Response {p.msg.toolID}
      </a>
      {isOpen &&
        <ToolResponseBlock msg={p.msg} />
      }
    </div>
  );
}

export function UserMessage(p: { msg: ChatMessageEntity }): React.ReactElement {
  return (
    <div className={`mb-2 d-flex justify-content-end justify-content-start"`}>
      <div className={`chat-bubble ${p.msg.role == "User" ? "user" : "bot"}`}>
        <React.Suspense fallback={null}>
          {ChatbotClient.renderMarkdown(p.msg.content!)}
        </React.Suspense>
      </div>
    </div>
  );
}

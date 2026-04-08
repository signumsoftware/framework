import { ErrorBoundary } from "@framework/Components";
import * as React from "react";
import { ChatbotMessage, ChatMessageEntity, ToolCallEmbedded, UserFeedback } from "./Signum.Agent";
import { FontAwesomeIcon } from "@framework/Lines";
import { ChatbotClient } from "./ChatbotClient";
import { getToString } from "@framework/Signum.Entities";
import { classes } from "@framework/Globals";
import { useForceUpdate } from "@framework/Hooks";
import AutoLineModal from "@framework/AutoLineModal";
import { PropertyRoute } from "@framework/Reflection";

export const Message: React.NamedExoticComponent<{ msg: ChatMessageEntity; toolResponses: number; sendToolResponse: (call: ToolCallEmbedded, response: unknown) => void }>
  = React.memo(function Message(p: { msg: ChatMessageEntity; toolResponses: number; sendToolResponse: (call: ToolCallEmbedded, response: unknown) => void }): React.ReactElement {

    const role =
      p.msg.role == "System" ? <SystemMessage msg={p.msg} /> :
        p.msg.role == "User" ? <UserMessage msg={p.msg} /> :
          p.msg.role == "Assistant" ? <AssistantMessage msg={p.msg} sendToolResponse={p.sendToolResponse}/> :
            p.msg.role == "Tool" ? <ToolMessage msg={p.msg} /> :
              null;

    return (
      <ErrorBoundary>
        {role}
      </ErrorBoundary>
    );
  }, (a, b) => a.msg.id != null && a.toolResponses == b.toolResponses);


export function looksLikeJson(text: string): boolean {
  return text != null && (text.trim().startsWith("{") || text.trim().startsWith("["));
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


export function AssistantMessage(p: { msg: ChatMessageEntity, sendToolResponse: (call: ToolCallEmbedded, response: unknown) => void }): React.ReactElement {
  const forceUpdate = useForceUpdate();

  const isFinalized = p.msg.id != null;

  async function handleThumbsUp() {
    if (!isFinalized)
      return;
    if (p.msg.userFeedback === "Positive") {
      await ChatbotClient.API.setFeedback(p.msg.id!, null);
      p.msg.userFeedback = null;
      p.msg.userFeedbackMessage = null;
    } else {
      await ChatbotClient.API.setFeedback(p.msg.id!, "Positive");
      p.msg.userFeedback = "Positive";
      p.msg.userFeedbackMessage = null;
    }
    forceUpdate();
  }

  async function handleThumbsDown() {
    if (!isFinalized)
      return;
    if (p.msg.userFeedback === "Negative") {
      await ChatbotClient.API.setFeedback(p.msg.id!, null);
      p.msg.userFeedback = null;
      p.msg.userFeedbackMessage = null;
      forceUpdate();
    } else {
      await openFeedbackModal();
    }
  }

  async function handleEditFeedback() {
    await openFeedbackModal();
  }

  async function openFeedbackModal() {
    const newMessage = await AutoLineModal.show({
      propertyRoute: ChatMessageEntity.propertyRouteAssert(a => a.userFeedbackMessage),
      initialValue: p.msg.userFeedbackMessage ?? "",
      title: ChatbotMessage.ProvideFeedback.niceToString(),
      message: ChatbotMessage.WhatWentWrong.niceToString(),
      modalSize: "md",
    });

    if (newMessage === undefined)
      return;

    const newFeedback: UserFeedback = "Negative";
    await ChatbotClient.API.setFeedback(p.msg.id!, newFeedback, newMessage || undefined);
    p.msg.userFeedback = newFeedback;
    p.msg.userFeedbackMessage = newMessage || null;
    forceUpdate();
  }

  return (
    <div className={`mb-2 justify-content-start`}>
      {
        p.msg.content && <React.Suspense fallback={null}>
          {ChatbotClient.renderMarkdown(p.msg.content)}
        </React.Suspense>
      }
      {p.msg.toolCalls.map(tc => <ToolCall toolCall={tc.element} sendToolResponse={p.sendToolResponse} />)}
      {isFinalized && p.msg.toolCalls.length == 0 && (
        <div className="chat-feedback-buttons">
          <button
            className={classes("btn btn-link btn-sm chat-feedback-btn", p.msg.userFeedback === "Positive" && "chat-feedback-active-positive")}
            onClick={handleThumbsUp}
            title="Good response"
          >
            <FontAwesomeIcon icon="thumbs-up" />
          </button>
          <button
            className={classes("btn btn-link btn-sm chat-feedback-btn", p.msg.userFeedback === "Negative" && "chat-feedback-active-negative")}
            onClick={handleThumbsDown}
            title="Bad response"
          >
            <FontAwesomeIcon icon="thumbs-down" />
          </button>
          {p.msg.userFeedback === "Negative" && (
            <button
              className="btn btn-link btn-sm chat-feedback-btn chat-feedback-edit"
              onClick={handleEditFeedback}
              title={ChatbotMessage.ProvideFeedback.niceToString()}
            >
              <FontAwesomeIcon icon="pen" />
            </button>
          )}
        </div>
      )}
    </div>
  );
}

function ToolCall(p: { toolCall: ToolCallEmbedded, sendToolResponse: (call: ToolCallEmbedded, response: unknown) => void }): React.ReactElement {
  const [isOpen, setIsOpen] = React.useState(false);

  const isJson = looksLikeJson(p.toolCall.arguments!);
  const [formatJson, setFormatJson] = React.useState<boolean>(false);

  const response = p.toolCall._response;

  // Render-type UITools are shown inline in the conversation instead of the raw collapsible tool block
  if (p.toolCall.isUITool) {
    var tool = ChatbotClient.getUITool(p.toolCall.toolId);
    if (tool && tool.renderWidget) {
      return tool.renderWidget(p.toolCall, p.sendToolResponse);
    }
  }

  return (
    <div className={`mb-2 justify-content-start`}>
      <a className="chat-internal" href="#" onClick={e => { e.preventDefault(); setIsOpen(!isOpen); }}>
        <FontAwesomeIcon icon={"hammer"} /> {p.toolCall.toolId} {p.toolCall._response?.exception && <FontAwesomeIcon icon={"bug"} color="red" />}
      </a>
      {isOpen &&
        <div>
          <h4 className="chatbot-request">Request {isJson && !formatJson && <button className={classes("btn btn-sm btn-link", formatJson && "active")} onClick={() => setFormatJson(!formatJson)}>
            <FontAwesomeIcon icon="code" /> Format JSON
          </button>}</h4>
          <div className={`chat-bubble tool-request`}>
            <FormatJson code={p.toolCall.arguments} formatJson={formatJson} className="mb-0" />
          </div>
          {response && <ToolResponseBlock msg={response} />}
        </div>
      }
    </div>
  );
}

export function ToolResponseBlock(p: { msg: ChatMessageEntity }): React.ReactElement {
  const isJson = looksLikeJson(p.msg.content!);
  const [formatJson, setFormatJson] = React.useState<boolean>(false);
 
  return (
    <div>
      <h4 className="chatbot-response">Response {isJson && !formatJson && <button className={classes("btn btn-sm btn-link", formatJson && "active")} onClick={() => setFormatJson(!formatJson)}>
        <FontAwesomeIcon icon="code" /> Format JSON
      </button>}</h4>
      <div className={`chat-bubble tool-response`}>
        {p.msg.exception ?
          <pre className="text-danger">
            {getToString(p.msg.exception)}
          </pre>
          : <MarkdownOrJson content={p.msg.content!} formatJson={formatJson} />
          
        }
      </div>
    </div>
  );
}

export function MarkdownOrJson(p: { content: string | null | undefined, formatJson?: boolean }) {
  if (!p.content)
    return <span className="text-muted">{p.content + ""}</span>;
  
  if (looksLikeJson(p.content!)) 
    return <FormatJson code={p.content} formatJson={p.formatJson ?? true} className="mb-0" />;

  return (
    <React.Suspense fallback={null}>
      {ChatbotClient.renderMarkdown(tryParseJsonString(p.content!))}
    </React.Suspense>
  );
}

export function tryParseJsonString(str: string) {
  try {
    if (str.startsWith("\"") && str.endsWith("\"")) {
      return JSON.parse(str);
    }

    return str;
  } catch {
    return str;
  }
}

export function FormatJson({ code, formatJson, ...rest }: { code: string | undefined | null, formatJson: boolean } & React.HTMLAttributes<HTMLDivElement>): React.ReactElement {


  const formattedJson = React.useMemo(() => {
    if (formatJson == false || code == undefined)
      return null;

    try {
      var obj = JSON.parse(code);

      //Useful when json is double serialized
      var obj2 = Object.fromEntries(Object.entries(obj).map(([key, value]) => {
        if (typeof value === 'string' && looksLikeJson(value))
          return [key, JSON.parse(value)];
        return [key, value]
      }));


      return JSON.stringify(obj2, undefined, 2);
    } catch {
      return "Invalid Json"
    }
  }, [formatJson, code])

  return (
    <div {...rest} >

      <pre style={{ whiteSpace: "pre-wrap" }}>
        <code>{formatJson ? formattedJson : code}</code>
      </pre>
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

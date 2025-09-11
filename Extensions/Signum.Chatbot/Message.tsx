import { ErrorBoundary } from "@framework/Components";
import * as React from "react";
import { ChatbotMessage, ChatMessageEntity, ToolCallEmbedded } from "./Signum.Chatbot";
import { FontAwesomeIcon } from "@framework/Lines";
import { ChatbotClient } from "./ChatbotClient";
import { getToString } from "@framework/Signum.Entities";
import { classes } from "@framework/Globals";

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
  }, (a, b) => a.msg.id != null && a.toolResponses != b.toolResponses );


function looksLikeJson(text: string) {
  return text && (text.trim().startsWith("{") || text.trim().startsWith("["));
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

  const isJson = looksLikeJson(p.toolCall.arguments!);
  const [formatJson, setFormatJson] = React.useState<boolean>(false);


  const response = p.toolCall._response;
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
          :
          looksLikeJson(p.msg.content!) ?
            <FormatJson code={p.msg.content} formatJson={formatJson} className="mb-0" />
            :
            <React.Suspense fallback={null}>
              {ChatbotClient.renderMarkdown(p.msg.content!)}
            </React.Suspense>
        }
      </div>
    </div>
  );
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

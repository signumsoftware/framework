import * as React from "react";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faPaperPlane } from "@fortawesome/free-solid-svg-icons";
import "./ChatbotModal.css";
import { getToString, Lite, newMListElement } from "@framework/Signum.Entities";
import { ChatbotMessage, ChatbotUICommand, ChatMessageEntity, ChatMessageRole, ChatSessionEntity, ToolCallEmbedded } from "./Signum.Chatbot";
import { useForceUpdate, useVersion } from "@framework/Hooks";
import { Finder } from "@framework/Finder";
import { AuthClient } from "../Signum.Authorization/AuthClient";
import { newLite } from "Signum/React/Reflection";
import { ChatbotClient } from "./ChatbotClient";
import { Message, ToolResponseBlock } from "./Message";
import { ExceptionEntity } from "@framework/Signum.Basics";
import { ServiceError } from "@framework/Services";

interface MessageCount {
  msg: ChatMessageEntity;
  toolResponses: number;
}

export default function ChatModal(p: { onClose: () => void }): React.ReactElement {

  const currentSessionRef = React.useRef<Lite<ChatSessionEntity> | null>(null);
  const messagesRef = React.useRef<MessageCount[] | undefined>([]);

  const isLoadingRef = React.useRef<boolean>(false);

  const answerRef = React.useRef<ChatMessageEntity | null>(null);
  const generalExceptionRef = React.useRef<Lite<ExceptionEntity> | null>(null);
  const questionRef = React.useRef<string>("");
  const forceUpdate = useForceUpdate();
  const scrollRef = React.useRef<HTMLDivElement>(null);

  React.useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    }
  }, [answerRef.current?.content?.length, messagesRef.current?.length]);

  function handleCreatNewSession() {
    currentSessionRef.current = null;
    messagesRef.current = [];
    answerRef.current = null;
    forceUpdate();
  }

  async function handleOpenSession() {
    const session = await Finder.find<ChatSessionEntity>({
      queryName: ChatSessionEntity,
      filterOptions: [{ token: ChatSessionEntity.token(a => a.user), value: AuthClient.currentUser() }]
    });
    if (session == null)
      return;

    currentSessionRef.current = session;
    messagesRef.current = undefined;
    forceUpdate();

    const messages = await ChatbotClient.API.getMessagesBySessionId(currentSessionRef.current?.id);

    messagesRef.current = [];
    messages.forEach(a => addMessage(messagesRef.current!, a))

    forceUpdate();
  }

  async function handleCreateRequestAsync() {

    if (questionRef.current.trim().length == 0)
      return;

    isLoadingRef.current = true;
    forceUpdate();

    const r = await ChatbotClient.API.ask(questionRef.current, currentSessionRef?.current?.id, undefined).catch(error => { });

    const reader = r!.body!.getReader();

    try {

      for await (const chunk of getWordsOrCommands(reader!)) {


        console.log(chunk);

        if (!chunk)
          continue;

        if (chunk.startsWith("$!")) {
          const after = chunk.after("$!").trim();
          const commmand = (after.tryBefore(":") ?? after) as ChatbotUICommand;
          const args = after.tryAfter(":");

          switch (commmand) {
            case "SessionId": {
              const id = ChatSessionEntity.parseId(args!);
              currentSessionRef.current = newLite(ChatSessionEntity, id);
              break;
            }
            case "SessionTitle": {
              currentSessionRef.current!.model = args;
              break;
            }
            case "QuestionId": {

              messagesRef.current!.push({
                msg: ChatMessageEntity.New({
                  id: ChatMessageEntity.parseId(args!),
                  modified: false,
                  isNew: false,
                  role: "User",
                  chatSession: currentSessionRef!.current!,
                  content: questionRef.current,
                }),
                toolResponses: 0
              });
              break;
            }

            case "Tool": {
              setAnswer("Tool", args!.before("/"), args!.after("/"));
              break;
            }
            case "System": {
              setAnswer("System");
              break;
            }
            case "AssistantAnswer": {
              setAnswer("Assistant");
              break;
            }
            case "AssistantTool": {
              answerRef.current!.toolCalls.push(newMListElement(ToolCallEmbedded.New({
                toolId: args!.before("/"),
                callId: args!.after("/"),
                arguments: "",
              })));
              break;
            }
            case "Exception": {

              if (answerRef.current){
                answerRef.current!.exception = newLite(ExceptionEntity, args!, "");
              }
              else
                generalExceptionRef.current = newLite(ExceptionEntity, args!, "");

              break;
            }
            case "AnswerId": {

              answerRef.current!.id = ChatMessageEntity.parseId(args!);
              answerRef.current!.modified = false;
              answerRef.current!.isNew = false;

              addMessage(messagesRef.current!, answerRef.current!);

              answerRef.current = null;

              break;
            }
            default: throw new Error("Unexpected UI command: " + commmand)
          }
        }
        else {
          var ans = answerRef.current;
          if (ans) {
            if (ans.toolCalls.length)
              ans.toolCalls.single().element.arguments += chunk;
            else if (ans.exception)
              ans.exception.model += chunk;
            else
              ans.content += chunk;
          } else if (generalExceptionRef.current)
            generalExceptionRef.current.model += chunk;
        }

        forceUpdate();
      }

      const ge = generalExceptionRef.current; 
      if (ge) {
        throw new ServiceError({
          exceptionId: ge.id!.toString(),
          exceptionMessage: (ge.model as string)!.after(":"),
          exceptionType: (ge.model as string)!.before(":"),
          stackTrace: null,
          innerException: null,
        });
      }


    }
    finally {
      isLoadingRef.current = false;
      forceUpdate();
    }
  }

  return (
    <div className="chat-modal">
      {/* Header */}
      <div className="d-flex justify-content-between p-2 border-bottom">
        <div className="d-flex gap-2">
          <button className="btn btn-outline-secondary btn-sm" onClick={handleOpenSession}>{ChatbotMessage.OpenSession.niceToString()}</button>
          {messagesRef.current && messagesRef.current.length > 0 && <button className="btn btn-outline-primary btn-sm" onClick={handleCreatNewSession}>{ChatbotMessage.NewSession.niceToString()}</button>}
        </div>
        <button type="button" className="btn-close" aria-label="Close" onClick={p.onClose} />
      </div>
      <h4 className="px-3 pt-2">
        <React.Suspense fallback={null}>
          {currentSessionRef.current && ChatbotClient.renderMarkdown(getToString(currentSessionRef.current))}
        </React.Suspense>
      </h4>
      {/* Chat History */}
      <div className="chat-history flex-grow-1 p-3 pt-0">
        {messagesRef.current?.map(a => <Message key={a.msg.id} msg={a.msg} toolResponses={a.toolResponses} />)}
        {answerRef.current && <Message msg={answerRef.current} toolResponses={0} />}
      </div>

      {/* Input */}
      <div className="p-2 border-top d-flex align-items-center">
        <textarea
          className="form-control me-2"
          rows={2}
          placeholder={ChatbotMessage.TypeAMessage.niceToString()}
          value={questionRef.current}
          disabled={isLoadingRef.current || messagesRef.current == undefined}
          onChange={(e) => { questionRef.current = e.target.value; forceUpdate() }}
          onKeyDown={(e) => {
            if (e.key === "Enter" && !e.shiftKey) {
              e.preventDefault();
              handleCreateRequestAsync();
            }
          }}
        />
        <button className="btn btn-primary" onClick={handleCreateRequestAsync} title={ChatbotMessage.Send.niceToString()} disabled={isLoadingRef.current || messagesRef.current == undefined}>
          <FontAwesomeIcon icon={faPaperPlane} />
        </button>
      </div>
    </div>
  );
  function setAnswer(role: ChatMessageRole, toolId?: string, callId?: string) {
    answerRef.current = ChatMessageEntity.New({
      toolID: toolId,
      toolCallID: callId,
      role: role,
      chatSession: currentSessionRef!.current!,
      content: "",
    });
  }
}

function addMessage(list: MessageCount[], msg: ChatMessageEntity) {
  const pair = msg.toolCallID ? list.lastOrNull(a => a.msg.role == "Assistant" && a.msg.toolCalls.some(tc => tc.element.callId == msg.toolCallID)) : null;

  const tools = pair?.msg.toolCalls.singleOrNull(a => a.element.callId == msg.toolCallID);

  if (tools == null)
    list.push({ msg, toolResponses: 0 });
  else {
    tools.element._response = msg;
    pair!.toolResponses++;
  }
}
async function* getWordsOrCommands(reader: ReadableStreamDefaultReader<Uint8Array>): AsyncGenerator<string> {
  const decoder = new TextDecoder();
  let buffer = "";

  while (true) {
    const { value, done } = await reader.read();
    if (done) {
      if (buffer.length > 0) {
        yield buffer;
      }
      break;
    }

    buffer += decoder.decode(value, { stream: true });

    while (true) {
      const newlineIndex = buffer.indexOf("\n");

      if (newlineIndex === -1) {
        // No complete line yet
        if (!buffer.startsWith("$!")) {
          // If not a command, yield whatever we have
          yield buffer;
          buffer = "";
        }
        break;
      }

      const line = buffer.slice(0, newlineIndex + 1); // include newline
      buffer = buffer.slice(newlineIndex + 1); // rest of the buffer

      if (line.startsWith("$!")) {
        yield line;
      } else {
        // Yield each non-command line chunk (including newline)
        yield line;
      }
    }
  }
}


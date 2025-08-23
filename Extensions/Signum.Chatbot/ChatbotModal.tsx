import * as React from "react";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faPaperPlane } from "@fortawesome/free-solid-svg-icons";
import "./ChatbotModal.css";
import { getToString, Lite } from "@framework/Signum.Entities";
import { ChatbotClient } from './ChatbotClient';
import { ChatbotMessage, ChatMessageEntity, ChatMessageRole, ChatSessionEntity } from "./Signum.Chatbot";
import { useAPI, useAPIWithReload, useForceUpdate } from "@framework/Hooks";
import { Navigator } from "@framework/Navigator";
import { Finder } from "@framework/Finder";
import { AuthClient } from "../Signum.Authorization/AuthClient";
import { newLite } from "Signum/React/Reflection";
import ReactMarkdown from "react-markdown";

export default function ChatModal(p: { onClose: () => void }): React.ReactElement {

  const currentSessionRef = React.useRef<Lite<ChatSessionEntity> | null>(null);
  const messagesRef = React.useRef<ChatMessageEntity[] | undefined>([]);

  const isLoadingRef = React.useRef<boolean>(false);

  const currentAnswerRef = React.useRef<ChatMessageEntity | null>(null);

  const questionRef = React.useRef<string>("");

  const forceUpdate = useForceUpdate();

  const scrollRef = React.useRef<HTMLDivElement>(null);

  React.useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    }
  }, [currentAnswerRef.current?.message.length, messagesRef.current?.length]);

  function handleCreatNewSession() {
    currentSessionRef.current = null;
    messagesRef.current = [];
    currentAnswerRef.current = null;
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

    messagesRef.current = await ChatbotClient.API.getMessagesBySessionId(currentSessionRef.current?.id);
    forceUpdate();
  }

  async function handleCreateRequestAsync() {


    isLoadingRef.current = true;
    forceUpdate();

    const r = await ChatbotClient.API.ask(questionRef.current, currentSessionRef?.current?.id, undefined).catch(error => { });

    const reader = r!.body!.getReader();

    try {

      for await (const chunk of getWordsOrCommands(reader!)) {

        console.log(JSON.stringify(chunk));

        if (!chunk)
          continue;

        if (chunk.startsWith("$!")) {
          const after = chunk.after("$!").trim();
          const commmand = after.tryBefore(":") ?? after;
          const argument = after.tryAfter(":");

          switch (commmand) {
            case "SessionId": {
              const id: string | number = ChatSessionEntity.memberInfo(a => a.id).type.name == "number" ? parseInt(argument!) : argument!;
              currentSessionRef.current = newLite(ChatSessionEntity, id);
              break;
            }
            case "SessionTitle": {
              currentSessionRef.current!.model = argument;
              break;
            }
            case "QuestionId": {
              const id: string | number = ChatMessageEntity.memberInfo(a => a.id).type.name == "number" ? parseInt(argument!) : argument!;

              messagesRef.current!.push(ChatMessageEntity.New({
                id: id,
                modified: false,
                isNew: false,
                isCommand: false,
                role: "User",
                chatSession: currentSessionRef!.current!,
                message: questionRef.current,
              }));
              break;
            }
            case "AssistantCommand": {
              setAnswer("Assistant", true);
              break;
            }
            case "Tool": {
              setAnswer("Tool");
              break;
            }
            case "System": {
              setAnswer("System");
              break;
            }
            case "AssistantFinalAnswer": {
              setAnswer("Assistant");
              break;
            }
            case "AnswerId": {
              const id: string | number = ChatMessageEntity.memberInfo(a => a.id).type.name == "number" ? parseInt(argument!) : argument!;

              currentAnswerRef.current!.id = id;
              currentAnswerRef.current!.modified = false;
              currentAnswerRef.current!.isNew = false;
              messagesRef.current!.push(currentAnswerRef.current!);

              currentAnswerRef.current = null;
              break;
            }
            default: throw new Error("Unexpected UI command: " + commmand)
          }
        }
        else {
          currentAnswerRef.current!.message += chunk;
        }

        forceUpdate();
      }


    }
    finally {
      isLoadingRef.current = false;
      forceUpdate();
    }
  }

  return (
    <div className="chat-modal" >
      {/* Header */}


      <div className="d-flex justify-content-between p-2 border-bottom">
        <div className="d-flex gap-2">
          <button className="btn btn-outline-primary btn-sm" onClick={handleCreatNewSession}>{ChatbotMessage.NewSession.niceToString()}</button>
          <button className="btn btn-outline-secondary btn-sm" onClick={handleOpenSession}>{ChatbotMessage.OpenSession.niceToString()}</button>
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
        {messagesRef.current?.map(msg => msg.role == "User" || msg.role == "Assistant" ?
          <Message key={msg.id} msg={msg} /> :
          <InternalMessage key={msg.id} msg={msg} />
        )}

        {currentAnswerRef.current && <CurrentMessage msg={currentAnswerRef.current} />}
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
        />
        <button className="btn btn-primary" onClick={handleCreateRequestAsync} title={ChatbotMessage.Send.niceToString()} disabled={isLoadingRef.current || messagesRef.current == undefined}>
          <FontAwesomeIcon icon={faPaperPlane} />
        </button>
      </div>
    </div>
  );
  function setAnswer(role: ChatMessageRole, isCommand: boolean = false) {
    currentAnswerRef.current = ChatMessageEntity.New({
      isCommand: isCommand,
      role: role,
      chatSession: currentSessionRef!.current!,
      message: "",
    });
  }
}

const InternalMessage = React.memo(function Message(p: { msg: ChatMessageEntity }): React.ReactElement {
  const [isOpen, setIsOpen] = React.useState(false);

  return (
    <div className={`mb-2 justify-content-start`}>
      <a className="chat-internal" href="#" onClick={e => { e.preventDefault(); setIsOpen(!isOpen); }}>
        <FontAwesomeIcon icon={p.msg.role == "Tool" ? "hammer" : "book"} /> {p.msg.role == "Tool" ? ChatbotMessage.UsingInternalTool.niceToString() : ChatbotMessage.ReceivingInstructions.niceToString()}…
      </a>
      {isOpen &&
        <div className={`chat-bubble bot`}>
          <React.Suspense fallback={null}>
            {ChatbotClient.renderMarkdown(p.msg.message)}
          </React.Suspense>
        </div>
      }
    </div>
  );
}, (a, b) => a.msg.id == b.msg.id);

const Message = React.memo(function Message(p: { msg: ChatMessageEntity }): React.ReactElement {
  return (
    <div className={`mb-2 d-flex ${p.msg.role == "User" ? "justify-content-end" : "justify-content-start"}`}>
      <div className={`chat-bubble ${p.msg.role == "User" ? "user" : "bot"}`}>
        <React.Suspense fallback={null}>
          {ChatbotClient.renderMarkdown(p.msg.message)}
        </React.Suspense>
      </div>
    </div>
  );
}, (a, b) => a.msg.id == b.msg.id);

function CurrentMessage(p: { msg: ChatMessageEntity }): React.ReactElement {
  return (
    <div className={`mb-2 d-flex ${p.msg.role == "User" ? "justify-content-end" : "justify-content-start"}`}>
      <div className={`chat-bubble ${p.msg.role == "User" ? "user" : "bot"}`}>
        <React.Suspense fallback={null}>
          {ChatbotClient.renderMarkdown(p.msg.message)}
        </React.Suspense>
      </div>
    </div>
  );
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


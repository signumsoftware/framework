import { ChatbotLanguageModelEntity, ChatMessageEntity, ChatSessionEntity } from './Signum.Chatbot';
import React, { useEffect, useState } from 'react';
import TextArea from '../../Signum/React/Components/TextArea';
import { useAPI, useAPIWithReload, useForceUpdate } from '../../Signum/React/Hooks';
import { ChatbotClient } from './ChatbotClient';
import { Lite, toLite } from '../../Signum/React/Signum.Entities';
import { ErrorBoundary } from '../../Signum/React/Components';
import { DateTime } from 'luxon'
import { ReadonlyBinding } from '@framework/Lines';
import HtmlEditor from '../Signum.HtmlEditor/HtmlEditor';
import ReactMarkdown from "react-markdown";
import remarkMathRaw from 'remark-math';
import rehypeKatexRaw from 'rehype-katex';
import 'katex/dist/katex.min.css';

const remarkMath = (remarkMathRaw as any).default ?? remarkMathRaw;
const rehypeKatex = (rehypeKatexRaw as any).default ?? rehypeKatexRaw;

export function Chatbot() : React.JSX.Element {

  const [currentSession, setCurrentSession] = useState<Lite<ChatSessionEntity> | null>();

  const [answer, setAnswer] = useState<string>("");

  const [messages, reloadMessages] = useAPIWithReload(signal => currentSession?.id ?
    ChatbotClient.API.getMessagesBySessionId(currentSession.id) : ChatbotClient.API.getMessagesBySessionId(undefined), [currentSession?.id], { avoidReset: true });

  const newChatMessageRef = React.useRef<ChatMessageEntity>(ChatMessageEntity.New());

  const forceUpdate = useForceUpdate();


  function creatNewSession() {
    setCurrentSession(null);
  }


  function reloadHistoryAndNotifyWidget() {
    reloadMessages();
    document.dispatchEvent(new Event("refresh-notify-config"));
  }


  function  handleCreateRequestAsync() {

    const newChatMessage = newChatMessageRef.current;

    newChatMessage.role = "User";

    const decoder = new TextDecoder();
    let buffer = "";
    let visibleText = "";

    ChatbotClient.API.askQuestionAsync(newChatMessage.message, currentSession?.id, undefined).then(async r => {
      const reader = r.body?.getReader();

      while (true) {
        const { value, done } = await reader!.read();
        if (done) break;

        const chunk = decoder.decode(value, { stream: true });

        visibleText += chunk;

        //visibleText = visibleText.replace(/,\r?\n\s*"/g, '');
        //visibleText = visibleText.replaceAll('\"', "");
        //visibleText = visibleText.replace(/\\n/g, "\n");
        //visibleText = visibleText.replace(/(?<!^)\s*(### )/g, '\n\n$1');
        setAnswer(visibleText);
        
      }
    });
  }

  return (

    <div>

      <div style={{ marginTop: "7px" }}>
        <a className="btn btn-success list-create-card-button" href="#" onClick={e => { e.preventDefault(); creatNewSession(); }}>Neue Chat Session</a>
      </div>

      <div style={{ marginTop: "12px" }}>
        <CurrentSession messages={messages} reload={reloadHistoryAndNotifyWidget} />
      </div>

      <div style={{ marginTop: "24px" }}>

       

        <ReactMarkdown remarkPlugins={[remarkMath as any]}
          rehypePlugins={[rehypeKatex as any]}>
          {answer!}
        </ReactMarkdown>

        <TextArea value={newChatMessageRef.current.message} className="form-control form-control-sm" onChange={e => {
          newChatMessageRef.current.message = e.target.value,
          newChatMessageRef.current.modified = true;
        }
        } />
      </div>

      <div style={{ marginTop: "7px" }}>
        <a className="btn btn-success list-create-card-button" href="#" onClick={e => { e.preventDefault(); handleCreateRequestAsync(); }}>Frage</a>
      </div>
    </div>
  );
}


export function HtmlViewer(p: { text: string; htmlAttributes?: React.HTMLAttributes<HTMLDivElement> }): React.JSX.Element {

  var binding = new ReadonlyBinding(p.text, "");

  return (
    <div className="html-viewer" >
      <ErrorBoundary>
        <HtmlEditor readOnly
          binding={binding}
          htmlAttributes={p.htmlAttributes}
          toolbarButtons={c => null} plugins={[
          ]} />
      </ErrorBoundary>
    </div>
  );
}

export function CurrentSession(p: {
  messages: Array<ChatMessageEntity> | undefined,
  reload: () => void,
}): React.JSX.Element {



  return (
    <div>
      {p.messages != null ? p.messages.orderByDescending(c => c.dateTime).map((c, i) => {

        var message = c.message.replace(/(?<!^)\s*(### )/g, '\n\n$1');

        {
          return (
            <div>
              <div className="history-item-title d-flex align-items-center">
                <span className="mx-1" title={DateTime.fromISO(c.dateTime).toFormat("DDD tt")}>{DateTime.fromISO(c.dateTime).toRelative()}</span>
              </div>
              <div>
                <ReactMarkdown remarkPlugins={[remarkMath as any]}
                  rehypePlugins={[rehypeKatex as any]}>
                  {message!}
                </ReactMarkdown>
              </div>
            </div>)

        }
      }) : null
      }
    </div>
  );
}


export async function* getWordsOrCommands(
  reader: ReadableStreamDefaultReader<Uint8Array>
): AsyncGenerator<string> {
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
        if (!buffer.startsWith("<&")) {
          // If not a command, yield whatever we have
          yield buffer;
          buffer = "";
        }
        break;
      }

      const line = buffer.slice(0, newlineIndex + 1); // include newline
      buffer = buffer.slice(newlineIndex + 1); // rest of the buffer

      if (line.startsWith("<&")) {
        yield line;
      } else {
        // Yield each non-command line chunk (including newline)
        yield line;
      }
    }
  }
}

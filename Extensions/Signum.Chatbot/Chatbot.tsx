import { ChatbotLanguageModelEntity, ChatMessageEntity, ChatSessionEntity } from './Signum.Chatbot';
import React, { useEffect, useState } from 'react';
import TextArea from '../../Signum/React/Components/TextArea';
import { useAPI, useAPIWithReload, useForceUpdate } from '../../Signum/React/Hooks';
import { ChatbotClient } from './ChatbotClient';
import { Lite, toLite } from '../../Signum/React/Signum.Entities';
import { ErrorBoundary } from '../../Signum/React/Components';
import { DateTime } from 'luxon'
import { ReadonlyBinding, TypeContext } from '@framework/Lines';
import HtmlEditor from '../Signum.HtmlEditor/HtmlEditor';
import ReactMarkdown from "react-markdown";
import remarkMath from 'remark-math';
import rehypeKatex from 'rehype-katex';
import 'katex/dist/katex.min.css';
import { Navigator } from '@framework/Navigator'
import "./ChatBot.css"

const remarkPlugins = [remarkMath as any];
const rehypePlugins = [rehypeKatex as any];

export function Chatbot() : React.JSX.Element {

  const [currentSession, setCurrentSession] = useState<Lite<ChatSessionEntity> | null>();

  const [answer, setAnswer] = useState<string>("");

  const [currentSessionTitle, setCurrentSessionTitle] = useState<string>("");

  const [userSessions, reloadUserSessions] = useAPIWithReload(signal => ChatbotClient.API.getUserSessions(), [currentSession?.id]);

  const [messages, reloadMessages] = useAPIWithReload(signal => currentSession?.id ?
    ChatbotClient.API.getMessagesBySessionId(currentSession.id) : ChatbotClient.API.getMessagesBySessionId(undefined), [currentSession?.id], { avoidReset: true });

  const newQuestionRef = React.useRef<string>();
  const scrollRef = React.useRef<HTMLDivElement>(null);


  useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    }
  }, [answer, messages]);
  const forceUpdate = useForceUpdate();


  function creatNewSession() {
    setCurrentSession(null);
    setCurrentSessionTitle("");
  }


  function reloadHistoryAndNotifyWidget() {
    reloadMessages();
    document.dispatchEvent(new Event("refresh-notify-config"));
  }


  function  handleCreateRequestAsync() {
    const newQuestion = newQuestionRef.current;
    
    let visibleText = "";

    ChatbotClient.API.askQuestionAsync(newQuestion!, currentSession?.id, undefined).then(async r => {
      const reader = r.body?.getReader();

      for await (const chunk of getWordsOrCommands(reader!)) {
        visibleText += chunk;
        setAnswer(visibleText);
      }
      reloadUserSessions();
      reloadHistoryAndNotifyWidget();
      newQuestionRef.current = undefined;
      setAnswer("");
      
    });
  }


  currentSession != undefined ? Navigator.API.fetch(currentSession).then(a => a.title!= null ? setCurrentSessionTitle(a.title): null) : null;

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
          if (!buffer.startsWith("§$%")) {
            yield buffer;
            buffer = "";
          }
          break;
        }

        const line = buffer.slice(0, newlineIndex + 1); 
        buffer = buffer.slice(newlineIndex + 1); 

        if (line.startsWith("§$%")) {
          if (line.contains("SessionId") && currentSession == undefined) {
            var id = line.after("Id:").before("\n");

            ChatbotClient.API.getChatSessionById(id).then(s => setCurrentSession(s))
          }
          if (line.contains("QuestionId")) {
            var id = line.after("Id:").before("\n");
            reloadMessages();
          }
        } else {
          // Yield each non-command line chunk (including newline)
          yield line;
        }
      }
    }
  }

  function handleSessionClick(session: ChatSessionEntity, index: number) {
    setActiveIndex(index);
    setCurrentSession(toLite(session));
    reloadMessages();
  }

  const [activeIndex, setActiveIndex] = useState<number | null>(null);

  return (

    <div>
      <div className="row">
        <div className="col-sm-3">
          <div style={{ marginTop: "7px" }}>
            <a className="btn btn-success list-create-card-button" href="#" onClick={e => { e.preventDefault(); creatNewSession(); }}>Neue Chat Session</a>
          </div>

          <div className={"scrollContainer"} style={{ height: "300px", marginTop: "12px" }}>
            {userSessions?.map((us, index) =>
              <div key={index} className={`session ${activeIndex === index ? 'active' : ''}`} onClick={e => handleSessionClick(us, index)}>
                
                    <ReactMarkdown remarkPlugins={[remarkMath as any]} 
                      rehypePlugins={[rehypeKatex as any]}>
                      {us.title != null ? us.title.replace(/(?<!^)\s*(### )/g, '\n\n$1') : null}
                    </ReactMarkdown>
                  
              </div>)}
          </div>
        </div>


        <div className="col-sm-9">
          <div className={"scrollContainer"} style={{ maxHeight: "600px", marginTop: "12px" }} ref={scrollRef}>
            <div style={{ marginTop: "12px" }}>
              <ReactMarkdown remarkPlugins={remarkPlugins}
                rehypePlugins={rehypePlugins}>
                {currentSessionTitle.replace(/(?<!^)\s*(### )/g, '\n\n$1')}
              </ReactMarkdown>
            </div>

            <div style={{ marginTop: "12px" }}>
              <CurrentSession messages={messages} reload={reloadHistoryAndNotifyWidget} />
            </div>

            <div style={{ marginTop: "12px" }}>
              <ReactMarkdown remarkPlugins={remarkPlugins}
                rehypePlugins={rehypePlugins} children={answer!} />
            </div>

          </div>

          <div style={{ marginTop: "24px" }}>
            
            <TextArea value={newQuestionRef.current} className="form-control form-control-sm" onChange={e => {
              newQuestionRef.current = e.target.value;
            }} />

          </div>

          <div style={{ marginTop: "7px" }}>
            <a className="btn btn-success list-create-card-button" href="#" onClick={e => { e.preventDefault(); handleCreateRequestAsync(); }}>Frage</a>
          </div>
        </div>
      </div>
    </div>
  );
}


export function convertInlineFormulas(text: string): string {
  // Regel 1: Ersetze [math] durch $math$
  text = text.replace(/\[([^\[\]]+?)\]/g, (_, eq) => `$${eq.trim()}$`);

  return text;
}

export function CurrentSession(p: {
  messages: Array<ChatMessageEntity> | undefined,
  reload: () => void,
}): React.JSX.Element {


  return (
    <div>
      {p.messages != null ? p.messages.orderBy(c => c.dateTime).map((c, i) => {

        var message = c.message.replace(/(?<!^)\s*(### )/g, '\n\n$1');
       
        {
          return (
            <div style={{marginTop: "12px" }}>
              {c.role == "User"  ? <div className="history-item-title d-flex align-items-center">
                <span className="mx-1" title={DateTime.fromISO(c.dateTime).toFormat("DDD tt")}>{DateTime.fromISO(c.dateTime).toRelative()}</span>
              </div> : null}
              <div  className={c.role == "User" ? "userMessage" : ""}>
                <ReactMarkdown remarkPlugins={remarkPlugins}
                  rehypePlugins={rehypePlugins} children={message!} />
              
              </div>
            </div>)
        }
      }) : null
      }
    </div>
  );
}



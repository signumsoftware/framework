import * as React from 'react'
import { AutoLine, TextAreaLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ChatbotAgentEntity, ChatbotAgentDescriptionsEmbedded } from '../Signum.Chatbot.Agents';
import { Tabs, Tab, CloseButton } from 'react-bootstrap';
import { newMListElement } from '../../../Signum/React/Signum.Entities';
import { useForceUpdate } from '../../../Signum/React/Hooks';
import { useState } from 'react';


export default function ChatbotAgent(p: { ctx: TypeContext<ChatbotAgentEntity> }): React.JSX.Element {
  const ctx = p.ctx;
  const ctx4 = ctx.subCtx({ labelColumns: 2 });

  const [activeKey, setActiveKey] = useState<string>(ctx.value.descriptions[0].element.promptName!);
  const [countPrompt, setCountPrompt] = useState<number>(1);

  const forceUpdate = useForceUpdate();

  function handleTabSelect(key: string | null) { 

    if (key === "plus") {
      var count = countPrompt;
      while (ctx.value.descriptions.find(d => d.element.promptName == "New Prompt" + count) != undefined) {
        count = count + 1;
      }

      ctx.value.descriptions.push(newMListElement(ChatbotAgentDescriptionsEmbedded.New({
        content: "New Content",
        promptName: "New Prompt" + count,
        isNew: true,
      })));

      setActiveKey("New Prompt" + count);
      setCountPrompt(count + 1);
    }
    else {
      setActiveKey(key || ctx.value.descriptions[0].element.promptName!);
    }
  }


  function closeTab(keyToRemove: string) {

    var element = ctx.value.descriptions.find(d => d.element.promptName == keyToRemove);

    if (element) {
      ctx.value.descriptions.remove(element!);

      ctx.value.modified = true;
    }

    setActiveKey(ctx.value.descriptions[0].element.promptName!);
    forceUpdate();
  }


  return (
    <div>
      <AutoLine ctx={ctx4.subCtx(n => n.shortDescription)} />

      <Tabs id="descriptions" onSelect={handleTabSelect} activeKey={activeKey}>
        {
          ctx.mlistItemCtxs(a => a.descriptions).map((etcx, i) => {
        
            return (
              <Tab eventKey={etcx.value.promptName ? etcx.value.promptName.toString() : "-"} title={
                <div className="d-flex align-items-center gap-2">
                  <span>{etcx.value.promptName ? etcx.value.promptName.toString() : "-"}</span>
                  {i > 0 ? <CloseButton onClick={(e) => {
                    e.stopPropagation();
                    closeTab(etcx.value.promptName ? etcx.value.promptName.toString() : "-");
                  }} /> : null}
                </div>
                } 
                >
                <div>
                  <AutoLine ctx={etcx.subCtx(n => n.promptName)} />
                    <TextAreaLine ctx={etcx.subCtx(n => n.content)} />
                </div>
              </Tab>
            );
          })
        }
        <Tab title={"+"} eventKey="plus">
        </Tab>
      </Tabs>
    </div>
  );
}

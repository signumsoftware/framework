import * as React from 'react'
import { AutoLine, EntityTabRepeater, TextAreaLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { Tabs, Tab, CloseButton } from 'react-bootstrap';
import { JavascriptMessage, newMListElement } from '@framework/Signum.Entities';
import { useAPI, useForceUpdate } from '@framework/Hooks';
import { useState } from 'react';
import { ChatbotAgentEntity } from '../Signum.Chatbot';
import { ChatbotClient } from '../ChatbotClient';


export default function ChatbotAgent(p: { ctx: TypeContext<ChatbotAgentEntity> }): React.JSX.Element {
  const ctx = p.ctx;
  const ctx4 = ctx.subCtx({ formGroupStyle: "Basic" });
  const info = useAPI(() => ChatbotClient.API.getAgentInfo(ctx.value.code), [ctx.value.code]);
  const forceUpdate = useForceUpdate();

  return (
    <div>

      <div className="row">
        <div className="col-sm-6">
          <AutoLine ctx={ctx4.subCtx(n => n.code)} />
          <AutoLine ctx={ctx4.subCtx(n => n.shortDescription)} />
        </div>
        <div className="col-sm-6">
          <div className="alert alert-info">
            {info == null ? JavascriptMessage.loading.niceToString() :
              info.resources.length == 0 ? "Kein Resources" :
                <ul>
                  {info.resources.map((a, i) => <li key={i}><code>${a}()</code></li>)}
                </ul>
            }
          </div>
        </div>
      </div>

    
      <EntityTabRepeater ctx={ctx.subCtx(a => a.descriptions)} avoidFieldSet="h3" getComponent={dctx => <div>
        <AutoLine ctx={dctx.subCtx(n => n.promptName)} onChange={forceUpdate} />
        <TextAreaLine ctx={dctx.subCtx(n => n.content)} />
      </div>}/>
    </div>
  );
}

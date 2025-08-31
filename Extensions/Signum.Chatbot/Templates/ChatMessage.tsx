import * as React from 'react'
import { AutoLine, EntityLine, EntityTable, EntityTabRepeater, TextAreaLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { Tabs, Tab, CloseButton } from 'react-bootstrap';
import { newMListElement } from 'Signum/React/Signum.Entities';
import { useForceUpdate } from 'Signum/React/Hooks';
import { useState } from 'react';
import { ChatMessageEntity, ChatSessionEntity } from '../Signum.Chatbot';
import { SearchControl } from 'Signum/React/Search';
import HtmlEditorLine from '../../Signum.HtmlEditor/HtmlEditorLine';


export default function ChatMessage(p: { ctx: TypeContext<ChatMessageEntity> }): React.JSX.Element {
  const ctx = p.ctx;
  const ctx4 = ctx.subCtx({ formGroupStyle: "Basic", readOnly: true });

  return (
    <div>

      <div className="row">
        <div className="col-sm-4">
          <EntityLine ctx={ctx4.subCtx(n => n.chatSession)} />
        </div>
        <div className="col-sm-4">
          <AutoLine ctx={ctx4.subCtx(n => n.creationDate)} />
        </div>

        <div className="col-sm-4">
          <AutoLine ctx={ctx4.subCtx(n => n.role)} />
        </div>
      </div>

      {ctx.value.role == "Tool" ? <>

        <div className="row">
          <div className="col-sm-3">
            <AutoLine ctx={ctx4.subCtx(n => n.toolCallID)} />
          </div>
          <div className="col-sm-3">
            <AutoLine ctx={ctx4.subCtx(n => n.toolID)} />
          </div>
          <div className="col-sm-6">
            <EntityLine ctx={ctx4.subCtx(n => n.exception)} />
          </div>
        </div>
        <pre >{ctx4.value.content}</pre>
      </> : <>
        <HtmlEditorLine ctx={ctx4.subCtx(n => n.content)} />
        {ctx.value.role == "Assistant" &&
          <EntityTable ctx={ctx4.subCtx(n => n.toolCalls)} columns={[
            { property: a => a.callId },
            { property: a => a.toolId },
            { property: a => a.arguments, template: ctx => <pre >{ctx.value.arguments}</pre> },
          ]} />
        }
      </>
      }
    </div>
  );
}

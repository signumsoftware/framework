import * as React from 'react'
import { AutoLine, EntityLine, EntityTable, EntityTabRepeater, NumberLine, TextAreaLine, FormGroup } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { Tabs, Tab, CloseButton } from 'react-bootstrap';
import { newMListElement } from 'Signum/React/Signum.Entities';
import { useForceUpdate } from 'Signum/React/Hooks';
import { useState } from 'react';
import { ChatMessageEntity, ChatSessionEntity } from '../Signum.Agent';
import { SearchControl } from 'Signum/React/Search';
import HtmlEditorLine from '../../Signum.HtmlEditor/HtmlEditorLine';
import Markdown from 'react-markdown';


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

      <div className="row">
        <div className="col-sm-4">
          <NumberLine ctx={ctx4.subCtx(n => n.inputTokens)} />
        </div>
        <div className="col-sm-4">
          <NumberLine ctx={ctx4.subCtx(n => n.outputTokens)} />
        </div>

        <div className="col-sm-4">
          <AutoLine ctx={ctx4.subCtx(n => n.duration)} />
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
        <FormGroup ctx={ctx4.subCtx(n => n.content)}>
          {id => <Markdown components={{ a: LinkRenderer }}>{ctx4.value.content}</Markdown>}
        </FormGroup>
        {ctx.value.role == "Assistant" && ctx4.value.toolCalls.length > 0 &&
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


function LinkRenderer(props: React.AnchorHTMLAttributes<HTMLAnchorElement>) {
  return (
    <a href={props.href} target="_blank" rel="noreferrer">
      {props.children}
    </a>
  );
}

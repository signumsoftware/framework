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
import {
  FormatJson,
  looksLikeJson, 
  MarkdownOrJson, 
  tryParseJsonString } from '../Message';
import { ChatbotClient } from '../ChatbotClient';


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
          <AutoLine ctx={ctx4.subCtx(n => n.languageModel)} />
        </div>
        <div className="col-sm-4">
          <AutoLine ctx={ctx4.subCtx(n => n.duration)} />
        </div>
      </div>

      <div className="row">
        <div className="col-sm-3">
          <NumberLine ctx={ctx4.subCtx(n => n.inputTokens)} />
        </div>
        <div className="col-sm-3">
          <NumberLine ctx={ctx4.subCtx(n => n.cachedInputTokens)} />
        </div>
        <div className="col-sm-3">
          <NumberLine ctx={ctx4.subCtx(n => n.outputTokens)} />
        </div>
        <div className="col-sm-3">
          <NumberLine ctx={ctx4.subCtx(n => n.reasoningOutputTokens)} />
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
        <MarkdownOrJson content={ctx4.value.content}/>
      </> : <>
        <FormGroup ctx={ctx4.subCtx(n => n.content)}>
          {id => ctx4.value.content && ChatbotClient.renderMarkdown(ctx4.value.content)}
        </FormGroup>
        {ctx.value.role == "Assistant" && ctx4.value.toolCalls.length > 0 &&
          <EntityTable ctx={ctx4.subCtx(n => n.toolCalls)} columns={[
            { property: a => a.callId },
            { property: a => a.toolId },
            {
              property: a => a.arguments, template: ctx => <MarkdownOrJson content={ctx.value.arguments}/>
            },
          ]} />
        }
        {ctx.value.role == "Assistant" && (ctx.value.userFeedback != null || ctx.value.userFeedbackMessage != null) && (
          <div className="row mt-2">
            <div className="col-sm-3">
              <AutoLine ctx={ctx4.subCtx(n => n.userFeedback)} />
            </div>
            {ctx.value.userFeedback === "Negative" && (
              <div className="col-sm-9">
                <AutoLine ctx={ctx4.subCtx(n => n.userFeedbackMessage)} />
              </div>
            )}
          </div>
        )}
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

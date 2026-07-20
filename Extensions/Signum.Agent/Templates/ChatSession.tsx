import * as React from 'react'
import { AutoLine, EntityTabRepeater, TextAreaLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { Tabs, Tab, CloseButton } from 'react-bootstrap';
import { newMListElement } from '@framework/Signum.Entities';
import { useForceUpdate } from '@framework/Hooks';
import { useState } from 'react';
import Markdown from 'react-markdown';
import {
  ChatbotMessage, ChatMessageEntity, 
  ChatMessageRole, ChatSessionEntity 
} from '../Signum.Agent';
import { SearchControl } from '@framework/Search';


export default function ChatSession(p: { ctx: TypeContext<ChatSessionEntity> }): React.JSX.Element {
  const ctx = p.ctx;
  const ctx4 = ctx.subCtx({ formGroupStyle: "Basic" });

  const forceUpdate = useForceUpdate();

  return (
    <div>
      <AutoLine ctx={ctx4.subCtx(n => n.title)} />

      <div className="row">
        <div className="col-sm-4">
          <AutoLine ctx={ctx4.subCtx(n => n.languageModel)} />
        </div>
        <div className="col-sm-4">
          <AutoLine ctx={ctx4.subCtx(n => n.user)} />
        </div>
        <div className="col-sm-4">
          <AutoLine ctx={ctx4.subCtx(n => n.startDate)} />
        </div>
      </div>

      <Tabs>
        <Tab title={ChatMessageEntity.nicePluralName()} eventKey="messages">
          <SearchControl findOptions={ChatMessageEntity.findOptions(token => ({
            filterOptions: [token(a => a.chatSession).filter("EqualTo", ctx.value, { frozen: true }), 
            token(a => a.role).filter("DistinctTo", "System", { pinned: { active: "NotCheckbox_Unchecked", column: 1, label: ChatbotMessage.ShowSystem.niceToString() } })],
            columnOptionsMode: "ReplaceAll",
            columnOptions: [
              token(a => a.id),
              token(a => a.role),
              token(a => a.toolID),
              token(a => a.entity.toolCalls).count().column({ displayName: "# Tools" }),
              token(a => a.content),
              token(a => a.entity.exception),
            ],
            orderOptions: [token(a => a.id).order("Ascending")]
          }))} />
        </Tab>

        <Tab title={ChatbotMessage.Price.niceToString()} eventKey="stats">
          <SearchControl findOptions={ChatMessageEntity.findOptions(token => ({
            filterOptions: [token(a => a.chatSession).filter("EqualTo", ctx.value, { frozen: true })],
            columnOptionsMode: "ReplaceAll",
            columnOptions: [
              token(a => a.id),
              token(a => a.role),
              token(a => a.toolID),
              token(a => a.entity.toolCalls).count().column({ displayName: "# Tools", summaryToken: token(a => a.entity.toolCalls).count().sum() }),
              token(a => a.entity.inputTokens).column({ summaryToken: token(a => a.entity.inputTokens).sum() }),
              token(a => a.entity.cachedInputTokens).column({ summaryToken: token(a => a.entity.cachedInputTokens).sum() }),
              token(a => a.entity.outputTokens).column({ summaryToken: token(a => a.entity.outputTokens).sum() }),
              token(a => a.entity.reasoningOutputTokens).column({ summaryToken: token(a => a.entity.reasoningOutputTokens).sum() }),
              token(a => a.entity).expression<number>("Price").column({ summaryToken: token(a => a.entity).expression<number>("Price").sum() }),
            ],
            orderOptions: [token(a => a.id).order("Ascending")]
          }))} />
        </Tab>


      </Tabs>

      

    </div>
  );
}

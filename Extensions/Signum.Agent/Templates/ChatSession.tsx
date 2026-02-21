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
          <SearchControl findOptions={{
            queryName: ChatMessageEntity,
            filterOptions: [{
              token: ChatMessageEntity.token(a => a.chatSession),
              value: ctx.value,
              frozen: true,
            }, 
            {
              token: ChatMessageEntity.token(a => a.role),
              operation: "DistinctTo",
              value: ChatMessageRole.value("System"),
              pinned: { active: "NotCheckbox_Unchecked", column: 1, label: ChatbotMessage.ShowSystem.niceToString() }
            }],
            columnOptionsMode: "ReplaceAll",
            columnOptions: [
              { token: ChatMessageEntity.token(a => a.id) },
              { token: ChatMessageEntity.token(a => a.role) },
              { token: ChatMessageEntity.token(a => a.toolID) },
              { token: ChatMessageEntity.token(a => a.entity.toolCalls).count(), displayName: "# Tools" },
              { token: ChatMessageEntity.token(a => a.content) },
              { token: ChatMessageEntity.token(a => a.entity.exception) },
            ],
            orderOptions: [{
              token: ChatMessageEntity.token(a => a.id),
              orderType: "Ascending"
            }]
          }} />
        </Tab>

        <Tab title={ChatbotMessage.Price.niceToString()} eventKey="stats">
          <SearchControl findOptions={{
            queryName: ChatMessageEntity,
            filterOptions: [{
              token: ChatMessageEntity.token(a => a.chatSession),
              value: ctx.value,
              frozen: true,
            }],
            columnOptionsMode: "ReplaceAll",
            columnOptions: [
              { token: ChatMessageEntity.token(a => a.id) },
              { token: ChatMessageEntity.token(a => a.role) },
              { token: ChatMessageEntity.token(a => a.toolID) },
              { token: ChatMessageEntity.token(a => a.entity.toolCalls).count(), displayName: "# Tools", summaryToken: ChatMessageEntity.token(a => a.entity.toolCalls).count().sum() },
              { token: ChatMessageEntity.token(a => a.entity.inputTokens), summaryToken: ChatMessageEntity.token(a => a.entity.inputTokens).sum() },
              { token: ChatMessageEntity.token(a => a.entity.cachedInputTokens), summaryToken: ChatMessageEntity.token(a => a.entity.cachedInputTokens).sum() },
              { token: ChatMessageEntity.token(a => a.entity.outputTokens), summaryToken: ChatMessageEntity.token(a => a.entity.outputTokens).sum() },
              { token: ChatMessageEntity.token(a => a.entity.reasoningOutputTokens), summaryToken: ChatMessageEntity.token(a => a.entity.reasoningOutputTokens).sum() },
              { token: ChatMessageEntity.token(a => a.entity).expression<number>("Price"), summaryToken: ChatMessageEntity.token(a => a.entity).expression<number>("Price").sum() },
            ],
            orderOptions: [{
              token: ChatMessageEntity.token(a => a.id),
              orderType: "Ascending"
            }]
          }} />
        </Tab>


      </Tabs>

      

    </div>
  );
}

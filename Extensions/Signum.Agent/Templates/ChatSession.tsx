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
        orderOptions: [{
          token: ChatMessageEntity.token(a => a.id),
          orderType: "Ascending"
        }]
      }} />

    </div>
  );
}

import * as React from 'react'
import { AutoLine, EntityTabRepeater, TextAreaLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ChatbotAgentEntity, ChatbotAgentDescriptionsEmbedded } from '../Signum.Chatbot.Agents';
import { Tabs, Tab, CloseButton } from 'react-bootstrap';
import { newMListElement } from '../../../Signum/React/Signum.Entities';
import { useForceUpdate } from '../../../Signum/React/Hooks';
import { useState } from 'react';
import { ChatMessageEntity, ChatSessionEntity } from '../Signum.Chatbot';
import { SearchControl } from '../../../Signum/React/Search';


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
          value: ctx.value
        }]
      }} />

    </div>
  );
}

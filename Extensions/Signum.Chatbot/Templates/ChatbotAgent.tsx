import * as React from 'react'
import { AutoLine, EntityTabRepeater, TextAreaLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ChatbotAgentEntity, ChatbotAgentDescriptionsEmbedded } from '../Signum.Chatbot.Agents';
import { Tabs, Tab, CloseButton } from 'react-bootstrap';
import { newMListElement } from '../../../Signum/React/Signum.Entities';
import { useForceUpdate } from '../../../Signum/React/Hooks';
import { useState } from 'react';


export default function ChatbotAgent(p: { ctx: TypeContext<ChatbotAgentEntity> }): React.JSX.Element {
  const ctx = p.ctx;
  const ctx4 = ctx.subCtx({ labelColumns: 2 });

  const forceUpdate = useForceUpdate();

  return (
    <div>
      <AutoLine ctx={ctx4.subCtx(n => n.shortDescription)} />
      <EntityTabRepeater ctx={ctx.subCtx(a => a.descriptions)} avoidFieldSet="h3" getComponent={dctx => <div>
        <AutoLine ctx={dctx.subCtx(n => n.promptName)} onChange={forceUpdate} />
        <TextAreaLine ctx={dctx.subCtx(n => n.content)} />
      </div>}/>
    </div>
  );
}

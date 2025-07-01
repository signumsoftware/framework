import * as React from 'react'
import { AutoLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ChatbotLanguageModelEntity } from '../Signum.Chatbot';

export default function ChatbotConfiguration(p: { ctx: TypeContext<ChatbotLanguageModelEntity> }): React.JSX.Element {
  const ctx = p.ctx;
  const ctx4 = ctx.subCtx({ labelColumns: 2 });
  return (
    <div>
      <AutoLine ctx={ctx4.subCtx(n => n.provider)} />
      <AutoLine ctx={ctx4.subCtx(n => n.model)} />
      <AutoLine ctx={ctx4.subCtx(n => n.version)} />
    </div>
  );
}

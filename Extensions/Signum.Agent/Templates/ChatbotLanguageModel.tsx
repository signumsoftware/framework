import * as React from 'react'
import { AutoLine, EntityCombo, EnumLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ChatbotLanguageModelEntity } from '../Signum.Agent';
import { useAPI, useForceUpdate } from '@framework/Hooks';
import { ChatbotClient } from '../ChatbotClient';

export default function ChatbotConfiguration(p: { ctx: TypeContext<ChatbotLanguageModelEntity> }): React.JSX.Element {
  const ctx = p.ctx;
  const ctx4 = ctx.subCtx({ labelColumns: 4 });
  const ctx6 = ctx.subCtx({ labelColumns: 5 });
  const forceUpdate = useForceUpdate();
  const provider = ctx.value.provider;

  const models = useAPI(() => provider && ChatbotClient.API.getModels(provider), [provider]);

  return (
    <div>
      
      <div className="row">
        <div className="col-sm-6">
          <AutoLine ctx={ctx4.subCtx(n => n.isDefault)} />
          <EntityCombo ctx={ctx4.subCtx(n => n.provider)} onChange={() => {
            ctx.value.model = null!;
            ctx.value.pricePerInputToken = null!;
            ctx.value.pricePerCachedInputToken = null!;
            ctx.value.pricePerOutputToken = null!;
            ctx.value.pricePerReasoningOutputToken = null!;
            forceUpdate();
          }} />
          <EnumLine ctx={ctx4.subCtx(n => n.model)} readOnly={models == null} optionItems={models ?? []} />
      
          <AutoLine ctx={ctx4.subCtx(n => n.temperature)} />
          <AutoLine ctx={ctx4.subCtx(n => n.maxTokens)} />
        </div>
        <div className="col-sm-6">
          <fieldset className="mt-0">
            <legend className="fs-6 fw-semibold">Pricing</legend>
            <AutoLine ctx={ctx6.subCtx(n => n.pricePerInputToken)} />
            <AutoLine ctx={ctx6.subCtx(n => n.pricePerCachedInputToken)} />
            <AutoLine ctx={ctx6.subCtx(n => n.pricePerOutputToken)} />
            <AutoLine ctx={ctx6.subCtx(n => n.pricePerReasoningOutputToken)} />
          </fieldset>
        </div>
      </div>
    </div>
  );
}

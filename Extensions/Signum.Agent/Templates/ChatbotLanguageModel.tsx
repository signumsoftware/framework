import * as React from 'react'
import { AutoLine, EntityCombo, EnumLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ChatbotLanguageModelEntity } from '../Signum.Agent';
import { useAPI, useForceUpdate } from '@framework/Hooks';
import { ChatbotClient } from '../ChatbotClient';

export default function ChatbotConfiguration(p: { ctx: TypeContext<ChatbotLanguageModelEntity> }): React.JSX.Element {
  const ctx = p.ctx;
  const ctx4 = ctx.subCtx({ labelColumns: 2 });
  const forceUpdate = useForceUpdate();
  const provider = ctx.value.provider;

  const models = useAPI(() => provider && ChatbotClient.API.getModels(provider), [provider]);

  return (
    <div>
      <AutoLine ctx={ctx4.subCtx(n => n.isDefault)} />
      <EntityCombo ctx={ctx4.subCtx(n => n.provider)} onChange={() => {
        ctx.value.model = null!;
        forceUpdate();
      }} />
      <EnumLine ctx={ctx4.subCtx(n => n.model)} readOnly={models == null} optionItems={models ?? []} />
      <AutoLine ctx={ctx4.subCtx(n => n.temperature)} />
      <AutoLine ctx={ctx4.subCtx(n => n.maxTokens)} />
      <fieldset className="mt-3">
        <legend className="fs-6 fw-semibold">Pricing ($ / 1M tokens)</legend>
        <div className="row">
          <div className="col-sm-3">
            <AutoLine ctx={ctx4.subCtx(n => n.pricePerInputToken)} />
          </div>
          <div className="col-sm-3">
            <AutoLine ctx={ctx4.subCtx(n => n.pricePerOutputToken)} />
          </div>
          <div className="col-sm-3">
            <AutoLine ctx={ctx4.subCtx(n => n.pricePerCachedInputToken)} />
          </div>
          <div className="col-sm-3">
            <AutoLine ctx={ctx4.subCtx(n => n.pricePerReasoningOutputToken)} />
          </div>
        </div>
      </fieldset>
    </div>
  );
}

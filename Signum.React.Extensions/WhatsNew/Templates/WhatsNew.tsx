import * as React from 'react'
import { WhatsNewEntity, WhatsNewMessage, WhatsNewMessageEmbedded } from '../Signum.Entities.WhatsNew';
import { useAPI, useForceUpdate } from '../../../../Framework/Signum.React/Scripts/Hooks';
import { Binding, EntityCombo, EntityTabRepeater, TypeContext, ValueLine } from '../../../Signum.React/Scripts/Lines';
import { FileLine } from '../../Files/FileLine';
import WhatsNewHtmlEditor from './WhatsNewHtmlEditor';
import "./WhatsNew.css";

export default function WhatsNew(p: { ctx: TypeContext<WhatsNewEntity> }) {
  const ctx = p.ctx;
  const forceUpdate = useForceUpdate();

  return (
    <div>
      <ValueLine ctx={ctx.subCtx(w => w.status)} readOnly />
      <ValueLine ctx={ctx.subCtx(w => w.name)} />
      <FileLine ctx={ctx.subCtx(w => w.previewPicture)} mandatory />

      <EntityTabRepeater ctx={ctx.subCtx(w => w.messages)} onChange={() => forceUpdate()} getComponent={(ctx: TypeContext<WhatsNewMessageEmbedded>) =>
        <WhatsNewMessageComponent ctx={ctx} invalidate={() => forceUpdate} />} />
    </div>
  );
}

export interface WhatsNewMessageComponentProps
{
  ctx: TypeContext<WhatsNewMessageEmbedded>;
  invalidate: () => void;
}

export function WhatsNewMessageComponent(p: WhatsNewMessageComponentProps) {

  const ec = p.ctx.subCtx({labelColumns: 4});
  return (
    <div>
      <EntityCombo ctx={ec.subCtx(e => e.culture)} label={WhatsNewMessage.Language.niceToString()} onChange={p.invalidate} />
      <ValueLine ctx={ec.subCtx(e => e.title)} label={ec.subCtx(e => e.title).niceName()} onChange={p.invalidate} />
      <div>
        <p>{ec.subCtx(e => e.description).niceName()}</p>
        <WhatsNewHtmlEditor binding={Binding.create(ec.value, w => w.description)} />
      </div>
    </div>
  );
}

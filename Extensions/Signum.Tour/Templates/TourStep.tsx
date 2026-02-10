import * as React from 'react'
import { TourStepEmbedded } from '../Signum.Tour'
import { AutoLine, TypeContext } from '@framework/Lines'
import HtmlCodeMirror from "@extensions/Signum.CodeMirror/HtmlCodeMirror"

export default function TourStep(p: { ctx: TypeContext<TourStepEmbedded>, invalidate: () => void; }) {
  const ctx = p.ctx;
  const sc = ctx.subCtx({ labelColumns: { sm: 2 } });
 
  return (
    <div>
      <AutoLine ctx={sc.subCtx(a => a.title)} onChange={p.invalidate}  />
      <AutoLine ctx={sc.subCtx(a => a.element)} />
      <AutoLine ctx={sc.subCtx(a => a.side)} />
      <AutoLine ctx={sc.subCtx(a => a.align)} />
      <div className="code-container">
        <HtmlCodeMirror ctx={sc.subCtx(a => a.description)} />
      </div>
    </div>
  );
}

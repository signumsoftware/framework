import * as React from 'react'
import { TourStepEmbedded } from '../Signum.Tour'
import { AutoLine, TypeContext } from '@framework/Lines'
import HtmlCodemirror from "@extensions/Signum.CodeMirror/HtmlCodemirror";

export default function TourStep(p: { ctx: TypeContext<TourStepEmbedded> }) {
  const ctx = p.ctx;
  const sc = ctx.subCtx({ labelColumns: { sm: 2 } });
  
  // Get the parent forceUpdate from the frame
  const handleChange = () => {
    ctx.frame?.frameComponent?.forceUpdate();
  };
  
  return (
    <div>
      <AutoLine ctx={sc.subCtx(a => a.element)} onChange={handleChange} />
      <AutoLine ctx={sc.subCtx(a => a.title)} onChange={handleChange} />
      <div className="row mb-3">
        <div className="col-sm-6">
          <AutoLine ctx={sc.subCtx(a => a.side)} onChange={handleChange} />
        </div>
        <div className="col-sm-6">
          <AutoLine ctx={sc.subCtx(a => a.align)} onChange={handleChange} />
        </div>
      </div>
      <div className="code-container">
        <HtmlCodemirror ctx={sc.subCtx(a => a.description)} onChange={handleChange} />
      </div>
    </div>
  );
}

import * as React from 'react'
import { JoyrideStepEntity } from '../Signum.Entities.Joyride'
import { ValueLine, EntityLine, TypeContext } from '@framework/Lines'
import HtmlCodemirror from "../../Codemirror/HtmlCodemirror";

export default function JoyrideStep(p : { ctx: TypeContext<JoyrideStepEntity> }){
  const ctx = p.ctx;
  return (
    <div>
      <ValueLine ctx={ctx.subCtx(a => a.title)} />
      <EntityLine ctx={ctx.subCtx(a => a.culture)} />
      <ValueLine ctx={ctx.subCtx(a => a.selector)} />
      <ValueLine ctx={ctx.subCtx(a => a.position)} />
      <ValueLine ctx={ctx.subCtx(a => a.type)} />
      <ValueLine ctx={ctx.subCtx(a => a.allowClicksThruHole)} />
      <ValueLine ctx={ctx.subCtx(a => a.isFixed)} />
      <EntityLine ctx={ctx.subCtx(a => a.style)} />
      <HtmlCodemirror ctx={ctx.subCtx(a => a.text)} />
    </div>
  );
}

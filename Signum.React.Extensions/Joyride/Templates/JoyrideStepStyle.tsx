import * as React from 'react'
import { JoyrideStepStyleEntity } from '../Signum.Entities.Joyride'
import { ValueLine, TypeContext } from '@framework/Lines'
import { ColorTypeaheadLine } from '../../Basics/Templates/ColorTypeahead';

export default function JoyrideStepStyle(p : { ctx: TypeContext<JoyrideStepStyleEntity> }){
  const ctx = p.ctx;
  return (
    <div>
      <ValueLine ctx={ctx.subCtx(a => a.name)} />
      <ColorTypeaheadLine ctx={ctx.subCtx(a => a.color)} />
      <ColorTypeaheadLine ctx={ctx.subCtx(a => a.mainColor)} />
      <ColorTypeaheadLine ctx={ctx.subCtx(a => a.backgroundColor)} />
      <ValueLine ctx={ctx.subCtx(a => a.borderRadius)} />
      <ValueLine ctx={ctx.subCtx(a => a.textAlign)} />
      <ValueLine ctx={ctx.subCtx(a => a.width)} />
    </div>
  );
}

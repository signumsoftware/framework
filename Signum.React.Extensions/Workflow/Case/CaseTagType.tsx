import * as React from 'react'
import { ColorTypeaheadLine } from '../../Basics/Templates/ColorTypeahead'
import { CaseTagTypeEntity } from '../Signum.Entities.Workflow'
import { ValueLine, TypeContext } from '@framework/Lines'
import Tag from './Tag'
import { useForceUpdate } from '@framework/Hooks'

export default function CaseTagTypeComponent(p : { ctx: TypeContext<CaseTagTypeEntity> }){
  const forceUpdate = useForceUpdate();
  var ctx = p.ctx;
  return (
    <div className="row">
      <div className="col-sm-10">
        <ValueLine ctx={ctx.subCtx(e => e.name)} onChange={() => forceUpdate()} />
        <ColorTypeaheadLine ctx={ctx.subCtx(e => e.color)} onChange={() => forceUpdate()} />
      </div>
      <div className="col-sm-2">
        <Tag tag={p.ctx.value} />
      </div>
    </div>
  );
}

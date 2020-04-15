import * as React from 'react'
import * as Navigator from '@framework/Navigator'
import { DynamicTypeEntity, DynamicIsolationMixin } from './Signum.Entities.Dynamic'
import { ValueLine } from '@framework/Lines'

export function start(options: { routes: JSX.Element[] }) {

  Navigator.getSettings(DynamicTypeEntity)!.overrideView(vr => {
    vr.insertAfterLine(a => a.baseType, ctx => [<ValueLine ctx={ctx.subCtx(DynamicIsolationMixin).subCtx(m => m.isolationStrategy)} labelColumns={3} />])
  });  
}

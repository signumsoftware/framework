import * as React from 'react'
import { RouteObject } from 'react-router'
import * as Navigator from '@framework/Navigator'
import { DynamicTypeEntity, DynamicIsolationMixin } from './Signum.Dynamic'
import { ValueLine } from '@framework/Lines'

export function start(options: { routes: RouteObject[] }) {

  Navigator.getSettings(DynamicTypeEntity)!.overrideView(vr => {
    vr.insertAfterLine(a => a.baseType, ctx => [<ValueLine ctx={ctx.subCtx(DynamicIsolationMixin).subCtx(m => m.isolationStrategy)} labelColumns={3} />])
  });  
}

import * as React from 'react'
import { RouteObject } from 'react-router'
import * as Navigator from '@framework/Navigator'
import { ValueLine } from '@framework/Lines'
import { DynamicTypeEntity } from './Signum.Dynamic.Types';
import { DynamicIsolationMixin } from './Signum.Dynamic.Isolation';

export function start(options: { routes: RouteObject[] }) {

  Navigator.getSettings(DynamicTypeEntity)!.overrideView(vr => {
    vr.insertAfterLine(a => a.baseType, ctx => [<ValueLine ctx={ctx.subCtx(DynamicIsolationMixin).subCtx(m => m.isolationStrategy)} labelColumns={3} />])
  });  
}

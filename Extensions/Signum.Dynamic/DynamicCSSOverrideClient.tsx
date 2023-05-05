import * as React from 'react'
import { RouteObject } from 'react-router'
import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import * as EvalClient from '../Signum.Eval/EvalClient'
import { DynamicCSSOverrideEntity } from './Signum.Dynamic.CSS'

export function start(options: { routes: RouteObject[] }) {
  Navigator.addSettings(new EntitySettings(DynamicCSSOverrideEntity, w => import('./CSS/DynamicCSSOverride')));
  EvalClient.Options.registerDynamicPanelSearch(DynamicCSSOverrideEntity, t => [
    { token: t.append(p => p.name), type: "Text" },
    { token: t.append(p => p.script), type: "Code" },
  ]);
}


import * as React from 'react'
import { Route } from 'react-router'
import { EntitySettings, ViewPromise } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import { DynamicCSSOverrideEntity } from './Signum.Entities.Dynamic'


export function start(options: { routes: JSX.Element[] }) {

    Navigator.addSettings(new EntitySettings(DynamicCSSOverrideEntity, w => import('./CSS/DynamicCSSOverride')));
}
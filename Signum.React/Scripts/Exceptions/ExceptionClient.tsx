
import * as React from 'react'
import { ExceptionEntity } from '../Signum.Entities.Basics'
import { EntitySettings, ViewPromise } from '../Navigator'
import * as Navigator from '../Navigator'

export function start(options: { routes: JSX.Element[] }) {
    Navigator.addSettings(new EntitySettings(ExceptionEntity, e => import('./Exception')));
}

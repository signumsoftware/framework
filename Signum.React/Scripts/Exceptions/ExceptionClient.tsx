/// <reference path="../../typings/react/react.d.ts" />

import * as React from 'react'
import { ExceptionEntity_Type } from '../Signum.Entities.Basics'
import { addSettings, EntitySettings } from '../Navigator'

export function start(options: { routes: JSX.Element[] }) {
    addSettings(new EntitySettings(ExceptionEntity_Type, e => new Promise(resolve => require(['./Exception'], resolve))));
}

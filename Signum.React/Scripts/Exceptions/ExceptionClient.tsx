/// <reference path="../../typings/react/react.d.ts" />

import * as React from 'react'
import { getMixin, Basics } from '../Signum.Entities'
import { addSettings, EntitySettings } from '../Navigator'

export function start(options: { routes: JSX.Element[] }) {
    addSettings(new EntitySettings(Basics.ExceptionEntity_Type, e => new Promise(resolve => require(['./Exception'], resolve))));
}

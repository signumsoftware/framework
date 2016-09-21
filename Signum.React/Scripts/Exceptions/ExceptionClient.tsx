/// <reference path="../../typings/react/react.d.ts" />

import * as React from 'react'
import { ExceptionEntity } from '../Signum.Entities.Basics'
import { EntitySettings, ViewPromise } from '../Navigator'
import * as Navigator from '../Navigator'

export function start(options: { routes: JSX.Element[] }) {
    Navigator.addSettings(new EntitySettings(ExceptionEntity, e => new ViewPromise(resolve => require(['./Exception'], resolve))));
}

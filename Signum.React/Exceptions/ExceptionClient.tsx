/// <reference path="../typings/react/react.d.ts" />

import * as React from 'react'
import { getMixin, Basics } from 'Framework/Signum.React/Scripts/Signum.Entities'
import { addSettings, EntitySettings } from 'Framework/Signum.React/Scripts/Navigator'

export function start(options: { routes: JSX.Element[] }) {
    addSettings(new EntitySettings(Basics.ExceptionEntity_Type, e=> 'Framework/Signum.React/Exceptions/Exception'));
}

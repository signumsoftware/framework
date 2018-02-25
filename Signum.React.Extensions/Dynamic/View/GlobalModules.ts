import * as React from 'react'
import * as numbro from 'numbro'
import * as moment from 'moment'
import * as Entities from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Operations from '../../../../Framework/Signum.React/Scripts/Operations'
import * as Globals from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import * as Reflection from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as Components from '../../../../Framework/Signum.React/Scripts/Components'
import * as AuthClient from '../../Authorization/AuthClient'


export const globalModules: any = {
    numbro,
    moment,
    React,
    Components,
    Globals,
    Navigator,
    Finder,
    Reflection,
    Entities,
    AuthClient,
    Operations,
};
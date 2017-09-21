import * as React from 'react'
import * as Reactstrap from 'reactstrap'
import * as numbro from 'numbro'
import * as moment from 'moment'
import * as Entities from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Operations from '../../../../Framework/Signum.React/Scripts/Operations'
import * as Globals from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import * as Reflection from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as AuthClient from '../../Authorization/AuthClient'


export const globalModules: any = {
    numbro,
    moment,
    React,
    Reactstrap,
    Globals,
    Navigator,
    Finder,
    Reflection,
    Entities,
    AuthClient,
    Operations,
};
import * as React from 'react'
import { RouteObject } from 'react-router'
import * as Navigator from '@framework/Navigator'
import * as Constructor from '@framework/Constructor'
import * as Finder from '@framework/Finder'
import { EntitySettings } from '@framework/Navigator'
import { getMixin } from '@framework/Signum.Entities'
import { EntityLine, AutoLine } from '@framework/Lines'
import { Tab } from 'react-bootstrap';
import { Pop3EmailReceptionServiceEntity } from './Signum.Mailing.Pop3'

export function start(options: {  routes: RouteObject[] }) {

  Navigator.addSettings(new EntitySettings(Pop3EmailReceptionServiceEntity, e => import('./Pop3EmailReceptionService')));

}

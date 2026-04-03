import * as React from 'react'
import { RouteObject } from 'react-router'
import { Navigator, EntitySettings } from '@framework/Navigator'
import { Constructor } from '@framework/Constructor'
import { Finder } from '@framework/Finder'
import { getMixin } from '@framework/Signum.Entities'
import { EntityLine, AutoLine } from '@framework/Lines'
import { Tab } from 'react-bootstrap';
import { Pop3EmailReceptionServiceEntity } from './Signum.Mailing.Pop3'

export namespace MailingPop3Client {
  
  export function start(options: {  routes: RouteObject[] }): void {
  
    Navigator.addSettings(new EntitySettings(Pop3EmailReceptionServiceEntity, e => import('./Pop3EmailReceptionService')));
  
  }
}

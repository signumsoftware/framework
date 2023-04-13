import * as React from 'react'
import { RouteObject } from 'react-router'
import * as Navigator from '@framework/Navigator'
import { EntitySettings } from '@framework/Navigator';
import { MicrosoftGraphEmailServiceEntity } from './Signum.Mailing.MicrosoftGraph';


export function start(options: {
  routes: RouteObject[],
}) {

  Navigator.addSettings(new EntitySettings(MicrosoftGraphEmailServiceEntity, e => import('./Templates/MicrosoftGraphEmailService')));
}

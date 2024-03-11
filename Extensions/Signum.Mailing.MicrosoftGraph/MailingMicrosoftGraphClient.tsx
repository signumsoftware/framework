import * as React from 'react'
import { RouteObject } from 'react-router'
import { Navigator, EntitySettings } from '@framework/Navigator'
import { MicrosoftGraphEmailServiceEntity } from './Signum.Mailing.MicrosoftGraph';
import { registerChangeLogModule } from '@framework/Basics/ChangeLogClient';


export function start(options: {
  routes: RouteObject[],
}) {

  registerChangeLogModule("Signum.MicrosoftGraph", () => import("./Changelog"));

  Navigator.addSettings(new EntitySettings(MicrosoftGraphEmailServiceEntity, e => import('./Templates/MicrosoftGraphEmailService')));
}

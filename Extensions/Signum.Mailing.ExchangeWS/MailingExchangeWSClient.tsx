import * as React from 'react'
import { RouteObject } from 'react-router'
import * as Navigator from '@framework/Navigator'
import { EntitySettings } from '@framework/Navigator';
import { ExchangeWebServiceEmailServiceEntity } from './Signum.Mailing.ExchangeWS';


export function start(options: {
  routes: RouteObject[],
}) {

  Navigator.addSettings(new EntitySettings(ExchangeWebServiceEmailServiceEntity, e => import('./Templates/ExchangeWebServiceEmailService')));

}

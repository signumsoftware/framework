import * as React from 'react'
import { RouteObject } from 'react-router'
import { Navigator, EntitySettings } from '@framework/Navigator'
import { ExchangeWebServiceEmailServiceEntity } from './Signum.Mailing.ExchangeWS';

export namespace MailingExchangeWSClient {
  
  
  export function start(options: {
    routes: RouteObject[],
  }): void {
  
    Navigator.addSettings(new EntitySettings(ExchangeWebServiceEmailServiceEntity, e => import('./Templates/ExchangeWebServiceEmailService')));
  
  }
}

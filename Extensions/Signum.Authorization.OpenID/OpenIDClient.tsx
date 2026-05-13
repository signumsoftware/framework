import * as React from 'react'
import { RouteObject } from 'react-router'
import { Navigator, EntitySettings } from '@framework/Navigator'
import { OpenIDConfigurationEmbedded } from './Signum.Authorization.OpenID'

export namespace OpenIDClient {
  export function start(_options: { routes: RouteObject[] }): void {
    Navigator.addSettings(new EntitySettings(OpenIDConfigurationEmbedded, e => import('./OpenIDConfiguration')));
  }
}

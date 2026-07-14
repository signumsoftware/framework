import { RouteObject } from 'react-router'
import { Navigator, EntitySettings } from '@framework/Navigator'
import { OpenIDConfigurationEmbedded } from './Signum.Authorization.OpenID'

export namespace OpenIDAdminClient {

  // Called from MainAdmin.tsx. Registers the configuration UI.
  // Navigator dependency is intentional — only loaded for authenticated users.
  export function start(_options: { routes: RouteObject[] }): void {
    Navigator.addSettings(new EntitySettings(OpenIDConfigurationEmbedded, e => import('./OpenIDConfiguration')));
  }
}

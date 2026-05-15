import * as React from 'react'
import { RouteObject } from 'react-router'
import { ImportComponent } from '@framework/ImportComponent'

export namespace OpenIDClient {

  // Called from MainPublic.tsx inside reload().
  // No Navigator dependency — safe to load for anonymous users.
  export function startPublic(options: { routes: RouteObject[] }): void {
    options.routes.push({
      path: "/openid-callback",
      element: <ImportComponent onImport={() => import('./OpenIDCallback')} />
    });
  }
}

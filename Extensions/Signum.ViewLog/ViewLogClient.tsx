import * as React from 'react'
import { RouteObject } from 'react-router'
import { QuickLinkClient, QuickLinkExplore } from '@framework/QuickLinkClient'
import { Navigator } from '@framework/Navigator'
import { Finder } from '@framework/Finder'
import { getQueryKey } from '@framework/Reflection'
import { ViewLogEntity } from './Signum.ViewLog'
import { ChangeLogClient } from '@framework/Basics/ChangeLogClient'

export namespace ViewLogClient {
  
  export function start(options: { routes: RouteObject[], showQuickLink?: (typeName: string) => boolean }): void {
  
    ChangeLogClient.registerChangeLogModule("Signum.ViewLog", () => import("./Changelog"));
  
    if (Finder.isFindable(ViewLogEntity, false)) {
      QuickLinkClient.registerGlobalQuickLink(entityType => Promise.resolve([
        new QuickLinkExplore(ViewLogEntity, ctx => ({ queryName: ViewLogEntity, filterOptions: [{ token: ViewLogEntity.token(e => e.target), value: ctx.lite }] }),
          {
            text: () => ViewLogEntity.nicePluralName(),
            isVisible: options.showQuickLink == null || options.showQuickLink(entityType),
            icon: "eye",
            iconColor: "#2E86C1",
          }
        )
      ]));
    }
  }
}




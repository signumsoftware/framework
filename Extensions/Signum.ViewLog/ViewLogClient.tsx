import * as React from 'react'
import { RouteObject } from 'react-router'
import * as QuickLinks from '@framework/QuickLinks'
import { Navigator } from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { getQueryKey } from '@framework/Reflection'
import { ViewLogEntity } from './Signum.ViewLog'
import { registerChangeLogModule } from '@framework/Basics/ChangeLogClient'

export function start(options: { routes: RouteObject[], showQuickLink?: (typeName: string) => boolean }) {

  registerChangeLogModule("Signum.ViewLog", () => import("./Changelog"));

  if (Finder.isFindable(ViewLogEntity, false)) {
    QuickLinks.registerGlobalQuickLink(entityType => Promise.resolve([
      new QuickLinks.QuickLinkExplore(ViewLogEntity, ctx => ({ queryName: ViewLogEntity, filterOptions: [{ token: ViewLogEntity.token(e => e.target), value: ctx.lite }] }),
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




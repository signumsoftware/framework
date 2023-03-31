import * as React from 'react'
import { RouteObject } from 'react-router'
import * as QuickLinks from '@framework/QuickLinks'
import * as Navigator from '@framework/Navigator'
import { ViewLogEntity } from './Signum.ViewLog'

export function start(options: { routes: RouteObject[], showQuickLink?: (typeName: string) => boolean }) {

  QuickLinks.registerGlobalQuickLink(ctx => new QuickLinks.QuickLinkExplore({
    queryName: ViewLogEntity,
    filterOptions: [{ token: ViewLogEntity.token(e => e.target), value: ctx.lite}]
  }, {
    isVisible: Navigator.isFindable(ViewLogEntity) && (options.showQuickLink == null || options.showQuickLink(ctx.lite.EntityType)),
    icon: "eye",
    iconColor: "#2E86C1",
  }));
}




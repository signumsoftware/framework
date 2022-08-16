import * as QuickLinks from '@framework/QuickLinks'
import { ViewLogEntity } from './Signum.Entities.ViewLog'

export function start(options: { routes: JSX.Element[], showAlerts?: (typeName: string, when: "CreateAlert" | "QuickLink") => boolean }) {
  QuickLinks.registerGlobalQuickLink(ctx => new QuickLinks.QuickLinkExplore({
    queryName: ViewLogEntity,
    filterOptions: [{ token: ViewLogEntity.token(e => e.target), value: ctx.lite}]
  }, {
    icon: "search",
    iconColor: "green",
  }));
}




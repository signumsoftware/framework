import * as QuickLinks from '@framework/QuickLinks'
import * as Navigator from '@framework/Navigator'
import { ViewLogEntity } from './Signum.Entities.ViewLog'
import { getQueryKey } from '@framework/Reflection'

export function start(options: { routes: JSX.Element[], showQuickLink?: (typeName: string) => boolean }) {

  QuickLinks.registerGlobalQuickLink(entityType => ({
    key: getQueryKey(ViewLogEntity),
    generator: {
      factory: ctx => new QuickLinks.QuickLinkExplore({
        queryName: ViewLogEntity,
        filterOptions: [{ token: ViewLogEntity.token(e => e.target), value: ctx.lite }]
      }),
      options:
      {
        text: () => ViewLogEntity.nicePluralName(),
        isVisible: Navigator.isFindable(ViewLogEntity) && (options.showQuickLink == null || options.showQuickLink(entityType)),
        icon: "eye",
        iconColor: "#2E86C1",
      }
    }
  }))
}




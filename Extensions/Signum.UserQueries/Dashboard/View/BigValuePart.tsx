
import * as React from 'react'
import { FindOptions } from '@framework/FindOptions'
import { QueryToken } from '@framework/QueryToken'
import { getQueryKey, getQueryNiceName } from '@framework/Reflection'
import { JavascriptMessage, toLite, liteKey, translated } from '@framework/Signum.Entities'
import { SearchControl, SearchControlHandler, SearchValue, SearchValueController } from '@framework/Search'
import { UserQueryClient } from '../../UserQueryClient'
import { classes, getColorContrasColorBWByHex, softCast } from '@framework/Globals';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { Finder } from '@framework/Finder'
import { useAPI, useVersion } from '@framework/Hooks'
import { DashboardClient, PanelPartContentProps } from '../../../Signum.Dashboard/DashboardClient'
import { FullscreenComponent } from '@framework/Components/FullscreenComponent'
import { parseIcon } from '@framework/Components/IconTypeahead'
import { CachedQueryJS, executeQueryCached, executeQueryValueCached } from '../../../Signum.Dashboard/CachedQueryExecutor'
import { DashboardPinnedFilters } from '../../../Signum.Dashboard/View/DashboardFilterController'
import { BigValuePartEntity, UserQueryEntity, UserQueryPartEntity } from '../../Signum.UserQueries'
import { UserAssetClient } from '../../../Signum.UserAssets/UserAssetClient'

export interface UserQueryPartHandler {
  findOptions: FindOptions;
  refresh: () => void;
}

export default function BigValuePart(p: PanelPartContentProps<BigValuePartEntity>): React.JSX.Element {

  let fo = useAPI(signal => p.content.userQuery == null ? null : UserQueryClient.Converter.toFindOptions(p.content.userQuery, p.entity), [p.content.userQuery, p.entity && liteKey(p.entity)]);

  let valueToken = p.content.valueToken && UserAssetClient.getToken(p.content.valueToken);

  const [version, updateVersion] = useVersion();

  React.useEffect(() => {

    if (fo) {
      var dashboardPinnedFilters = fo.filterOptions?.filter(a => a?.dashboardBehaviour == "PromoteToDasboardPinnedFilter") ?? [];

      if (dashboardPinnedFilters.length) {
        Finder.getQueryDescription(fo.queryName)
          .then(qd => Finder.parseFilterOptions(dashboardPinnedFilters, fo!.groupResults ?? false, qd))
          .then(fops => {
            p.dashboardController.setPinnedFilter(new DashboardPinnedFilters(p.partEmbedded, getQueryKey(fo!.queryName), fops));
            p.dashboardController.registerInvalidations(p.partEmbedded, () => updateVersion());
          });
      } else {
        p.dashboardController.clearPinnesFilter(p.partEmbedded);
        p.dashboardController.registerInvalidations(p.partEmbedded, () => updateVersion());
      }
    }
  }, [fo, p.partEmbedded]);

  const cachedQuery = p.content.userQuery && p.cachedQueries[liteKey(toLite(p.content.userQuery))];

  if (p.content.userQuery == null)
    fo = {
      queryName: p.dashboardController.dashboard.entityType!.model as string,
      filterOptions: [{ token: "Entity", value: p.entity }]
    };

  if (!fo)
    return <span>{JavascriptMessage.loading.niceToString()}</span>;

  if (p.dashboardController.isLoading)
    return <span>{JavascriptMessage.loading.niceToString()}...</span>;

  const foExpanded = p.dashboardController.applyToFindOptions(p.partEmbedded, fo);

  p.customDataRef.current = softCast<UserQueryPartHandler>({
    findOptions: foExpanded,
    refresh: updateVersion,
  });

  return <BigValueSearchCounter
    findOptions={foExpanded}
    valueToken={valueToken}
    text={translated(p.partEmbedded, a => a.title) || (p.content.userQuery ? translated(p.content.userQuery, a => a.displayName) : valueToken?.niceName)}
    iconName={p.partEmbedded.iconName ?? undefined}
    iconColor={p.partEmbedded.iconColor ?? undefined}
    deps={[...p.deps ?? [], version]}
    customColor={p.partEmbedded.customColor}
    titleColor={p.partEmbedded.titleColor}
    userQuery={p.content.userQuery}
  />;
}

interface BigValueBadgeProps {
  findOptions: FindOptions;
  valueToken: QueryToken | null;
  text?: string;
  iconName?: string;
  iconColor?: string;
  titleColor?: string | null;
  deps?: React.DependencyList;
  //cachedQuery?: Promise<CachedQueryJS>;
  customColor: string | null;
  userQuery: UserQueryEntity | null;
}

export function BigValueSearchCounter(p: BigValueBadgeProps): React.JSX.Element {

  const vsc = React.useRef<SearchValueController>(null);

  return (
    <div className={classes("card", "border-light shadow-sm mb-3", "o-hidden")}
      style={{
      backgroundColor: p.customColor ?? undefined,
      color: Boolean(p.customColor) ? getColorContrasColorBWByHex(p.customColor!) : "var(--bs-body-color)"
    }}>
      <div className={classes("card-body")} onClick={e => vsc.current!.handleClick(e)} style={{
        cursor: "pointer",
        color: p.titleColor ?? (Boolean(p.customColor) ? getColorContrasColorBWByHex(p.customColor!) : "var(--bs-body-color)")
      }}>
        <div className="row">
          <div className="col-lg-3">
            {p.iconName &&
              <FontAwesomeIcon icon={parseIcon(p.iconName)!} color={p.iconColor} size="4x" />}
          </div>
          <div className={classes("col-lg-9 flip", "text-end")}>
            <h3>
              <SearchValue ref={vsc} findOptions={p.findOptions} isLink={false} isBadge={false} deps={p.deps}
                searchControlProps={{ extraOptions: { userQuery: toLite(p.userQuery) } }}
                valueToken={p.valueToken ?? undefined}
              />
                {/*customRequest={p.cachedQuery && ((req, fop, token) => p.cachedQuery!.then(cq => executeQueryValueCached(req, fop, token, cq)))}*/}
            </h3>
          </div>
        </div>
        <div className={classes("flip", "text-end")}>
          <h6 className="large">{p.text}</h6>
        </div>
      </div>
    </div>
  );
}

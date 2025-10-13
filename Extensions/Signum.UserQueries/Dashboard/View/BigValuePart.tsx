import * as React from 'react'
import { FindOptions, QueryToken } from '@framework/FindOptions'
import { getQueryKey } from '@framework/Reflection'
import { JavascriptMessage, toLite, liteKey, translated } from '@framework/Signum.Entities'
import { SearchValue, SearchValueController } from '@framework/Search'
import { UserQueryClient } from '../../UserQueryClient'
import { classes, getColorContrasColorBWByHex, softCast } from '@framework/Globals';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { Finder } from '@framework/Finder'
import { useAPI, useVersion } from '@framework/Hooks'
import { PanelPartContentProps } from '../../../Signum.Dashboard/DashboardClient'
import { parseIcon } from '@framework/Components/IconTypeahead'
import { DashboardPinnedFilters } from '../../../Signum.Dashboard/View/DashboardFilterController'
import { BigValuePartEntity, UserQueryEntity } from '../../Signum.UserQueries'
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
    clickable={p.content.userQuery != null}
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
  clickable: boolean;
  valueToken: QueryToken | null;
  text?: React.ReactNode;
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
    <div className={classes("card", "border-tertiary shadow-sm mb-3 w-100", "o-hidden")}
      style={{
      backgroundColor: p.customColor ?? undefined,
      color: Boolean(p.customColor) ? getColorContrasColorBWByHex(p.customColor!) : "var(--bs-body-color)"
      }}>
      <div className={classes("card-body")} onClick={p.clickable ? (e => vsc.current!.handleClick(e)) : undefined} style={{
        cursor: p.clickable ? "pointer" : undefined,
        color: p.titleColor ?? (Boolean(p.customColor) ? getColorContrasColorBWByHex(p.customColor!) : "var(--bs-body-color)")
      }}>
        <div className="dashboard-flex">
          <div className="left">
            <h3>
              <SearchValue ref={vsc} findOptions={p.findOptions} isLink={false} isBadge={false} deps={p.deps}
                searchControlProps={{ extraOptions: { userQuery: toLite(p.userQuery) } }}
                valueToken={p.valueToken ?? undefined}
              />
              {/*customRequest={p.cachedQuery && ((req, fop, token) => p.cachedQuery!.then(cq => executeQueryValueCached(req, fop, token, cq)))}*/}
            </h3>
            

          </div>
          <div className="right"> 
            {p.iconName &&
              <FontAwesomeIcon icon={parseIcon(p.iconName)!} color={p.iconColor} size="2x" />}
          </div>
        </div>
        <h3 className="medium">{p.text}</h3>
      </div>
    </div>
  );
}

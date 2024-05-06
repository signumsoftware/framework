
import * as React from 'react'
import { FindOptions } from '@framework/FindOptions'
import { getQueryKey, getQueryNiceName } from '@framework/Reflection'
import { JavascriptMessage, toLite, liteKey, translated } from '@framework/Signum.Entities'
import { SearchControl, SearchControlHandler, SearchValue, SearchValueController } from '@framework/Search'
import { UserQueryClient } from '../../UserQueryClient'
import { classes, getColorContrasColorBWByHex, softCast } from '@framework/Globals';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { Finder } from '@framework/Finder'
import { useAPI } from '@framework/Hooks'
import { DashboardClient, PanelPartContentProps } from '../../../Signum.Dashboard/DashboardClient'
import { FullscreenComponent } from '@framework/Components/FullscreenComponent'
import { parseIcon } from '@framework/Components/IconTypeahead'
import { CachedQueryJS, executeQueryCached, executeQueryValueCached } from '../../../Signum.Dashboard/CachedQueryExecutor'
import { DashboardPinnedFilters } from '../../../Signum.Dashboard/View/DashboardFilterController'
import { UserQueryEntity, UserQueryPartEntity } from '../../Signum.UserQueries'

export interface UserQueryPartHandler {
  findOptions: FindOptions;
  refresh: () => void;
}

export default function UserQueryPart(p: PanelPartContentProps<UserQueryPartEntity>) {

  const [fo, setFo] = React.useState<FindOptions>();
  const [refreshKey, setRefreshKey] = React.useState<number>(0);

  React.useEffect(() => {
    UserQueryClient.Converter.toFindOptions(p.content.userQuery, p.entity)
      .then(resFo => setFo(resFo));
  }, [p.content.userQuery, p.entity && liteKey(p.entity)]);

  React.useEffect(() => {

    if (fo) {
      var dashboardPinnedFilters = fo.filterOptions?.filter(a => a?.dashboardBehaviour == "PromoteToDasboardPinnedFilter") ?? [];

      if (dashboardPinnedFilters.length) {
        Finder.getQueryDescription(fo.queryName)
          .then(qd => Finder.parseFilterOptions(dashboardPinnedFilters, fo!.groupResults ?? false, qd))
          .then(fops => {
            p.dashboardController.setPinnedFilter(new DashboardPinnedFilters(p.partEmbedded, getQueryKey(fo!.queryName), fops));
            p.dashboardController.registerInvalidations(p.partEmbedded, () => setRefreshKey(a => a + 1));
          });
      } else {
        p.dashboardController.clearPinnesFilter(p.partEmbedded);
        p.dashboardController.registerInvalidations(p.partEmbedded, () => setRefreshKey(a => a + 1));
      }
    }
  }, [fo, p.partEmbedded]);

  const cachedQuery = p.cachedQueries[liteKey(toLite(p.content.userQuery))];

  if (!fo)
    return <span>{JavascriptMessage.loading.niceToString()}</span>;

  if (p.dashboardController.isLoading)
    return <span>{JavascriptMessage.loading.niceToString()}...</span>;

  const foExpanded = p.dashboardController.applyToFindOptions(p.partEmbedded, fo);

  p.customDataRef.current = softCast<UserQueryPartHandler>({
    findOptions: foExpanded,
    refresh: handleOnRefresh,
  });

  if (p.content.renderMode == "BigValue") {
    return <BigValueSearchCounter
      findOptions={foExpanded}
      aggregateFromSummaryHeader={p.content.aggregateFromSummaryHeader}
      text={translated(p.partEmbedded, a => a.title) || translated(p.content.userQuery, a => a.displayName)}
      iconName={p.partEmbedded.iconName ?? undefined}
      iconColor={p.partEmbedded.iconColor ?? undefined}
      deps={[...p.deps ?? [], refreshKey]}
      cachedQuery={cachedQuery}
      customColor={p.partEmbedded.customColor}
      titleColor={p.partEmbedded.titleColor}
      userQuery={p.content.userQuery}
    />;
  }

  function handleOnRefresh() {
    setRefreshKey(a => a + 1);
    handleOnDataChanged(fo!);
  }

  function handleOnDataChanged(fo: FindOptions) {
    setFo(fo);

    if (p.content.autoUpdate == "Dashboard") {
      p.dashboardController.invalidate(p.partEmbedded, null);
    }
    else if (p.content.autoUpdate == "InteractionGroup" && p.partEmbedded.interactionGroup != null) {
      p.dashboardController.invalidate(p.partEmbedded, p.partEmbedded.interactionGroup)
    }
  }

  return (
    <SearchContolInPart
      part={p.content}
      findOptions={foExpanded}
      deps={[...p.deps ?? [], refreshKey]}
      onDataChanged={handleOnDataChanged}
      cachedQuery={cachedQuery} />
  );
}

function SearchContolInPart({ findOptions, part, deps, cachedQuery, onDataChanged, onReload }: {
  findOptions: FindOptions,
  onDataChanged: (fo: FindOptions) => void,
  part: UserQueryPartEntity,
  cachedQuery?: Promise<CachedQueryJS>,
  deps?: React.DependencyList;
  onReload?: () => void;
}) {

  const sc = React.useRef<SearchControlHandler>(null);

  return (
    <FullscreenComponent onReload={e => { e.preventDefault(); sc.current?.doSearch({ dataChanged: true }); onReload?.(); }}>
      <SearchControl
        ref={sc}
        deps={deps}
        findOptions={findOptions}
        showHeader={"PinnedFilters"}
        pinnedFilterVisible={fop => fop.dashboardBehaviour == null}
        showFooter={part.showFooter}
        allowSelection={part.allowSelection}
        defaultRefreshMode={part.userQuery.refreshMode}
        searchOnLoad={part.userQuery.refreshMode == "Auto"}
        customRequest={cachedQuery && ((req, fop) => cachedQuery!.then(cq => executeQueryCached(req, fop, cq)))}
        onSearch={(fop, dataChange, scl) => dataChange && onDataChanged(Finder.toFindOptions(fop, scl.props.queryDescription, scl.props.defaultIncudeDefaultFilters))}
        maxResultsHeight={part.allowMaxHeight ? "none" : undefined}
        extraOptions={{ userQuery: toLite(part.userQuery) }}
      />
    </FullscreenComponent>
  );
}

interface BigValueBadgeProps {
  findOptions: FindOptions;
  aggregateFromSummaryHeader: boolean;
  text?: string;
  iconName?: string;
  iconColor?: string;
  titleColor?: string | null;
  deps?: React.DependencyList;
  cachedQuery?: Promise<CachedQueryJS>;
  customColor: string | null;
  userQuery: UserQueryEntity;
}

export function BigValueSearchCounter(p: BigValueBadgeProps) {

  const vsc = React.useRef<SearchValueController>(null);

  return (
    <div className={classes("card", !p.customColor && "bg-ligth", "o-hidden")}
      style={{
      backgroundColor: p.customColor ?? undefined,
      color: Boolean(p.customColor) ? getColorContrasColorBWByHex(p.customColor!) : "black"
    }}>
      <div className={classes("card-body")} onClick={e => vsc.current!.handleClick(e)} style={{
        cursor: "pointer",
        color: p.titleColor ?? (Boolean(p.customColor) ? getColorContrasColorBWByHex(p.customColor!) : "black")
      }}>
        <div className="row">
          <div className="col-lg-3">
            {p.iconName &&
              <FontAwesomeIcon icon={parseIcon(p.iconName)!} color={p.iconColor} size="4x" />}
          </div>
          <div className={classes("col-lg-9 flip", "text-end")}>
            <h1>
              <SearchValue ref={vsc} findOptions={p.findOptions} isLink={false} isBadge={false} deps={p.deps}
                searchControlProps={{ extraOptions: { userQuery: toLite(p.userQuery) } }}
                valueToken={!p.aggregateFromSummaryHeader ? undefined : p.findOptions.columnOptions!.first(a => a?.summaryToken != null)?.summaryToken}
                customRequest={p.cachedQuery && ((req, fop, token) => p.cachedQuery!.then(cq => executeQueryValueCached(req, fop, token, cq)))}
              />
            </h1>
          </div>
        </div>
        <div className={classes("flip", "text-end")}>
          <h6 className="large">{p.text || getQueryNiceName(p.findOptions.queryName)}</h6>
        </div>
      </div>
    </div>
  );
}

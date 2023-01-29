
import * as React from 'react'
import { FindOptions } from '@framework/FindOptions'
import { getQueryKey, getQueryNiceName, getTypeInfos } from '@framework/Reflection'
import { Entity, Lite, is, JavascriptMessage, toLite, liteKey } from '@framework/Signum.Entities'
import { SearchControl, SearchValue, SearchValueController } from '@framework/Search'
import * as UserQueryClient from '../../UserQueries/UserQueryClient'
import { UserQueryPartEntity, PanelPartEmbedded } from '../Signum.Entities.Dashboard'
import { classes, getColorContrasColorBWByHex, softCast } from '@framework/Globals';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import * as Constructor from '@framework/Constructor'
import { useAPI } from '@framework/Hooks'
import { PanelPartContentProps } from '../DashboardClient'
import { FullscreenComponent } from '../../Chart/Templates/FullscreenComponent'
import SelectorModal from '@framework/SelectorModal'
import { parseIcon } from '../../Basics/Templates/IconTypeahead'
import { translated } from '../../Translation/TranslatedInstanceTools'
import { CachedQueryJS, executeQueryCached, executeQueryValueCached } from '../CachedQueryExecutor'
import { DashboardController, DashboardPinnedFilters } from './DashboardFilterController'

export interface UserQueryPartHandler {
  findOptions: FindOptions;
  refresh: () => void;
}

export default function UserQueryPart(p: PanelPartContentProps<UserQueryPartEntity>) {

  let fo = useAPI(signal => UserQueryClient.Converter.toFindOptions(p.content.userQuery, p.entity), [p.content.userQuery, p.entity]);

  const [refreshKey, setRefreshKey] = React.useState<number>(0);

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
      sameColor={p.partEmbedded.useIconColorForTitle}
    />;
  }

  function handleOnRefresh() {
    setRefreshKey(a => a + 1);
    handleOnDataChanged();
  }

  function handleOnDataChanged() {

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
      onReload={() => setRefreshKey(a => a + 1)}
      onDataChanged={handleOnDataChanged}
      cachedQuery={cachedQuery} />
  );
}

function SearchContolInPart({ findOptions, part, deps, cachedQuery, onDataChanged, onReload }: {
  findOptions: FindOptions,
  onDataChanged: () => void,
  part: UserQueryPartEntity,
  cachedQuery?: Promise<CachedQueryJS>,
  deps?: React.DependencyList;
  onReload: () => void;
}) {

  return (
    <FullscreenComponent onReload={e => { e.preventDefault(); onReload(); }}>
      <SearchControl
        deps={deps}
        findOptions={findOptions}
        showHeader={"PinnedFilters"}
        pinnedFilterVisible={fop => fop.dashboardBehaviour == null}
        showFooter={part.showFooter}
        allowSelection={part.allowSelection}
        defaultRefreshMode={part.userQuery.refreshMode}
        searchOnLoad={part.userQuery.refreshMode == "Auto"}
        customRequest={cachedQuery && ((req, fop) => cachedQuery!.then(cq => executeQueryCached(req, fop, cq)))}
        onSearch={(fo, dataChange) => dataChange && onDataChanged()}
        maxResultsHeight={part.allowMaxHeight ? "none" : undefined}        
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
  sameColor: boolean;
  deps?: React.DependencyList;
  cachedQuery?: Promise<CachedQueryJS>;
  customColor: string | null;
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
        color: p.sameColor ? p.iconColor : (Boolean(p.customColor) ? getColorContrasColorBWByHex(p.customColor!) : "black")
      }}>
        <div className="row">
          <div className="col-3">
            {p.iconName &&
              <FontAwesomeIcon icon={parseIcon(p.iconName)!} color={p.iconColor} size="4x" />}
          </div>
          <div className={classes("col-9 flip", "text-end")}>
            <h1>
              <SearchValue ref={vsc} findOptions={p.findOptions} isLink={false} isBadge={false} deps={p.deps}
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

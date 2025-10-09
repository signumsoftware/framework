
import * as React from 'react'
import { FindOptions } from '@framework/FindOptions'
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
import { UserQueryEntity, UserQueryPartEntity } from '../../Signum.UserQueries'

export interface UserQueryPartHandler {
  findOptions: FindOptions;
  refresh: () => void;
}

export default function UserQueryPart(p: PanelPartContentProps<UserQueryPartEntity>): React.JSX.Element {

  let fo = useAPI(signal => UserQueryClient.Converter.toFindOptions(p.content.userQuery, p.entity), [p.content.userQuery, p.entity && liteKey(p.entity)]);

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

  const cachedQuery = p.cachedQueries[liteKey(toLite(p.content.userQuery))];

  if (!fo)
    return <span>{JavascriptMessage.loading.niceToString()}</span>;

  if (p.dashboardController.isLoading)
    return <span>{JavascriptMessage.loading.niceToString()}...</span>;

  const foExpanded = p.dashboardController.applyToFindOptions(p.partEmbedded, fo);

  p.customDataRef.current = softCast<UserQueryPartHandler>({
    findOptions: foExpanded,
    refresh: updateVersion,
  });

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
      deps={[...p.deps ?? [], version]}
      //onReload={() => setRefreshKey(a => a + 1)}
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
  onReload?: () => void;
}) {

  const scRef = React.useRef<SearchControlHandler>(null);

  return (
    <FullscreenComponent onReload={e => { e.preventDefault(); onReload ? onReload() : scRef.current!.doSearch({dataChanged : false}); }}>
      {fullScreen => <SearchControl
        ref={scRef}
        deps={deps}
        findOptions={findOptions}
        showHeader={"PinnedFilters"}
        avoidTableFooterContainer={true}
        pinnedFilterVisible={fop => fop.dashboardBehaviour == null}
        showFooter={part.showFooter}
        allowSelection={part.allowSelection}
        defaultRefreshMode={part.userQuery.refreshMode}
        searchOnLoad={part.userQuery.refreshMode == "Auto"}
        customRequest={cachedQuery && ((req, fop) => cachedQuery!.then(cq => executeQueryCached(req, fop, cq)))}
        onSearch={(fo, dataChange) => dataChange && onDataChanged()}
        maxResultsHeight={part.allowMaxHeight ? "none" : undefined}
        extraOptions={{ userQuery: toLite(part.userQuery) }}
      />}
    </FullscreenComponent>
  );
}

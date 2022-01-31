
import * as React from 'react'
import { FindOptions } from '@framework/FindOptions'
import { getQueryKey, getQueryNiceName, getTypeInfos } from '@framework/Reflection'
import { Entity, Lite, is, JavascriptMessage, toLite, liteKey } from '@framework/Signum.Entities'
import { SearchControl, ValueSearchControl } from '@framework/Search'
import * as UserQueryClient from '../../UserQueries/UserQueryClient'
import { UserQueryPartEntity, PanelPartEmbedded } from '../Signum.Entities.Dashboard'
import { classes, getColorContrasColorBWByHex } from '@framework/Globals';
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

export default function UserQueryPart(p: PanelPartContentProps<UserQueryPartEntity>) {

  let fo = useAPI(signal => UserQueryClient.Converter.toFindOptions(p.part.userQuery, p.entity), [p.part.userQuery, p.entity]);

  const [refreshKey, setRefreshKey] = React.useState<number>(0);

  React.useEffect(() => {

    if (fo) {
      var dashboardPinnedFilters = fo.filterOptions?.filter(a => a?.dashboardBehaviour == "PromoteToDasboardPinnedFilter") ?? [];

      if (dashboardPinnedFilters.length) {
        Finder.getQueryDescription(fo.queryName)
          .then(qd => Finder.parseFilterOptions(dashboardPinnedFilters, fo!.groupResults ?? false, qd))
          .then(fops => p.dashboardController.setPinnedFilter(new DashboardPinnedFilters(p.partEmbedded, getQueryKey(fo!.queryName), fops)))
          .done();
      }
    }
  }, [fo]);

  React.useEffect(() => {
    p.dashboardController.registerInvalidations(p.partEmbedded, () => setRefreshKey(a => a + 1));
  }, [p.partEmbedded])

  const cachedQuery = p.cachedQueries[liteKey(toLite(p.part.userQuery))];

  if (!fo)
    return <span>{JavascriptMessage.loading.niceToString()}</span>;

  const foExpanded = p.dashboardController.applyToFindOptions(p.partEmbedded, fo);

  if (p.part.renderMode == "BigValue") {
    return <BigValueSearchCounter
      findOptions={foExpanded}
      text={translated(p.partEmbedded, a => a.title) || translated(p.part.userQuery, a => a.displayName)}
      iconName={p.partEmbedded.iconName ?? undefined}
      iconColor={p.partEmbedded.iconColor ?? undefined}
      deps={[...p.deps ?? [], refreshKey]}
      cachedQuery={cachedQuery}
      customColor={p.partEmbedded.customColor}
      sameColor={p.partEmbedded.useIconColorForTitle}
    />;
  }

  return (
    <SearchContolInPart
      part={p.part}
      findOptions={foExpanded}
      deps={[...p.deps ?? [], refreshKey]}
      onDataChanged={() => {
        if (p.part.autoUpdate == "Dashboard") {
          p.dashboardController.invalidate(p.partEmbedded, null);
        }
        else if (p.part.autoUpdate == "InteractionGroup" && p.partEmbedded.interactionGroup != null) {
          p.dashboardController.invalidate(p.partEmbedded, p.partEmbedded.interactionGroup)
        }
      }}
      cachedQuery={cachedQuery} />
  );
}

function SearchContolInPart({ findOptions, part, deps, cachedQuery, onDataChanged }: {
  findOptions: FindOptions,
  onDataChanged: () => void,
  part: UserQueryPartEntity,
  cachedQuery?: Promise<CachedQueryJS>,
  deps?: React.DependencyList
}) {

  const [refreshCount, setRefreshCount] = React.useState<number>(0)
  const qd = useAPI(() => Finder.getQueryDescription(part.userQuery.query.key), [part.userQuery.query.key]);
  const typeInfos = qd && getTypeInfos(qd.columns["Entity"].type).filter(ti => Navigator.isCreable(ti, { isSearch: true }));

  function handleCreateNew(e: React.MouseEvent<any>) {
    e.preventDefault();

    return Finder.parseFilterOptions(findOptions.filterOptions ?? [], findOptions.groupResults ?? false, qd!)
      .then(fop => SelectorModal.chooseType(typeInfos!)
        .then(ti => ti && Finder.getPropsFromFilters(ti, fop)
          .then(props => Constructor.constructPack(ti.name, props)))
        .then(pack => pack && Navigator.view(pack))
        .then(() => {
          onDataChanged();
          setRefreshCount(a => a + 1);
        }))
      .done();
  }

  return (
    <FullscreenComponent onReload={e => { e.preventDefault(); setRefreshCount(a => a + 1); }} onCreateNew={part.createNew ? handleCreateNew : undefined} typeInfos={typeInfos}>
      <SearchControl
        deps={[refreshCount, ...deps ?? []]}
        findOptions={findOptions}
        showHeader={"PinnedFilters"}
        pinnedFilterVisible={fop => fop.dashboardBehaviour == null}
        showFooter={part.showFooter}
        allowSelection={part.allowSelection}
        defaultRefreshMode={part.userQuery.refreshMode}
        searchOnLoad={part.userQuery.refreshMode == "Auto"}
        customRequest={cachedQuery && ((req, fop) => cachedQuery!.then(cq => executeQueryCached(req, fop, cq)))}
        onSearch={(fo, dataChange) => dataChange && onDataChanged()}
        
      />
    </FullscreenComponent>
  );
}

interface BigValueBadgeProps {
  findOptions: FindOptions;
  text?: string;
  iconName?: string;
  iconColor?: string;
  sameColor: boolean;
  deps?: React.DependencyList;
  cachedQuery?: Promise<CachedQueryJS>;
  customColor: string | null;
}

export function BigValueSearchCounter(p: BigValueBadgeProps) {

  const vsc = React.useRef<ValueSearchControl>(null);

  return (
    <div className={classes(
      "card",
      !p.customColor && ("bg-ligth"),
      "o-hidden"
    )} style={{ backgroundColor: p.customColor ?? undefined, color: p.customColor != null ? getColorContrasColorBWByHex(p.customColor) : "black" }}>
      <div className={classes("card-body")} onClick={e => vsc.current!.handleClick(e)} style={{ cursor: "pointer", color: p.sameColor ? p.iconColor : (p.customColor != null ? getColorContrasColorBWByHex(p.customColor) : "black") }}>
        <div className="row">
          <div className="col-3">
            {p.iconName &&
              <FontAwesomeIcon icon={parseIcon(p.iconName)!} color={p.iconColor} size="4x" />}
          </div>
          <div className={classes("col-9 flip", "text-end")}>
            <h1>
              <ValueSearchControl ref={vsc} findOptions={p.findOptions} isLink={false} isBadge={false} deps={p.deps}
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

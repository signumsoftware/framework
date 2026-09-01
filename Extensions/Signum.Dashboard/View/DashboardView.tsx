import * as React from 'react'
import { Link } from 'react-router-dom'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes, getContrastingTextColorWCAG } from '@framework/Globals'
import { MListElementBinding } from '@framework/Reflection'
import { Entity, JavascriptMessage, getToString, liteKey, toLite, translated } from '@framework/Signum.Entities'
import { TypeContext, mlistItemContext } from '@framework/TypeContext'
import { DashboardClient, PanelPartContentProps } from '../DashboardClient'
import { DashboardEntity, PanelPartEmbedded, IPartEntity, DashboardMessage } from '../Signum.Dashboard'
import "../Dashboard.css"
import { ErrorBoundary } from '@framework/Components';
import { useAPI, useForceUpdate } from '@framework/Hooks'
import { fallbackIcon, parseIcon } from '@framework/Components/IconTypeahead'
import { DashboardController } from './DashboardFilterController'
import { CachedQueryJS } from '../CachedQueryExecutor'
import PinnedFilterBuilder from '@framework/SearchControl/PinnedFilterBuilder'
import { Navigator } from '@framework/Navigator'
import { LinkButton } from '@framework/Basics/LinkButton'
import { DashboardTooltipIcon } from './DashboardTooltipIcon'

export default function DashboardView(p: { dashboard: DashboardEntity, cachedQueries: { [userAssetKey: string]: Promise<CachedQueryJS> }, entity?: Entity, embedded?: boolean, deps?: React.DependencyList; reload: () => void; hideEditButton?: boolean }): React.JSX.Element {

  const forceUpdate = useForceUpdate();
  const dashboardController = React.useMemo(() => new DashboardController(forceUpdate, p.dashboard), [p.dashboard]);

  const entityLite = p.entity ? toLite(p.entity) : undefined;
  const hiddenPartsProvider = entityLite && DashboardClient.Options.hiddenPartsProviders[entityLite.EntityType];
  const hiddenParts = useAPI(() => entityLite && hiddenPartsProvider ? hiddenPartsProvider(p.dashboard, entityLite) : null,
    [p.dashboard, entityLite && liteKey(entityLite)]);

  // Tell the controller what is not being rendered before it decides whether the dashboard is still loading.
  dashboardController.hiddenParts = hiddenParts ?? new Set<string>();
  dashboardController.setIsLoading();

  // Wait for the provider rather than laying out parts it is about to remove: a panel that appears and then
  // vanishes, taking the row's proportions with it, is exactly what the provider is there to prevent.
  const loadingHiddenParts = hiddenPartsProvider != null && hiddenParts === undefined;

  const layout = React.useMemo(() => layoutParts(p.dashboard, hiddenParts ?? undefined), [p.dashboard, hiddenParts]);

  function renderBasic() {
    return (
      <div>
        <div className="sf-dashboard-view">
          {
            layout.parts
              .groupBy(c => c.value.row!.toString())
              .orderBy(gr => Number(gr.key))
              .map(gr =>
                <div className="row row-control-panel" key={"row" + gr.key}>
                  {gr.elements.orderBy(ctx => layout.startColumn(ctx)).map((c, j, list) => {

                    const prev = j == 0 ? undefined : list[j - 1];

                    const offset = layout.startColumn(c) - (prev ? (layout.startColumn(prev) + layout.columns(prev)) : 0);

                    return (
                      <div key={j} className={`col-sm-${layout.columns(c)} offset-sm-${offset}`}>
                        <PanelPart ctx={c} entity={p.entity}
                          dashboardController={dashboardController} reload={p.reload} cachedQueries={p.cachedQueries} deps={p.deps} />
                      </div>
                    );
                  })}
                </div>)
          }
        </div>
      </div>
    );
  }

  function renderCombinedRows() {
    var rows = layout.parts
      .groupBy(c => c.value.row!.toString())
      .orderBy(g => Number(g.key))
      .map(g => ({
        columns: g.elements.orderBy(a => layout.startColumn(a)).map(p => ({
          startColumn: layout.startColumn(p),
          columnWidth: layout.columns(p),
          parts: [p],
        }) as CombinedColumn)
      }) as CombinedRow);

    var combinedRows = combineRows(rows);

    return (
      <div className="sf-dashboard-view">
        {combinedRows.map((r, i) =>
          <div className="row row-control-panel" key={"row" + i}>
            {r.columns.orderBy(ctx => ctx.startColumn).map((c, j, list) => {
              const last = j == 0 ? undefined : list[j - 1];
              const offset = c.startColumn! - (last ? (last.startColumn! + last.columnWidth!) : 0);
              return (
                <div key={j} className={`col-sm-${c.columnWidth} offset-sm-${offset}`} style={{ display: "flex", flexDirection: "column" }}>
                  {c.parts.map((pctx, i) => <PanelPart key={i} ctx={pctx} entity={p.entity} dashboardController={dashboardController} reload={p.reload} cachedQueries={p.cachedQueries} deps={p.deps} flex />)}
                </div>
              );
            })}
          </div>
        )}
      </div>
    );
  }


  return (
    <div className={p.embedded ? "sf-dashboard-view-embedded" : undefined}>
      {p.hideEditButton != true &&
        <div className="d-flex flex-row-reverse align-items-center m-1">
          {DashboardClient.onDashboardPageActions.map((fn, i) => <React.Fragment key={i}>{fn(p.dashboard)}</React.Fragment>)}
          {!Navigator.isReadOnly(DashboardEntity) &&
            <Link className="sf-hide" style={{ textDecoration: "none" }} to={Navigator.navigateRoute(p.dashboard)} title={DashboardMessage.Edit.niceToString()}>
              <FontAwesomeIcon aria-hidden={true} icon="pen-to-square" />
            </Link>}
        </div>}
      <div>
        {Array.from(dashboardController.pinnedFilters.values())
          .filter(pf => pf.pinnedFilters.length > 0)
          .map((pf, i) => <PinnedFilterBuilder key={i}
            queryDescription={pf.queryDescription}
            filterOptions={pf.pinnedFilters}
            onFiltersChanged={forceUpdate} />)}
        {
          loadingHiddenParts ? JavascriptMessage.loading.niceToString() :
          p.dashboard.combineSimilarRows ?
            renderCombinedRows() :
            renderBasic()
        }
      </div>
    </div>
  );
}

interface PartLayout {
  parts: TypeContext<PanelPartEmbedded>[];
  startColumn: (ctx: TypeContext<PanelPartEmbedded>) => number;
  columns: (ctx: TypeContext<PanelPartEmbedded>) => number;
}

/**
 * The Guid row id of a part in DashboardEntity.parts, which is what identifies it (there is no property on the
 * embedded entity). Undefined for a part that has not been saved yet, since the database assigns the row id.
 */
export function partRowId(ctx: TypeContext<PanelPartEmbedded>): string | undefined {
  const rowId = ctx.binding instanceof MListElementBinding ? ctx.binding.getMListElement()?.rowId : undefined;

  return rowId?.toString();
}

/**
 * Drops the parts a `hiddenPartsProvider` asked to leave out and closes the gaps they leave behind, so that
 * tailoring a panel away does not punch a hole in the grid. A row that filled its 12 columns keeps filling them,
 * its remaining panels growing in proportion; a row that did not simply closes up. The positions live here
 * instead of on the entity - writing them back would mark the dashboard as modified.
 */
function layoutParts(dashboard: DashboardEntity, hiddenGuids: Set<string> | undefined): PartLayout {

  const all = mlistItemContext(TypeContext.root(dashboard).subCtx(a => a.parts));

  const asDesigned: PartLayout = {
    parts: all,
    startColumn: ctx => ctx.value.startColumn!,
    columns: ctx => ctx.value.columns!,
  };

  if (!hiddenGuids?.size)
    return asDesigned;

  const isHidden = (ctx: TypeContext<PanelPartEmbedded>) => {
    const rowId = partRowId(ctx);
    return rowId != null && hiddenGuids.has(rowId);
  };

  const visible = all.filter(ctx => !isHidden(ctx));
  if (visible.length == all.length)
    return asDesigned;

  //Keyed by the context, not by the row id, because a part that has not been saved yet has no row id
  const repacked = new Map<TypeContext<PanelPartEmbedded>, { startColumn: number, columns: number }>();

  all.groupBy(ctx => ctx.value.row!.toString()).forEach(gr => {
    const row = gr.elements.orderBy(ctx => ctx.value.startColumn);
    const kept = row.filter(ctx => !isHidden(ctx));

    if (kept.length == 0 || kept.length == row.length)
      return;

    const start = row[0].value.startColumn!;
    const available = 12 - start;
    const wasFull = row.sum(ctx => ctx.value.columns!) == available;

    const widths = wasFull
      ? growToFill(kept.map(ctx => ctx.value.columns!), available)
      : kept.map(ctx => ctx.value.columns!);

    let next = start;
    kept.forEach((ctx, i) => {
      repacked.set(ctx, { startColumn: next, columns: widths[i] });
      next += widths[i];
    });
  });

  return {
    parts: visible,
    startColumn: ctx => repacked.get(ctx)?.startColumn ?? ctx.value.startColumn!,
    columns: ctx => repacked.get(ctx)?.columns ?? ctx.value.columns!,
  };
}

/** Grows `widths` proportionally until they add up to `target`, handing the leftover columns to the widest gaps. */
function growToFill(widths: number[], target: number): number[] {
  const total = widths.sum();

  if (total >= target || total == 0)
    return widths;

  const exact = widths.map(w => w * target / total);
  const result = exact.map(e => Math.max(1, Math.floor(e)));

  let rest = target - result.sum();
  if (rest <= 0)
    return result;

  const byFraction = exact.map((e, i) => ({ i, fraction: e - Math.floor(e) })).orderByDescending(a => a.fraction);
  for (let k = 0; rest > 0; k = (k + 1) % byFraction.length, rest--)
    result[byFraction[k].i]++;

  return result;
}

function combineRows(rows: CombinedRow[]): CombinedRow[] {

  const newRows: CombinedRow[] = [];

  for (let i = 0; i < rows.length; i++) {

    const row = {
      columns: rows[i].columns.map(c =>
        ({
          startColumn: c.startColumn,
          columnWidth: c.columnWidth,
          parts: [...c.parts]
        }) as CombinedColumn)
    } as CombinedRow;

    newRows.push(row);
    let j = 1;
    for (; i + j < rows.length; j++) {
      if (!tryCombine(row, rows[i + j])) {
        break;
      }
    }

    i = i + j - 1;
  }

  return newRows;
}

function tryCombine(row: CombinedRow, newRow: CombinedRow): boolean {
  if (!newRow.columns.every(nc =>
    row.columns.some(c => identical(nc, c)) ||
    !row.columns.some(c => overlaps(nc, c))))
    return false;

  newRow.columns.forEach(nc => {
    var c = row.columns.singleOrNull(c => identical(c, nc));

    if (c)
      c.parts.push(...nc.parts);
    else
      row.columns.push(nc);
  });

  return true;
}

export function identical(col1: CombinedColumn, col2: CombinedColumn): boolean {
  return col1.startColumn == col2.startColumn && col1.columnWidth == col2.columnWidth;
}

export function overlaps(col1: CombinedColumn, col2: CombinedColumn): boolean {

  var columnEnd1 = col1.startColumn + col1.columnWidth;
  var columnEnd2 = col2.startColumn + col2.columnWidth;


  return !(columnEnd1 <= col2.startColumn || columnEnd2 <= col1.startColumn);

}


interface CombinedRow {
  columns: CombinedColumn[];
}

interface CombinedColumn {
  startColumn: number;
  columnWidth: number;

  parts: TypeContext<PanelPartEmbedded>[];
}

export interface PanelPartProps {
  ctx: TypeContext<PanelPartEmbedded>;
  entity?: Entity;
  deps?: React.DependencyList;
  dashboardController: DashboardController;
  flex?: boolean;
  reload: () => void;
  cachedQueries: { [userAssetKey: string]: Promise<CachedQueryJS>, }
}

export function PanelPart(p: PanelPartProps): React.JSX.Element | null {
  const content = p.ctx.value.content;

  const customDataRef = React.useRef<any>(undefined);

  const state = useAPI(signal => DashboardClient.partRenderers[content.Type].component().then(c => ({ component: c, lastType: content.Type })),
    [content.Type], { avoidReset: true });

  if (state == null || state.lastType == null)
    return null;

  const part = p.ctx.value;

  const renderer = DashboardClient.partRenderers[content.Type];

  const lite = p.entity ? toLite(p.entity) : undefined;

  //The MList row id identifies the part, the Tour targets it through the data-part-content attribute
  const partContentKey = partRowId(p.ctx);

  if (renderer.withPanel && !renderer.withPanel(content, lite)) {
    const tooltipHtml = translated(part, p => p.tooltip);

    const partContent = (
      <div data-part-content={partContentKey}>
        <ErrorBoundary>
          {React.createElement(state.component, {
            partEmbedded: part,
            content: content,
            entity: lite,
            deps: p.deps,
            dashboardController: p.dashboardController,
            cachedQueries: p.cachedQueries,
            customDataRef: customDataRef,
          } as PanelPartContentProps<IPartEntity>)}
        </ErrorBoundary >
      </div>
    );

    return partContent;
  }

  const titleText = translated(part, p => p.title) ?? (renderer.defaultTitle ? renderer.defaultTitle(content) : getToString(content));
  const tooltipHtml = translated(part, p => p.tooltip);
  const icon = parseIcon(part.iconName);
  const iconColor = part.iconColor;

  const iconElement = icon ? (
    <FontAwesomeIcon aria-hidden={true} icon={fallbackIcon(icon)} color={iconColor ?? undefined} className="me-1" style={{ fontSize: "16px" }} />
  ) : null;

  const title = part.hideTitle ? null : !icon ? (
    <>
      {titleText}
      {tooltipHtml && (
        <DashboardTooltipIcon
          tooltipHtml={tooltipHtml}
          className="ms-2"
          iconClassName="sf-tooltip-icon"
        />
      )}
    </>
  ) : (
    <span>
      {iconElement}{titleText}
      {tooltipHtml && (
        <DashboardTooltipIcon
          tooltipHtml={tooltipHtml}
          className="ms-2"
          iconClassName="sf-tooltip-icon"
        />
      )}
    </span>
  );

  var dashboardFilter = p.dashboardController?.filters.get(p.ctx.value);

  function handleClearFilter(e: React.MouseEvent) {
    p.dashboardController.clearFilters(p.ctx.value);
  }

  const cardContent = (
    <div className={classes("card", !part.customColor && "border-tertiary", "shadow-sm", "mb-4")} style={{ flex: p.flex ? 1 : undefined,/* overflow: "hidden"*/ }}>
      {title &&
        <div className={classes("card-header fw-bold", "sf-show-hover", "d-flex", !part.customColor)}
          style={{ backgroundColor: part.customColor ?? undefined, color: part.customColor ? getContrastingTextColorWCAG(part.customColor) : undefined }}
        >

          {renderer.handleTitleClick == undefined ? title :
            <LinkButton title={undefined} className="sf-pointer"
              style={{ color: part.titleColor ?? (part.customColor ? getContrastingTextColorWCAG(part.customColor) : undefined), textDecoration: "none" }}
              onClick={e => { renderer.handleTitleClick!(content, lite, customDataRef, e); }}>
              {title}
            </LinkButton>
          }
          {
            dashboardFilter && <span className="badge bg-tertiary text-dark border ms-2 sf-filter-pill">
              {dashboardFilter.rows.length} {DashboardMessage.RowsSelected.niceToString().forGenderAndNumber(dashboardFilter.rows.length)}
              <button type="button" aria-label={DashboardMessage.Close.niceToString()} className="btn-close" onClick={handleClearFilter} />
            </span>
          }

          <div className="ms-auto">
            {renderer.customTitleButtons?.(content, lite, customDataRef)}
            {
              renderer.handleEditClick &&
              <LinkButton className="sf-pointer sf-hide" onClick={e => { renderer.handleEditClick!(content, lite, customDataRef, e).then(v => v && p.reload()); }} title={DashboardMessage.Edit.niceToString()}>
                <FontAwesomeIcon aria-hidden={true} icon="pen-to-square" className="me-1" />
              </LinkButton>
            }
          </div>
        </div>
      }
      <div data-part-content={partContentKey} className="card-body py-2 px-3 d-flex flex-column">
        <ErrorBoundary>
          {
            React.createElement(state.component, {
              partEmbedded: part,
              content: content,
              entity: lite,
              deps: p.deps,
              dashboardController: p.dashboardController,
              cachedQueries: p.cachedQueries,
              customDataRef: customDataRef,
            } as PanelPartContentProps<IPartEntity>)
          }
        </ErrorBoundary>
      </div>
    </div>
  );

  return cardContent;
}

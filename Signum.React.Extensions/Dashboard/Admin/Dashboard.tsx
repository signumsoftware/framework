import * as React from 'react'
import { Tabs, Tab } from 'react-bootstrap'
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { ValueLine, EntityLine, RenderEntity, OptionItem, EntityDetail, EntityRepeater, EntityTable, EntityCombo } from '@framework/Lines'
import { tryGetTypeInfos, New, getTypeInfos } from '@framework/Reflection'
import SelectorModal from '@framework/SelectorModal'
import { TypeContext } from '@framework/TypeContext'
import { DashboardEntity, PanelPartEmbedded, IPartEntity, InteractionGroup, CacheQueryConfigurationEmbedded, CachedQueryEntity, DashboardOperation, TokenEquivalenceGroupEntity, TokenEquivalenceEmbedded } from '../Signum.Entities.Dashboard'
import { EntityGridRepeater, EntityGridItem } from './EntityGridRepeater'
import * as DashboardClient from "../DashboardClient";
import { iconToString, IconTypeaheadLine, parseIcon } from "../../Basics/Templates/IconTypeahead";
import { ColorTypeaheadLine } from "../../Basics/Templates/ColorTypeahead";
import "../Dashboard.css"
import { getToString, toLite } from '@framework/Signum.Entities';
import { useForceUpdate } from '@framework/Hooks'
import { SearchValueLine } from '@framework/Search';
import * as Navigator from '@framework/Navigator';
import { classes } from '@framework/Globals';
import { OperationButton } from '@framework/Operations/EntityOperations';
import { EntityOperationContext } from '@framework/Operations';
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEmbeddedBuilder';
import { SubTokensOptions } from '@framework/FindOptions';

export default function Dashboard(p: { ctx: TypeContext<DashboardEntity> }) {
  const forceUpdate = useForceUpdate();
  function handleEntityTypeChange() {
    if (!p.ctx.value.entityType)
      p.ctx.value.embeddedInEntity = null;

    forceUpdate()
  }

  var allQueryNames = p.ctx.value.parts.flatMap(a => DashboardClient.getQueryNames(a.element.content))
    .distinctBy(a => a.id!.toString())
    .orderBy(a => getToString(a))
    .map(a => toLite(a));


  function handleOnCreate() {
    const pr = DashboardEntity.memberInfo(a => a.parts![0].element.content);
    return SelectorModal.chooseType(getTypeInfos(pr.type), {
      size: "def" as any,
      buttonDisplay: ti => {
        var icon = DashboardClient.defaultIcon(ti);
        return <><FontAwesomeIcon icon={icon.icon} color={icon.iconColor} /><span className="ms-2">{ti.niceName}</span></>;
      }
    })
      .then(ti => {
        if (ti == undefined)
          return undefined;

        const part = New(ti.name) as any as IPartEntity;

        const icon = DashboardClient.defaultIcon(ti);

        return PanelPartEmbedded.New({
          content: part,
          iconName: iconToString(icon.icon),
          iconColor: icon.iconColor,
        });
      });
  }

  var colors = ["#DFFF00", "#FFBF00", "#FF7F50", "#DE3163", "#9FE2BF", "#40E0D0", "#6495ED", "#CCCCFF"]

  function renderPart(tc: TypeContext<PanelPartEmbedded>) {
    const tcs = tc.subCtx({ formGroupStyle: "SrOnly", formSize: "xs", placeholderLabels: true });

    var icon = parseIcon(tc.value.iconName);

    var avoidDrag: React.HTMLAttributes<any> = {
      draggable: true,
      onDragStart: e => {
        e.preventDefault();
        e.stopPropagation();
      }
    };

    const title = (
      <div>
        <div className="d-flex">
          {icon && <div className="mx-2">
            <FontAwesomeIcon icon={icon} style={{ color: tc.value.iconColor ?? undefined, fontSize: "25px" }} {...avoidDrag}
              onClick={() => Navigator.view(tc.value, { propertyRoute: tc.propertyRoute }).then(a => {
                if (a) {
                  tc.value.iconName = a.iconName;
                  tc.value.iconColor = a.iconColor;
                  tc.value.useIconColorForTitle = a.useIconColorForTitle;
                  tc.value.modified = true;
                  forceUpdate();
                }
              })} />
          </div>}
          <div style={{ flexGrow: 1 }} className="me-2">

            <ValueLine ctx={tcs.subCtx(pp => pp.title)} label={getToString(tcs.value.content) ?? tcs.niceName(pp => pp.title)} valueHtmlAttributes={avoidDrag} />
            <div className="row">
              <div className="col-sm-6">
                <ColorTypeaheadLine ctx={tcs.subCtx(pp => pp.customColor)} onChange={() => forceUpdate()} />

              </div>
              <div className="col-sm-6">
                <ValueLine ctx={tcs.subCtx(pp => pp.interactionGroup)} valueHtmlAttributes={avoidDrag}
                  onRenderDropDownListItem={(io) => <span><span className="sf-dot" style={{ backgroundColor: colors[InteractionGroup.values().indexOf(io.value)] }} />{io.label}</span>} />
              </div>
            </div>

            
          </div>
        </div>
      </div>
    );

    return (
      <EntityGridItem title={title} customColor={tc.value.customColor ?? undefined}>
        <RenderEntity ctx={tc.subCtx(a => a.content)} extraProps={{ dashboard: ctx.value }} />
      </EntityGridItem>
    );
  }

  const ctx = p.ctx;
  const ctxBasic = ctx.subCtx({ formGroupStyle: "Basic" });
  return (
    <div>
      <div>
        <div className="row">
          <div className="col-sm-4">
            <ValueLine ctx={ctxBasic.subCtx(cp => cp.displayName)}
              helpText={<ValueLine ctx={ctxBasic.subCtx(cp => cp.hideDisplayName)} inlineCheckbox />} />
          </div>
          <div className="col-sm-2">
            <ValueLine ctx={ctxBasic.subCtx(cp => cp.dashboardPriority)} />
          </div>
          <div className="col-sm-2">
            <ValueLine ctx={ctxBasic.subCtx(cp => cp.autoRefreshPeriod)} />
          </div>
        </div>
        <div className="row">
          <div className="col-sm-4">
            <EntityLine ctx={ctxBasic.subCtx(cp => cp.owner)} create={false} />
          </div>
          <div className="col-sm-4">
            <EntityLine ctx={ctxBasic.subCtx(cp => cp.entityType)} onChange={handleEntityTypeChange} />
          </div>
          {ctxBasic.value.entityType && <div className="col-sm-4">
            <ValueLine ctx={ctxBasic.subCtx(f => f.embeddedInEntity)} />
          </div>}
        </div>
      </div>

      <EntityDetail ctx={ctxBasic.subCtx(cp => cp.cacheQueryConfiguration)}
        onChange={forceUpdate}
        onCreate={() => Promise.resolve(CacheQueryConfigurationEmbedded.New({ timeoutForQueries: 5 * 60, maxRows: 1000 * 1000 }))}
        getComponent={(ectx: TypeContext<CacheQueryConfigurationEmbedded>) => <div className="row">
          <div className="col-sm-2">
            <ValueLine ctx={ectx.subCtx(cp => cp.timeoutForQueries)} />
          </div>
          <div className="col-sm-2">
            <ValueLine ctx={ectx.subCtx(cp => cp.maxRows)} />
          </div>
          <div className="col-sm-2">
            <ValueLine ctx={ectx.subCtx(cp => cp.autoRegenerateWhenOlderThan)} />
          </div>
          <div className="col-sm-2">
            {!ctx.value.isNew && <SearchValueLine ctx={ectx} findOptions={{ queryName: CachedQueryEntity, filterOptions: [{ token: CachedQueryEntity.token(a => a.dashboard), value: ctxBasic.value }] }} />}
          </div>
          <div className="col-sm-3 pt-4">
            {!ctx.value.isNew && <OperationButton eoc={EntityOperationContext.fromTypeContext(ctx, DashboardOperation.RegenerateCachedQueries)} hideOnCanExecute className="w-100" />}
          </div>
        </div>} />

      <Tabs id={ctxBasic.getUniqueId("tabs")}>
        <Tab title={ctxBasic.niceName(a => a.parts)} eventKey="parts">
          <ValueLine ctx={ctxBasic.subCtx(cp => cp.combineSimilarRows)} inlineCheckbox={true} />
          <div className="sf-dashboard-admin">
            <EntityGridRepeater ctx={ctx.subCtx(cp => cp.parts)} getComponent={renderPart} onCreate={handleOnCreate} />
          </div>
        </Tab>
        <Tab title={ctxBasic.niceName(a => a.tokenEquivalencesGroups)} eventKey="equivalences">
          <EntityRepeater ctx={ctx.subCtx(a => a.tokenEquivalencesGroups, { formSize: "xs" })} avoidFieldSet getComponent={(ctxGr: TypeContext<TokenEquivalenceGroupEntity>) => 
            <div>
              <ValueLine ctx={ctxGr.subCtx(pp => pp.interactionGroup)}
                onRenderDropDownListItem={(io) => <span><span className="sf-dot" style={{ backgroundColor: colors[InteractionGroup.values().indexOf(io.value)] }} />{io.label}</span>} />
              <EntityTable ctx={ctxGr.subCtx(p => p.tokenEquivalences)} avoidFieldSet columns={EntityTable.typedColumns<TokenEquivalenceEmbedded>([
                {
                  property: p => p.query,
                  template: (ectx, row) => <EntityCombo ctx={ectx.subCtx(p => p.query)} data={allQueryNames} onChange={row.forceUpdate} />,
                  headerHtmlAttributes: { style: { width: "30%" } },
                },
                {
                  property: p => p.token,
                  template: (ectx) => ectx.value.query && <QueryTokenEntityBuilder ctx={ectx.subCtx(p => p.token)}
                    queryKey={ectx.value.query.key} subTokenOptions={SubTokensOptions.CanAggregate | SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll} />,
                  headerHtmlAttributes: { style: { width: "100%" } },
                },
              ])}
              />
            </div>
          }/>
        </Tab>

        </Tabs>
    </div>
  );
}

export function IsQueryCachedLine(p: { ctx: TypeContext<boolean> }) {
  const forceUpate = useForceUpdate();
  return <ValueLine ctx={p.ctx} label={<span className={classes("fw-bold", p.ctx.value ? "text-success" : "text-danger")}> {p.ctx.niceName()}</span>} inlineCheckbox="block" onChange={forceUpate} />
}

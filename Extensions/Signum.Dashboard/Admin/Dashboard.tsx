import * as React from 'react'
import { Tabs, Tab } from 'react-bootstrap'
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { EntityLine, RenderEntity, EntityDetail, EntityRepeater, EntityTable, EntityCombo, TextBoxLine, ColorLine, EnumLine, NumberLine, CheckboxLine, AutoLine } from '@framework/Lines'
import { tryGetTypeInfos, New, getTypeInfos } from '@framework/Reflection'
import SelectorModal from '@framework/SelectorModal'
import { TypeContext } from '@framework/TypeContext'
import { DashboardEntity, PanelPartEmbedded, IPartEntity, InteractionGroup, CacheQueryConfigurationEmbedded, CachedQueryEntity, DashboardOperation, TokenEquivalenceGroupEntity, TokenEquivalenceEmbedded, DashboardMessage } from '../Signum.Dashboard'
import { EntityGridRepeater, EntityGridItem } from './EntityGridRepeater'
import { DashboardClient } from "../DashboardClient";
import { fallbackIcon, iconToString, parseIcon } from "@framework/Components/IconTypeahead";
import "../Dashboard.css"
import { getToString, toLite } from '@framework/Signum.Entities';
import { useForceUpdate } from '@framework/Hooks'
import { SearchValueLine } from '@framework/Search';
import { Navigator, ViewPromise } from '@framework/Navigator';
import { classes } from '@framework/Globals';
import { EntityOperations, OperationButton } from '@framework/Operations/EntityOperations';
import { EntityOperationContext } from '@framework/Operations';
import QueryTokenEntityBuilder from '../../Signum.UserAssets/Templates/QueryTokenEmbeddedBuilder';
import { SubTokensOptions } from '@framework/FindOptions';
import { ToolbarEntity, ToolbarMenuEntity } from '../../Signum.Toolbar/Signum.Toolbar';
import CollapsableCard from '@framework/Components/CollapsableCard';
import { UserAssetMessage } from '../../Signum.UserAssets/Signum.UserAssets';

export default function Dashboard(p: { ctx: TypeContext<DashboardEntity> }): React.JSX.Element {
  const forceUpdate = useForceUpdate();
  const ctx4 = p.ctx.subCtx({ labelColumns: 4 })
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
        var icon = DashboardClient.icon(ti);
        return <><FontAwesomeIcon aria-hidden={true} icon={icon.icon} color={icon.iconColor} /><span className="ms-2">{ti.niceName}</span></>;
      }
    })
      .then(ti => {
        if (ti == undefined)
          return undefined;

        const part = New(ti.name) as any as IPartEntity;


        return PanelPartEmbedded.New({
          content: part,
          iconName: null,
          iconColor: null,
        });
      });
  }

  var colors = ["#DFFF00", "#FFBF00", "#FF7F50", "#DE3163", "#9FE2BF", "#40E0D0", "#6495ED", "#CCCCFF"]

  function renderPart(tc: TypeContext<PanelPartEmbedded>) {
    const tcs = tc.subCtx({ formGroupStyle: "SrOnly", formSize: "xs", placeholderLabels: true });

    var icon = parseIcon(tc.value.iconName) ?? "border-none";

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
            <button
              type="button"
              style={{ background: "none", border: "none", padding: 0 }}
              aria-label={DashboardMessage.SelectIcon.niceToString()}
              onClick={() => selectIcon(tc).then(a => {
                if (a) {
                  tc.value.iconName = a.iconName;
                  tc.value.iconColor = a.iconColor;
                  tc.value.titleColor = a.titleColor;
                  tc.value.modified = true;
                  forceUpdate();
                }
              })}>
              <FontAwesomeIcon aria-hidden={true} icon={fallbackIcon(icon)} style={{ color: ctx.value.iconColor ?? undefined, fontSize: "25px" }} {...avoidDrag as any} />
            </button>
          </div>}
          <div style={{ flexGrow: 1 }} className="me-2">

            <TextBoxLine ctx={tcs.subCtx(pp => pp.title)} label={getToString(tcs.value.content) ?? tcs.niceName(pp => pp.title)} valueHtmlAttributes={avoidDrag} />
            <div className="row">
              <div className="col-sm-6">
                <ColorLine ctx={tcs.subCtx(pp => pp.customColor)} onChange={() => forceUpdate()} />

              </div>
              <div className="col-sm-6">
                <EnumLine ctx={tcs.subCtx(pp => pp.interactionGroup)} valueHtmlAttributes={avoidDrag}
                  onRenderDropDownListItem={(io) => <span className="sf-dot-container"><span className="sf-dot" style={{ backgroundColor: colors[InteractionGroup.values().indexOf(io.value)] }} />{io.label}</span>} />
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
  const ctxLabel5 = ctx.subCtx({ labelColumns: 5 });
  const icon = parseIcon(ctx.value.iconName) ?? "border-none";

  return (
    <div>
      <div>
        <EntityLine ctx={ctx.subCtx(cp => cp.owner)} create={false} />
        <AutoLine ctx={ctx.subCtx(cp => cp.displayName)}
          helpText={<div className="d-flex">
            {icon && <div className="mx-2">
              <FontAwesomeIcon icon={icon} style={{ color: ctx.value.iconColor ?? undefined, fontSize: "25px", cursor: "pointer" }}
                onClick={() => selectIcon(ctx).then(a => {
                  if (a) {
                    ctx.value.iconName = a.iconName;
                    ctx.value.iconColor = a.iconColor;
                    ctx.value.titleColor = (a as DashboardEntity).titleColor;
                    ctx.value.modified = true;
                    forceUpdate();
                  }
                })} />
            </div>}
            <CheckboxLine ctx={ctx.subCtx(cp => cp.hideDisplayName)} inlineCheckbox />
          </div>} />


        <div className="row">
          <div className="col-sm-8">
            <EntityLine ctx={ctx.subCtx(cp => cp.entityType)} onChange={handleEntityTypeChange} labelColumns={3}
              helpText={ctx.value.entityType && <CheckboxLine ctx={ctx.subCtx(e => e.hideQuickLink)} inlineCheckbox />}
            />
          </div>
          {ctx.value.entityType && <div className="col-sm-4">
            <AutoLine ctx={ctxLabel5.subCtx(f => f.embeddedInEntity)} />
          </div>}
        </div>

        <CollapsableCard header={UserAssetMessage.Advanced.niceToString()} size="xs">

          <div className="row">
            <div className="col-sm-3 pt-3">
              <NumberLine ctx={ctxBasic.subCtx(cp => cp.dashboardPriority)} />
              <AutoLine ctx={ctxBasic.subCtx(cp => cp.autoRefreshPeriod)} />
            </div>
            {!ctx.value.isNew && <div className="col-sm-3">
              <h2 className="mt-3 h5">{UserAssetMessage.UsedBy.niceToString()}</h2>
              <SearchValueLine ctx={ctx4} findOptions={{ queryName: ToolbarMenuEntity, filterOptions: [{ token: ToolbarMenuEntity.token(a => a.entity.elements).any().append(a => a.content), value: ctx.value }] }} />
              <SearchValueLine ctx={ctx4} findOptions={{ queryName: ToolbarEntity, filterOptions: [{ token: ToolbarEntity.token(a => a.entity.elements).any().append(a => a.content), value: ctx.value }] }} />
            </div>
            }
            <div className="col-sm-6">

              <EntityDetail ctx={ctxBasic.subCtx(cp => cp.cacheQueryConfiguration)} avoidFieldSet="h5"
                onChange={forceUpdate}
                onCreate={() => Promise.resolve(CacheQueryConfigurationEmbedded.New({ timeoutForQueries: 5 * 60, maxRows: 1000 * 1000 }))}
                getComponent={ectx =>
                  <div>
                    <div className="row">
                      <div className="col-sm-4">
                        <AutoLine ctx={ectx.subCtx(cp => cp.timeoutForQueries)} />
                      </div>
                      <div className="col-sm-4">
                        <AutoLine ctx={ectx.subCtx(cp => cp.maxRows)} />
                      </div>
                      <div className="col-sm-4">
                        <AutoLine ctx={ectx.subCtx(cp => cp.autoRegenerateWhenOlderThan)} />
                      </div>
                    </div>
                    <div className="row">
                      <div className="col-sm-4">
                        {!ctx.value.isNew && <SearchValueLine ctx={ectx} findOptions={{ queryName: CachedQueryEntity, filterOptions: [{ token: CachedQueryEntity.token(a => a.dashboard), value: ctxBasic.value }] }} />}
                      </div>
                      <div className="col-sm-4 pt-4">
                        {!ctx.value.isNew && <OperationButton eoc={EntityOperationContext.fromTypeContext(ctx, DashboardOperation.RegenerateCachedQueries)} hideOnCanExecute className="w-100" />}
                      </div>
                    </div>
                  </div>} />
            </div>
          </div>

        </CollapsableCard>
      </div>



      <Tabs id={ctxBasic.getUniqueId("tabs")} className="mt-3">
        <Tab title={ctxBasic.niceName(a => a.parts)} eventKey="parts">
          <CheckboxLine ctx={ctxBasic.subCtx(cp => cp.combineSimilarRows)} inlineCheckbox={true} />
          <div className="sf-dashboard-admin">
            <EntityGridRepeater ctx={ctx.subCtx(cp => cp.parts)} getComponent={renderPart} onCreate={handleOnCreate} />
          </div>
        </Tab>
        <Tab title={ctxBasic.niceName(a => a.tokenEquivalencesGroups)} eventKey="equivalences">
          <EntityRepeater ctx={ctx.subCtx(a => a.tokenEquivalencesGroups, { formSize: "xs" })} avoidFieldSet getComponent={ctxGr =>
            <div>
              <EnumLine ctx={ctxGr.subCtx(pp => pp.interactionGroup)}
                onRenderDropDownListItem={(io) => <span className="sf-dot-container"><span className="sf-dot" style={{ backgroundColor: colors[InteractionGroup.values().indexOf(io.value)] }} />{io.label}</span>} />
              <EntityTable ctx={ctxGr.subCtx(p => p.tokenEquivalences)} avoidFieldSet columns={[
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
              ]}
              />
            </div>
          } />
        </Tab>

      </Tabs>
    </div>
  );

  function selectIcon(ctx: TypeContext<DashboardEntity | PanelPartEmbedded>) {
    return Navigator.view(ctx.value, {
      propertyRoute: ctx.propertyRoute,
      getViewPromise: e => new ViewPromise(import("./PanelIcon")),
      modalSize: "md",
      buttons: "ok_cancel",
      isOperationVisible: e => false,
      requiresSaveOperation: false,
    })
  }
}

export function IsQueryCachedLine(p: { ctx: TypeContext<boolean> }): React.JSX.Element {
  const forceUpate = useForceUpdate();
  return <CheckboxLine ctx={p.ctx} label={<span className={classes("fw-bold", p.ctx.value ? "text-success" : "text-danger")}> {p.ctx.niceName()}</span>} inlineCheckbox="block" onChange={forceUpate} />
}

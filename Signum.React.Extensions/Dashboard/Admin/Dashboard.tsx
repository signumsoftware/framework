
import * as React from 'react'
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { ValueLine, EntityLine, RenderEntity, OptionItem, EntityDetail } from '@framework/Lines'
import { tryGetTypeInfos, New, getTypeInfos } from '@framework/Reflection'
import SelectorModal from '@framework/SelectorModal'
import { TypeContext } from '@framework/TypeContext'
import { DashboardEntity, PanelPartEmbedded, IPartEntity, InteractionGroup, CacheQueryConfigurationEmbedded, CachedQueryEntity, DashboardOperation } from '../Signum.Entities.Dashboard'
import { EntityGridRepeater, EntityGridItem } from './EntityGridRepeater'
import * as DashboardClient from "../DashboardClient";
import { iconToString, IconTypeaheadLine, parseIcon } from "../../Basics/Templates/IconTypeahead";
import { ColorTypeaheadLine } from "../../Basics/Templates/ColorTypeahead";
import "../Dashboard.css"
import { getToString } from '@framework/Signum.Entities';
import { useForceUpdate } from '@framework/Hooks'
import { ValueSearchControlLine } from '../../../Signum.React/Scripts/Search';
import { withClassName } from '../../Dynamic/View/HtmlAttributesExpression';
import { classes } from '../../../Signum.React/Scripts/Globals';
import { OperationButton } from '../../../Signum.React/Scripts/Operations/EntityOperations';
import { EntityOperationContext } from '../../../Signum.React/Scripts/Operations';

export default function Dashboard(p: { ctx: TypeContext<DashboardEntity> }) {
  const forceUpdate = useForceUpdate();
  function handleEntityTypeChange() {
    if (!p.ctx.value.entityType)
      p.ctx.value.embeddedInEntity = null;

    forceUpdate()
  }


  function handleOnCreate() {
    const pr = DashboardEntity.memberInfo(a => a.parts![0].element.content);

    return SelectorModal.chooseType(getTypeInfos(pr.type))
      .then(ti => {
        if (ti == undefined)
          return undefined;

        const part = New(ti.name) as any as IPartEntity;

        const icon = DashboardClient.defaultIcon(part);

        return PanelPartEmbedded.New({
          content: part,
          iconName: iconToString(icon.icon),
          iconColor: icon.iconColor,
          style: "Light"
        });
      });
  }

  var colors = ["#DFFF00", "#FFBF00", "#FF7F50", "#DE3163", "#9FE2BF", "#40E0D0", "#6495ED", "#CCCCFF"]

  function renderPart(tc: TypeContext<PanelPartEmbedded>) {
    const tcs = tc.subCtx({ formGroupStyle: "SrOnly", formSize: "ExtraSmall", placeholderLabels: true });

    var icon = parseIcon(tc.value.iconName);

    const title = (
      <div>
        <div className="d-flex">
          {icon && <div className="mx-2"><FontAwesomeIcon icon={icon} style={{ color: tc.value.iconColor ?? undefined, fontSize: "25px", marginTop: "17px" }} /> </div>}
          <div style={{ flexGrow: 1 }} className="me-2">

            <div className="row">
              <div className="col-sm-8">
                <ValueLine ctx={tcs.subCtx(pp => pp.title)} labelText={getToString(tcs.value.content) ?? tcs.niceName(pp => pp.title)} />
              </div>
              <div className="col-sm-4">
                <ValueLine ctx={tcs.subCtx(pp => pp.interactionGroup)}
                  onRenderDropDownListItem={(io) => <span><span className="sf-dot" style={{ backgroundColor: colors[InteractionGroup.values().indexOf(io.value)] }} />{io.label}</span>} />
              </div>
            </div>

            <div className="row">
              <div className="col-sm-4">
                <ValueLine ctx={tcs.subCtx(pp => pp.style)} onChange={() => forceUpdate()} />
              </div>
              <div className="col-sm-4">
                <IconTypeaheadLine ctx={tcs.subCtx(t => t.iconName)} onChange={() => forceUpdate()} />
              </div>
              <div className="col-sm-4">
                <ColorTypeaheadLine ctx={tcs.subCtx(t => t.iconColor)} onChange={() => forceUpdate()} />
              </div>
            </div>
          </div>
        </div>
      </div>
    );

    return (
      <EntityGridItem title={title} bsStyle={tc.value.style}>
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
          <div className="col-sm-6">
            <ValueLine ctx={ctxBasic.subCtx(cp => cp.displayName)} />
          </div>
          <div className="col-sm-3">
            <ValueLine ctx={ctxBasic.subCtx(cp => cp.dashboardPriority)} />
          </div>
          <div className="col-sm-3">
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
          <div className="col-sm-3">
            <ValueLine ctx={ectx.subCtx(cp => cp.timeoutForQueries)} />
          </div>
          <div className="col-sm-3">
            <ValueLine ctx={ectx.subCtx(cp => cp.maxRows)} />
          </div>
          <div className="col-sm-2">
            {!ctx.value.isNew && <ValueSearchControlLine ctx={ectx} findOptions={{ queryName: CachedQueryEntity, filterOptions: [{ token: CachedQueryEntity.token(a => a.dashboard), value: ctxBasic.value }] }} />}
          </div>
          <div className="col-sm-2 pt-4">
            {!ctx.value.isNew && <OperationButton eoc={EntityOperationContext.fromTypeContext(ctx, DashboardOperation.RegenerateCachedQueries)} className="w-100" />}
          </div>
        </div>} />

      <ValueLine ctx={ctxBasic.subCtx(cp => cp.combineSimilarRows)} inlineCheckbox={true} />
      <div className="sf-dashboard-admin">
        <EntityGridRepeater ctx={ctx.subCtx(cp => cp.parts)} getComponent={renderPart} onCreate={handleOnCreate} />
      </div>
    </div>
  );
}

export function IsQueryCachedLine(p: { ctx: TypeContext<boolean> }) {
  const forceUpate = useForceUpdate();
  return <ValueLine ctx={p.ctx} labelText={<span className={classes("fw-bold", p.ctx.value ? "text-success" : "text-danger")}> {p.ctx.niceName()}</span>} inlineCheckbox="block" onChange={forceUpate} />
}

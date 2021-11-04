
import * as React from 'react'
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { ValueLine, EntityLine, RenderEntity, OptionItem } from '@framework/Lines'
import { tryGetTypeInfos, New, getTypeInfos } from '@framework/Reflection'
import SelectorModal from '@framework/SelectorModal'
import { TypeContext } from '@framework/TypeContext'
import { DashboardEntity, PanelPartEmbedded, IPartEntity, InteractionGroup } from '../Signum.Entities.Dashboard'
import { EntityGridRepeater, EntityGridItem } from './EntityGridRepeater'
import * as DashboardClient from "../DashboardClient";
import { iconToString, IconTypeaheadLine, parseIcon } from "../../Basics/Templates/IconTypeahead";
import { ColorTypeaheadLine } from "../../Basics/Templates/ColorTypeahead";
import "../Dashboard.css"
import { getToString } from '@framework/Signum.Entities';
import { useForceUpdate } from '@framework/Hooks'
import { softCast } from '../../../Signum.React/Scripts/Globals';

export default function Dashboard(p : { ctx: TypeContext<DashboardEntity> }){
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
          <div style={{ flexGrow: 1 }} className="mr-2">

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
        <RenderEntity ctx={tc.subCtx(a => a.content)} />
      </EntityGridItem>
    );
  }

  const ctx = p.ctx;
  const sc = ctx.subCtx({ formGroupStyle: "Basic" });
  return (
    <div>
      <div>
        <div className="row">
          <div className="col-sm-6">
            <ValueLine ctx={sc.subCtx(cp => cp.displayName)} />
          </div>
          <div className="col-sm-3">
            <ValueLine ctx={sc.subCtx(cp => cp.dashboardPriority)} />
          </div>
          <div className="col-sm-3">
            <ValueLine ctx={sc.subCtx(cp => cp.autoRefreshPeriod)} />
          </div>
        </div>
        <div className="row">
          <div className="col-sm-4">
            <EntityLine ctx={sc.subCtx(cp => cp.owner)} create={false} />
          </div>
          <div className="col-sm-4">
            <EntityLine ctx={sc.subCtx(cp => cp.entityType)} onChange={handleEntityTypeChange} />
          </div>
          {sc.value.entityType && <div className="col-sm-4">
            <ValueLine ctx={sc.subCtx(f => f.embeddedInEntity)} />
          </div>}
        </div>
      </div>
      <ValueLine ctx={sc.subCtx(cp => cp.combineSimilarRows)} inlineCheckbox={true} />
      <div className="sf-dashboard-admin">
      <EntityGridRepeater ctx={ctx.subCtx(cp => cp.parts)} getComponent={renderPart} onCreate={handleOnCreate} />
    </div>
    </div>
  );
}


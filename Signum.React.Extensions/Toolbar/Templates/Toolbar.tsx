import * as React from 'react'
import { ValueLine, EntityLine, EntityRepeater, EntityTable } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ToolbarEntity, ToolbarElementEmbedded } from '../Signum.Entities.Toolbar'
import * as Dashboard from '../../Dashboard/Admin/Dashboard';
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { fixToolbarElementType } from './ToolbarElement';
import { MList } from '@framework/Signum.Entities';

export default function Toolbar(p: { ctx: TypeContext<ToolbarEntity> }) {
  const ctx = p.ctx;
  const ctx3 = ctx.subCtx({ labelColumns: 3 });
  return (
    <div>
      <div className="row">
        <div className="col-sm-7">
          <ValueLine ctx={ctx3.subCtx(f => f.name)} />
          <EntityLine ctx={ctx3.subCtx(e => e.owner)} />
        </div>

        <div className="col-sm-5">
          <ValueLine ctx={ctx3.subCtx(f => f.location)} />
          <ValueLine ctx={ctx3.subCtx(e => e.priority)} />
        </div>
      </div>
      <ToolbarElementTable ctx={ctx3.subCtx(a => a.elements)} />
    </div>
  );
}

export function ToolbarElementTable({ ctx }: { ctx: TypeContext<MList<ToolbarElementEmbedded>> }) {
  return (
    <EntityTable ctx={ctx} view
      onCreate={() => Promise.resolve(ToolbarElementEmbedded.New({ type: "Item" }))}
      columns={EntityTable.typedColumns<ToolbarElementEmbedded>([
      {
        header: "Icon",
        headerHtmlAttributes: { style: { width: "5%" } },
        template: ctx => {
          var icon = Dashboard.parseIcon(ctx.value.iconName);
          var bgColor = (ctx.value.iconColor && ctx.value.iconColor.toLowerCase() == "white" ? "black" : undefined)
          return icon && <FontAwesomeIcon icon={icon} style={{ backgroundColor: bgColor, color: ctx.value.iconColor ?? undefined, fontSize: "25px" }} />
        },
      },
      { property: a => a.type, headerHtmlAttributes: { style: { width: "15%" } }, template: (ctx, row) => <ValueLine ctx={ctx.subCtx(a => a.type)} onChange={() => { fixToolbarElementType(ctx.value); row.forceUpdate(); }} /> },
      { property: a => a.content, headerHtmlAttributes: { style: { width: "30%" } }, template: ctx => ctx.value.type != "Divider" && <EntityLine ctx={ctx.subCtx(a => a.content)} /> },
      { property: a => a.label, headerHtmlAttributes: { style: { width: "25%" } }, template: ctx => ctx.value.type != "Divider" && <ValueLine ctx={ctx.subCtx(a => a.label)} /> },
      { property: a => a.url, headerHtmlAttributes: { style: { width: "25%" } }, template: ctx => ctx.value.type != "Divider" && <ValueLine ctx={ctx.subCtx(a => a.url)} /> },
    ])} />
  );

}

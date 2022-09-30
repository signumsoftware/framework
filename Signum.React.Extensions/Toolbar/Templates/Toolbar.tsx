import * as React from 'react'
import { ValueLine, EntityLine, EntityRepeater, EntityTable } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ToolbarEntity, ToolbarElementEmbedded, ToolbarMenuEntity } from '../Signum.Entities.Toolbar'
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { MList } from '@framework/Signum.Entities';
import { parseIcon } from '../../Basics/Templates/IconTypeahead';
import * as ToolbarClient from '../ToolbarClient';
import SelectorModal from '../../../Signum.React/Scripts/SelectorModal';
import { getTypeInfos, TypeInfo } from '@framework/Reflection';
import * as Finder from '@framework/Finder';
import * as Constructor from '@framework/Constructor';
import * as Navigator from '@framework/Navigator';
import { PermissionSymbol } from '../../Authorization/Signum.Entities.Authorization';
import { classes } from '../../../Signum.React/Scripts/Globals';
import { ToolbarCount } from '../QueryToolbarConfig';

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


function getDefaultIcon(ti: TypeInfo): ToolbarClient.IconColor | null {

  if (ti.name == ToolbarEntity.typeName)
    return ({ icon: "bars", iconColor: "#229954" });

  if (ti.name == ToolbarMenuEntity.typeName)
    return ({ icon: "bars", iconColor: "#52BE80" });

  if (ti.name == PermissionSymbol.typeName)
    return ({ icon: "key", iconColor: "#F1C40F" });

  var conf = ToolbarClient.configs[ti.name];
  if (conf == null || conf.length == 0)
    return null;

  return  conf.first().getDefaultIcon();
}

export function ToolbarElementTable({ ctx }: { ctx: TypeContext<MList<ToolbarElementEmbedded>> }) {

  function selectContentType(filter: (ti: TypeInfo) => boolean) {
    const pr = ctx.memberInfo(ml => ml[0].element.content);
    return SelectorModal.chooseType(getTypeInfos(pr.type).filter(filter), {
      size: "def" as any,
      buttonDisplay: ti => {
        var icon = getDefaultIcon(ti);

        if (icon == null)
          return ti.niceName;

        return <><FontAwesomeIcon icon={icon.icon} color={icon.iconColor} /><span className="ms-2">{ti.niceName}</span></>;
      }
    });
  }


  return (
    <EntityTable ctx={ctx} view
      onCreate={() => Promise.resolve(ToolbarElementEmbedded.New({ type: "Item" }))}
      columns={EntityTable.typedColumns<ToolbarElementEmbedded>([
        {
          header: "Icon",
          headerHtmlAttributes: { style: { width: "5%" } },
          template: ctx => {
            var icon = parseIcon(ctx.value.iconName);
            var bgColor = (ctx.value.iconColor && ctx.value.iconColor.toLowerCase() == "white" ? "black" : undefined)
            return icon && <div>
              <FontAwesomeIcon icon={icon} style={{ backgroundColor: bgColor, color: ctx.value.iconColor ?? undefined, fontSize: "25px" }} />
              {ctx.value.showCount && <ToolbarCount num={ctx.value.showCount == "Always" ? 0 : 1} />}
            </div>
        },
      },
        { property: a => a.type, headerHtmlAttributes: { style: { width: "15%" } }, template: (ctx, row) => <ValueLine ctx={ctx.subCtx(a => a.type)} onChange={() => { row.forceUpdate(); }} /> },
        {
          property: a => a.content, headerHtmlAttributes: { style: { width: "30%" } }, template: ctx => <EntityLine ctx={ctx.subCtx(a => a.content)}
            onFind={() => selectContentType(ti => Navigator.isFindable(ti)).then(ti => ti && Finder.find({ queryName: ti.name }))}
            onCreate={() => selectContentType(ti => Navigator.isCreable(ti)).then(ti => ti && Constructor.construct(ti.name))}
          />
        },
      { property: a => a.label, headerHtmlAttributes: { style: { width: "25%" } }, template: ctx => <ValueLine ctx={ctx.subCtx(a => a.label)} /> },
      { property: a => a.url, headerHtmlAttributes: { style: { width: "25%" } }, template: ctx => <ValueLine ctx={ctx.subCtx(a => a.url)} /> },
    ])} />
  );

}

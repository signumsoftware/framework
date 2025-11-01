import * as React from 'react'
import { AutoLine, EntityRepeater, EntityLine, EntityTable, FontAwesomeIcon } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ToolbarMenuEntity, ToolbarSwitcherEntity } from '../Signum.Toolbar'
import { ToolbarElementTable } from './Toolbar';
import { fallbackIcon, parseIcon } from '../../../Signum/React/Components/IconTypeahead';

export default function ToolbarSwitcher(p: { ctx: TypeContext<ToolbarSwitcherEntity> }): React.JSX.Element {
  const ctx = p.ctx;

  return (
    <div>
      <AutoLine ctx={ctx.subCtx(f => f.name)} />
      <AutoLine ctx={ctx.subCtx(f => f.owner)} />
      <EntityTable ctx={ctx.subCtx(a => a.options)} view
        columns={[
          {
            header: "Icon",
            headerHtmlAttributes: { style: { width: "10%" } },
            template: ctx => {
              var icon = parseIcon(ctx.value.iconName);
              var bgColor = (ctx.value.iconColor && ctx.value.iconColor.toLowerCase() == "var(--bs-body-bg)" ? "var(--bs-body-color)" : undefined)
              return icon && <div>
                <FontAwesomeIcon icon={fallbackIcon(icon)} style={{ backgroundColor: bgColor, color: ctx.value.iconColor ?? undefined, fontSize: "25px" }} />
              </div>
            },
          },
          {
            headerHtmlAttributes: { style: { width: "90%" } },
            property: a => a.toolbarMenu,
          },
        ]} />
    </div>
  );
}

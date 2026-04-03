import * as React from 'react';
import { TypeContext, AutoLine, CheckboxLine } from '@framework/Lines';
import { IconTypeaheadLine, parseIcon } from '@framework/Components/IconTypeahead';
import { DashboardEntity, PanelPartEmbedded } from '../Signum.Dashboard';
import { useForceUpdate } from '@framework/Hooks'
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { getToString } from '@framework/Signum.Entities';

export default function PanelIcon(p: { ctx: TypeContext<DashboardEntity | PanelPartEmbedded> }): React.JSX.Element {
  const forceUpdate = useForceUpdate();
  const ctx = p.ctx.subCtx({ formGroupStyle: "Basic", formSize: "xs" });
  const title = DashboardEntity.isInstance(ctx.value) ? ctx.value.displayName : (ctx.value.title ?? getToString(ctx.value.content));
  const titleColor = ctx.value.titleColor;

  var icon = parseIcon(p.ctx.value.iconName);

  return (
    <div>
      {icon &&
        <div className="mb-2">
          <FontAwesomeIcon aria-hidden={true} icon={icon} style={{ color: ctx.value.iconColor ?? undefined, fontSize: "25px" }} />
          &nbsp;<span style={{ color: titleColor ?? undefined }}>{title}</span>
        </div>}
      <IconTypeaheadLine ctx={ctx.subCtx(t => t.iconName)} onChange={() => forceUpdate()} />
      <AutoLine ctx={ctx.subCtx(t => t.iconColor)} onChange={() => forceUpdate()} />
      <AutoLine ctx={ctx.subCtx(t => t.titleColor)} onChange={() => forceUpdate()} />
    </div>
  );
}

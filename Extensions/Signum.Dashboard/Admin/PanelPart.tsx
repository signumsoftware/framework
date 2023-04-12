import * as React from 'react';
import { TypeContext, ValueLine } from '@framework/Lines';
import { IconTypeaheadLine, parseIcon } from '@framework/Components/IconTypeahead';
import { PanelPartEmbedded } from '../Signum.Dashboard';
import { useForceUpdate } from '@framework/Hooks'
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";

export default function PanelPart(p: { ctx: TypeContext<PanelPartEmbedded> }) {
  const ctx = p.ctx.subCtx({ formGroupStyle: "Basic", formSize: "xs" });
  const forceUpdate = useForceUpdate();

  var icon = parseIcon(p.ctx.value.iconName);
  return (

    <div className="row">
      <div className="col-sm-2">
        {icon && <FontAwesomeIcon icon={icon} style={{ color: ctx.value.iconColor ?? undefined, fontSize: "25px", marginTop: "30px", marginLeft: "25px" }} />}
      </div>
      <div className="col-sm-9">
        <IconTypeaheadLine ctx={ctx.subCtx(t => t.iconName)}  onChange={() => forceUpdate()} />
        <ValueLine ctx={ctx.subCtx(t => t.iconColor)} onChange={() => forceUpdate()} />
        <ValueLine ctx={ctx.subCtx(t => t.useIconColorForTitle)} onChange={() => forceUpdate()} inlineCheckbox="block" />
      </div>
    </div>
  );
}

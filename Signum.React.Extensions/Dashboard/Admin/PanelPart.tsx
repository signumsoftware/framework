import * as React from 'react';
import { TypeContext, ValueLine } from '../../../../Framework/Signum.React/Scripts/Lines';
import { ColorTypeaheadLine } from '../../Basics/Templates/ColorTypeahead';
import { IconTypeaheadLine, parseIcon } from '../../Basics/Templates/IconTypeahead';
import { PanelPartEmbedded } from '../Signum.Entities.Dashboard';
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
        <ColorTypeaheadLine ctx={ctx.subCtx(t => t.iconColor)} onChange={() => forceUpdate()} />
        <ValueLine ctx={ctx.subCtx(t => t.useIconColorForTitle)} onChange={() => forceUpdate()} inlineCheckbox="block" />
      </div>
    </div>
  );
}

import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { AutoLine, ColorLine, EnumLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { PanelPartEmbedded, InteractionGroup } from '../Signum.Dashboard'
import { useForceUpdate } from '@framework/Hooks'
import HtmlEditorLine from '../../Signum.HtmlEditor/HtmlEditorLine'
import { IconTypeaheadLine, parseIcon } from '@framework/Components/IconTypeahead'
import { getToString } from '@framework/Signum.Entities'
import * as React from 'react'

export default function PanelPart(p: { ctx: TypeContext<PanelPartEmbedded> }): React.JSX.Element {
  const forceUpdate = useForceUpdate();
  const ctx = p.ctx;
  const settingsCtx = ctx.subCtx({ formGroupStyle: "Basic" });

  const colors = ["#DFFF00", "#FFBF00", "#FF7F50", "#DE3163", "#9FE2BF", "#40E0D0", "#6495ED", "#CCCCFF"];
  
  const icon = parseIcon(ctx.value.iconName);
  const title = ctx.value.title ?? getToString(ctx.value.content);
  const titleColor = ctx.value.titleColor;

  return (
    <div>
      {icon && (
        <div className="mb-3 p-3 border rounded bg-tertiary">
          <div className="d-flex align-items-center">
            <FontAwesomeIcon 
              aria-hidden={true} 
              icon={icon} 
              style={{ color: ctx.value.iconColor ?? undefined, fontSize: "40px" }} 
            />
            <span className="ms-3" style={{ color: titleColor ?? undefined, fontSize: "18px", fontWeight: "500" }}>
              {title}
            </span>
          </div>
        </div>
      )}
      
      <IconTypeaheadLine ctx={settingsCtx.subCtx(pp => pp.iconName)} onChange={() => forceUpdate()} />
      
      <div className="row">
        <div className="col-sm-4">
          <ColorLine ctx={settingsCtx.subCtx(pp => pp.iconColor)} onChange={() => forceUpdate()} />
        </div>
        <div className="col-sm-4">
          <ColorLine ctx={settingsCtx.subCtx(pp => pp.titleColor)} onChange={() => forceUpdate()} />
        </div>
        <div className="col-sm-4">
          <ColorLine ctx={settingsCtx.subCtx(pp => pp.customColor)} onChange={() => forceUpdate()} />
        </div>
      </div>
      
      <EnumLine ctx={settingsCtx.subCtx(pp => pp.interactionGroup)}
        onRenderDropDownListItem={(io) => (
          <span className="sf-dot-container">
            <span className="sf-dot" style={{ backgroundColor: colors[InteractionGroup.values().indexOf(io.value)] }} />
            {io.label}
          </span>
        )} 
      />
      
      <HtmlEditorLine ctx={settingsCtx.subCtx(pp => pp.tooltip)} 
        labelIcon={<FontAwesomeIcon icon="language" title="Translation available" className="text-muted" />}
      />
    </div>
  );
}

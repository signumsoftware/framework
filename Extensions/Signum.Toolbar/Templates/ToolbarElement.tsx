import * as React from 'react'
import { AutoLine, EntityLine, TextBoxLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ToolbarElementEmbedded } from '../Signum.Toolbar'
import { IconTypeaheadLine, parseIcon } from '@framework/Components/IconTypeahead'
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { getToString } from '@framework/Signum.Entities'
import { useForceUpdate } from '@framework/Hooks'
import { ToolbarCount } from '../QueryToolbarConfig'
import { PermissionSymbol } from '@framework/Signum.Basics'

export default function ToolbarElement(p: { ctx: TypeContext<ToolbarElementEmbedded> }): React.JSX.Element {
  const forceUpdate = useForceUpdate();
  function handleTypeChanges() {
    var a = p.ctx.value;
    fixToolbarElementType(a);
    forceUpdate();
  }

  function handleContentChange() {
    const tbe = p.ctx.value;
    if (tbe.content && !PermissionSymbol.isLite(tbe.content)) {
      tbe.url = null;
    }
    forceUpdate();
  }

  const ctx = p.ctx;

  const ctx4 = ctx.subCtx({ labelColumns: 4 });
  const ctx2 = ctx.subCtx({ labelColumns: 2 });
  const ctx6 = ctx.subCtx({ labelColumns: 6 });
  const bgColor = (ctx4.value.iconColor && ctx4.value.iconColor.toLowerCase() == "var(--bs-body-bg)" ? "var(--bs-body-color)" : undefined);

  var content = ctx2.value.content;

  var icon = parseIcon(ctx4.value.iconName);

  return (
    <div>
      <div className="row">
        <div className="col-sm-5">
          <AutoLine ctx={ctx4.subCtx(t => t.type)} onChange={handleTypeChanges} />
        </div>
        <div className="col-sm-5 offset-sm-1">
          {ctx2.value.type != "Divider" && <EntityLine ctx={ctx2.subCtx(t => t.content)} onChange={handleContentChange} />}
        </div>
      </div>

      {ctx4.value.type != "Divider" &&
        <div className="row">
          <div className="col-sm-5">
            <IconTypeaheadLine ctx={ctx4.subCtx(t => t.iconName)} onChange={() => forceUpdate()} extraIcons={["none"]} />
            <AutoLine ctx={ctx4.subCtx(t => t.iconColor)} onChange={() => forceUpdate()} />
            {content && (content.EntityType == "UserQuery" || content.EntityType == "Query") && <AutoLine ctx={ctx4.subCtx(a => a.showCount)} onChange={() => forceUpdate()} />}
          </div>
          <div className="col-sm-1">
            {icon && <div style={{ marginTop: "17px" }}>
              <FontAwesomeIcon icon={icon} style={{ backgroundColor: bgColor, color: ctx4.value.iconColor || undefined, fontSize: "25px", }} />
              {ctx.value.showCount && <ToolbarCount showCount={ctx.value.showCount} num={ctx.value.showCount == "Always" ? 0 : 1} />}
            </div>
            }
          </div>
          <div className="col-sm-5">
            <TextBoxLine ctx={ctx2.subCtx(t => t.label)} valueHtmlAttributes={{ placeholder: getToString(content) || undefined }} />
            {(ctx2.value.type == "Header" || ctx2.value.type == "Item") && (ctx2.value.content == null || PermissionSymbol.isLite(ctx2.value.content)) && <AutoLine ctx={ctx2.subCtx(t => t.url)} />}
            {content && (content.EntityType == "UserQuery" || content.EntityType == "Query") &&
              <div>
                <AutoLine ctx={ctx6.subCtx(t => t.openInPopup)} />
                <AutoLine ctx={ctx6.subCtx(t => t.autoRefreshPeriod)} />
              </div>
            }
          </div>
        </div>
      }
    </div>
  );
}

function fixToolbarElementType(a: ToolbarElementEmbedded): void {
  if (a.type == "Divider") {
    a.iconName = null;
    a.content = null;
    a.label = null;
    a.url = null;
    a.modified = true;
  }
}

import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ToolbarElementEmbedded } from '../Signum.Entities.Toolbar'
import { ColorTypeaheadLine } from '../../Basics/Templates/ColorTypeahead'
import { IconTypeaheadLine } from '../../Basics/Templates/IconTypeahead'
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import * as Dashboard from '../../Dashboard/Admin/Dashboard'
import { PermissionSymbol } from '../../Authorization/Signum.Entities.Authorization';

export default class ToolbarElement extends React.Component<{ ctx: TypeContext<ToolbarElementEmbedded> }> {

  handleTypeChanges = () => {
    var a = this.props.ctx.value;
    if (a.type == "Divider") {
      a.iconName == null;
      a.content == null;
      a.label == null;
      a.modified = true;
    }
    this.forceUpdate();
  }

  handleContentChange = () => {
    const tbe = this.props.ctx.value;
    if (tbe.content && !PermissionSymbol.isLite(tbe.content)) {
      tbe.url = null;
    }
    this.forceUpdate();
  }

  render() {
    const ctx = this.props.ctx;

    const ctx4 = ctx.subCtx({ labelColumns: 4 });
    const ctx2 = ctx.subCtx({ labelColumns: 2 });
    const ctx6 = ctx.subCtx({ labelColumns: 6 });
    const bgColor = (ctx4.value.iconColor && ctx4.value.iconColor.toLowerCase() == "white" ? "black" : undefined);

    var content = ctx2.value.content;

    var icon = Dashboard.parseIcon(ctx4.value.iconName);
    
    return (
      <div>
        <div className="row">
          <div className="col-sm-5">
            <ValueLine ctx={ctx4.subCtx(t => t.type)} onChange={this.handleTypeChanges} />
          </div>
          <div className="col-sm-5 offset-sm-1">
            {ctx2.value.type != "Divider" && <EntityLine ctx={ctx2.subCtx(t => t.content)} onChange={this.handleContentChange} />}
          </div>
        </div>

        {ctx4.value.type != "Divider" &&
          <div className="row">
            <div className="col-sm-5">
              <IconTypeaheadLine ctx={ctx4.subCtx(t => t.iconName)} onChange={() => this.forceUpdate()} extraIcons={["none"].concat(content && content.EntityType == "UserQuery" ? ["count"] : [] as string[])} />
              <ColorTypeaheadLine ctx={ctx4.subCtx(t => t.iconColor)} onChange={() => this.forceUpdate()} />
            </div>
            <div className="col-sm-1">
              {icon && <FontAwesomeIcon icon={icon} style={{ backgroundColor: bgColor, color: ctx4.value.iconColor || undefined, fontSize: "25px", marginTop: "17px" }} />}
            </div>
            <div className="col-sm-5">
            <ValueLine ctx={ctx2.subCtx(t => t.label)} valueHtmlAttributes={{ placeholder: content && content.toStr || undefined }} />
            {(ctx2.value.type == "Header" || ctx2.value.type == "Item") && (ctx2.value.content == null || PermissionSymbol.isLite(ctx2.value.content)) && <ValueLine ctx={ctx2.subCtx(t => t.url)} />}
              {content && (content.EntityType == "UserQuery" || content.EntityType == "Query") &&
                <div>
                  <ValueLine ctx={ctx6.subCtx(t => t.openInPopup)} />
                  <ValueLine ctx={ctx6.subCtx(t => t.autoRefreshPeriod)} />
                </div>
              }
            </div>
          </div>
        }
      </div>
    );
  }
}

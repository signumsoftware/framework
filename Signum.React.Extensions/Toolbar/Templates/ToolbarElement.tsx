import * as React from 'react'
import { Tab, Tabs } from 'react-bootstrap'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater, EntityTabRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SubTokensOptions, QueryToken, QueryTokenType, hasAnyOrAll } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import { getToString, getMixin } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { ToolbarElementEntity } from '../Signum.Entities.Toolbar'
import { ColorTypeaheadLine } from './ColorTypeahead'
import { IconTypeaheadLine } from './IconTypeahead'

export default class ToolbarElement extends React.Component<{ ctx: TypeContext<ToolbarElementEntity> }, void> {

    handleTypeChanges = () => {
        var a = this.props.ctx.value;
        a.iconName == null;
        a.content == null;
        a.label == null;
        a.modified = true;
        this.forceUpdate();
    }

    render() {
        const ctx = this.props.ctx;

        const ctx4 = ctx.subCtx({ labelColumns: 4 });
        const ctx2 = ctx.subCtx({ labelColumns: 2 });

        return (
            <div>
                <div className="row">
                    <div className="col-sm-5">
                        <ValueLine ctx={ctx4.subCtx(t => t.type)} onChange={this.handleTypeChanges} />
                    </div>
                    <div className="col-sm-5 col-sm-offset-1">
                        {ctx2.value.type != "Divider" && <EntityLine ctx={ctx2.subCtx(t => t.content)} onChange={() => this.forceUpdate()} />}
                    </div>
                </div>

                <div className="row">
                    <div className="col-sm-5">
                        {ctx4.value.type != "Divider" && <IconTypeaheadLine ctx={ctx4.subCtx(t => t.iconName)} onChange={() => this.forceUpdate()} extraIcons={["none"].concat(ctx.value.content && ctx.value.content.EntityType == "UserQuery" ? ["count"] : [] as string[])} />}
                        {ctx4.value.type != "Divider" && <ColorTypeaheadLine ctx={ctx4.subCtx(t => t.iconColor)} onChange={() => this.forceUpdate()}/>}
                    </div>
                    <div className="col-sm-1">
                        {ctx4.value.iconName && <span className={ctx4.value.iconName} style={{ color: ctx4.value.iconColor, fontSize: "25px", marginTop: "17px" }} />}
                    </div>
                    <div className="col-sm-5">
                        {ctx2.value.type != "Divider" && <ValueLine ctx={ctx2.subCtx(t => t.label)} valueHtmlProps={{ placeholder: ctx2.value.content && ctx2.value.content.toStr || undefined }} />}
                    </div>
                </div>
            </div>
        );
    }
}





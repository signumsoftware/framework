import * as React from 'react'
import { Tab, Tabs } from 'react-bootstrap'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater, EntityTabRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SubTokensOptions, QueryToken, QueryTokenType, hasAnyOrAll } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import { getToString, getMixin } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { ToolbarElementEntity } from '../Signum.Entities.Toolbar'
import { TemplateTokenMessage } from '../../Templating/Signum.Entities.Templating'
import FileLine from '../../Files/FileLine'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'


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
        
        return (
            <div>
                <div className="row">
                    <div className="col-sm-4">
                        <ValueLine ctx={ctx.subCtx(f => f.type)} labelColumns={4} onChange={this.handleTypeChanges} />
                        {ctx.value.type != "Divider" && <ValueLine ctx={ctx.subCtx(f => f.iconName)} labelColumns={4} />}
                    </div>

                    <div className="col-sm-8">
                        {ctx.value.type != "Divider" && <EntityLine ctx={ctx.subCtx(f => f.content)} />}
                        {ctx.value.type != "Divider" && <ValueLine ctx={ctx.subCtx(f => f.label)} valueHtmlProps={{ placeholder: ctx.value.content && ctx.value.content.toStr || undefined }} />}
                    </div>
                </div>
            </div>
        );
    }
}

import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater, EntityTabRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SubTokensOptions, QueryToken, QueryTokenType, hasAnyOrAll } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import { getToString, getMixin } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { TimeSpanEmbedded } from '../Signum.Entities.Basics'
import { TemplateTokenMessage } from '../../Templating/Signum.Entities.Templating'
import FileLine from '../../Files/FileLine'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import TemplateControls from '../../Templating/TemplateControls'
import ValueLineModal from '../../../../Framework/Signum.React/Scripts/ValueLineModal'

export default class TimeSpan extends React.Component<{ ctx: TypeContext<TimeSpanEmbedded> }> {

    render() {

        const e = this.props.ctx;
        const sc = e.subCtx({ formGroupStyle: "BasicDown" });

        return (
            <div className="row">
                <div className="col-sm-3">
                    <ValueLine ctx={sc.subCtx(n => n.days)} />
                </div>
                <div className="col-sm-3">
                    <ValueLine ctx={sc.subCtx(n => n.hours)} />
                </div>
                <div className="col-sm-3">
                    <ValueLine ctx={sc.subCtx(n => n.minutes)} />
                </div>
                <div className="col-sm-3">
                    <ValueLine ctx={sc.subCtx(n => n.seconds)} />
                </div>
            </div>
        );
    }
}

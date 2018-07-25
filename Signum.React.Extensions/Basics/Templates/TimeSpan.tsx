import * as React from 'react'
import { classes } from '@framework/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater, EntityTabRepeater } from '@framework/Lines'
import { SubTokensOptions, QueryToken, QueryTokenType, hasAnyOrAll } from '@framework/FindOptions'
import { SearchControl } from '@framework/Search'
import { getToString, getMixin } from '@framework/Signum.Entities'
import { TypeContext, FormGroupStyle } from '@framework/TypeContext'
import { TimeSpanEmbedded } from '../Signum.Entities.Basics'
import { TemplateTokenMessage } from '../../Templating/Signum.Entities.Templating'
import FileLine from '../../Files/FileLine'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import TemplateControls from '../../Templating/TemplateControls'
import ValueLineModal from '@framework/ValueLineModal'

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

import * as React from 'react'
import { classes } from '@framework/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater, EntityTabRepeater } from '@framework/Lines'
import { SubTokensOptions, QueryToken, QueryTokenType, hasAnyOrAll } from '@framework/FindOptions'
import { SearchControl } from '@framework/Search'
import { getToString, getMixin } from '@framework/Signum.Entities'
import { TypeContext, FormGroupStyle } from '@framework/TypeContext'
import { DateSpanEmbedded } from '../Signum.Entities.Basics'
import { TemplateTokenMessage } from '../../Templating/Signum.Entities.Templating'
import FileLine from '../../Files/FileLine'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import TemplateControls from '../../Templating/TemplateControls'
import ValueLineModal from '@framework/ValueLineModal'

export default class DateSpan extends React.Component<{ ctx: TypeContext<DateSpanEmbedded> }> {

    render() {

        const e = this.props.ctx;
        const sc = e.subCtx({ formGroupStyle: "BasicDown" });

        return (
            <div className="row">
                <div className="col-sm-4">
                    <ValueLine ctx={sc.subCtx(n => n.years)} />
                </div>
                <div className="col-sm-4">
                    <ValueLine ctx={sc.subCtx(n => n.months)} />
                </div>
                <div className="col-sm-4">
                    <ValueLine ctx={sc.subCtx(n => n.days)} />
                </div>
            </div>
        );
    }
}

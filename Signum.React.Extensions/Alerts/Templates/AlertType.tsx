import * as React from 'react'
import { classes } from '@framework/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater, EntityTabRepeater } from '@framework/Lines'
import { SearchControl } from '@framework/Search'
import { getToString, getMixin } from '@framework/Signum.Entities'
import { TypeContext, FormGroupStyle } from '@framework/TypeContext'
import { AlertTypeEntity } from '../Signum.Entities.Alerts'
import { TemplateTokenMessage } from '../../Templating/Signum.Entities.Templating'
import FileLine from '../../Files/FileLine'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import TemplateControls from '../../Templating/TemplateControls'
import ValueLineModal from '@framework/ValueLineModal'

export default class AlertType extends React.Component<{ ctx: TypeContext<AlertTypeEntity> }> {

    render() {
        const ctx = this.props.ctx;
        const ctx4 = ctx.subCtx({ labelColumns: 2 });
        return (
            <div>
                <ValueLine ctx={ctx4.subCtx(n => n.name)} />
            </div>
        );
    }
}
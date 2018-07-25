import * as React from 'react'
import { classes } from '@framework/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater, EntityTabRepeater } from '@framework/Lines'
import { SubTokensOptions, QueryToken, QueryTokenType, hasAnyOrAll } from '@framework/FindOptions'
import { SearchControl } from '@framework/Search'
import { getToString, getMixin } from '@framework/Signum.Entities'
import { TypeContext, FormGroupStyle } from '@framework/TypeContext'
import { AlertEntity, AlertState } from '../Signum.Entities.Alerts'
import { TemplateTokenMessage } from '../../Templating/Signum.Entities.Templating'
import FileLine from '../../Files/FileLine'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import TemplateControls from '../../Templating/TemplateControls'
import ValueLineModal from '@framework/ValueLineModal'

export default class Alert extends React.Component<{ ctx: TypeContext<AlertEntity> }> {

    render() {

        const e = this.props.ctx;

        const ec = e.subCtx({ labelColumns: { sm: 2 } });
        const sc = ec.subCtx({ formGroupStyle: "Basic" });


        return (
            <div>
                {!ec.value.isNew &&
                    <div>
                        <EntityLine ctx={ec.subCtx(e => e.createdBy)} readOnly={true} />
                        <ValueLine ctx={ec.subCtx(e => e.creationDate)} readOnly={true} />
                    </div>
                }
                {ec.value.target && <EntityLine ctx={ec.subCtx(n => n.target)} readOnly={true} />}
                <EntityLine ctx={ec.subCtx(n => n.recipient)} />
                <hr />
                <ValueLine ctx={ec.subCtx(n => n.title)} />
                <EntityCombo ctx={ec.subCtx(n => n.alertType)} />
                <ValueLine ctx={ec.subCtx(n => n.alertDate)} />
                <ValueLine ctx={ec.subCtx(n => n.text)} valueHtmlAttributes={{ style: { height: "180px" } }} />
                {ec.value.state == "Attended" &&
                    <div>
                        <hr />
                        <ValueLine ctx={ec.subCtx(e => e.attendedDate)} readOnly={true} />
                        <EntityLine ctx={ec.subCtx(e => e.attendedBy)} readOnly={true} />
                    </div>
                }
            </div>
        );
    }
}
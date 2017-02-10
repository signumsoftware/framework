import * as React from 'react'
import { Tab, Tabs } from 'react-bootstrap'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater, EntityTabRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SubTokensOptions, QueryToken, QueryTokenType, hasAnyOrAll } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import { getToString, getMixin } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { AlertEntity, AlertState } from '../Signum.Entities.Alerts'
import { TemplateTokenMessage } from '../../Templating/Signum.Entities.Templating'
import FileLine from '../../Files/FileLine'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import TemplateControls from '../../Templating/TemplateControls'
import ValueLineModal from '../../../../Framework/Signum.React/Scripts/ValueLineModal'

export default class Alert extends React.Component<{ ctx: TypeContext<AlertEntity> }, void> {

    render() {

        const e = this.props.ctx;

        const ec = e.subCtx({ labelColumns: { sm: 4 } });
        const sc = ec.subCtx({ formGroupStyle: "Basic" });


        return (
            <div>
                {!ec.value.isNew &&
                    <div>
                        <EntityLine ctx={ec.subCtx(e => e.createdBy)} readOnly={true} />
                        <ValueLine ctx={ec.subCtx(e => e.creationDate)} readOnly={true} />
                    </div>
                }
                <EntityLine ctx={ec.subCtx(n => n.target)} readOnly={true} />
                <hr />
                <ValueLine ctx={ec.subCtx(n => n.title)} />
                <EntityCombo ctx={ec.subCtx(n => n.alertType)} />
                <ValueLine ctx={ec.subCtx(n => n.alertDate)} />
                <ValueLine ctx={ec.subCtx(n => n.text)} valueLineType={ValueLineType.TextArea} valueHtmlProps={{ style: { height: "180px" } }} />
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

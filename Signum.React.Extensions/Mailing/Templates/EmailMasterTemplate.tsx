import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTabRepeater} from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl }  from '../../../../Framework/Signum.React/Scripts/Search'
import { getToString }  from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { EmailMasterTemplateEntity, EmailMasterTemplateMessageEntity } from '../Signum.Entities.Mailing'

export default class EmailMasterTemplate extends React.Component<{ ctx: TypeContext<EmailMasterTemplateEntity> }, void> {

    render() {

        var e = this.props.ctx;

        return (
            <div>
                <ValueLine ctx={e.subCtx(f => f.name) }  />
                <EntityTabRepeater ctx={e.subCtx(f => f.messages)} getComponent={this.renderMessage}/>
            </div>
        );
    }

    renderMessage = (ec: TypeContext<EmailMasterTemplateMessageEntity>) => {
        return (
            <div className="sf-email-template-message">
                <input type="hidden" className="sf-tab-title" value={getToString(ec.value.cultureInfo) } />

                <EntityCombo ctx={ec.subCtx(e => e.cultureInfo) }  onChange={() => this.forceUpdate() } />
                <div className="sf-template-message-insert-container">
                    <input type="button" className="sf-button sf-master-template-insert-content" value="@(EmailTemplateViewMessage.InsertMessageContent.NiceToString())" />
                </div>

                <ValueLine ctx={ec.subCtx(e => e.text) }  formGroupStyle={"None"} valueLineType={ValueLineType.TextArea} valueHtmlProps={{ style: { width: "100%", height: "180px;" }, className: "sf-rich-text-editor sf-email-template-message-text" }} />
            </div>
        );
    };
}


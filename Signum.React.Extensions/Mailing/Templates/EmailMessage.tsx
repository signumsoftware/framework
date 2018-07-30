import * as React from 'react'
import { classes } from '@framework/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater, EntityTabRepeater } from '@framework/Lines'
import { SearchControl } from '@framework/Search'
import { getToString, getMixin } from '@framework/Signum.Entities'
import { TypeContext, FormGroupStyle } from '@framework/TypeContext'
import {
    EmailMessageEntity, EmailAddressEmbedded, EmailRecipientEntity, EmailAttachmentEmbedded,
    EmailReceptionMixin, EmailFileType
} from '../Signum.Entities.Mailing'
import { EmailTemplateEntity, EmailTemplateContactEmbedded, EmailTemplateRecipientEntity, EmailTemplateMessageEmbedded, EmailTemplateViewMessage, EmailTemplateMessage } from '../Signum.Entities.Mailing'
import FileLine from '../../Files/FileLine'
import IFrameRenderer from './IFrameRenderer'
import HtmlCodemirror from '../../Codemirror/HtmlCodemirror'
import { tryGetMixin } from "@framework/Signum.Entities";
import { UncontrolledTabs, Tab } from '@framework/Components';


export default class EmailMessage extends React.Component<{ ctx: TypeContext<EmailMessageEntity> }> {

    render() {

        let e = this.props.ctx;

        if (e.value.state != "Created")
            e = e.subCtx({ readOnly: true });

        const sc4 = e.subCtx({ labelColumns: { sm: 4 } });
        const sc1 = e.subCtx({ labelColumns: { sm: 1 } });

        return (
            <UncontrolledTabs id="emailTabs">
                <Tab title={EmailMessageEntity.niceName()} eventKey="mainTab">
                    <fieldset>
                        <legend>Properties</legend>
                        <div className="row">
                            <div className="col-sm-5">
                                <ValueLine ctx={sc4.subCtx(f => f.state)} />
                                <ValueLine ctx={sc4.subCtx(f => f.sent)} />
                                <ValueLine ctx={sc4.subCtx(f => f.bodyHash)} />
                            </div>
                            <div className="col-sm-7">
                                <EntityLine ctx={e.subCtx(f => f.template)} />
                                <EntityLine ctx={e.subCtx(f => f.package)} />
                                <EntityLine ctx={e.subCtx(f => f.exception)} />
                            </div>
                        </div>
                    </fieldset>


                    <div className="form-inline">
                        <EntityDetail ctx={e.subCtx(f => f.from)} />
                        <EntityRepeater ctx={e.subCtx(f => f.recipients)} />
                        <EntityRepeater ctx={e.subCtx(f => f.attachments)} getComponent={this.renderAttachment} />
                    </div>

                    <EntityLine ctx={sc1.subCtx(f => f.target)} />
                    <ValueLine ctx={sc1.subCtx(f => f.subject)} />
                    <ValueLine ctx={sc1.subCtx(f => f.isBodyHtml)} inlineCheckbox={true} onChange={() => this.forceUpdate()} />
                    {sc1.value.state != "Created" ? <IFrameRenderer style={{ width: "100%" }} html={e.value.body} /> :
                        sc1.value.isBodyHtml ? <div className="code-container"><HtmlCodemirror ctx={e.subCtx(f => f.body)} /></div> :
                            <div>
                                <ValueLine ctx={e.subCtx(f => f.body)} valueLineType="TextArea" valueHtmlAttributes={{ style: { height: "180px" } }} formGroupStyle="SrOnly" />
                            </div>
                    }
                    <EmailMessageComponent ctx={e} invalidate={() => this.forceUpdate()} />
                </Tab>
                {this.renderEmailReceptionMixin()}
            </UncontrolledTabs>
        );
    }


    renderEmailReceptionMixin(){
        
        var erm = tryGetMixin(this.props.ctx.value, EmailReceptionMixin);
        if (!erm || !erm.receptionInfo)
            return null;

        const ri = this.props.ctx.subCtx(EmailReceptionMixin).subCtx(a => a.receptionInfo!);

        return (
            <Tab title={EmailReceptionMixin.niceName()} eventKey="receptionMixin">
                <fieldset>
                    <legend>Properties</legend>

                    <EntityLine ctx={ri.subCtx(f => f.reception)} />
                    <ValueLine ctx={ri.subCtx(f => f.uniqueId)} />
                    <ValueLine ctx={ri.subCtx(f => f.sentDate)} />
                    <ValueLine ctx={ri.subCtx(f => f.receivedDate)} />
                    <ValueLine ctx={ri.subCtx(f => f.deletionDate)} />

                </fieldset>

                <pre>{ri.value.rawContent}</pre>
            </Tab>
        );
    };


    renderAttachment = (ec: TypeContext<EmailAttachmentEmbedded>) => {
        const sc = ec.subCtx({ formGroupStyle: "SrOnly" });
        return (
            <div>
                <FileLine ctx={ec.subCtx(a => a.file)} remove={false}
                    fileType={EmailFileType.Attachment} />
            </div>
        );
    };
}

export interface EmailMessageComponentProps {
    ctx: TypeContext<EmailMessageEntity>;
    invalidate: () => void;
}

export class EmailMessageComponent extends React.Component<EmailMessageComponentProps, { showPreview: boolean }>{
    constructor(props: EmailMessageComponentProps) {
        super(props);
        this.state = { showPreview: false }
    }

    handlePreviewClick = (e: React.FormEvent<any>) => {
        e.preventDefault();
        this.setState({
            showPreview: !this.state.showPreview
        });
    }

    handleCodeMirrorChange = () => {
        if (this.state.showPreview)
            this.forceUpdate();
    }


    render() {
        const ec = this.props.ctx.subCtx({ labelColumns: { sm: 2 } });
        return (
            <div className="sf-email-template-message">
                <div>
                    <br />
                    <a href="#" onClick={this.handlePreviewClick}>
                        {this.state.showPreview ?
                            EmailTemplateMessage.HidePreview.niceToString() :
                            EmailTemplateMessage.ShowPreview.niceToString()}
                    </a>
                    {this.state.showPreview && <IFrameRenderer style={{ width: "100%", height: "150px" }} html={ec.value.body} />}
                </div>
            </div>
        );
    }
}

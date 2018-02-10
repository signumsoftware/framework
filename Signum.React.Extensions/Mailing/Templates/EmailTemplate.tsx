import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater, EntityTabRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SubTokensOptions, QueryToken, QueryTokenType, hasAnyOrAll } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import { getToString, getMixin } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { EmailTemplateEntity, EmailTemplateContactEmbedded, EmailTemplateRecipientEntity, EmailTemplateMessageEmbedded, EmailTemplateViewMessage, EmailTemplateMessage } from '../Signum.Entities.Mailing'
import { TemplateTokenMessage, TemplateApplicableEval } from '../../Templating/Signum.Entities.Templating'
import FileLine from '../../Files/FileLine'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import TemplateControls from '../../Templating/TemplateControls'
import HtmlCodemirror from '../../Codemirror/HtmlCodemirror'
import IFrameRenderer from './IFrameRenderer'
import ValueLineModal from '../../../../Framework/Signum.React/Scripts/ValueLineModal'
import TemplateApplicable from '../../Templating/Templates/TemplateApplicable';


export default class EmailTemplate extends React.Component<{ ctx: TypeContext<EmailTemplateEntity> }> {

    render() {

        const ctx = this.props.ctx;
        const ctx3 = ctx.subCtx({ labelColumns: { sm: 3 } });

        return (
            <div>

                <ValueLine ctx={ctx3.subCtx(e => e.name)} />
                <EntityCombo ctx={ctx3.subCtx(e => e.systemEmail)} />
                <EntityLine ctx={ctx3.subCtx(e => e.query)} onChange={() => this.forceUpdate()}
                    remove={ctx.value.from == undefined &&
                        (ctx.value.recipients == null || ctx.value.recipients.length == 0) &&
                        (ctx.value.messages == null || ctx.value.messages.length == 0)} />
                <div className="row">
                    <div className="col-sm-4">
                        <ValueLine ctx={ctx3.subCtx(e => e.editableMessage)} inlineCheckbox={true} />
                    </div>
                    <div className="col-sm-4">
                        <ValueLine ctx={ctx3.subCtx(e => e.disableAuthorization)} inlineCheckbox={true} />
                    </div>
                    <div className="col-sm-4">
                        <ValueLine ctx={ctx3.subCtx(e => e.sendDifferentMessages)} inlineCheckbox={true} />
                    </div>
                </div>
                {ctx3.value.query && <EntityDetail ctx={ctx3.subCtx(e => e.applicable)}
                    getComponent={(ec2: TypeContext<TemplateApplicableEval>) => <TemplateApplicable ctx={ec2} query={ctx.value.query!} />} />}

                {ctx3.value.query && this.renderQueryPart()}
            </div>
        );
    }

    renderQueryPart() {
        const ec = this.props.ctx.subCtx({ labelColumns: { sm: 2 } });
        const ecXs = ec.subCtx({ formSize: "ExtraSmall" });
        return (
            <div>
                <EntityDetail ctx={ecXs.subCtx(e => e.from)} onChange={() => this.forceUpdate()} getComponent={this.renderContact} />
                <EntityRepeater ctx={ecXs.subCtx(e => e.recipients)} onChange={() => this.forceUpdate()} getComponent={this.renderRecipient} />
                <EntityRepeater ctx={ecXs.subCtx(e => e.attachments)} />
                <EntityLine ctx={ec.subCtx(e => e.masterTemplate)} />
                <ValueLine ctx={ec.subCtx(e => e.isBodyHtml)} />

                <div className="sf-email-replacements-container">
                    <EntityTabRepeater ctx={ec.subCtx(a => a.messages)} onChange={() => this.forceUpdate()} getComponent={(ctx: TypeContext<EmailTemplateMessageEmbedded>) =>
                        <EmailTemplateMessageComponent ctx={ctx} queryKey={ec.value.query!.key!} invalidate={() => this.forceUpdate()} />} />
                </div>
            </div>
        );
    }

    renderContact = (ec: TypeContext<EmailTemplateContactEmbedded>) => {

        const sc = ec.subCtx({ formGroupStyle: "Basic" });

        return (
            <div>
                <div className="row">
                    <div className="col-sm-2" >
                        <FormGroup labelText={EmailTemplateEntity.nicePropertyName(a => a.recipients![0].element.kind)} ctx={sc}>
                            <span className="form-control">{EmailTemplateEntity.nicePropertyName(a => a.from)} </span>
                        </FormGroup>
                    </div>
                    <div className="col-sm-5">
                        <ValueLine ctx={sc.subCtx(c => c.emailAddress)} />
                    </div>
                    <div className="col-sm-5">
                        <ValueLine ctx={sc.subCtx(c => c.displayName)} />
                    </div>
                </div>
                {this.props.ctx.value.query &&
                    <QueryTokenEntityBuilder
                        ctx={ec.subCtx(a => a.token)}
                        queryKey={this.props.ctx.value.query.key}
                        subTokenOptions={SubTokensOptions.CanElement} />
                }
            </div>
        );
    };

    renderRecipient = (ec: TypeContext<EmailTemplateRecipientEntity>) => {

        const sc = ec.subCtx({ formGroupStyle: "Basic" });

        return (
            <div>
                <div className="row">
                    <div className="col-sm-2">
                        <label>
                            <ValueLine ctx={sc.subCtx(c => c.kind)} />
                        </label>
                    </div>
                    <div className="col-sm-5">
                        <ValueLine ctx={sc.subCtx(c => c.emailAddress)} />
                    </div>
                    <div className="col-sm-5">
                        <ValueLine ctx={sc.subCtx(c => c.displayName)} />
                    </div>
                </div>
                {this.props.ctx.value.query &&
                    <QueryTokenEntityBuilder
                        ctx={ec.subCtx(a => a.token)}
                        queryKey={this.props.ctx.value.query.key}
                        subTokenOptions={SubTokensOptions.CanElement} />
                }
            </div>
        );
    };
}

export interface EmailTemplateMessageComponentProps {
    ctx: TypeContext<EmailTemplateMessageEmbedded>;
    queryKey: string;
    invalidate: () => void;
}

export class EmailTemplateMessageComponent extends React.Component<EmailTemplateMessageComponentProps, { showPreview: boolean }>{
    constructor(props: EmailTemplateMessageComponentProps) {
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
                <EntityCombo ctx={ec.subCtx(e => e.cultureInfo)} labelText={EmailTemplateViewMessage.Language.niceToString()} onChange={this.props.invalidate} />
                <div>
                    <TemplateControls queryKey={this.props.queryKey} onInsert={this.handleOnInsert} forHtml={true} />
                    <ValueLine ctx={ec.subCtx(e => e.subject)} formGroupStyle={"SrOnly"} placeholderLabels={true} labelHtmlAttributes={{ style: { width: "100px" } }} />
                    <div className="code-container">
                        <HtmlCodemirror ctx={ec.subCtx(e => e.text)} onChange={this.handleCodeMirrorChange} />
                    </div>
                    <br />
                    <a href="#" onClick={this.handlePreviewClick}>
                        {this.state.showPreview ?
                            EmailTemplateMessage.HidePreview.niceToString() :
                            EmailTemplateMessage.ShowPreview.niceToString()}
                    </a>
                    {this.state.showPreview && <IFrameRenderer style={{ width: "100%" }} html={ec.value.text} />}
                </div>
            </div>
        );
    }

    handleOnInsert = (newCode: string) => {
        ValueLineModal.show({
            type: { name: "string" },
            initialValue: newCode,
            title: "Template",
            message: "Copy to clipboard: Ctrl+C, ESC",
            initiallyFocused: true,
        }).done();
    }
}

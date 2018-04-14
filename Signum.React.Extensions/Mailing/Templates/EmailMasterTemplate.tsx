import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTabRepeater} from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl }  from '../../../../Framework/Signum.React/Scripts/Search'
import { getToString }  from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { EmailMasterTemplateEntity, EmailMasterTemplateMessageEmbedded, EmailTemplateViewMessage, EmailTemplateMessage } from '../Signum.Entities.Mailing'
import TemplateControls from '../../Templating/TemplateControls'
import HtmlCodemirror from '../../Codemirror/HtmlCodemirror'
import IFrameRenderer from './IFrameRenderer'

export default class EmailMasterTemplate extends React.Component<{ ctx: TypeContext<EmailMasterTemplateEntity> }> {

    render() {

        const e = this.props.ctx;

        return (
            <div>
                <ValueLine ctx={e.subCtx(f => f.name)} />
                <EntityTabRepeater ctx={e.subCtx(a => a.messages)} onChange={() => this.forceUpdate()} getComponent={(ctx: TypeContext<EmailMasterTemplateMessageEmbedded>) =>
                    <EmailTemplateMessageComponent ctx={ctx} invalidate={() => this.forceUpdate() } /> } />
            </div>
        );
    }
}

export interface EmailMasterTemplateMessageComponentProps {
    ctx: TypeContext<EmailMasterTemplateMessageEmbedded>;
    invalidate: () => void;
}

export class EmailTemplateMessageComponent extends React.Component<EmailMasterTemplateMessageComponentProps, { showPreview: boolean }>{
    constructor(props: EmailMasterTemplateMessageComponentProps) {
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

        const ec = this.props.ctx;
        return (
            <div className="sf-email-template-message">
                <EntityCombo ctx={ec.subCtx(e => e.cultureInfo) } labelText={EmailTemplateViewMessage.Language.niceToString() } onChange={this.props.invalidate} />
                <div>
                    <div className="code-container">
                        <HtmlCodemirror ctx={ec.subCtx(e => e.text)} onChange={this.handleCodeMirrorChange} />
                    </div>
                    <br/>
                    <a href="#" onClick={this.handlePreviewClick}>
                        {this.state.showPreview ?
                            EmailTemplateMessage.HidePreview.niceToString() :
                            EmailTemplateMessage.ShowPreview.niceToString() }
                    </a>
                    {this.state.showPreview && <IFrameRenderer style={{ width: "100%" }}  html={ec.value.text}/>}
                </div>
            </div>
        );
    }
}



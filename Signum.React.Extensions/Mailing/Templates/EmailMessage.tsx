import * as React from 'react'
import { ValueLine, EntityLine, EntityDetail, EntityRepeater } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { EmailMessageEntity, EmailAttachmentEmbedded, EmailReceptionMixin, EmailFileType } from '../Signum.Entities.Mailing'
import { EmailTemplateMessage } from '../Signum.Entities.Mailing'
import FileLine from '../../Files/FileLine'
import IFrameRenderer from './IFrameRenderer'
import HtmlCodemirror from '../../Codemirror/HtmlCodemirror'
import { tryGetMixin } from "@framework/Signum.Entities";
import { UncontrolledTabs, Tab } from '@framework/Components';
import { LabelWithHelp } from '../../MachineLearning/Templates/NeuralNetworkSettings';

export default class EmailMessage extends React.Component<{ ctx: TypeContext<EmailMessageEntity> }> {
  render() {
    let ctx = this.props.ctx;

    if (ctx.value.state != "Created")
      ctx = ctx.subCtx({ readOnly: true });

    const ctx4 = ctx.subCtx({ labelColumns: 4 });

    return (
      <UncontrolledTabs id="emailTabs">
        <Tab title={EmailMessageEntity.niceName()} eventKey="mainTab">
          <fieldset>
            <legend>Properties</legend>
            <div className="row">
              <div className="col-sm-4">
                <ValueLine ctx={ctx4.subCtx(f => f.state)} />
                <ValueLine ctx={ctx4.subCtx(f => f.sent)} />
                <ValueLine ctx={ctx4.subCtx(f => f.bodyHash)} />
                <ValueLine ctx={ctx4.subCtx(f => f.creationDate)} />
              </div>
              <div className="col-sm-8">
                <EntityLine ctx={ctx.subCtx(f => f.target, { labelColumns: 2 })} />
                <EntityLine ctx={ctx.subCtx(f => f.template)} />
                <EntityLine ctx={ctx.subCtx(f => f.package)} />
                <EntityLine ctx={ctx.subCtx(f => f.exception)} />
              </div>
            </div>
            <hr/>
            <div className="row">
              <div className="col-sm-4">
                <ValueLine ctx={ctx4.subCtx(f => f.receptionNotified)} />
              </div>
              <div className="col-sm-8">
                <ValueLine ctx={ctx.subCtx(f => f.uniqueIdentifier)} />
              </div>
            </div>
          </fieldset>

          <EntityDetail ctx={ctx.subCtx(f => f.from)} />
          <EntityRepeater ctx={ctx.subCtx(f => f.recipients)} />
          <EntityRepeater ctx={ctx.subCtx(f => f.attachments)} getComponent={this.renderAttachment} />

          <ValueLine ctx={ctx.subCtx(f => f.subject, { labelColumns: 1 })} />
          <ValueLine ctx={ctx.subCtx(f => f.isBodyHtml)} inlineCheckbox={true} onChange={() => this.forceUpdate()} />
          {ctx.value.isBodyHtml ? <div className="code-container"><HtmlCodemirror ctx={ctx.subCtx(f => f.body)} /></div> :
              <div>
                <ValueLine ctx={ctx.subCtx(f => f.body)} valueLineType="TextArea" valueHtmlAttributes={{ style: { height: "180px" } }} formGroupStyle="SrOnly" />
              </div>
          }
          <EmailMessageComponent ctx={ctx} invalidate={() => this.forceUpdate()} />
        </Tab>
        {this.renderEmailReceptionMixin()}
      </UncontrolledTabs>
    );
  }


  renderEmailReceptionMixin() {

    var erm = tryGetMixin(this.props.ctx.value, EmailReceptionMixin);
    if (!erm || !erm.receptionInfo)
      return null;

    const ri = this.props.ctx.subCtx(EmailReceptionMixin).subCtx(a => a.receptionInfo!);

    return (
      <Tab title={ri.niceName()} eventKey="receptionMixin">
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
    this.state = { showPreview: true }
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

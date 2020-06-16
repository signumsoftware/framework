import * as React from 'react'
import { ValueLine, EntityLine, EntityDetail, EntityRepeater } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { EmailMessageEntity, EmailAttachmentEmbedded, EmailReceptionMixin, EmailFileType } from '../Signum.Entities.Mailing'
import { EmailTemplateMessage } from '../Signum.Entities.Mailing'
import { FileLine } from '../../Files/FileLine'
import IFrameRenderer from './IFrameRenderer'
import HtmlCodemirror from '../../Codemirror/HtmlCodemirror'
import { tryGetMixin } from "@framework/Signum.Entities";
import { Tabs, Tab } from 'react-bootstrap';
import { LabelWithHelp } from '../../MachineLearning/Templates/NeuralNetworkSettings';
import { useForceUpdate } from '@framework/Hooks'

export default function EmailMessage(p : { ctx: TypeContext<EmailMessageEntity> }){
  const forceUpdate = useForceUpdate();

  function renderEmailReceptionMixin() {
    var erm = tryGetMixin(p.ctx.value, EmailReceptionMixin);
    if (!erm || !erm.receptionInfo)
      return null;

    const ri = p.ctx.subCtx(EmailReceptionMixin).subCtx(a => a.receptionInfo!);

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


  function renderAttachment(ec: TypeContext<EmailAttachmentEmbedded>) {
    const sc = ec.subCtx({ formGroupStyle: "SrOnly" });
    return (
      <div>
        <FileLine ctx={ec.subCtx(a => a.file)} remove={false}
          fileType={EmailFileType.Attachment} />
      </div>
    );
  };
  let ctx = p.ctx;

  if (ctx.value.state != "Created")
    ctx = ctx.subCtx({ readOnly: true });

  const ctx4 = ctx.subCtx({ labelColumns: 4 });

  return (
    <Tabs id="emailTabs">
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
        <EntityRepeater ctx={ctx.subCtx(f => f.attachments)} getComponent={renderAttachment} />

        <ValueLine ctx={ctx.subCtx(f => f.subject, { labelColumns: 1 })} />
        <ValueLine ctx={ctx.subCtx(f => f.isBodyHtml)} inlineCheckbox={true} onChange={() => forceUpdate()} />
        {ctx.value.isBodyHtml ? <div className="code-container"><HtmlCodemirror ctx={ctx.subCtx(f => f.body)} /></div> :
            <div>
              <ValueLine ctx={ctx.subCtx(f => f.body)} valueLineType="TextArea" valueHtmlAttributes={{ style: { height: "180px" } }} formGroupStyle="SrOnly" />
            </div>
        }
        <EmailMessageComponent ctx={ctx} invalidate={() => forceUpdate()} />
      </Tab>
      {renderEmailReceptionMixin()}
    </Tabs>
  );
}

export interface EmailMessageComponentProps {
  ctx: TypeContext<EmailMessageEntity>;
  invalidate: () => void;
}

export function EmailMessageComponent(p : EmailMessageComponentProps){
  const forceUpdate = useForceUpdate();
  const [showPreview, setShowPreview] = React.useState(true);

  function handlePreviewClick(e: React.FormEvent<any>) {
    e.preventDefault();
    setShowPreview(!showPreview);
  }

  function handleCodeMirrorChange() {
    if (showPreview)
      forceUpdate();
  }

  const ec = p.ctx.subCtx({ labelColumns: { sm: 2 } });
  return (
    <div className="sf-email-template-message">
      <div>
        <br />
        <a href="#" onClick={handlePreviewClick}>
          {showPreview ?
            EmailTemplateMessage.HidePreview.niceToString() :
            EmailTemplateMessage.ShowPreview.niceToString()}
        </a>
        {showPreview && <IFrameRenderer style={{ width: "100%", height: "150px" }} html={ec.value.body} />}
      </div>
    </div>
  );
}

import * as React from 'react'
import { ValueLine, EntityLine, EntityDetail, EntityRepeater, EntityAccordion, EntityTable } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { EmailMessageEntity, EmailAttachmentEmbedded, EmailFileType, EmailRecipientEmbedded } from '../Signum.Mailing'
import HtmlCodemirror from '../../Signum.Codemirror/HtmlCodemirror'
import { tryGetMixin } from "@framework/Signum.Entities";
import { Tabs, Tab } from 'react-bootstrap';
import { useForceUpdate } from '@framework/Hooks'
import { EmailTemplateMessage } from '../Signum.Mailing.Templates'
import IFrameRenderer from './IframeRenderer'

export default function EmailMessage(p: { ctx: TypeContext<EmailMessageEntity> }) {
  const forceUpdate = useForceUpdate();

  let ctx = p.ctx.subCtx({ formGroupStyle: "Basic", readOnly: p.ctx.value.state == "Created" || p.ctx.value.state == "Draft" ? undefined  : true });

  return (
    <Tabs id="emailTabs">
      <Tab title={EmailMessageEntity.niceName()} eventKey="mainTab">
        <fieldset>
          <legend>Properties</legend>

          <div className="row">
            <div className="col-sm-2">
              <ValueLine ctx={ctx.subCtx(f => f.state)} />
              <ValueLine ctx={ctx.subCtx(f => f.creationDate)} />
            </div>
            <div className="col-sm-2">
              <ValueLine ctx={ctx.subCtx(f => f.sent)} hideIfNull />
              <ValueLine ctx={ctx.subCtx(f => f.receptionNotified)} hideIfNull />
            </div>
            <div className="col-sm-2">
              <ValueLine ctx={ctx.subCtx(f => f.uniqueIdentifier)} />
              <ValueLine ctx={ctx.subCtx(f => f.bodyHash)} hideIfNull />
            </div>
            <div className="col-sm-2">
              <EntityLine ctx={ctx.subCtx(f => f.sentBy)} hideIfNull />
              <EntityLine ctx={ctx.subCtx(f => f.exception)} hideIfNull />
            </div>
 
            <div className="col-sm-4">
              <EntityLine ctx={ctx.subCtx(f => f.target, { labelColumns: 2 })} />
              <EntityLine ctx={ctx.subCtx(f => f.template)} />
            </div>
          </div>
        </fieldset>

        <EntityDetail ctx={ctx.subCtx(f => f.from)} />
        <EntityAccordion avoidFieldSet ctx={ctx.subCtx(s => s.recipients)}
          getTitle={(ctx: TypeContext<EmailRecipientEmbedded>) => <span>
            {ctx.value.kind && <strong className="me-1">{ctx.value.kind}:</strong>}
            {ctx.value.displayName && <span className="me-1">{ctx.value.displayName}</span>}
            {ctx.value.emailAddress && <span>{"<"}{ctx.value.emailAddress}{">"}</span>}
          </span>
          }
          onChange={() => forceUpdate()} />
        <EntityTable ctx={ctx.subCtx(p => p.attachments)} hideIfNull columns={EntityTable.typedColumns<EmailAttachmentEmbedded>([
          { property: p => p.file },
          { property: p => p.type },
          { property: p => p.contentId }
        ])} />

        <ValueLine ctx={ctx.subCtx(f => f.subject, { labelColumns: 1 })} />
        <ValueLine ctx={ctx.subCtx(f => f.isBodyHtml)} inlineCheckbox={true} onChange={() => forceUpdate()} />
        {ctx.value.isBodyHtml ? <div className="code-container">
          <HtmlCodemirror ctx={ctx.subCtx(f => f.body.text)} />
        </div> :
          <div>
            <ValueLine ctx={ctx.subCtx(f => f.body.text)} valueLineType="TextArea" valueHtmlAttributes={{ style: { height: "180px" } }} formGroupStyle="SrOnly" />
          </div>
        }
        <EmailMessageComponent ctx={ctx} invalidate={() => forceUpdate()} />
      </Tab>
    </Tabs>
  );
}

export interface EmailMessageComponentProps {
  ctx: TypeContext<EmailMessageEntity>;
  invalidate: () => void;
}

export function EmailMessageComponent(p: EmailMessageComponentProps) {
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
        {showPreview && <IFrameRenderer style={{ width: "100%", height: "800px" }} html={ec.value.body.text} />}
      </div>
    </div>
  );
}

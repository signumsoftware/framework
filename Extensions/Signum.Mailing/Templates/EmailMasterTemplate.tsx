import * as React from 'react'
import { AutoLine, EntityCombo, EntityTabRepeater, EntityRepeater } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import HtmlCodeMirror from '../../Signum.CodeMirror/HtmlCodeMirror'
import IFrameRenderer from './IframeRenderer'
import { useForceUpdate } from '@framework/Hooks'
import { Tabs, Tab } from 'react-bootstrap'
import { EmailMasterTemplateEntity, EmailMasterTemplateMessageEmbedded, EmailTemplateMessage, EmailTemplateViewMessage } from '../Signum.Mailing.Templates'

export default function EmailMasterTemplate(p : { ctx: TypeContext<EmailMasterTemplateEntity> }): React.JSX.Element {
  const forceUpdate = useForceUpdate();
  const ctx = p.ctx;

  return (
    <div>
      <AutoLine ctx={ctx.subCtx(f => f.name)} />
      <AutoLine ctx={ctx.subCtx(f => f.isDefault)} />
      <Tabs id={ctx.prefix + "tabs"}>
        <Tab eventKey="messages" title={ctx.niceName(a => a.messages)}>
          <EntityTabRepeater ctx={ctx.subCtx(a => a.messages)} avoidFieldSet onChange={() => forceUpdate()} getComponent={ctxMsg =>
            <EmailTemplateMessageComponent ctx={ctxMsg} invalidate={() => forceUpdate()} />} />
        </Tab>
        <Tab eventKey="attachments" title={ctx.niceName(a => a.attachments)}>
          <EntityRepeater ctx={ctx.subCtx(e => e.attachments)} avoidFieldSet onChange={() => forceUpdate()} />
        </Tab>
      </Tabs>
    </div>
  );
}

export interface EmailMasterTemplateMessageComponentProps {
  ctx: TypeContext<EmailMasterTemplateMessageEmbedded>;
  invalidate: () => void;
}

export function EmailTemplateMessageComponent(p : EmailMasterTemplateMessageComponentProps): React.JSX.Element{
  const forceUpdate = useForceUpdate();
  const [showPreview, setShowPreview] = React.useState(false);

  function handlePreviewClick(e: React.FormEvent<any>) {
    e.preventDefault();
    setShowPreview(!showPreview);
  }

  function handleCodeMirrorChange() {
    if (showPreview)
      forceUpdate();
  }

  const ec = p.ctx;
  return (
    <div className="sf-email-template-message">
      <EntityCombo ctx={ec.subCtx(e => e.cultureInfo)} label={EmailTemplateViewMessage.Language.niceToString()} onChange={p.invalidate} />
      <div>
        <div className="code-container">
          <HtmlCodeMirror ctx={ec.subCtx(e => e.text)} onChange={handleCodeMirrorChange} />
        </div>
        <br />
        <a href="#" onClick={handlePreviewClick}>
          {showPreview ?
            EmailTemplateMessage.HidePreview.niceToString() :
            EmailTemplateMessage.ShowPreview.niceToString()}
        </a>
        {showPreview && <IFrameRenderer style={{ width: "100%", minHeight: "800px" }} html={ec.value.text} />}
      </div>
    </div>
  );
}



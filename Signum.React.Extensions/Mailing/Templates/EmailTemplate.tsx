import * as React from 'react'
import { FormGroup, ValueLine, EntityLine, EntityCombo, EntityDetail, EntityRepeater, EntityTabRepeater } from '@framework/Lines'
import { SubTokensOptions } from '@framework/FindOptions'
import { TypeContext } from '@framework/TypeContext'
import { EmailTemplateEntity, EmailTemplateContactEmbedded, EmailTemplateMessageEmbedded, EmailTemplateViewMessage, EmailTemplateMessage, EmailTemplateRecipientEmbedded } from '../Signum.Entities.Mailing'
import { TemplateApplicableEval } from '../../Templating/Signum.Entities.Templating'
import QueryTokenEmbeddedBuilder from '../../UserAssets/Templates/QueryTokenEmbeddedBuilder'
import TemplateControls from '../../Templating/TemplateControls'
import HtmlCodemirror from '../../Codemirror/HtmlCodemirror'
import IFrameRenderer from './IFrameRenderer'
import ValueLineModal from '@framework/ValueLineModal'
import TemplateApplicable from '../../Templating/Templates/TemplateApplicable';
import { useForceUpdate } from '@framework/Hooks'

export default function EmailTemplate(p : { ctx: TypeContext<EmailTemplateEntity> }){
  const forceUpdate = useForceUpdate();

  function renderQueryPart() {
    const ec = p.ctx.subCtx({ labelColumns: { sm: 2 } });
    const ecXs = ec.subCtx({ formSize: "ExtraSmall" });
    return (
      <div>
        <EntityDetail ctx={ecXs.subCtx(e => e.from)} onChange={() => forceUpdate()} getComponent={renderContact} />
        <EntityRepeater ctx={ecXs.subCtx(e => e.recipients)} onChange={() => forceUpdate()} getComponent={renderRecipient} />
        <EntityRepeater ctx={ecXs.subCtx(e => e.attachments)} />
        <EntityLine ctx={ec.subCtx(e => e.masterTemplate)} />
        <ValueLine ctx={ec.subCtx(e => e.isBodyHtml)} />

        <div className="sf-email-replacements-container">
          <EntityTabRepeater ctx={ec.subCtx(a => a.messages)} onChange={() => forceUpdate()} getComponent={(ctx: TypeContext<EmailTemplateMessageEmbedded>) =>
            <EmailTemplateMessageComponent ctx={ctx} queryKey={ec.value.query!.key!} invalidate={() => forceUpdate()} />} />
        </div>
      </div>
    );
  }

  function renderContact(ec: TypeContext<EmailTemplateContactEmbedded>) {
    const sc = ec.subCtx({ formGroupStyle: "Basic" });

    return (
      <div>
        <div className="row">
          <div className="col-sm-2" >
            <FormGroup labelText={EmailTemplateEntity.nicePropertyName(a => a.recipients![0].element.kind)} ctx={sc}>
              <span className={sc.formControlClass}>{EmailTemplateEntity.nicePropertyName(a => a.from)} </span>
            </FormGroup>
          </div>
          <div className="col-sm-10">
            {p.ctx.value.query &&
              <QueryTokenEmbeddedBuilder
                ctx={sc.subCtx(a => a.token)}
                queryKey={p.ctx.value.query.key}
                subTokenOptions={SubTokensOptions.CanElement}
                helpText="Expression pointing to an EmailOwnerData (recommended)" />
            }
          </div>
        </div>
        <div className="row">
          <div className="col-sm-5 offset-sm-2">
            <ValueLine ctx={sc.subCtx(c => c.emailAddress)} helpText="Hardcoded E-Mail address" />
          </div>
          <div className="col-sm-5">
            <ValueLine ctx={sc.subCtx(c => c.displayName)} helpText="Hardcoded display name" />
          </div>
        </div>
      </div>
    );
  };

  function renderRecipient(ec: TypeContext<EmailTemplateRecipientEmbedded>) {
    const sc = ec.subCtx({ formGroupStyle: "Basic" });

    return (
      <div>
        <div className="row">
          <div className="col-sm-2" >
            <ValueLine ctx={sc.subCtx(a => a.kind)} />
          </div>
          <div className="col-sm-10">
            {p.ctx.value.query &&
              <QueryTokenEmbeddedBuilder
                ctx={sc.subCtx(a => a.token)}
                queryKey={p.ctx.value.query.key}
                subTokenOptions={SubTokensOptions.CanElement}
                helpText="Expression pointing to an EmailOwnerData (recommended)" />
            }
          </div>
        </div>
        <div className="row">
          <div className="col-sm-5 offset-sm-2">
            <ValueLine ctx={sc.subCtx(c => c.emailAddress)} helpText="Hardcoded E-Mail address" />
          </div>
          <div className="col-sm-5">
            <ValueLine ctx={sc.subCtx(c => c.displayName)} helpText="Hardcoded display name" />
          </div>
        </div>
      </div>
    );
  };
  const ctx = p.ctx;
  const ctx3 = ctx.subCtx({ labelColumns: { sm: 3 } });

  return (
    <div>
      <ValueLine ctx={ctx3.subCtx(e => e.name)} />
      <EntityCombo ctx={ctx3.subCtx(e => e.model)} />
      <EntityLine ctx={ctx3.subCtx(e => e.query)} onChange={() => forceUpdate()}
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

      {ctx3.value.query && renderQueryPart()}
    </div>
  );
}

export interface EmailTemplateMessageComponentProps {
  ctx: TypeContext<EmailTemplateMessageEmbedded>;
  queryKey: string;
  invalidate: () => void;
}

export function EmailTemplateMessageComponent(p : EmailTemplateMessageComponentProps){
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

  function handleOnInsert(newCode: string) {
    ValueLineModal.show({
      type: { name: "string" },
      initialValue: newCode,
      title: "Template",
      message: "Copy to clipboard: Ctrl+C, ESC",
      initiallyFocused: true,
    }).done();
  }
  const ec = p.ctx.subCtx({ labelColumns: { sm: 2 } });
  return (
    <div className="sf-email-template-message">
      <EntityCombo ctx={ec.subCtx(e => e.cultureInfo)} labelText={EmailTemplateViewMessage.Language.niceToString()} onChange={p.invalidate} />
      <div>
        <TemplateControls queryKey={p.queryKey} onInsert={handleOnInsert} forHtml={true} />
        <ValueLine ctx={ec.subCtx(e => e.subject)} formGroupStyle={"SrOnly"} placeholderLabels={true} labelHtmlAttributes={{ style: { width: "100px" } }} />
        <div className="code-container">
          <HtmlCodemirror ctx={ec.subCtx(e => e.text)} onChange={handleCodeMirrorChange} />
        </div>
        <br />
        <a href="#" onClick={handlePreviewClick}>
          {showPreview ?
            EmailTemplateMessage.HidePreview.niceToString() :
            EmailTemplateMessage.ShowPreview.niceToString()}
        </a>
        {showPreview && <IFrameRenderer style={{ width: "100%" }} html={ec.value.text} />}
      </div>
    </div>
  );
}

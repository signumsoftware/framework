import * as React from 'react'
import { FormGroup, AutoLine, EntityLine, EntityCombo, EntityDetail, EntityRepeater, EntityTabRepeater, EntityTable, EntityAccordion, Binding } from '@framework/Lines'
import { SubTokensOptions } from '@framework/FindOptions'
import { TypeContext } from '@framework/TypeContext'
import { TemplateApplicableEval } from '../../Signum.Templating/Signum.Templating'
import QueryTokenEmbeddedBuilder from '../../Signum.UserAssets/Templates/QueryTokenEmbeddedBuilder'
import TemplateControls from '../../Signum.Templating/TemplateControls'
import HtmlCodeMirror from '../../Signum.CodeMirror/HtmlCodeMirror'
import IFrameRenderer from './IframeRenderer'
import AutoLineModal from '@framework/AutoLineModal'
import TemplateApplicable from '../../Signum.Templating/Templates/TemplateApplicable';
import { useForceUpdate, useUpdatedRef } from '@framework/Hooks'
import FilterBuilderEmbedded from '../../Signum.UserAssets/Templates/FilterBuilderEmbedded'
import { Tabs, Tab } from 'react-bootstrap';
import { QueryEntity } from '@framework/Signum.Basics'
import HtmlEditor from '../../Signum.HtmlEditor/HtmlEditor'
import { EmailMessageFormat, EmailTemplateEntity, EmailTemplateFromEmbedded, EmailTemplateMessage, EmailTemplateMessageEmbedded, EmailTemplateRecipientEmbedded, EmailTemplateViewMessage } from '../Signum.Mailing.Templates'
import { QueryOrderEmbedded } from '../../Signum.UserAssets/Signum.UserAssets.Queries'

export default function EmailTemplate(p: { ctx: TypeContext<EmailTemplateEntity> }) {
  const forceUpdate = useForceUpdate();

  function renderQueryPart() {
    const ec = p.ctx.subCtx({ labelColumns: { sm: 2 } });
    const ecXs = ec.subCtx({ formSize: "xs" });
    var canAggregate = ctx.value.groupResults ? SubTokensOptions.CanAggregate : 0;
    return (
      <div>
        <div className="mb-4">
          <Tabs id={ctx.prefix + "tabs"}>
            <Tab eventKey="recipients" title={ctx.niceName(a => a.recipients)}>
              <EntityDetail ctx={ecXs.subCtx(e => e.from)} onChange={() => forceUpdate()}
                onCreate={() => Promise.resolve(EmailTemplateFromEmbedded.New({ whenNone: "ThrowException", whenMany: "SplitMessages" }))}
                getComponent={fctx => <EmailTemplateFrom ctx={fctx} query={p.ctx.value.query} />} />
              <h5 className="text-muted">{ecXs.niceName(s => s.recipients)}</h5>
              <EntityAccordion avoidFieldSet ctx={ecXs.subCtx(s => s.recipients)}
                getTitle={(ctx: TypeContext<EmailTemplateRecipientEmbedded>) => <span>
                  {ctx.value.kind && <strong className="me-1">{ctx.value.kind}:</strong>}
                  {ctx.value.token && <span className="me-1">{ctx.value.token.tokenString}</span>}
                  {ctx.value.displayName && <span className="me-1">{ctx.value.displayName}</span>}
                  {ctx.value.emailAddress && <span>{"<"}{ctx.value.emailAddress}{">"}</span>}
                </span>
                }
                onChange={() => forceUpdate()}
                onCreate={() => Promise.resolve(EmailTemplateRecipientEmbedded.New({ whenNone: "ThrowException", whenMany: "SplitMessages" }))}
                getComponent={rctx => <EmailTemplateRecipient ctx={rctx} query={p.ctx.value.query} />} />
            </Tab>
            <Tab eventKey="attachments" title={
              <span style={{ fontWeight: ctx.value.attachments.length > 0 ? "bold" : undefined }}>
                {ctx.niceName(a => a.attachments)}
              </span>}>
              <EntityRepeater ctx={ecXs.subCtx(e => e.attachments)} avoidFieldSet={true} onChange={() => forceUpdate()} />
            </Tab>
            <Tab eventKey="query" title={<span style={{ fontWeight: ctx.value.groupResults || ctx.value.filters.length > 0 || ctx.value.orders.length ? "bold" : undefined }}>
              {ctx.niceName(a => a.query)}
            </span>}>

              <div className="row">
                <div className="col-sm-4">
                  <AutoLine ctx={ctx3.subCtx(e => e.disableAuthorization)} inlineCheckbox />
                </div>
                <div className="col-sm-4">
                  <AutoLine ctx={ctx.subCtx(e => e.groupResults)} inlineCheckbox onChange={forceUpdate} />
                </div>
                <div className="col-sm-4">
                </div>
              </div>

              <FilterBuilderEmbedded ctx={ctx.subCtx(e => e.filters)} onChanged={forceUpdate}
                subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | canAggregate}
                queryKey={ctx.value.query!.key}/>
              <EntityTable ctx={ctx.subCtx(e => e.orders)} onChange={forceUpdate} columns={EntityTable.typedColumns<QueryOrderEmbedded>([
                {
                  property: a => a.token,
                  template: ctx => <QueryTokenEmbeddedBuilder
                    ctx={ctx.subCtx(a => a.token, { formGroupStyle: "SrOnly" })}
                    queryKey={p.ctx.value.query!.key}
                    subTokenOptions={SubTokensOptions.CanElement | canAggregate} />
                },
                { property: a => a.orderType }
              ])} />
            </Tab>
            <Tab eventKey="applicable" title={
              <span style={{ fontWeight: ctx.value.applicable ? "bold" : undefined }}>
                {ctx.niceName(a => a.applicable)}
              </span>}>
              <EntityDetail ctx={ctx3.subCtx(e => e.applicable)} onChange={forceUpdate}
                getComponent={(ec2: TypeContext<TemplateApplicableEval>) => <TemplateApplicable ctx={ec2} query={ctx.value.query!} />} />
            </Tab>
          </Tabs>
        </div>

        <div className="row mb-3">
          <div className="col-sm-6">
            <AutoLine ctx={ctx3.subCtx(e => e.messageFormat, { labelColumns:4 })} onChange={forceUpdate} />
          </div>
          <div className="col-sm-6">
            <AutoLine ctx={ctx3.subCtx(e => e.editableMessage)} inlineCheckbox={true} />
          </div>
        </div>
        <EntityLine ctx={ec.subCtx(e => e.masterTemplate, { labelColumns: 2 })} />
        <div className="sf-email-replacements-container">
          <EntityTabRepeater ctx={ec.subCtx(a => a.messages)} onChange={() => forceUpdate()} getComponent={(ctx: TypeContext<EmailTemplateMessageEmbedded>) =>
            <EmailTemplateMessageComponent ctx={ctx} queryKey={ec.value.query!.key!} messageFormat={ec.value.messageFormat} invalidate={() => forceUpdate()} />} />
        </div>
      </div>
    );
  }



  const ctx = p.ctx;
  const ctx3 = ctx.subCtx({ labelColumns: { sm: 3 } });

  return (
    <div>
      <AutoLine ctx={ctx3.subCtx(e => e.name)} />
      <EntityCombo ctx={ctx3.subCtx(e => e.model)} />
      <EntityLine ctx={ctx3.subCtx(e => e.query)} onChange={() => forceUpdate()}
        remove={ctx.value.from == undefined &&
          (ctx.value.recipients == null || ctx.value.recipients.length == 0) &&
          (ctx.value.messages == null || ctx.value.messages.length == 0)} />

      {ctx3.value.query && renderQueryPart()}
    </div>
  );
}

function EmailTemplateFrom(p: { ctx: TypeContext<EmailTemplateFromEmbedded>, query: QueryEntity | undefined  }) {
  const sc = p.ctx.subCtx({ formGroupStyle: "Basic" });

  const forceUpdate = useForceUpdate();
  return (
    <div>
      <div className="row">
        <div className="col-sm-2" >
          <FormGroup label={EmailTemplateEntity.nicePropertyName(a => a.recipients![0].element.kind)} ctx={sc}>
            {() => <span className={sc.formControlClass}>{EmailTemplateEntity.nicePropertyName(a => a.from)} </span>}
          </FormGroup>
        </div>
        <div className="col-sm-2" >
          <AutoLine ctx={sc.subCtx(a => a.addressSource)} onChange={() => { sc.value.token = null; sc.value.emailAddress = null; sc.value.displayName = null; forceUpdate(); }} />
        </div>
        <div className="col-sm-8">
          {p.query && sc.value.addressSource == "QueryToken" &&
            <div>
              <QueryTokenEmbeddedBuilder
                ctx={sc.subCtx(a => a.token)}
                queryKey={p.query.key}
                subTokenOptions={SubTokensOptions.CanElement}
                onTokenChanged={forceUpdate} />
              <div className="row">
                <div className="col-sm-6">
                  <AutoLine ctx={sc.subCtx(c => c.whenNone)} />
                </div>
                <div className="col-sm-6">
                  <AutoLine ctx={sc.subCtx(c => c.whenMany)} />
                </div>
              </div>
            </div>
          }

          {sc.value.addressSource == "HardcodedAddress" && <div className="row">
            <div className="col-sm-6">
              <AutoLine ctx={sc.subCtx(c => c.emailAddress)} onChange={forceUpdate} />
              <AutoLine ctx={sc.subCtx(c => c.azureUserId)} onChange={forceUpdate} />
            </div>
            <div className="col-sm-6">
              <AutoLine ctx={sc.subCtx(c => c.displayName)}  onChange={forceUpdate} />
            </div>
          </div>
          }
        </div>
      </div>
     
    </div>
  );
};

function EmailTemplateRecipient(p: { ctx: TypeContext<EmailTemplateRecipientEmbedded>, query: QueryEntity | undefined }) {
  const sc = p.ctx.subCtx({ formGroupStyle: "Basic" });
  const forceUpdate = useForceUpdate();

  return (
    <div>
      <div className="row">
        <div className="col-sm-2" >
          <AutoLine ctx={sc.subCtx(a => a.kind)} />
        </div>
        <div className="col-sm-2" >
          <AutoLine ctx={sc.subCtx(a => a.addressSource)} onChange={() => { sc.value.token = null; sc.value.emailAddress = null; sc.value.displayName = null; forceUpdate(); }} />
        </div>
        <div className="col-sm-8">
          {p.query && sc.value.addressSource == "QueryToken" &&
            <div>
              <QueryTokenEmbeddedBuilder
                ctx={sc.subCtx(a => a.token)}
                queryKey={p.query.key}
                subTokenOptions={SubTokensOptions.CanElement}
                onTokenChanged={forceUpdate} />
              <div className="row">
                <div className="col-sm-6">
                  <AutoLine ctx={sc.subCtx(c => c.whenNone)} />
                </div>
                <div className="col-sm-6">
                  <AutoLine ctx={sc.subCtx(c => c.whenMany)} />
                </div>
              </div>
            </div>
          }

          {sc.value.addressSource == "HardcodedAddress" && <div className="row">
            <div className="col-sm-6">
              <AutoLine ctx={sc.subCtx(c => c.emailAddress)} onChange={forceUpdate} />
            </div>
            <div className="col-sm-6">
              <AutoLine ctx={sc.subCtx(c => c.displayName)} onChange={forceUpdate} />
            </div>
          </div>
          }
        </div>
      </div>
    </div>
  );
};


export interface EmailTemplateMessageComponentProps {
  ctx: TypeContext<EmailTemplateMessageEmbedded>;
  queryKey: string;
  messageFormat: EmailMessageFormat;
  invalidate: () => void;
}

export function EmailTemplateMessageComponent(p: EmailTemplateMessageComponentProps) {
  const forceUpdate = useForceUpdate();
  const [showPreview, setShowPreview] = React.useState(false);
  const showPreviewRef = useUpdatedRef(showPreview);

  function handlePreviewClick(e: React.FormEvent<any>) {
    e.preventDefault();
    setShowPreview(!showPreview);
  }

  function handleCodeMirrorChange() {
    if (showPreviewRef.current)
      forceUpdate();
  }

  const ec = p.ctx.subCtx({ labelColumns: { sm: 2 } });
  return (
    <div className="sf-email-template-message">
      <EntityCombo ctx={ec.subCtx(e => e.cultureInfo)} label={EmailTemplateViewMessage.Language.niceToString()} onChange={p.invalidate} />
      <br/>
      <div>
        <TemplateControls queryKey={p.queryKey} forHtml={true} />
        <AutoLine ctx={ec.subCtx(e => e.subject)} formGroupStyle={"SrOnly"} placeholderLabels={true} labelHtmlAttributes={{ style: { width: "100px" } }} />
        {p.messageFormat != 'HtmlSimple' ?
          <div className="code-container">
            <HtmlCodeMirror ctx={ec.subCtx(e => e.text)} onChange={handleCodeMirrorChange} />
          </div> : <HtmlEditor binding={Binding.create(ec.value, e => e.text)} readOnly={ec.readOnly} />}
        <br />
        {p.messageFormat == 'HtmlComplex' && <a href="#" onClick={handlePreviewClick}>
          {showPreview ?
            EmailTemplateMessage.HidePreview.niceToString() :
            EmailTemplateMessage.ShowPreview.niceToString()}
        </a>}
        {showPreview && <IFrameRenderer style={{ width: "100%", minHeight: "800px" }} html={ec.value.text} />}
      </div>
    </div>
  );
}

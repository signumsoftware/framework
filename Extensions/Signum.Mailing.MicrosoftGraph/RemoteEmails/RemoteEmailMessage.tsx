import * as React from 'react'
import { EntityDetail, EntityLine, EntityStrip, FormGroup, ValueLine } from '@framework/Lines'
import { SearchControl } from '@framework/Search'
import { TypeContext } from '@framework/TypeContext'
import * as AppContext from '@framework/AppContext'
import * as FilesClient from '../../Signum.Files/FilesClient'
import { EmailMessageEntity } from '../../Signum.Mailing/Signum.Mailing';
import IFrameRenderer from '../../Signum.Mailing/Templates/IframeRenderer';
import { RemoteAttachmentEmbedded, RemoteEmailMessageModel } from './Signum.Mailing.MicrosoftGraph.RemoteEmails';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import * as MicrosoftGraphRemoteEmailsClient from './MicrosoftGraphRemoteEmailsClient'
import { UserLiteModel } from '../../Signum.Authorization/Signum.Authorization'
import { saveFile } from '@framework/Services'

export default function RemoteEmailMessage(p: { ctx: TypeContext<RemoteEmailMessageModel> }) {
  const ctx = p.ctx.subCtx({ readOnly: true });
  var oid = (ctx.value.user.model as UserLiteModel).oID!;

  function manipulateDom(doc: Document) {
    doc.body.querySelectorAll("a").forEach(a => a.target = "_blank");
    doc.body.querySelectorAll("img").forEach(a => {
      if (a.src && a.src.startsWith("cid")) {
        var name = a.src.after("cid:").before("@");

        var att = ctx.value.attachments.singleOrNull(a => a.element.name == name);
        if (att) {
          a.src = AppContext.toAbsoluteUrl(MicrosoftGraphRemoteEmailsClient.API.getRemoteAttachmentUrl(oid, ctx.value.id, att?.element.id));
        }
      }
    });
  }

  return (
    <div>
      <FormGroup ctx={ctx.subCtx(f => f.id)}>
        {id => <a id={id} href={ctx.value.webLink!} className={ctx.formControlClass} target="_blank">{ctx.value.id}</a>}
      </FormGroup>

      <div className="row">
        <div className="col-sm-8">
          <EntityLine ctx={ctx.subCtx(f => f.from)} labelColumns={3} />
        </div>
        <div className="col-sm-4">
          <ValueLine ctx={ctx.subCtx(f => f.sentDateTime)} labelColumns={6} />
        </div>
      </div>

      <div className="row">
        <div className="col-sm-8">
          <EntityLine ctx={ctx.subCtx(f => f.user)} labelColumns={3} />
        </div>
        <div className="col-sm-4">
          <ValueLine ctx={ctx.subCtx(f => f.receivedDateTime)} labelColumns={6} />
        </div>
      </div>

      <EntityStrip ctx={ctx.subCtx(f => f.toRecipients)} />
      <EntityStrip ctx={ctx.subCtx(f => f.ccRecipients)} hideIfNull />
      <EntityStrip ctx={ctx.subCtx(f => f.bccRecipients)} hideIfNull />

      {ctx.value.attachments.some(a => !a.element.isInline) &&
        <EntityStrip ctx={ctx.subCtx(f => f.attachments)}
          filterRows={(ctxs: TypeContext<RemoteAttachmentEmbedded>[]) => ctxs.filter(a => !a.value.isInline)}


          onRenderItem={(item: RemoteAttachmentEmbedded) => {
            var info = FilesClient.extensionInfo[item.name.tryAfterLast(".")?.toLowerCase()!]

            return (
              <span>
                <FontAwesomeIcon className="me-1"
                  icon={Array.isArray(info?.icon) ? info.icon : typeof info?.icon == "string" ? ["far", info?.icon] : ["far", "file"]}
                  color={info?.color ?? "grey"} />
                {item.name}
              </span>
            );
          }}
          onView={async (item: RemoteAttachmentEmbedded) => {
           

            MicrosoftGraphRemoteEmailsClient.API.getRemoteAttachment(oid!, ctx.value.id, item.id).then(res => saveFile(res));

            return undefined;
          }}
        />
      }
      <ValueLine ctx={ctx.subCtx(f => f.subject)} />

      {ctx.value.isBodyHtml ?
        <IFrameRenderer style={{ width: "100%", height: "800px" }} html={ctx.value.body} manipulateDom={manipulateDom} /> :
        <pre>{ctx.value.body}</pre>
      }
    </div>
  );
}


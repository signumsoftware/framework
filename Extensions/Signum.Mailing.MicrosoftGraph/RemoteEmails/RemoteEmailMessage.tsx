import * as React from 'react'
import { EntityDetail, EntityLine, EntityStrip, FormGroup, AutoLine, MultiValueLine } from '@framework/Lines'
import { SearchControl } from '@framework/Search'
import { TypeContext } from '@framework/TypeContext'
import * as AppContext from '@framework/AppContext'
import { FilesClient } from '../../Signum.Files/FilesClient'
import { EmailMessageEntity } from '../../Signum.Mailing/Signum.Mailing';
import IFrameRenderer from '../../Signum.Mailing/Templates/IframeRenderer';
import { RemoteAttachmentEmbedded, RemoteEmailMessageModel } from './Signum.Mailing.MicrosoftGraph.RemoteEmails';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { RemoteEmailsClient } from './RemoteEmailsClient'
import { UserLiteModel } from '../../Signum.Authorization/Signum.Authorization'
import { saveFile } from '@framework/Services'
import { getToString } from '@framework/Signum.Entities'

export default function RemoteEmailMessage(p: { ctx: TypeContext<RemoteEmailMessageModel> }): React.JSX.Element {
  const ctx = p.ctx.subCtx({ readOnly: true });

  var oid = (ctx.value.user.model as UserLiteModel).oID!;

  return (
    <div>

      <div className="row mb-3">
        <div className="col-sm-2">
          <FontAwesomeIcon icon={["far", "envelope"]} style={{
            color: "#d6d6d6",
            transform: "translate(-23px, -22px) rotate(12deg)",
            fontSize: "100px",
            position: "absolute"
          }} />
        </div>
        <div className="col-sm-4 custom-placeholder">
        </div>
        <div className="col-sm-6">
          <EntityLine ctx={ctx.subCtx(f => f.user)} labelColumns={3}
            helpText={<a href={ctx.value.webLink!} target="_blank">Outlook Web</a>} />
        </div>
      </div>



      <div className="row mb-3">
        <div className="col-sm-8">
          <EntityLine ctx={ctx.subCtx(f => f.from)} labelColumns={3} />
          <EntityStrip ctx={ctx.subCtx(f => f.toRecipients)} labelColumns={3} />
          <EntityStrip ctx={ctx.subCtx(f => f.ccRecipients)} labelColumns={3} hideIfNull />
          <EntityStrip ctx={ctx.subCtx(f => f.bccRecipients)} labelColumns={3} hideIfNull />
        </div>
        <div className="col-sm-4">
          <AutoLine ctx={ctx.subCtx(f => f.sentDateTime)} labelColumns={6} />
          <AutoLine ctx={ctx.subCtx(f => f.receivedDateTime)} labelColumns={6} />
          <MultiValueLine ctx={ctx.subCtx(f => f.categories)} labelColumns={3} />
        </div>
      </div>

      {ctx.value.attachments.some(a => !a.element.isInline) &&
        <EntityStrip ctx={ctx.subCtx(f => f.attachments)}
          filterRows={(ctxs: TypeContext<RemoteAttachmentEmbedded>[]) => ctxs.filter(a => !a.value.isInline)}


          onRenderItem={(item: RemoteAttachmentEmbedded) => {
            var info = FilesClient.extensionInfo[item.name.tryAfterLast(".")?.toLowerCase()!]

            return (
              <span>
                <FontAwesomeIcon className="me-1"
                  icon={info?.icon ?? "file"}
                  color={info?.color ?? "grey"} />
                {item.toStr}
              </span>
            );
          }}
          onView={async (item: RemoteAttachmentEmbedded) => {
           
            RemoteEmailsClient.API.getRemoteAttachment(oid!, ctx.value.id, item.id)
              .then(res => saveFile(res));

            return undefined;
          }}
        />
      }
      <AutoLine ctx={ctx.subCtx(f => f.subject)} />

      {ctx.value.isBodyHtml ?
        <RemoteEmailRenderer remoteEmail={ctx.value} /> : 
        <pre>{ctx.value.body}</pre>
      }
    </div>
  );
}

export function RemoteEmailRenderer(p: { remoteEmail: RemoteEmailMessageModel }): React.JSX.Element {

  var oid = (p.remoteEmail.user.model as UserLiteModel).oID!;

  function manipulateDom(doc: Document) {
    doc.body.querySelectorAll("a").forEach(a => a.target = "_blank");
    doc.body.querySelectorAll("img").forEach(a => {
      if (a.src && a.src.startsWith("cid")) {
        var contentId = a.src.after("cid:");

        var att = p.remoteEmail.attachments.firstOrNull(a => a.element.contentId == contentId || contentId.tryBefore("@") != null && a.element.contentId == contentId.tryBefore("@"));
        if (att) {
          a.src = AppContext.toAbsoluteUrl(RemoteEmailsClient.API.getRemoteAttachmentUrl(oid, p.remoteEmail.id, att?.element.id));
        }
      }
    });
  }

  return <IFrameRenderer style={{ width: "100%", height: "800px" }} html={p.remoteEmail.body} manipulateDom={manipulateDom} />;
}


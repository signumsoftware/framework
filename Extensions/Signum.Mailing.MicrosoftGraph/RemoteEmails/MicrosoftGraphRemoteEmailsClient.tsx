import * as React from 'react'
import { RouteObject } from 'react-router'
import * as Navigator from '@framework/Navigator'
import * as AppContext from '@framework/AppContext'
import * as Finder from '@framework/Finder'
import { EntitySettings } from '@framework/Navigator';
import { RecipientEmbedded, RemoteEmailMessageModel, RemoteEmailMessageQuery } from './Signum.Mailing.MicrosoftGraph.RemoteEmails';
import { EntityOperationSettings } from '@framework/Operations'
import { EmailMessageEntity } from '../../Signum.Mailing/Signum.Mailing'
import { Lite, SearchMessage, newMListElement } from '@framework/Signum.Entities'
import { EntityBaseController } from '@framework/Lines'
import { ajaxGet, ajaxGetRaw} from '@framework/Services'
import { UserEntity, UserLiteModel } from '../../Signum.Authorization/Signum.Authorization'
import MessageModal from '@framework/Modals/MessageModal'
import { ResultRow } from '@framework/FindOptions'
import { SearchControlHandler, SearchControlLoaded } from '@framework/Search'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'


export function start(options: {
  routes: RouteObject[],
}) {
  Navigator.addSettings(new EntitySettings(RemoteEmailMessageModel, e => import('./RemoteEmailMessage')));
  Navigator.addSettings(new EntitySettings(RecipientEmbedded, undefined, { isViewable: "Never" }));

  Finder.addSettings({
    queryName: RemoteEmailMessageQuery.RemoteEmailMessages,
    defaultFilters: [
      { token: "User", value: AppContext.currentUser, pinned: { active: "Always" } },
    ],
    hiddenColumns: [
      { token: "User" },
      { token: "Id" },
      { token: "HasAttachments" },
      { token: "IsRead" }
    ],
    onDoubleClick: (e, row, col, sc) => {
      openMessage(row, sc!);
    },
    entityFormatter: new Finder.EntityFormatter(ctx => {
      return (
        <a href="#" onClick={async e => openMessage(ctx.row, ctx.searchControl!)}>
          <span title={SearchMessage.View.niceToString()}>
            {EntityBaseController.getViewIcon()}
          </span>
        </a>
      );

    }),
    formatters: {
      "Subject": new Finder.CellFormatter((val, cfc) => {
        var hasAttachments = cfc.searchControl?.getRowValue(cfc.row, "HasAttachments") ? <FontAwesomeIcon icon="paperclip" className="me-1" /> : null;
        var isRead = cfc.searchControl?.getRowValue(cfc.row, "IsRead");

        if (isRead)
          return <span>{hasAttachments} {val}</span>;
        else
          return <strong>{hasAttachments} {val}</strong>;
      }, true)
    }
  });
}

async function openMessage(row: ResultRow, sc: SearchControlLoaded) {
  var user = sc.getRowValue<Lite<UserEntity>>(row, "User");
  var messageId = sc.getRowValue<string>(row, "Id");
  if (messageId == null)
    throw new Error("No User found");

  var oid = (user?.model as UserLiteModel).oID;
  if (oid == null)
    throw new Error("User has no OID");

  if (messageId == null)
    throw new Error("No message Id found");

  var message = await API.getRemoteEmail(oid, messageId);

  await Navigator.view(message);
}

export module API {
  export function getRemoteEmail(userOID: string, messageId: string): Promise<RemoteEmailMessageModel> {
    return ajaxGet({ url: `/api/remoteEmail/${userOID}/${messageId}` });
  }

  export function getRemoteAttachment(userOID: string, messageId: string, attachmentId: string): Promise<Response> {
    return ajaxGetRaw({ url: `/api/remoteEmail/${userOID}/${messageId}/attachment/${attachmentId}`});
  }

  export function getRemoteAttachmentUrl(userOID: string, messageId: string, attachmentId: string) {
    return `/api/remoteEmail/${userOID}/${messageId}/attachment/${attachmentId}`
  }
}

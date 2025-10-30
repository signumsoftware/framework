import * as React from 'react'
import { RouteObject } from 'react-router'
import { Navigator, EntitySettings } from '@framework/Navigator'
import * as AppContext from '@framework/AppContext'
import { Finder } from '@framework/Finder'
import { RecipientEmbedded, RemoteEmailFolderModel, RemoteEmailMessageModel, RemoteEmailMessageQuery } from './Signum.Mailing.MicrosoftGraph.RemoteEmails';
import { EntityOperationSettings } from '@framework/Operations'
import { EmailMessageEntity } from '../../Signum.Mailing/Signum.Mailing'
import { Lite, ModifiableEntity, SearchMessage, newMListElement } from '@framework/Signum.Entities'
import { EntityBaseController, EntityCombo, EntityLine, LiteAutocompleteConfig } from '@framework/Lines'
import { ajaxGet, ajaxGetRaw} from '@framework/Services'
import { UserEntity, UserLiteModel } from '../../Signum.Authorization/Signum.Authorization'
import MessageModal from '@framework/Modals/MessageModal'
import { QueryToken, ResultRow, SubTokensOptions, isFilterCondition } from '@framework/FindOptions'
import { FilterOptionParsed, SearchControlHandler, SearchControlLoaded } from '@framework/Search'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import RemoteEmailPopover from './RemoteEmailPopover'
import { FolderLine } from './FolderLine'
import { ModelConverterSymbol } from '../../Signum.Templating/Signum.Templating'
import { getTypeInfo } from '@framework/Reflection'
import { LinkButton } from '@framework/Basics/LinkButton'

export namespace RemoteEmailsClient {
  
  
  export function start(options: {
    routes: RouteObject[],
  }): void {
    Navigator.addSettings(new EntitySettings(RemoteEmailMessageModel, e => import('./RemoteEmailMessage'), {
      renderSubTitle: r => <span>
        {getTypeInfo(r.Type).niceName}
        <span className="sf-hide-id ms-1"> {r.id}</span>
      </span>
    }));
  
    Navigator.addSettings(new EntitySettings(RecipientEmbedded, undefined, { isViewable: "Never" }));
  
    Finder.quickFilterRules.push({
      name: "EmailAddress",
      applicable: (qt: QueryToken, value: unknown, sc: SearchControlLoaded) => qt.filterType == "Embedded" && qt.type.name == RecipientEmbedded.typeName,
      execute: async (qt: QueryToken, value: unknown, sc: SearchControlLoaded) => {
        var token = await sc.parseSingleFilterToken(qt.fullKey + ".EmailAddress");
  
        return sc.addQuickFilter(token, "EqualTo", (value as RecipientEmbedded | undefined)?.emailAddress);
      }
    });
  
    Finder.quickFilterRules.push({
      name: "RemoteEmailFolder",
      applicable: (qt: QueryToken, value: unknown, sc: SearchControlLoaded) => qt.filterType == "Model" && qt.type.name == RemoteEmailFolderModel.typeName,
      execute: async (qt: QueryToken, value: unknown, sc: SearchControlLoaded) => {
        return sc.addQuickFilter(qt, "EqualTo", value);
      }
    });
  
    Finder.Encoder.encodeModel[RemoteEmailFolderModel.typeName] = (model: RemoteEmailFolderModel) => {
      return model.folderId;
    };
    Finder.Decoder.decodeModel[RemoteEmailFolderModel.typeName] = (folderId: string | RemoteEmailFolderModel | null) => {
      if (!folderId)
        return null;
  
      if (typeof folderId == "string")
        return RemoteEmailFolderModel.New({ folderId: folderId, displayName: folderId });
  
      if (RemoteEmailFolderModel.isInstance(folderId))
        return folderId;
  
      throw new Error("Unexpected " + folderId); 
    };  
  
    Finder.filterValueFormatRules.push({
      name: "User",
      applicable: (f: FilterOptionParsed, ffc: Finder.FilterFormatterContext) => isFilterCondition(f) && f.token?.fullKey == "User" && f.operation == "EqualTo",
      renderValue: (f: FilterOptionParsed, ffc: Finder.FilterFormatterContext) => {
  
        return <EntityLine ctx={ffc.ctx} type={f.token!.type} create={false} onChange={() =>  ffc.handleValueChange(f)} label={ffc.label} mandatory={ffc.mandatory} />;
      }
    });
  
    Finder.filterValueFormatRules.push({
      name: "EmailFolder",
      applicable: (f: FilterOptionParsed, ffc: Finder.FilterFormatterContext) => isFilterCondition(f) && f.token?.type.name == RemoteEmailFolderModel.typeName ,
      renderValue: (f: FilterOptionParsed, ffc: Finder.FilterFormatterContext) => {
        var user = ffc.filterOptions.firstOrNull(a => isFilterCondition(a) && a.token?.fullKey == "User" && a.operation == "EqualTo");
        return <FolderLine ctx={ffc.ctx} mandatory={ffc.mandatory} label={ffc.label} user={user?.value as Lite<UserEntity>} onChange={()=> ffc.handleValueChange(f)} />
      }
    });
  
    Finder.addSettings({
      queryName: RemoteEmailMessageQuery.RemoteEmailMessages,
      allowCreate: false,
      markRowsColumn: "Id",
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
          <LinkButton title={SearchMessage.View.niceToString()} onClick={async e => openMessage(ctx.row, ctx.searchControl!)}>
            {EntityBaseController.getViewIcon()}
          </LinkButton>
        );
  
      }),
      formatters: {
        "Subject": new Finder.CellFormatter((val, cfc) => {
          var hasAttachments = cfc.searchControl?.getRowValue(cfc.row, "HasAttachments") ? <FontAwesomeIcon icon="paperclip" className="me-1" /> : null;
          var isRead = cfc.searchControl?.getRowValue(cfc.row, "IsRead") as boolean;
          var user = cfc.searchControl?.getRowValue(cfc.row, "User") as Lite<UserEntity>;
          var id = cfc.searchControl?.getRowValue(cfc.row, "Id") as string;
  
          var popIcon = <RemoteEmailPopover subject={val} isRead={isRead} user={user} remoteEmailId={id} />
  
          if (isRead)
            return <span className="try-no-wrap">{popIcon} {hasAttachments} {(val as string)?.etc(100)}</span>;
          else
            return <strong className="try-no-wrap">{popIcon} {hasAttachments} {(val as string)?.etc(100)}</strong>;
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
  
  export namespace API {
    export function getRemoteEmail(userOID: string, messageId: string): Promise<RemoteEmailMessageModel> {
      return ajaxGet({ url: `/api/remoteEmail/${userOID}/${messageId}` });
    }
  
    export function getRemoteFolders(oid: string): Promise<Array<RemoteEmailFolderModel>> {
      return ajaxGet({ url: `/api/remoteEmailFolders/${oid}` });
    }
  
    export function getRemoteAttachment(userOID: string, messageId: string, attachmentId: string): Promise<Response> {
      return ajaxGetRaw({ url: `/api/remoteEmail/${userOID}/${messageId}/attachment/${attachmentId}`});
    }
  
    export function getRemoteAttachmentUrl(userOID: string, messageId: string, attachmentId: string): string {
      return `/api/remoteEmail/${userOID}/${messageId}/attachment/${attachmentId}`
    }
  }
}


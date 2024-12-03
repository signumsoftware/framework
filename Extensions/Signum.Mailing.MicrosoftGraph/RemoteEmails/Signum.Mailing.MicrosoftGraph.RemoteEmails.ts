//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Signum/React/Reflection'
import * as Entities from '../../../Signum/React/Signum.Entities'
import * as Basics from '../../../Signum/React/Signum.Basics'
import * as Authorization from '../../Signum.Authorization/Signum.Authorization'


export const RecipientEmbedded: Type<RecipientEmbedded> = new Type<RecipientEmbedded>("RecipientEmbedded");
export interface RecipientEmbedded extends Entities.EmbeddedEntity {
  Type: "RecipientEmbedded";
  emailAddress: string | null;
  name: string | null;
}

export const RemoteAttachmentEmbedded: Type<RemoteAttachmentEmbedded> = new Type<RemoteAttachmentEmbedded>("RemoteAttachmentEmbedded");
export interface RemoteAttachmentEmbedded extends Entities.EmbeddedEntity {
  Type: "RemoteAttachmentEmbedded";
  id: string;
  name: string;
  size: number;
  lastModifiedDateTime: string /*DateTimeOffset*/;
  isInline: boolean;
  contentId: string | null;
}

export const RemoteEmailFolderModel: Type<RemoteEmailFolderModel> = new Type<RemoteEmailFolderModel>("RemoteEmailFolderModel");
export interface RemoteEmailFolderModel extends Entities.ModelEntity {
  Type: "RemoteEmailFolderModel";
  folderId: string;
  displayName: string;
}

export namespace RemoteEmailMessage {
  export const NotAuthorizedToViewEmailsFromOtherUsers: MessageKey = new MessageKey("RemoteEmailMessage", "NotAuthorizedToViewEmailsFromOtherUsers");
}

export const RemoteEmailMessageModel: Type<RemoteEmailMessageModel> = new Type<RemoteEmailMessageModel>("RemoteEmailMessageModel");
export interface RemoteEmailMessageModel extends Entities.ModelEntity {
  Type: "RemoteEmailMessageModel";
  id: string;
  user: Entities.Lite<Authorization.UserEntity>;
  subject: string;
  body: string;
  isBodyHtml: boolean;
  isDraft: boolean;
  isRead: boolean;
  hasAttachments: boolean;
  from: RecipientEmbedded;
  toRecipients: Entities.MList<RecipientEmbedded>;
  ccRecipients: Entities.MList<RecipientEmbedded>;
  bccRecipients: Entities.MList<RecipientEmbedded>;
  attachments: Entities.MList<RemoteAttachmentEmbedded>;
  createdDateTime: string /*DateTimeOffset*/ | null;
  lastModifiedDateTime: string /*DateTimeOffset*/ | null;
  receivedDateTime: string /*DateTimeOffset*/ | null;
  sentDateTime: string /*DateTimeOffset*/ | null;
  webLink: string | null;
  extension0: string | null;
  extension1: string | null;
  extension2: string | null;
  extension3: string | null;
}

export namespace RemoteEmailMessagePermission {
  export const ViewEmailMessagesFromOtherUsers : Basics.PermissionSymbol = registerSymbol("Permission", "RemoteEmailMessagePermission.ViewEmailMessagesFromOtherUsers");
}

export namespace RemoteEmailMessageQuery {
  export const RemoteEmailMessages: QueryKey = new QueryKey("RemoteEmailMessageQuery", "RemoteEmailMessages");
}


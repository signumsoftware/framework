//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Signum/React/Reflection'
import * as Entities from '../../../Signum/React/Signum.Entities'
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

export namespace RemoteEmailMessageMessage {
  export const UserFilterNotFound: MessageKey = new MessageKey("RemoteEmailMessageMessage", "UserFilterNotFound");
  export const User0HasNoMailbox: MessageKey = new MessageKey("RemoteEmailMessageMessage", "User0HasNoMailbox");
  export const Deleting: MessageKey = new MessageKey("RemoteEmailMessageMessage", "Deleting");
  export const Delete: MessageKey = new MessageKey("RemoteEmailMessageMessage", "Delete");
  export const Moving: MessageKey = new MessageKey("RemoteEmailMessageMessage", "Moving");
  export const Move: MessageKey = new MessageKey("RemoteEmailMessageMessage", "Move");
  export const AddCategory: MessageKey = new MessageKey("RemoteEmailMessageMessage", "AddCategory");
  export const RemoveCategory: MessageKey = new MessageKey("RemoteEmailMessageMessage", "RemoveCategory");
  export const ChangingCategories: MessageKey = new MessageKey("RemoteEmailMessageMessage", "ChangingCategories");
  export const Messages: MessageKey = new MessageKey("RemoteEmailMessageMessage", "Messages");
  export const Message: MessageKey = new MessageKey("RemoteEmailMessageMessage", "Message");
  export const SelectAFolder: MessageKey = new MessageKey("RemoteEmailMessageMessage", "SelectAFolder");
  export const PleaseConfirmYouWouldLikeToDelete0FromOutlook: MessageKey = new MessageKey("RemoteEmailMessageMessage", "PleaseConfirmYouWouldLikeToDelete0FromOutlook");
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
  folder: RemoteEmailFolderModel | null;
  categories: Entities.MList<string>;
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

export namespace RemoteEmailMessageQuery {
  export const RemoteEmailMessages: QueryKey = new QueryKey("RemoteEmailMessageQuery", "RemoteEmailMessages");
}


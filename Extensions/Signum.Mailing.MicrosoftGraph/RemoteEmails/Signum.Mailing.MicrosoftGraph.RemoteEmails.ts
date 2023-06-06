//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Signum/React/Reflection'
import * as Entities from '../../../Signum/React/Signum.Entities'
import * as Authorization from '../../Signum.Authorization/Signum.Authorization'


export const RecipientEmbedded = new Type<RecipientEmbedded>("RecipientEmbedded");
export interface RecipientEmbedded extends Entities.EmbeddedEntity {
  Type: "RecipientEmbedded";
  emailAddress: string | null;
  name: string | null;
}

export const RemoteAttachmentEmbedded = new Type<RemoteAttachmentEmbedded>("RemoteAttachmentEmbedded");
export interface RemoteAttachmentEmbedded extends Entities.EmbeddedEntity {
  Type: "RemoteAttachmentEmbedded";
  id: string;
  name: string;
  size: number;
  lastModifiedDateTime: string /*DateTimeOffset*/;
  isInline: boolean;
}

export const RemoteEmailMessageModel = new Type<RemoteEmailMessageModel>("RemoteEmailMessageModel");
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
}

export module RemoteEmailMessageQuery {
  export const RemoteEmailMessages = new QueryKey("RemoteEmailMessageQuery", "RemoteEmailMessages");
}


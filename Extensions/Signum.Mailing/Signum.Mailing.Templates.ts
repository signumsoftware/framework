//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as UserAssets from '../Signum.UserAssets/Signum.UserAssets'
import * as Queries from '../Signum.UserAssets/Signum.UserAssets.Queries'
import * as Templating from '../Signum.Templating/Signum.Templating'
import * as Mailing from './Signum.Mailing'
import * as Files from '../Signum.Files/Signum.Files'


export const EmailAddressSource: EnumType<EmailAddressSource> = new EnumType<EmailAddressSource>("EmailAddressSource");
export type EmailAddressSource =
  "QueryToken" |
  "HardcodedAddress" |
  "CurrentUser";

export const EmailMasterTemplateEntity: Type<EmailMasterTemplateEntity> = new Type<EmailMasterTemplateEntity>("EmailMasterTemplate");
export interface EmailMasterTemplateEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
  Type: "EmailMasterTemplate";
  name: string;
  isDefault: boolean;
  messages: Entities.MList<EmailMasterTemplateMessageEmbedded>;
  guid: string /*Guid*/;
  attachments: Entities.MList<IAttachmentGeneratorEntity>;
}

export const EmailMasterTemplateMessageEmbedded: Type<EmailMasterTemplateMessageEmbedded> = new Type<EmailMasterTemplateMessageEmbedded>("EmailMasterTemplateMessageEmbedded");
export interface EmailMasterTemplateMessageEmbedded extends Entities.EmbeddedEntity {
  Type: "EmailMasterTemplateMessageEmbedded";
  cultureInfo: Basics.CultureInfoEntity;
  text: string;
}

export namespace EmailMasterTemplateOperation {
  export const Clone : Operations.ConstructSymbol_From<EmailMasterTemplateEntity, EmailMasterTemplateEntity> = registerSymbol("Operation", "EmailMasterTemplateOperation.Clone");
  export const Create : Operations.ConstructSymbol_Simple<EmailMasterTemplateEntity> = registerSymbol("Operation", "EmailMasterTemplateOperation.Create");
  export const Save : Operations.ExecuteSymbol<EmailMasterTemplateEntity> = registerSymbol("Operation", "EmailMasterTemplateOperation.Save");
  export const Delete : Operations.DeleteSymbol<EmailMasterTemplateEntity> = registerSymbol("Operation", "EmailMasterTemplateOperation.Delete");
}

export const EmailMessageFormat: EnumType<EmailMessageFormat> = new EnumType<EmailMessageFormat>("EmailMessageFormat");
export type EmailMessageFormat =
  "PlainText" |
  "HtmlComplex" |
  "HtmlSimple";

export interface EmailTemplateAddressEmbedded extends Entities.EmbeddedEntity {
  addressSource: EmailAddressSource;
  emailAddress: string | null;
  displayName: string | null;
  token: Queries.QueryTokenEmbedded | null;
}

export const EmailTemplateEntity: Type<EmailTemplateEntity> = new Type<EmailTemplateEntity>("EmailTemplate");
export interface EmailTemplateEntity extends Entities.Entity, UserAssets.IUserAssetEntity, Templating.IContainsQuery {
  Type: "EmailTemplate";
  guid: string /*Guid*/;
  name: string;
  editableMessage: boolean;
  disableAuthorization: boolean;
  query: Basics.QueryEntity | null;
  model: Mailing.EmailModelEntity | null;
  from: EmailTemplateFromEmbedded | null;
  recipients: Entities.MList<EmailTemplateRecipientEmbedded>;
  groupResults: boolean;
  filters: Entities.MList<Queries.QueryFilterEmbedded>;
  orders: Entities.MList<Queries.QueryOrderEmbedded>;
  attachments: Entities.MList<IAttachmentGeneratorEntity>;
  masterTemplate: Entities.Lite<EmailMasterTemplateEntity> | null;
  messageFormat: EmailMessageFormat;
  messages: Entities.MList<EmailTemplateMessageEmbedded>;
  applicable: Templating.TemplateApplicableEval | null;
}

export const EmailTemplateFromEmbedded: Type<EmailTemplateFromEmbedded> = new Type<EmailTemplateFromEmbedded>("EmailTemplateFromEmbedded");
export interface EmailTemplateFromEmbedded extends EmailTemplateAddressEmbedded {
  Type: "EmailTemplateFromEmbedded";
  whenNone: WhenNoneFromBehaviour;
  whenMany: WhenManyFromBehaviour;
  azureUserId: string /*Guid*/ | null;
}

export namespace EmailTemplateMessage {
  export const EndDateMustBeHigherThanStartDate: MessageKey = new MessageKey("EmailTemplateMessage", "EndDateMustBeHigherThanStartDate");
  export const ThereAreNoMessagesForTheTemplate: MessageKey = new MessageKey("EmailTemplateMessage", "ThereAreNoMessagesForTheTemplate");
  export const ThereMustBeAMessageFor0: MessageKey = new MessageKey("EmailTemplateMessage", "ThereMustBeAMessageFor0");
  export const TheresMoreThanOneMessageForTheSameLanguage: MessageKey = new MessageKey("EmailTemplateMessage", "TheresMoreThanOneMessageForTheSameLanguage");
  export const TheTextMustContain0IndicatingReplacementPoint: MessageKey = new MessageKey("EmailTemplateMessage", "TheTextMustContain0IndicatingReplacementPoint");
  export const NewCulture: MessageKey = new MessageKey("EmailTemplateMessage", "NewCulture");
  export const TokenOrEmailAddressMustBeSet: MessageKey = new MessageKey("EmailTemplateMessage", "TokenOrEmailAddressMustBeSet");
  export const TokenAndEmailAddressCanNotBeSetAtTheSameTime: MessageKey = new MessageKey("EmailTemplateMessage", "TokenAndEmailAddressCanNotBeSetAtTheSameTime");
  export const TokenMustBeA0: MessageKey = new MessageKey("EmailTemplateMessage", "TokenMustBeA0");
  export const ShowPreview: MessageKey = new MessageKey("EmailTemplateMessage", "ShowPreview");
  export const HidePreview: MessageKey = new MessageKey("EmailTemplateMessage", "HidePreview");
}

export const EmailTemplateMessageEmbedded: Type<EmailTemplateMessageEmbedded> = new Type<EmailTemplateMessageEmbedded>("EmailTemplateMessageEmbedded");
export interface EmailTemplateMessageEmbedded extends Entities.EmbeddedEntity {
  Type: "EmailTemplateMessageEmbedded";
  cultureInfo: Basics.CultureInfoEntity;
  text: string;
  subject: string;
}

export namespace EmailTemplateOperation {
  export const CreateEmailTemplateFromModel : Operations.ConstructSymbol_From<EmailTemplateEntity, Mailing.EmailModelEntity> = registerSymbol("Operation", "EmailTemplateOperation.CreateEmailTemplateFromModel");
  export const Clone : Operations.ConstructSymbol_From<EmailTemplateEntity, EmailTemplateEntity> = registerSymbol("Operation", "EmailTemplateOperation.Clone");
  export const Create : Operations.ConstructSymbol_Simple<EmailTemplateEntity> = registerSymbol("Operation", "EmailTemplateOperation.Create");
  export const Save : Operations.ExecuteSymbol<EmailTemplateEntity> = registerSymbol("Operation", "EmailTemplateOperation.Save");
  export const Delete : Operations.DeleteSymbol<EmailTemplateEntity> = registerSymbol("Operation", "EmailTemplateOperation.Delete");
}

export const EmailTemplateRecipientEmbedded: Type<EmailTemplateRecipientEmbedded> = new Type<EmailTemplateRecipientEmbedded>("EmailTemplateRecipientEmbedded");
export interface EmailTemplateRecipientEmbedded extends EmailTemplateAddressEmbedded {
  Type: "EmailTemplateRecipientEmbedded";
  kind: Mailing.EmailRecipientKind;
  whenNone: WhenNoneRecipientsBehaviour;
  whenMany: WhenManyRecipiensBehaviour;
}

export namespace EmailTemplateViewMessage {
  export const InsertMessageContent: MessageKey = new MessageKey("EmailTemplateViewMessage", "InsertMessageContent");
  export const Insert: MessageKey = new MessageKey("EmailTemplateViewMessage", "Insert");
  export const Language: MessageKey = new MessageKey("EmailTemplateViewMessage", "Language");
}

export const EmailTemplateVisibleOn: EnumType<EmailTemplateVisibleOn> = new EnumType<EmailTemplateVisibleOn>("EmailTemplateVisibleOn");
export type EmailTemplateVisibleOn =
  "Single" |
  "Multiple" |
  "Query";

export const FileTokenAttachmentEntity: Type<FileTokenAttachmentEntity> = new Type<FileTokenAttachmentEntity>("FileTokenAttachment");
export interface FileTokenAttachmentEntity extends Entities.Entity, IAttachmentGeneratorEntity {
  Type: "FileTokenAttachment";
  fileName: string | null;
  contentId: string | null;
  type: Mailing.EmailAttachmentType;
  fileToken: Queries.QueryTokenEmbedded;
}

export interface IAttachmentGeneratorEntity extends Entities.Entity {
}

export const ImageAttachmentEntity: Type<ImageAttachmentEntity> = new Type<ImageAttachmentEntity>("ImageAttachment");
export interface ImageAttachmentEntity extends Entities.Entity, IAttachmentGeneratorEntity {
  Type: "ImageAttachment";
  fileName: string | null;
  contentId: string;
  type: Mailing.EmailAttachmentType;
  file: Files.FileEmbedded;
}

export const WhenManyFromBehaviour: EnumType<WhenManyFromBehaviour> = new EnumType<WhenManyFromBehaviour>("WhenManyFromBehaviour");
export type WhenManyFromBehaviour =
  "SplitMessages" |
  "FistResult";

export const WhenManyRecipiensBehaviour: EnumType<WhenManyRecipiensBehaviour> = new EnumType<WhenManyRecipiensBehaviour>("WhenManyRecipiensBehaviour");
export type WhenManyRecipiensBehaviour =
  "SplitMessages" |
  "KeepOneMessageWithManyRecipients";

export const WhenNoneFromBehaviour: EnumType<WhenNoneFromBehaviour> = new EnumType<WhenNoneFromBehaviour>("WhenNoneFromBehaviour");
export type WhenNoneFromBehaviour =
  "ThrowException" |
  "NoMessage" |
  "DefaultFrom";

export const WhenNoneRecipientsBehaviour: EnumType<WhenNoneRecipientsBehaviour> = new EnumType<WhenNoneRecipientsBehaviour>("WhenNoneRecipientsBehaviour");
export type WhenNoneRecipientsBehaviour =
  "ThrowException" |
  "NoMessage" |
  "NoRecipients";


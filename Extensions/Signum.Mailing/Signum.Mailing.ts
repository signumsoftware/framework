//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as Templates from './Signum.Mailing.Templates'
import * as Files from '../Signum.Files/Signum.Files'

import * as External from './Signum.Mailing.External'

export namespace AsyncEmailSenderPermission {
  export const ViewAsyncEmailSenderPanel : Basics.PermissionSymbol = registerSymbol("Permission", "AsyncEmailSenderPermission.ViewAsyncEmailSenderPanel");
}

export const ClientCertificationFileEmbedded: Type<ClientCertificationFileEmbedded> = new Type<ClientCertificationFileEmbedded>("ClientCertificationFileEmbedded");
export interface ClientCertificationFileEmbedded extends Entities.EmbeddedEntity {
  Type: "ClientCertificationFileEmbedded";
  fullFilePath: string;
}

export interface EmailAddressEmbedded extends Entities.EmbeddedEntity {
  emailOwner: Entities.Lite<Basics.IEmailOwnerEntity> | null;
  emailAddress: string;
  invalidEmail: boolean;
  displayName: string | null;
}

export const EmailAttachmentEmbedded: Type<EmailAttachmentEmbedded> = new Type<EmailAttachmentEmbedded>("EmailAttachmentEmbedded");
export interface EmailAttachmentEmbedded extends Entities.EmbeddedEntity {
  Type: "EmailAttachmentEmbedded";
  type: EmailAttachmentType;
  file: Files.FilePathEmbedded;
  contentId: string;
}

export const EmailAttachmentType: EnumType<EmailAttachmentType> = new EnumType<EmailAttachmentType>("EmailAttachmentType");
export type EmailAttachmentType =
  "Attachment" |
  "LinkedResource";

export const EmailConfigurationEmbedded: Type<EmailConfigurationEmbedded> = new Type<EmailConfigurationEmbedded>("EmailConfigurationEmbedded");
export interface EmailConfigurationEmbedded extends Entities.EmbeddedEntity {
  Type: "EmailConfigurationEmbedded";
  defaultCulture: Basics.CultureInfoEntity;
  urlLeft: string;
  sendEmails: boolean;
  reciveEmails: boolean;
  overrideEmailAddress: string | null;
  avoidSendingEmailsOlderThan: number | null;
  chunkSizeSendingEmails: number;
  maxEmailSendRetries: number;
  asyncSenderPeriod: number;
}

export namespace EmailFileType {
  export const Attachment : Files.FileTypeSymbol = registerSymbol("FileType", "EmailFileType.Attachment");
}

export const EmailFromEmbedded: Type<EmailFromEmbedded> = new Type<EmailFromEmbedded>("EmailFromEmbedded");
export interface EmailFromEmbedded extends EmailAddressEmbedded {
  Type: "EmailFromEmbedded";
  azureUserId: string /*Guid*/ | null;
}

export const EmailMessageEntity: Type<EmailMessageEntity> = new Type<EmailMessageEntity>("EmailMessage");
export interface EmailMessageEntity extends Entities.Entity {
  Type: "EmailMessage";
  recipients: Entities.MList<EmailRecipientEmbedded>;
  target: Entities.Lite<Entities.Entity> | null;
  from: EmailFromEmbedded;
  template: Entities.Lite<Templates.EmailTemplateEntity> | null;
  creationDate: string /*DateTime*/;
  sent: string /*DateTime*/ | null;
  sentBy: Entities.Lite<EmailSenderConfigurationEntity> | null;
  receptionNotified: string /*DateTime*/ | null;
  subject: string | null;
  body: Entities.BigStringEmbedded;
  bodyHash: string | null;
  isBodyHtml: boolean;
  exception: Entities.Lite<Basics.ExceptionEntity> | null;
  state: EmailMessageState;
  uniqueIdentifier: string /*Guid*/ | null;
  editableMessage: boolean;
  processIdentifier: string /*Guid*/ | null;
  sendRetries: number;
  attachments: Entities.MList<EmailAttachmentEmbedded>;
}

export namespace EmailMessageMessage {
  export const TheEmailMessageCannotBeSentFromState0: MessageKey = new MessageKey("EmailMessageMessage", "TheEmailMessageCannotBeSentFromState0");
  export const Message: MessageKey = new MessageKey("EmailMessageMessage", "Message");
  export const Messages: MessageKey = new MessageKey("EmailMessageMessage", "Messages");
  export const RemainingMessages: MessageKey = new MessageKey("EmailMessageMessage", "RemainingMessages");
  export const ExceptionMessages: MessageKey = new MessageKey("EmailMessageMessage", "ExceptionMessages");
  export const _01requiresExtraParameters: MessageKey = new MessageKey("EmailMessageMessage", "_01requiresExtraParameters");
}

export namespace EmailMessageOperation {
  export const Save : Operations.ExecuteSymbol<EmailMessageEntity> = registerSymbol("Operation", "EmailMessageOperation.Save");
  export const ReadyToSend : Operations.ExecuteSymbol<EmailMessageEntity> = registerSymbol("Operation", "EmailMessageOperation.ReadyToSend");
  export const Send : Operations.ExecuteSymbol<EmailMessageEntity> = registerSymbol("Operation", "EmailMessageOperation.Send");
  export const ReSend : Operations.ConstructSymbol_From<EmailMessageEntity, EmailMessageEntity> = registerSymbol("Operation", "EmailMessageOperation.ReSend");
  export const CreateMail : Operations.ConstructSymbol_Simple<EmailMessageEntity> = registerSymbol("Operation", "EmailMessageOperation.CreateMail");
  export const CreateEmailFromTemplate : Operations.ConstructSymbol_From<EmailMessageEntity, Templates.EmailTemplateEntity> = registerSymbol("Operation", "EmailMessageOperation.CreateEmailFromTemplate");
  export const Delete : Operations.DeleteSymbol<EmailMessageEntity> = registerSymbol("Operation", "EmailMessageOperation.Delete");
}

export const EmailMessageState: EnumType<EmailMessageState> = new EnumType<EmailMessageState>("EmailMessageState");
export type EmailMessageState =
  "Created" |
  "Draft" |
  "ReadyToSend" |
  "RecruitedForSending" |
  "Sent" |
  "SentException" |
  "ReceptionNotified" |
  "Received" |
  "Outdated";

export const EmailModelEntity: Type<EmailModelEntity> = new Type<EmailModelEntity>("EmailModel");
export interface EmailModelEntity extends Entities.Entity {
  Type: "EmailModel";
  fullClassName: string;
}

export const EmailRecipientEmbedded: Type<EmailRecipientEmbedded> = new Type<EmailRecipientEmbedded>("EmailRecipientEmbedded");
export interface EmailRecipientEmbedded extends EmailAddressEmbedded {
  Type: "EmailRecipientEmbedded";
  kind: EmailRecipientKind;
}

export const EmailRecipientKind: EnumType<EmailRecipientKind> = new EnumType<EmailRecipientKind>("EmailRecipientKind");
export type EmailRecipientKind =
  "To" |
  "Cc" |
  "Bcc";

export const EmailSenderConfigurationEntity: Type<EmailSenderConfigurationEntity> = new Type<EmailSenderConfigurationEntity>("EmailSenderConfiguration");
export interface EmailSenderConfigurationEntity extends Entities.Entity {
  Type: "EmailSenderConfiguration";
  name: string;
  defaultFrom: EmailFromEmbedded | null;
  additionalRecipients: Entities.MList<EmailRecipientEmbedded>;
  service: EmailServiceEntity;
}

export namespace EmailSenderConfigurationOperation {
  export const Save : Operations.ExecuteSymbol<EmailSenderConfigurationEntity> = registerSymbol("Operation", "EmailSenderConfigurationOperation.Save");
  export const Clone : Operations.ConstructSymbol_From<EmailSenderConfigurationEntity, EmailSenderConfigurationEntity> = registerSymbol("Operation", "EmailSenderConfigurationOperation.Clone");
}

export interface EmailServiceEntity extends Entities.Entity {
}

export const SmtpEmailServiceEntity: Type<SmtpEmailServiceEntity> = new Type<SmtpEmailServiceEntity>("SmtpEmailService");
export interface SmtpEmailServiceEntity extends EmailServiceEntity {
  Type: "SmtpEmailService";
  deliveryFormat: External.SmtpDeliveryFormat;
  deliveryMethod: External.SmtpDeliveryMethod;
  network: SmtpNetworkDeliveryEmbedded | null;
  pickupDirectoryLocation: string | null;
}

export const SmtpNetworkDeliveryEmbedded: Type<SmtpNetworkDeliveryEmbedded> = new Type<SmtpNetworkDeliveryEmbedded>("SmtpNetworkDeliveryEmbedded");
export interface SmtpNetworkDeliveryEmbedded extends Entities.EmbeddedEntity {
  Type: "SmtpNetworkDeliveryEmbedded";
  host: string;
  port: number;
  username: string | null;
  password: string | null;
  newPassword: string | null;
  useDefaultCredentials: boolean;
  enableSSL: boolean;
  clientCertificationFiles: Entities.MList<ClientCertificationFileEmbedded>;
}


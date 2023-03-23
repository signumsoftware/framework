//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum.React/Reflection'
import * as Entities from '../../Signum.React/Signum.Entities'
import * as Basics from '../../Signum.React/Signum.Entities.Basics'
import * as Signum from '../../Signum.React/Signum.Basics'
import * as Operations from '../../Signum.React/Signum.Operations'
import * as Templates from './Signum.Mailing.Templates'
import * as Files from '../Signum.Files.React/Signum.Files'


export module AsyncEmailSenderPermission {
  export const ViewAsyncEmailSenderPanel : Signum.PermissionSymbol = registerSymbol("Permission", "AsyncEmailSenderPermission.ViewAsyncEmailSenderPanel");
}

export const CertFileType = new EnumType<CertFileType>("CertFileType");
export type CertFileType =
  "CertFile" |
  "SignedFile";

export const ClientCertificationFileEmbedded = new Type<ClientCertificationFileEmbedded>("ClientCertificationFileEmbedded");
export interface ClientCertificationFileEmbedded extends Entities.EmbeddedEntity {
  Type: "ClientCertificationFileEmbedded";
  fullFilePath: string;
  certFileType: CertFileType;
}

export interface EmailAddressEmbedded extends Entities.EmbeddedEntity {
  emailOwner: Entities.Lite<Signum.IEmailOwnerEntity> | null;
  emailAddress: string;
  invalidEmail: boolean;
  displayName: string | null;
}

export const EmailAttachmentEmbedded = new Type<EmailAttachmentEmbedded>("EmailAttachmentEmbedded");
export interface EmailAttachmentEmbedded extends Entities.EmbeddedEntity {
  Type: "EmailAttachmentEmbedded";
  type: EmailAttachmentType;
  file: Files.FilePathEmbedded;
  contentId: string;
}

export const EmailAttachmentType = new EnumType<EmailAttachmentType>("EmailAttachmentType");
export type EmailAttachmentType =
  "Attachment" |
  "LinkedResource";

export const EmailConfigurationEmbedded = new Type<EmailConfigurationEmbedded>("EmailConfigurationEmbedded");
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

export module EmailFileType {
  export const Attachment : Files.FileTypeSymbol = registerSymbol("FileType", "EmailFileType.Attachment");
}

export const EmailFromEmbedded = new Type<EmailFromEmbedded>("EmailFromEmbedded");
export interface EmailFromEmbedded extends EmailAddressEmbedded {
  Type: "EmailFromEmbedded";
  azureUserId: string /*Guid*/ | null;
}

export const EmailMessageEntity = new Type<EmailMessageEntity>("EmailMessage");
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
  exception: Entities.Lite<Signum.ExceptionEntity> | null;
  state: EmailMessageState;
  uniqueIdentifier: string /*Guid*/ | null;
  editableMessage: boolean;
  processIdentifier: string /*Guid*/ | null;
  sendRetries: number;
  attachments: Entities.MList<EmailAttachmentEmbedded>;
}

export module EmailMessageMessage {
  export const TheEmailMessageCannotBeSentFromState0 = new MessageKey("EmailMessageMessage", "TheEmailMessageCannotBeSentFromState0");
  export const Message = new MessageKey("EmailMessageMessage", "Message");
  export const Messages = new MessageKey("EmailMessageMessage", "Messages");
  export const RemainingMessages = new MessageKey("EmailMessageMessage", "RemainingMessages");
  export const ExceptionMessages = new MessageKey("EmailMessageMessage", "ExceptionMessages");
  export const _01requiresExtraParameters = new MessageKey("EmailMessageMessage", "_01requiresExtraParameters");
}

export module EmailMessageOperation {
  export const Save : Operations.ExecuteSymbol<EmailMessageEntity> = registerSymbol("Operation", "EmailMessageOperation.Save");
  export const ReadyToSend : Operations.ExecuteSymbol<EmailMessageEntity> = registerSymbol("Operation", "EmailMessageOperation.ReadyToSend");
  export const Send : Operations.ExecuteSymbol<EmailMessageEntity> = registerSymbol("Operation", "EmailMessageOperation.Send");
  export const ReSend : Operations.ConstructSymbol_From<EmailMessageEntity, EmailMessageEntity> = registerSymbol("Operation", "EmailMessageOperation.ReSend");
  export const CreateMail : Operations.ConstructSymbol_Simple<EmailMessageEntity> = registerSymbol("Operation", "EmailMessageOperation.CreateMail");
  export const CreateEmailFromTemplate : Operations.ConstructSymbol_From<EmailMessageEntity, Templates.EmailTemplateEntity> = registerSymbol("Operation", "EmailMessageOperation.CreateEmailFromTemplate");
  export const Delete : Operations.DeleteSymbol<EmailMessageEntity> = registerSymbol("Operation", "EmailMessageOperation.Delete");
}

export const EmailMessageState = new EnumType<EmailMessageState>("EmailMessageState");
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

export const EmailModelEntity = new Type<EmailModelEntity>("EmailModel");
export interface EmailModelEntity extends Entities.Entity {
  Type: "EmailModel";
  fullClassName: string;
}

export const EmailRecipientEmbedded = new Type<EmailRecipientEmbedded>("EmailRecipientEmbedded");
export interface EmailRecipientEmbedded extends EmailAddressEmbedded {
  Type: "EmailRecipientEmbedded";
  kind: EmailRecipientKind;
}

export const EmailRecipientKind = new EnumType<EmailRecipientKind>("EmailRecipientKind");
export type EmailRecipientKind =
  "To" |
  "Cc" |
  "Bcc";

export const EmailSenderConfigurationEntity = new Type<EmailSenderConfigurationEntity>("EmailSenderConfiguration");
export interface EmailSenderConfigurationEntity extends Entities.Entity {
  Type: "EmailSenderConfiguration";
  name: string;
  defaultFrom: EmailFromEmbedded | null;
  additionalRecipients: Entities.MList<EmailRecipientEmbedded>;
  service: EmailServiceEntity;
}

export module EmailSenderConfigurationOperation {
  export const Save : Operations.ExecuteSymbol<EmailSenderConfigurationEntity> = registerSymbol("Operation", "EmailSenderConfigurationOperation.Save");
  export const Clone : Operations.ConstructSymbol_From<EmailSenderConfigurationEntity, EmailSenderConfigurationEntity> = registerSymbol("Operation", "EmailSenderConfigurationOperation.Clone");
}

export interface EmailServiceEntity extends Entities.Entity {
}

export const SmtpEmailServiceEntity = new Type<SmtpEmailServiceEntity>("SmtpEmailService");
export interface SmtpEmailServiceEntity extends EmailServiceEntity {
  Type: "SmtpEmailService";
  deliveryFormat: External.SmtpDeliveryFormat;
  deliveryMethod: External.SmtpDeliveryMethod;
  network: SmtpNetworkDeliveryEmbedded | null;
  pickupDirectoryLocation: string | null;
}

export const SmtpNetworkDeliveryEmbedded = new Type<SmtpNetworkDeliveryEmbedded>("SmtpNetworkDeliveryEmbedded");
export interface SmtpNetworkDeliveryEmbedded extends Entities.EmbeddedEntity {
  Type: "SmtpNetworkDeliveryEmbedded";
  host: string;
  port: number;
  username: string | null;
  password: string | null;
  useDefaultCredentials: boolean;
  enableSSL: boolean;
  clientCertificationFiles: Entities.MList<ClientCertificationFileEmbedded>;
}


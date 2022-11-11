//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum.React/Scripts/Reflection'
import * as Entities from '../../Signum.React/Scripts/Signum.Entities'
import * as Signum from '../../Signum.React/Scripts/Signum.Entities.Basics'
import * as Basics from '../Basics/Signum.Entities.Basics'
import * as UserAssets from '../UserAssets/Signum.Entities.UserAssets'
import * as Processes from '../Processes/Signum.Entities.Processes'
import * as Files from '../Files/Signum.Entities.Files'
import * as UserQueries from '../UserQueries/Signum.Entities.UserQueries'
import * as Templating from '../Templating/Signum.Entities.Templating'
import * as Scheduler from '../Scheduler/Signum.Entities.Scheduler'
import * as Authorization from '../Authorization/Signum.Entities.Authorization'

export interface Pop3ConfigurationEntity {
    newPassword: string;
}

export interface SmtpNetworkDeliveryEmbedded {
    newPassword: string;
}

export interface ExchangeWebServiceEmailServiceEntity {
    newPassword: string;
}

export module AsyncEmailSenderPermission {
  export const ViewAsyncEmailSenderPanel : Authorization.PermissionSymbol = registerSymbol("Permission", "AsyncEmailSenderPermission.ViewAsyncEmailSenderPanel");
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
  emailOwner: Entities.Lite<IEmailOwnerEntity> | null;
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

export const EmailMasterTemplateEntity = new Type<EmailMasterTemplateEntity>("EmailMasterTemplate");
export interface EmailMasterTemplateEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
  Type: "EmailMasterTemplate";
  name: string;
  isDefault: boolean;
  messages: Entities.MList<EmailMasterTemplateMessageEmbedded>;
  guid: string /*Guid*/;
  attachments: Entities.MList<IAttachmentGeneratorEntity>;
}

export const EmailMasterTemplateMessageEmbedded = new Type<EmailMasterTemplateMessageEmbedded>("EmailMasterTemplateMessageEmbedded");
export interface EmailMasterTemplateMessageEmbedded extends Entities.EmbeddedEntity {
  Type: "EmailMasterTemplateMessageEmbedded";
  cultureInfo: Basics.CultureInfoEntity;
  text: string;
}

export module EmailMasterTemplateOperation {
  export const Create : Entities.ConstructSymbol_Simple<EmailMasterTemplateEntity> = registerSymbol("Operation", "EmailMasterTemplateOperation.Create");
  export const Save : Entities.ExecuteSymbol<EmailMasterTemplateEntity> = registerSymbol("Operation", "EmailMasterTemplateOperation.Save");
}

export const EmailMessageEntity = new Type<EmailMessageEntity>("EmailMessage");
export interface EmailMessageEntity extends Entities.Entity, Processes.IProcessLineDataEntity {
  Type: "EmailMessage";
  recipients: Entities.MList<EmailRecipientEmbedded>;
  target: Entities.Lite<Entities.Entity> | null;
  from: EmailFromEmbedded;
  template: Entities.Lite<EmailTemplateEntity> | null;
  creationDate: string /*DateTime*/;
  sent: string /*DateTime*/ | null;
  sentBy: Entities.Lite<EmailSenderConfigurationEntity> | null;
  receptionNotified: string /*DateTime*/ | null;
  subject: string | null;
  body: Signum.BigStringEmbedded;
  bodyHash: string | null;
  isBodyHtml: boolean;
  exception: Entities.Lite<Signum.ExceptionEntity> | null;
  state: EmailMessageState;
  uniqueIdentifier: string /*Guid*/ | null;
  editableMessage: boolean;
  package: Entities.Lite<EmailPackageEntity> | null;
  processIdentifier: string /*Guid*/ | null;
  sendRetries: number;
  attachments: Entities.MList<EmailAttachmentEmbedded>;
}

export const EmailMessageFormat = new EnumType<EmailMessageFormat>("EmailMessageFormat");
export type EmailMessageFormat =
  "PlainText" |
  "HtmlComplex" |
  "HtmlSimple";

export module EmailMessageMessage {
  export const TheEmailMessageCannotBeSentFromState0 = new MessageKey("EmailMessageMessage", "TheEmailMessageCannotBeSentFromState0");
  export const Message = new MessageKey("EmailMessageMessage", "Message");
  export const Messages = new MessageKey("EmailMessageMessage", "Messages");
  export const RemainingMessages = new MessageKey("EmailMessageMessage", "RemainingMessages");
  export const ExceptionMessages = new MessageKey("EmailMessageMessage", "ExceptionMessages");
  export const _01requiresExtraParameters = new MessageKey("EmailMessageMessage", "_01requiresExtraParameters");
}

export module EmailMessageOperation {
  export const Save : Entities.ExecuteSymbol<EmailMessageEntity> = registerSymbol("Operation", "EmailMessageOperation.Save");
  export const ReadyToSend : Entities.ExecuteSymbol<EmailMessageEntity> = registerSymbol("Operation", "EmailMessageOperation.ReadyToSend");
  export const Send : Entities.ExecuteSymbol<EmailMessageEntity> = registerSymbol("Operation", "EmailMessageOperation.Send");
  export const ReSend : Entities.ConstructSymbol_From<EmailMessageEntity, EmailMessageEntity> = registerSymbol("Operation", "EmailMessageOperation.ReSend");
  export const ReSendEmails : Entities.ConstructSymbol_FromMany<Processes.ProcessEntity, EmailMessageEntity> = registerSymbol("Operation", "EmailMessageOperation.ReSendEmails");
  export const CreateMail : Entities.ConstructSymbol_Simple<EmailMessageEntity> = registerSymbol("Operation", "EmailMessageOperation.CreateMail");
  export const CreateEmailFromTemplate : Entities.ConstructSymbol_From<EmailMessageEntity, EmailTemplateEntity> = registerSymbol("Operation", "EmailMessageOperation.CreateEmailFromTemplate");
  export const Delete : Entities.DeleteSymbol<EmailMessageEntity> = registerSymbol("Operation", "EmailMessageOperation.Delete");
}

export module EmailMessageProcess {
  export const CreateEmailsSendAsync : Processes.ProcessAlgorithmSymbol = registerSymbol("ProcessAlgorithm", "EmailMessageProcess.CreateEmailsSendAsync");
  export const SendEmails : Processes.ProcessAlgorithmSymbol = registerSymbol("ProcessAlgorithm", "EmailMessageProcess.SendEmails");
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

export const EmailPackageEntity = new Type<EmailPackageEntity>("EmailPackage");
export interface EmailPackageEntity extends Entities.Entity, Processes.IProcessDataEntity {
  Type: "EmailPackage";
  name: string | null;
}

export const EmailReceptionInfoEmbedded = new Type<EmailReceptionInfoEmbedded>("EmailReceptionInfoEmbedded");
export interface EmailReceptionInfoEmbedded extends Entities.EmbeddedEntity {
  Type: "EmailReceptionInfoEmbedded";
  uniqueId: string;
  reception: Entities.Lite<Pop3ReceptionEntity>;
  rawContent: Signum.BigStringEmbedded;
  sentDate: string /*DateTime*/;
  receivedDate: string /*DateTime*/;
  deletionDate: string /*DateTime*/ | null;
}

export const EmailReceptionMixin = new Type<EmailReceptionMixin>("EmailReceptionMixin");
export interface EmailReceptionMixin extends Entities.MixinEntity {
  Type: "EmailReceptionMixin";
  receptionInfo: EmailReceptionInfoEmbedded | null;
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
  export const Save : Entities.ExecuteSymbol<EmailSenderConfigurationEntity> = registerSymbol("Operation", "EmailSenderConfigurationOperation.Save");
  export const Clone : Entities.ConstructSymbol_From<EmailSenderConfigurationEntity, EmailSenderConfigurationEntity> = registerSymbol("Operation", "EmailSenderConfigurationOperation.Clone");
}

export interface EmailServiceEntity extends Entities.Entity {
}

export interface EmailTemplateAddressEmbedded extends Entities.EmbeddedEntity {
  emailAddress: string | null;
  displayName: string | null;
  token: UserAssets.QueryTokenEmbedded | null;
}

export const EmailTemplateEntity = new Type<EmailTemplateEntity>("EmailTemplate");
export interface EmailTemplateEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
  Type: "EmailTemplate";
  guid: string /*Guid*/;
  name: string;
  editableMessage: boolean;
  disableAuthorization: boolean;
  query: Signum.QueryEntity;
  model: EmailModelEntity | null;
  from: EmailTemplateFromEmbedded | null;
  recipients: Entities.MList<EmailTemplateRecipientEmbedded>;
  groupResults: boolean;
  filters: Entities.MList<UserQueries.QueryFilterEmbedded>;
  orders: Entities.MList<UserQueries.QueryOrderEmbedded>;
  attachments: Entities.MList<IAttachmentGeneratorEntity>;
  masterTemplate: Entities.Lite<EmailMasterTemplateEntity> | null;
  messageFormat: EmailMessageFormat;
  messages: Entities.MList<EmailTemplateMessageEmbedded>;
  applicable: Templating.TemplateApplicableEval | null;
}

export const EmailTemplateFromEmbedded = new Type<EmailTemplateFromEmbedded>("EmailTemplateFromEmbedded");
export interface EmailTemplateFromEmbedded extends EmailTemplateAddressEmbedded {
  Type: "EmailTemplateFromEmbedded";
  whenNone: WhenNoneFromBehaviour;
  whenMany: WhenManyFromBehaviour;
  azureUserId: string /*Guid*/ | null;
}

export module EmailTemplateMessage {
  export const EndDateMustBeHigherThanStartDate = new MessageKey("EmailTemplateMessage", "EndDateMustBeHigherThanStartDate");
  export const ThereAreNoMessagesForTheTemplate = new MessageKey("EmailTemplateMessage", "ThereAreNoMessagesForTheTemplate");
  export const ThereMustBeAMessageFor0 = new MessageKey("EmailTemplateMessage", "ThereMustBeAMessageFor0");
  export const TheresMoreThanOneMessageForTheSameLanguage = new MessageKey("EmailTemplateMessage", "TheresMoreThanOneMessageForTheSameLanguage");
  export const TheTextMustContain0IndicatingReplacementPoint = new MessageKey("EmailTemplateMessage", "TheTextMustContain0IndicatingReplacementPoint");
  export const ImpossibleToAccess0BecauseTheTemplateHAsNo1 = new MessageKey("EmailTemplateMessage", "ImpossibleToAccess0BecauseTheTemplateHAsNo1");
  export const NewCulture = new MessageKey("EmailTemplateMessage", "NewCulture");
  export const TokenOrEmailAddressMustBeSet = new MessageKey("EmailTemplateMessage", "TokenOrEmailAddressMustBeSet");
  export const TokenAndEmailAddressCanNotBeSetAtTheSameTime = new MessageKey("EmailTemplateMessage", "TokenAndEmailAddressCanNotBeSetAtTheSameTime");
  export const TokenMustBeA0 = new MessageKey("EmailTemplateMessage", "TokenMustBeA0");
  export const ShowPreview = new MessageKey("EmailTemplateMessage", "ShowPreview");
  export const HidePreview = new MessageKey("EmailTemplateMessage", "HidePreview");
}

export const EmailTemplateMessageEmbedded = new Type<EmailTemplateMessageEmbedded>("EmailTemplateMessageEmbedded");
export interface EmailTemplateMessageEmbedded extends Entities.EmbeddedEntity {
  Type: "EmailTemplateMessageEmbedded";
  cultureInfo: Basics.CultureInfoEntity;
  text: string;
  subject: string;
}

export module EmailTemplateOperation {
  export const CreateEmailTemplateFromModel : Entities.ConstructSymbol_From<EmailTemplateEntity, EmailModelEntity> = registerSymbol("Operation", "EmailTemplateOperation.CreateEmailTemplateFromModel");
  export const Create : Entities.ConstructSymbol_Simple<EmailTemplateEntity> = registerSymbol("Operation", "EmailTemplateOperation.Create");
  export const Save : Entities.ExecuteSymbol<EmailTemplateEntity> = registerSymbol("Operation", "EmailTemplateOperation.Save");
  export const Delete : Entities.DeleteSymbol<EmailTemplateEntity> = registerSymbol("Operation", "EmailTemplateOperation.Delete");
}

export const EmailTemplateRecipientEmbedded = new Type<EmailTemplateRecipientEmbedded>("EmailTemplateRecipientEmbedded");
export interface EmailTemplateRecipientEmbedded extends EmailTemplateAddressEmbedded {
  Type: "EmailTemplateRecipientEmbedded";
  kind: EmailRecipientKind;
  whenNone: WhenNoneRecipientsBehaviour;
  whenMany: WhenManyRecipiensBehaviour;
}

export module EmailTemplateViewMessage {
  export const InsertMessageContent = new MessageKey("EmailTemplateViewMessage", "InsertMessageContent");
  export const Insert = new MessageKey("EmailTemplateViewMessage", "Insert");
  export const Language = new MessageKey("EmailTemplateViewMessage", "Language");
}

export const EmailTemplateVisibleOn = new EnumType<EmailTemplateVisibleOn>("EmailTemplateVisibleOn");
export type EmailTemplateVisibleOn =
  "Single" |
  "Multiple" |
  "Query";

export const ExchangeWebServiceEmailServiceEntity = new Type<ExchangeWebServiceEmailServiceEntity>("ExchangeWebServiceEmailService");
export interface ExchangeWebServiceEmailServiceEntity extends EmailServiceEntity {
  Type: "ExchangeWebServiceEmailService";
  exchangeVersion: External.ExchangeVersion;
  url: string | null;
  username: string | null;
  password: string | null;
  useDefaultCredentials: boolean;
}

export interface IAttachmentGeneratorEntity extends Entities.Entity {
}

export interface IEmailOwnerEntity extends Entities.Entity {
}

export const ImageAttachmentEntity = new Type<ImageAttachmentEntity>("ImageAttachment");
export interface ImageAttachmentEntity extends Entities.Entity, IAttachmentGeneratorEntity {
  Type: "ImageAttachment";
  fileName: string | null;
  contentId: string;
  type: EmailAttachmentType;
  file: Files.FileEmbedded;
}

export const MicrosoftGraphEmailServiceEntity = new Type<MicrosoftGraphEmailServiceEntity>("MicrosoftGraphEmailService");
export interface MicrosoftGraphEmailServiceEntity extends EmailServiceEntity {
  Type: "MicrosoftGraphEmailService";
  useActiveDirectoryConfiguration: boolean;
  azure_ApplicationID: string /*Guid*/ | null;
  azure_DirectoryID: string /*Guid*/ | null;
  azure_ClientSecret: string | null;
}

export module Pop3ConfigurationAction {
  export const ReceiveAllActivePop3Configurations : Scheduler.SimpleTaskSymbol = registerSymbol("SimpleTask", "Pop3ConfigurationAction.ReceiveAllActivePop3Configurations");
}

export const Pop3ConfigurationEntity = new Type<Pop3ConfigurationEntity>("Pop3Configuration");
export interface Pop3ConfigurationEntity extends Entities.Entity, Scheduler.ITaskEntity {
  Type: "Pop3Configuration";
  active: boolean;
  fullComparation: boolean;
  port: number;
  host: string;
  username: string | null;
  password: string | null;
  enableSSL: boolean;
  readTimeout: number;
  deleteMessagesAfter: number | null;
  clientCertificationFiles: Entities.MList<ClientCertificationFileEmbedded>;
}

export module Pop3ConfigurationOperation {
  export const Save : Entities.ExecuteSymbol<Pop3ConfigurationEntity> = registerSymbol("Operation", "Pop3ConfigurationOperation.Save");
  export const ReceiveEmails : Entities.ConstructSymbol_From<Pop3ReceptionEntity, Pop3ConfigurationEntity> = registerSymbol("Operation", "Pop3ConfigurationOperation.ReceiveEmails");
  export const ReceiveLastEmails : Entities.ConstructSymbol_From<Pop3ReceptionEntity, Pop3ConfigurationEntity> = registerSymbol("Operation", "Pop3ConfigurationOperation.ReceiveLastEmails");
}

export const Pop3ReceptionEntity = new Type<Pop3ReceptionEntity>("Pop3Reception");
export interface Pop3ReceptionEntity extends Entities.Entity {
  Type: "Pop3Reception";
  pop3Configuration: Entities.Lite<Pop3ConfigurationEntity>;
  startDate: string /*DateTime*/;
  endDate: string /*DateTime*/ | null;
  newEmails: number;
  serverEmails: number;
  lastServerMessageUID: string | null;
  mailsFromDifferentAccounts: boolean;
  exception: Entities.Lite<Signum.ExceptionEntity> | null;
}

export const Pop3ReceptionExceptionEntity = new Type<Pop3ReceptionExceptionEntity>("Pop3ReceptionException");
export interface Pop3ReceptionExceptionEntity extends Entities.Entity {
  Type: "Pop3ReceptionException";
  reception: Entities.Lite<Pop3ReceptionEntity>;
  exception: Entities.Lite<Signum.ExceptionEntity>;
}

export const SendEmailTaskEntity = new Type<SendEmailTaskEntity>("SendEmailTask");
export interface SendEmailTaskEntity extends Entities.Entity, Scheduler.ITaskEntity {
  Type: "SendEmailTask";
  name: string;
  emailTemplate: Entities.Lite<EmailTemplateEntity>;
  uniqueTarget: Entities.Lite<Entities.Entity> | null;
  targetsFromUserQuery: Entities.Lite<UserQueries.UserQueryEntity> | null;
  modelConverter: Templating.ModelConverterSymbol | null;
}

export module SendEmailTaskOperation {
  export const Save : Entities.ExecuteSymbol<SendEmailTaskEntity> = registerSymbol("Operation", "SendEmailTaskOperation.Save");
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

export const WhenManyFromBehaviour = new EnumType<WhenManyFromBehaviour>("WhenManyFromBehaviour");
export type WhenManyFromBehaviour =
  "SplitMessages" |
  "FistResult";

export const WhenManyRecipiensBehaviour = new EnumType<WhenManyRecipiensBehaviour>("WhenManyRecipiensBehaviour");
export type WhenManyRecipiensBehaviour =
  "SplitMessages" |
  "KeepOneMessageWithManyRecipients";

export const WhenNoneFromBehaviour = new EnumType<WhenNoneFromBehaviour>("WhenNoneFromBehaviour");
export type WhenNoneFromBehaviour =
  "ThrowException" |
  "NoMessage" |
  "DefaultFrom";

export const WhenNoneRecipientsBehaviour = new EnumType<WhenNoneRecipientsBehaviour>("WhenNoneRecipientsBehaviour");
export type WhenNoneRecipientsBehaviour =
  "ThrowException" |
  "NoMessage" |
  "NoRecipients";

export namespace External {

  export const ExchangeVersion = new EnumType<ExchangeVersion>("ExchangeVersion");
  export type ExchangeVersion =
    "Exchange2007_SP1" |
    "Exchange2010" |
    "Exchange2010_SP1" |
    "Exchange2010_SP2" |
    "Exchange2013" |
    "Exchange2013_SP1" |
    "Exchange2015" |
    "Exchange2016" |
    "V2015_10_05";
  
  export const SmtpDeliveryFormat = new EnumType<SmtpDeliveryFormat>("SmtpDeliveryFormat");
  export type SmtpDeliveryFormat =
    "SevenBit" |
    "International";
  
  export const SmtpDeliveryMethod = new EnumType<SmtpDeliveryMethod>("SmtpDeliveryMethod");
  export type SmtpDeliveryMethod =
    "Network" |
    "SpecifiedPickupDirectory" |
    "PickupDirectoryFromIis";
  
}



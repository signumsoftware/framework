//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Signum from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import * as Basics from '../Basics/Signum.Entities.Basics'
import * as UserAssets from '../UserAssets/Signum.Entities.UserAssets'
import * as Processes from '../Processes/Signum.Entities.Processes'
import * as Files from '../Files/Signum.Entities.Files'
import * as Templating from '../Templating/Signum.Entities.Templating'
import * as Scheduler from '../Scheduler/Signum.Entities.Scheduler'
import * as UserQueries from '../UserQueries/Signum.Entities.UserQueries'
import * as Authorization from '../Authorization/Signum.Entities.Authorization'



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

export const EmailAddressEmbedded = new Type<EmailAddressEmbedded>("EmailAddressEmbedded");
export interface EmailAddressEmbedded extends Entities.EmbeddedEntity {
  Type: "EmailAddressEmbedded";
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

export const EmailMasterTemplateEntity = new Type<EmailMasterTemplateEntity>("EmailMasterTemplate");
export interface EmailMasterTemplateEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
  Type: "EmailMasterTemplate";
  name: string;
  messages: Entities.MList<EmailMasterTemplateMessageEmbedded>;
  guid: string;
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
  from: EmailAddressEmbedded;
  template: Entities.Lite<EmailTemplateEntity> | null;
  creationDate: string;
  sent: string | null;
  receptionNotified: string | null;
  subject: string | null;
  body: string | null;
  bodyHash: string | null;
  isBodyHtml: boolean;
  exception: Entities.Lite<Signum.ExceptionEntity> | null;
  state: EmailMessageState;
  uniqueIdentifier: string | null;
  editableMessage: boolean;
  package: Entities.Lite<EmailPackageEntity> | null;
  processIdentifier: string | null;
  sendRetries: number;
  attachments: Entities.MList<EmailAttachmentEmbedded>;
}

export module EmailMessageMessage {
  export const TheEmailMessageCannotBeSentFromState0 = new MessageKey("EmailMessageMessage", "TheEmailMessageCannotBeSentFromState0");
  export const Message = new MessageKey("EmailMessageMessage", "Message");
  export const Messages = new MessageKey("EmailMessageMessage", "Messages");
  export const RemainingMessages = new MessageKey("EmailMessageMessage", "RemainingMessages");
  export const ExceptionMessages = new MessageKey("EmailMessageMessage", "ExceptionMessages");
  export const DefaultFromIsMandatory = new MessageKey("EmailMessageMessage", "DefaultFromIsMandatory");
  export const From = new MessageKey("EmailMessageMessage", "From");
  export const To = new MessageKey("EmailMessageMessage", "To");
  export const Attachments = new MessageKey("EmailMessageMessage", "Attachments");
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
  rawContent: string;
  sentDate: string;
  receivedDate: string;
  deletionDate: string | null;
}

export const EmailReceptionMixin = new Type<EmailReceptionMixin>("EmailReceptionMixin");
export interface EmailReceptionMixin extends Entities.MixinEntity {
  Type: "EmailReceptionMixin";
  receptionInfo: EmailReceptionInfoEmbedded | null;
}

export const EmailRecipientEmbedded = new Type<EmailRecipientEmbedded>("EmailRecipientEmbedded");
export interface EmailRecipientEmbedded extends EmailAddressEmbedded {
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
  defaultFrom: EmailAddressEmbedded | null;
  additionalRecipients: Entities.MList<EmailRecipientEmbedded>;
  sMTP: SmtpEmbedded | null;
  exchange: ExchangeWebServiceEmbedded | null;
}

export module EmailSenderConfigurationOperation {
  export const Save : Entities.ExecuteSymbol<EmailSenderConfigurationEntity> = registerSymbol("Operation", "EmailSenderConfigurationOperation.Save");
}

export const EmailTemplateContactEmbedded = new Type<EmailTemplateContactEmbedded>("EmailTemplateContactEmbedded");
export interface EmailTemplateContactEmbedded extends Entities.EmbeddedEntity {
  Type: "EmailTemplateContactEmbedded";
  token: UserAssets.QueryTokenEmbedded | null;
  emailAddress: string | null;
  displayName: string | null;
}

export const EmailTemplateEntity = new Type<EmailTemplateEntity>("EmailTemplate");
export interface EmailTemplateEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
  Type: "EmailTemplate";
  guid: string;
  name: string;
  editableMessage: boolean;
  disableAuthorization: boolean;
  query: Signum.QueryEntity;
  model: EmailModelEntity | null;
  sendDifferentMessages: boolean;
  from: EmailTemplateContactEmbedded | null;
  recipients: Entities.MList<EmailTemplateRecipientEmbedded>;
  attachments: Entities.MList<IAttachmentGeneratorEntity>;
  masterTemplate: Entities.Lite<EmailMasterTemplateEntity> | null;
  isBodyHtml: boolean;
  messages: Entities.MList<EmailTemplateMessageEmbedded>;
  applicable: Templating.TemplateApplicableEval | null;
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
export interface EmailTemplateRecipientEmbedded extends EmailTemplateContactEmbedded {
  kind: EmailRecipientKind;
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

export const ExchangeWebServiceEmbedded = new Type<ExchangeWebServiceEmbedded>("ExchangeWebServiceEmbedded");
export interface ExchangeWebServiceEmbedded extends Entities.EmbeddedEntity {
  Type: "ExchangeWebServiceEmbedded";
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

export const NewsletterDeliveryEntity = new Type<NewsletterDeliveryEntity>("NewsletterDelivery");
export interface NewsletterDeliveryEntity extends Entities.Entity, Processes.IProcessLineDataEntity {
  Type: "NewsletterDelivery";
  sent: boolean;
  sendDate: string | null;
  recipient: Entities.Lite<IEmailOwnerEntity> | null;
  newsletter: Entities.Lite<NewsletterEntity>;
}

export const NewsletterEntity = new Type<NewsletterEntity>("Newsletter");
export interface NewsletterEntity extends Entities.Entity, Processes.IProcessDataEntity {
  Type: "Newsletter";
  name: string;
  state: NewsletterState;
  from: string;
  displayFrom: string;
  subject: string | null;
  text: string | null;
  query: Signum.QueryEntity | null;
}

export module NewsletterOperation {
  export const Save : Entities.ExecuteSymbol<NewsletterEntity> = registerSymbol("Operation", "NewsletterOperation.Save");
  export const Send : Entities.ConstructSymbol_From<Processes.ProcessEntity, NewsletterEntity> = registerSymbol("Operation", "NewsletterOperation.Send");
  export const AddRecipients : Entities.ExecuteSymbol<NewsletterEntity> = registerSymbol("Operation", "NewsletterOperation.AddRecipients");
  export const RemoveRecipients : Entities.ExecuteSymbol<NewsletterEntity> = registerSymbol("Operation", "NewsletterOperation.RemoveRecipients");
  export const Clone : Entities.ConstructSymbol_From<NewsletterEntity, NewsletterEntity> = registerSymbol("Operation", "NewsletterOperation.Clone");
}

export module NewsletterProcess {
  export const SendNewsletter : Processes.ProcessAlgorithmSymbol = registerSymbol("ProcessAlgorithm", "NewsletterProcess.SendNewsletter");
}

export const NewsletterState = new EnumType<NewsletterState>("NewsletterState");
export type NewsletterState =
  "Created" |
  "Saved" |
  "Sent";

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
}

export const Pop3ReceptionEntity = new Type<Pop3ReceptionEntity>("Pop3Reception");
export interface Pop3ReceptionEntity extends Entities.Entity {
  Type: "Pop3Reception";
  pop3Configuration: Entities.Lite<Pop3ConfigurationEntity>;
  startDate: string;
  endDate: string | null;
  newEmails: number;
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
  uniqueTarget: Entities.Lite<Entities.Entity>;
  targetsFromUserQuery: Entities.Lite<UserQueries.UserQueryEntity> | null;
  modelConverter: Templating.ModelConverterSymbol | null;
}

export module SendEmailTaskOperation {
  export const Save : Entities.ExecuteSymbol<SendEmailTaskEntity> = registerSymbol("Operation", "SendEmailTaskOperation.Save");
}

export const SmtpEmbedded = new Type<SmtpEmbedded>("SmtpEmbedded");
export interface SmtpEmbedded extends Entities.EmbeddedEntity {
  Type: "SmtpEmbedded";
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



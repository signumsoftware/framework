//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Signum from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import * as Basics from '../Basics/Signum.Entities.Basics'
import * as Scheduler from '../Scheduler/Signum.Entities.Scheduler'
import * as UserQueries from '../UserQueries/Signum.Entities.UserQueries'
import * as Processes from '../Processes/Signum.Entities.Processes'
import * as Files from '../Files/Signum.Entities.Files'
import * as UserAssets from '../UserAssets/Signum.Entities.UserAssets'
import * as Authorization from '../Authorization/Signum.Entities.Authorization'



export module AsyncEmailSenderPermission {
    export const ViewAsyncEmailSenderPanel : Authorization.PermissionSymbol = registerSymbol("Permission", "AsyncEmailSenderPermission.ViewAsyncEmailSenderPanel");
}

export const CertFileType = new EnumType<CertFileType>("CertFileType");
export type CertFileType =
    "CertFile" |
    "SignedFile";

export const ClientCertificationFileEntity = new Type<ClientCertificationFileEntity>("ClientCertificationFileEntity");
export interface ClientCertificationFileEntity extends Entities.EmbeddedEntity {
    Type: "ClientCertificationFileEntity";
    fullFilePath?: string | null;
    certFileType?: CertFileType;
}

export const EmailAddressEntity = new Type<EmailAddressEntity>("EmailAddressEntity");
export interface EmailAddressEntity extends Entities.EmbeddedEntity {
    Type: "EmailAddressEntity";
    emailOwner?: Entities.Lite<IEmailOwnerEntity> | null;
    emailAddress?: string | null;
    invalidEmail?: boolean;
    displayName?: string | null;
}

export const EmailAttachmentEntity = new Type<EmailAttachmentEntity>("EmailAttachmentEntity");
export interface EmailAttachmentEntity extends Entities.EmbeddedEntity {
    Type: "EmailAttachmentEntity";
    type?: EmailAttachmentType;
    file?: Files.EmbeddedFilePathEntity | null;
    contentId?: string | null;
}

export const EmailAttachmentType = new EnumType<EmailAttachmentType>("EmailAttachmentType");
export type EmailAttachmentType =
    "Attachment" |
    "LinkedResource";

export const EmailConfigurationEntity = new Type<EmailConfigurationEntity>("EmailConfigurationEntity");
export interface EmailConfigurationEntity extends Entities.EmbeddedEntity {
    Type: "EmailConfigurationEntity";
    defaultCulture?: Basics.CultureInfoEntity | null;
    urlLeft?: string | null;
    sendEmails?: boolean;
    reciveEmails?: boolean;
    overrideEmailAddress?: string | null;
    avoidSendingEmailsOlderThan?: number | null;
    chunkSizeSendingEmails?: number;
    maxEmailSendRetries?: number;
    asyncSenderPeriod?: number;
}

export module EmailFileType {
    export const Attachment : Files.FileTypeSymbol = registerSymbol("FileType", "EmailFileType.Attachment");
}

export const EmailMasterTemplateEntity = new Type<EmailMasterTemplateEntity>("EmailMasterTemplate");
export interface EmailMasterTemplateEntity extends Entities.Entity {
    Type: "EmailMasterTemplate";
    name?: string | null;
    messages: Entities.MList<EmailMasterTemplateMessageEntity>;
}

export const EmailMasterTemplateMessageEntity = new Type<EmailMasterTemplateMessageEntity>("EmailMasterTemplateMessageEntity");
export interface EmailMasterTemplateMessageEntity extends Entities.EmbeddedEntity {
    Type: "EmailMasterTemplateMessageEntity";
    cultureInfo?: Basics.CultureInfoEntity | null;
    text?: string | null;
}

export module EmailMasterTemplateOperation {
    export const Create : Entities.ConstructSymbol_Simple<EmailMasterTemplateEntity> = registerSymbol("Operation", "EmailMasterTemplateOperation.Create");
    export const Save : Entities.ExecuteSymbol<EmailMasterTemplateEntity> = registerSymbol("Operation", "EmailMasterTemplateOperation.Save");
}

export const EmailMessageEntity = new Type<EmailMessageEntity>("EmailMessage");
export interface EmailMessageEntity extends Entities.Entity, Processes.IProcessLineDataEntity {
    Type: "EmailMessage";
    recipients: Entities.MList<EmailRecipientEntity>;
    target?: Entities.Lite<Entities.Entity> | null;
    from?: EmailAddressEntity | null;
    template?: Entities.Lite<EmailTemplateEntity> | null;
    creationDate?: string;
    sent?: string | null;
    receptionNotified?: string | null;
    subject?: string | null;
    body?: string | null;
    bodyHash?: string | null;
    isBodyHtml?: boolean;
    exception?: Entities.Lite<Signum.ExceptionEntity> | null;
    state?: EmailMessageState;
    uniqueIdentifier?: string | null;
    editableMessage?: boolean;
    package?: Entities.Lite<EmailPackageEntity> | null;
    processIdentifier?: string | null;
    sendRetries?: number;
    attachments: Entities.MList<EmailAttachmentEntity>;
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
    export const CreateMailFromTemplate : Entities.ConstructSymbol_From<EmailMessageEntity, EmailTemplateEntity> = registerSymbol("Operation", "EmailMessageOperation.CreateMailFromTemplate");
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

export const EmailPackageEntity = new Type<EmailPackageEntity>("EmailPackage");
export interface EmailPackageEntity extends Entities.Entity, Processes.IProcessDataEntity {
    Type: "EmailPackage";
    name?: string | null;
}

export const EmailReceptionInfoEntity = new Type<EmailReceptionInfoEntity>("EmailReceptionInfoEntity");
export interface EmailReceptionInfoEntity extends Entities.EmbeddedEntity {
    Type: "EmailReceptionInfoEntity";
    uniqueId?: string | null;
    reception?: Entities.Lite<Pop3ReceptionEntity> | null;
    rawContent?: string | null;
    sentDate?: string;
    receivedDate?: string;
    deletionDate?: string | null;
}

export const EmailReceptionMixin = new Type<EmailReceptionMixin>("EmailReceptionMixin");
export interface EmailReceptionMixin extends Entities.MixinEntity {
    Type: "EmailReceptionMixin";
    receptionInfo?: EmailReceptionInfoEntity | null;
}

export const EmailRecipientEntity = new Type<EmailRecipientEntity>("EmailRecipientEntity");
export interface EmailRecipientEntity extends EmailAddressEntity {
    kind?: EmailRecipientKind;
}

export const EmailRecipientKind = new EnumType<EmailRecipientKind>("EmailRecipientKind");
export type EmailRecipientKind =
    "To" |
    "Cc" |
    "Bcc";

export const EmailTemplateContactEntity = new Type<EmailTemplateContactEntity>("EmailTemplateContactEntity");
export interface EmailTemplateContactEntity extends Entities.EmbeddedEntity {
    Type: "EmailTemplateContactEntity";
    token?: UserAssets.QueryTokenEntity | null;
    emailAddress?: string | null;
    displayName?: string | null;
}

export const EmailTemplateEntity = new Type<EmailTemplateEntity>("EmailTemplate");
export interface EmailTemplateEntity extends Entities.Entity {
    Type: "EmailTemplate";
    name?: string | null;
    editableMessage?: boolean;
    disableAuthorization?: boolean;
    query?: Signum.QueryEntity | null;
    systemEmail?: SystemEmailEntity | null;
    sendDifferentMessages?: boolean;
    from?: EmailTemplateContactEntity | null;
    recipients: Entities.MList<EmailTemplateRecipientEntity>;
    attachments: Entities.MList<IAttachmentGeneratorEntity>;
    masterTemplate?: Entities.Lite<EmailMasterTemplateEntity> | null;
    isBodyHtml?: boolean;
    messages: Entities.MList<EmailTemplateMessageEntity>;
    active?: boolean;
    startDate?: string | null;
    endDate?: string | null;
}

export module EmailTemplateMessage {
    export const EndDateMustBeHigherThanStartDate = new MessageKey("EmailTemplateMessage", "EndDateMustBeHigherThanStartDate");
    export const ThereAreNoMessagesForTheTemplate = new MessageKey("EmailTemplateMessage", "ThereAreNoMessagesForTheTemplate");
    export const ThereMustBeAMessageFor0 = new MessageKey("EmailTemplateMessage", "ThereMustBeAMessageFor0");
    export const TheresMoreThanOneMessageForTheSameLanguage = new MessageKey("EmailTemplateMessage", "TheresMoreThanOneMessageForTheSameLanguage");
    export const TheTextMustContain0IndicatingReplacementPoint = new MessageKey("EmailTemplateMessage", "TheTextMustContain0IndicatingReplacementPoint");
    export const TheTemplateIsAlreadyActive = new MessageKey("EmailTemplateMessage", "TheTemplateIsAlreadyActive");
    export const TheTemplateIsAlreadyInactive = new MessageKey("EmailTemplateMessage", "TheTemplateIsAlreadyInactive");
    export const SystemEmailShouldBeSetToAccessModel0 = new MessageKey("EmailTemplateMessage", "SystemEmailShouldBeSetToAccessModel0");
    export const NewCulture = new MessageKey("EmailTemplateMessage", "NewCulture");
    export const TokenOrEmailAddressMustBeSet = new MessageKey("EmailTemplateMessage", "TokenOrEmailAddressMustBeSet");
    export const TokenAndEmailAddressCanNotBeSetAtTheSameTime = new MessageKey("EmailTemplateMessage", "TokenAndEmailAddressCanNotBeSetAtTheSameTime");
    export const TokenMustBeA0 = new MessageKey("EmailTemplateMessage", "TokenMustBeA0");
    export const ShowPreview = new MessageKey("EmailTemplateMessage", "ShowPreview");
    export const HidePreview = new MessageKey("EmailTemplateMessage", "HidePreview");
}

export const EmailTemplateMessageEntity = new Type<EmailTemplateMessageEntity>("EmailTemplateMessageEntity");
export interface EmailTemplateMessageEntity extends Entities.EmbeddedEntity {
    Type: "EmailTemplateMessageEntity";
    cultureInfo?: Basics.CultureInfoEntity | null;
    text?: string | null;
    subject?: string | null;
}

export module EmailTemplateOperation {
    export const CreateEmailTemplateFromSystemEmail : Entities.ConstructSymbol_From<EmailTemplateEntity, SystemEmailEntity> = registerSymbol("Operation", "EmailTemplateOperation.CreateEmailTemplateFromSystemEmail");
    export const Create : Entities.ConstructSymbol_Simple<EmailTemplateEntity> = registerSymbol("Operation", "EmailTemplateOperation.Create");
    export const Save : Entities.ExecuteSymbol<EmailTemplateEntity> = registerSymbol("Operation", "EmailTemplateOperation.Save");
    export const Enable : Entities.ExecuteSymbol<EmailTemplateEntity> = registerSymbol("Operation", "EmailTemplateOperation.Enable");
    export const Disable : Entities.ExecuteSymbol<EmailTemplateEntity> = registerSymbol("Operation", "EmailTemplateOperation.Disable");
}

export const EmailTemplateRecipientEntity = new Type<EmailTemplateRecipientEntity>("EmailTemplateRecipientEntity");
export interface EmailTemplateRecipientEntity extends EmailTemplateContactEntity {
    kind?: EmailRecipientKind;
}

export module EmailTemplateViewMessage {
    export const InsertMessageContent = new MessageKey("EmailTemplateViewMessage", "InsertMessageContent");
    export const Insert = new MessageKey("EmailTemplateViewMessage", "Insert");
    export const Language = new MessageKey("EmailTemplateViewMessage", "Language");
}

export interface IAttachmentGeneratorEntity extends Entities.Entity {
    template?: EmailTemplateEntity | null;
}

export interface IEmailOwnerEntity extends Entities.Entity {
}

export const NewsletterDeliveryEntity = new Type<NewsletterDeliveryEntity>("NewsletterDelivery");
export interface NewsletterDeliveryEntity extends Entities.Entity, Processes.IProcessLineDataEntity {
    Type: "NewsletterDelivery";
    sent?: boolean;
    sendDate?: string | null;
    recipient?: Entities.Lite<IEmailOwnerEntity> | null;
    newsletter?: Entities.Lite<NewsletterEntity> | null;
}

export const NewsletterEntity = new Type<NewsletterEntity>("Newsletter");
export interface NewsletterEntity extends Entities.Entity, Processes.IProcessDataEntity {
    Type: "Newsletter";
    name?: string | null;
    state?: NewsletterState;
    from?: string | null;
    displayFrom?: string | null;
    subject?: string | null;
    text?: string | null;
    query?: Signum.QueryEntity | null;
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
    active?: boolean;
    port?: number;
    host?: string | null;
    username?: string | null;
    password?: string | null;
    enableSSL?: boolean;
    readTimeout?: number;
    deleteMessagesAfter?: number | null;
    clientCertificationFiles: Entities.MList<ClientCertificationFileEntity>;
}

export module Pop3ConfigurationOperation {
    export const Save : Entities.ExecuteSymbol<Pop3ConfigurationEntity> = registerSymbol("Operation", "Pop3ConfigurationOperation.Save");
    export const ReceiveEmails : Entities.ConstructSymbol_From<Pop3ReceptionEntity, Pop3ConfigurationEntity> = registerSymbol("Operation", "Pop3ConfigurationOperation.ReceiveEmails");
}

export const Pop3ReceptionEntity = new Type<Pop3ReceptionEntity>("Pop3Reception");
export interface Pop3ReceptionEntity extends Entities.Entity {
    Type: "Pop3Reception";
    pop3Configuration?: Entities.Lite<Pop3ConfigurationEntity> | null;
    startDate?: string;
    endDate?: string | null;
    newEmails?: number;
    exception?: Entities.Lite<Signum.ExceptionEntity> | null;
}

export const Pop3ReceptionExceptionEntity = new Type<Pop3ReceptionExceptionEntity>("Pop3ReceptionException");
export interface Pop3ReceptionExceptionEntity extends Entities.Entity {
    Type: "Pop3ReceptionException";
    reception?: Entities.Lite<Pop3ReceptionEntity> | null;
    exception?: Entities.Lite<Signum.ExceptionEntity> | null;
}

export const SendEmailTaskEntity = new Type<SendEmailTaskEntity>("SendEmailTask");
export interface SendEmailTaskEntity extends Entities.Entity, Scheduler.ITaskEntity {
    Type: "SendEmailTask";
    name?: string | null;
    emailTemplate?: Entities.Lite<EmailTemplateEntity> | null;
    uniqueTarget?: Entities.Lite<Entities.Entity> | null;
    targetsFromUserQuery?: Entities.Lite<UserQueries.UserQueryEntity> | null;
}

export module SendEmailTaskOperation {
    export const Save : Entities.ExecuteSymbol<SendEmailTaskEntity> = registerSymbol("Operation", "SendEmailTaskOperation.Save");
}

export const SmtpConfigurationEntity = new Type<SmtpConfigurationEntity>("SmtpConfiguration");
export interface SmtpConfigurationEntity extends Entities.Entity {
    Type: "SmtpConfiguration";
    name?: string | null;
    deliveryFormat?: External.SmtpDeliveryFormat;
    deliveryMethod?: External.SmtpDeliveryMethod;
    network?: SmtpNetworkDeliveryEntity | null;
    pickupDirectoryLocation?: string | null;
    defaultFrom?: EmailAddressEntity | null;
    additionalRecipients: Entities.MList<EmailRecipientEntity>;
}

export module SmtpConfigurationOperation {
    export const Save : Entities.ExecuteSymbol<SmtpConfigurationEntity> = registerSymbol("Operation", "SmtpConfigurationOperation.Save");
}

export const SmtpNetworkDeliveryEntity = new Type<SmtpNetworkDeliveryEntity>("SmtpNetworkDeliveryEntity");
export interface SmtpNetworkDeliveryEntity extends Entities.EmbeddedEntity {
    Type: "SmtpNetworkDeliveryEntity";
    host?: string | null;
    port?: number;
    username?: string | null;
    password?: string | null;
    useDefaultCredentials?: boolean;
    enableSSL?: boolean;
    clientCertificationFiles: Entities.MList<ClientCertificationFileEntity>;
}

export const SystemEmailEntity = new Type<SystemEmailEntity>("SystemEmail");
export interface SystemEmailEntity extends Entities.Entity {
    Type: "SystemEmail";
    fullClassName?: string | null;
}

export namespace External {

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



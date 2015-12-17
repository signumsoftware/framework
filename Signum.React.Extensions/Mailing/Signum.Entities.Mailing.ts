//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////
import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from 'Framework/Signum.React/Scripts/Reflection' 

import * as Entities from 'Framework/Signum.React/Scripts/Signum.Entities' 

import * as Processes from 'Extensions/Signum.React.Extensions/Processes/Signum.Entities.Processes' 

import * as Scheduler from 'Extensions/Signum.React.Extensions/Scheduler/Signum.Entities.Scheduler' 

import * as UserAssets from 'Extensions/Signum.React.Extensions/UserAssets/Signum.Entities.UserAssets' 

import * as Basics from 'Extensions/Signum.React.Extensions/Basics/Signum.Entities.Basics' 

import * as Authorization from 'Extensions/Signum.React.Extensions/Authorization/Signum.Entities.Authorization' 

import * as Files from 'Extensions/Signum.React.Extensions/Files/Signum.Entities.Files' 
export module AsyncEmailSenderPermission {
    export const ViewAsyncEmailSenderPanel : Authorization.PermissionSymbol = registerSymbol({ key: "AsyncEmailSenderPermission.ViewAsyncEmailSenderPanel" });
}

export enum CertFileType {
    CertFile = "CertFile" as any,
    SignedFile = "SignedFile" as any,
}
export const CertFileType_Type = new EnumType<CertFileType>("CertFileType", CertFileType);

export const ClientCertificationFileEntity_Type = new Type<ClientCertificationFileEntity>("ClientCertificationFileEntity");
export interface ClientCertificationFileEntity extends Entities.EmbeddedEntity {
    fullFilePath?: string;
    certFileType?: CertFileType;
}

export const EmailAddressEntity_Type = new Type<EmailAddressEntity>("EmailAddressEntity");
export interface EmailAddressEntity extends Entities.EmbeddedEntity {
    emailOwner?: Entities.Lite<IEmailOwnerEntity>;
    emailAddress?: string;
    displayName?: string;
}

export const EmailAttachmentEntity_Type = new Type<EmailAttachmentEntity>("EmailAttachmentEntity");
export interface EmailAttachmentEntity extends Entities.EmbeddedEntity {
    type?: EmailAttachmentType;
    file?: Files.EmbeddedFilePathEntity;
    contentId?: string;
}

export enum EmailAttachmentType {
    Attachment = "Attachment" as any,
    LinkedResource = "LinkedResource" as any,
}
export const EmailAttachmentType_Type = new EnumType<EmailAttachmentType>("EmailAttachmentType", EmailAttachmentType);

export const EmailConfigurationEntity_Type = new Type<EmailConfigurationEntity>("EmailConfigurationEntity");
export interface EmailConfigurationEntity extends Entities.EmbeddedEntity {
    defaultCulture?: Basics.CultureInfoEntity;
    urlLeft?: string;
    sendEmails?: boolean;
    reciveEmails?: boolean;
    overrideEmailAddress?: string;
    avoidSendingEmailsOlderThan?: number;
    chunkSizeSendingEmails?: number;
    maxEmailSendRetries?: number;
    asyncSenderPeriod?: number;
}

export module EmailFileType {
    export const Attachment : Files.FileTypeSymbol = registerSymbol({ key: "EmailFileType.Attachment" });
}

export const EmailMasterTemplateEntity_Type = new Type<EmailMasterTemplateEntity>("EmailMasterTemplateEntity");
export interface EmailMasterTemplateEntity extends Entities.Entity {
    name?: string;
    messages?: Entities.MList<EmailMasterTemplateMessageEntity>;
}

export const EmailMasterTemplateMessageEntity_Type = new Type<EmailMasterTemplateMessageEntity>("EmailMasterTemplateMessageEntity");
export interface EmailMasterTemplateMessageEntity extends Entities.EmbeddedEntity {
    masterTemplate?: EmailMasterTemplateEntity;
    cultureInfo?: Basics.CultureInfoEntity;
    text?: string;
}

export module EmailMasterTemplateOperation {
    export const Create : Entities.ConstructSymbol_Simple<EmailMasterTemplateEntity> = registerSymbol({ key: "EmailMasterTemplateOperation.Create" });
    export const Save : Entities.ExecuteSymbol<EmailMasterTemplateEntity> = registerSymbol({ key: "EmailMasterTemplateOperation.Save" });
}

export const EmailMessageEntity_Type = new Type<EmailMessageEntity>("EmailMessageEntity");
export interface EmailMessageEntity extends Entities.Entity, Processes.IProcessLineDataEntity {
    recipients?: Entities.MList<EmailRecipientEntity>;
    target?: Entities.Lite<Entities.Entity>;
    from?: EmailAddressEntity;
    template?: Entities.Lite<EmailTemplateEntity>;
    creationDate?: string;
    sent?: string;
    receptionNotified?: string;
    subject?: string;
    body?: string;
    bodyHash?: string;
    isBodyHtml?: boolean;
    exception?: Entities.Lite<Entities.Basics.ExceptionEntity>;
    state?: EmailMessageState;
    uniqueIdentifier?: string;
    editableMessage?: boolean;
    package?: Entities.Lite<EmailPackageEntity>;
    processIdentifier?: string;
    sendRetries?: number;
    attachments?: Entities.MList<EmailAttachmentEntity>;
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
    export const Save : Entities.ExecuteSymbol<EmailMessageEntity> = registerSymbol({ key: "EmailMessageOperation.Save" });
    export const ReadyToSend : Entities.ExecuteSymbol<EmailMessageEntity> = registerSymbol({ key: "EmailMessageOperation.ReadyToSend" });
    export const Send : Entities.ExecuteSymbol<EmailMessageEntity> = registerSymbol({ key: "EmailMessageOperation.Send" });
    export const ReSend : Entities.ConstructSymbol_From<EmailMessageEntity, EmailMessageEntity> = registerSymbol({ key: "EmailMessageOperation.ReSend" });
    export const ReSendEmails : Entities.ConstructSymbol_FromMany<Processes.ProcessEntity, EmailMessageEntity> = registerSymbol({ key: "EmailMessageOperation.ReSendEmails" });
    export const CreateMail : Entities.ConstructSymbol_Simple<EmailMessageEntity> = registerSymbol({ key: "EmailMessageOperation.CreateMail" });
    export const CreateMailFromTemplate : Entities.ConstructSymbol_From<EmailMessageEntity, EmailTemplateEntity> = registerSymbol({ key: "EmailMessageOperation.CreateMailFromTemplate" });
    export const Delete : Entities.DeleteSymbol<EmailMessageEntity> = registerSymbol({ key: "EmailMessageOperation.Delete" });
}

export module EmailMessageProcess {
    export const SendEmails : Processes.ProcessAlgorithmSymbol = registerSymbol({ key: "EmailMessageProcess.SendEmails" });
}

export enum EmailMessageState {
    Created = "Created" as any,
    Draft = "Draft" as any,
    ReadyToSend = "ReadyToSend" as any,
    RecruitedForSending = "RecruitedForSending" as any,
    Sent = "Sent" as any,
    SentException = "SentException" as any,
    ReceptionNotified = "ReceptionNotified" as any,
    Received = "Received" as any,
    Outdated = "Outdated" as any,
}
export const EmailMessageState_Type = new EnumType<EmailMessageState>("EmailMessageState", EmailMessageState);

export const EmailPackageEntity_Type = new Type<EmailPackageEntity>("EmailPackageEntity");
export interface EmailPackageEntity extends Entities.Entity, Processes.IProcessDataEntity {
    name?: string;
}

export const EmailReceptionInfoEntity_Type = new Type<EmailReceptionInfoEntity>("EmailReceptionInfoEntity");
export interface EmailReceptionInfoEntity extends Entities.EmbeddedEntity {
    uniqueId?: string;
    reception?: Entities.Lite<Pop3ReceptionEntity>;
    rawContent?: string;
    sentDate?: string;
    receivedDate?: string;
    deletionDate?: string;
}

export const EmailReceptionMixin_Type = new Type<EmailReceptionMixin>("EmailReceptionMixin");
export interface EmailReceptionMixin extends Entities.MixinEntity {
    receptionInfo?: EmailReceptionInfoEntity;
}

export const EmailRecipientEntity_Type = new Type<EmailRecipientEntity>("EmailRecipientEntity");
export interface EmailRecipientEntity extends EmailAddressEntity {
    kind?: EmailRecipientKind;
}

export enum EmailRecipientKind {
    To = "To" as any,
    Cc = "Cc" as any,
    Bcc = "Bcc" as any,
}
export const EmailRecipientKind_Type = new EnumType<EmailRecipientKind>("EmailRecipientKind", EmailRecipientKind);

export const EmailTemplateContactEntity_Type = new Type<EmailTemplateContactEntity>("EmailTemplateContactEntity");
export interface EmailTemplateContactEntity extends Entities.EmbeddedEntity {
    token?: UserAssets.QueryTokenEntity;
    emailAddress?: string;
    displayName?: string;
}

export const EmailTemplateEntity_Type = new Type<EmailTemplateEntity>("EmailTemplateEntity");
export interface EmailTemplateEntity extends Entities.Entity {
    name?: string;
    editableMessage?: boolean;
    disableAuthorization?: boolean;
    query?: Entities.Basics.QueryEntity;
    systemEmail?: SystemEmailEntity;
    sendDifferentMessages?: boolean;
    from?: EmailTemplateContactEntity;
    recipients?: Entities.MList<EmailTemplateRecipientEntity>;
    masterTemplate?: Entities.Lite<EmailMasterTemplateEntity>;
    isBodyHtml?: boolean;
    messages?: Entities.MList<EmailTemplateMessageEntity>;
    active?: boolean;
    startDate?: string;
    endDate?: string;
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
}

export const EmailTemplateMessageEntity_Type = new Type<EmailTemplateMessageEntity>("EmailTemplateMessageEntity");
export interface EmailTemplateMessageEntity extends Entities.EmbeddedEntity {
    template?: EmailTemplateEntity;
    cultureInfo?: Basics.CultureInfoEntity;
    text?: string;
    subject?: string;
}

export module EmailTemplateOperation {
    export const CreateEmailTemplateFromSystemEmail : Entities.ConstructSymbol_From<EmailTemplateEntity, SystemEmailEntity> = registerSymbol({ key: "EmailTemplateOperation.CreateEmailTemplateFromSystemEmail" });
    export const Create : Entities.ConstructSymbol_Simple<EmailTemplateEntity> = registerSymbol({ key: "EmailTemplateOperation.Create" });
    export const Save : Entities.ExecuteSymbol<EmailTemplateEntity> = registerSymbol({ key: "EmailTemplateOperation.Save" });
    export const Enable : Entities.ExecuteSymbol<EmailTemplateEntity> = registerSymbol({ key: "EmailTemplateOperation.Enable" });
    export const Disable : Entities.ExecuteSymbol<EmailTemplateEntity> = registerSymbol({ key: "EmailTemplateOperation.Disable" });
}

export const EmailTemplateRecipientEntity_Type = new Type<EmailTemplateRecipientEntity>("EmailTemplateRecipientEntity");
export interface EmailTemplateRecipientEntity extends EmailTemplateContactEntity {
    kind?: EmailRecipientKind;
}

export module EmailTemplateViewMessage {
    export const InsertMessageContent = new MessageKey("EmailTemplateViewMessage", "InsertMessageContent");
    export const Insert = new MessageKey("EmailTemplateViewMessage", "Insert");
    export const Language = new MessageKey("EmailTemplateViewMessage", "Language");
}

export interface IEmailOwnerEntity extends Entities.IEntity {
}

export const NewsletterDeliveryEntity_Type = new Type<NewsletterDeliveryEntity>("NewsletterDeliveryEntity");
export interface NewsletterDeliveryEntity extends Entities.Entity, Processes.IProcessLineDataEntity {
    sent?: boolean;
    sendDate?: string;
    recipient?: Entities.Lite<IEmailOwnerEntity>;
    newsletter?: Entities.Lite<NewsletterEntity>;
}

export const NewsletterEntity_Type = new Type<NewsletterEntity>("NewsletterEntity");
export interface NewsletterEntity extends Entities.Entity, Processes.IProcessDataEntity {
    name?: string;
    state?: NewsletterState;
    from?: string;
    displayFrom?: string;
    subject?: string;
    text?: string;
    query?: Entities.Basics.QueryEntity;
}

export module NewsletterOperation {
    export const Save : Entities.ExecuteSymbol<NewsletterEntity> = registerSymbol({ key: "NewsletterOperation.Save" });
    export const Send : Entities.ConstructSymbol_From<Processes.ProcessEntity, NewsletterEntity> = registerSymbol({ key: "NewsletterOperation.Send" });
    export const AddRecipients : Entities.ExecuteSymbol<NewsletterEntity> = registerSymbol({ key: "NewsletterOperation.AddRecipients" });
    export const RemoveRecipients : Entities.ExecuteSymbol<NewsletterEntity> = registerSymbol({ key: "NewsletterOperation.RemoveRecipients" });
    export const Clone : Entities.ConstructSymbol_From<NewsletterEntity, NewsletterEntity> = registerSymbol({ key: "NewsletterOperation.Clone" });
}

export module NewsletterProcess {
    export const SendNewsletter : Processes.ProcessAlgorithmSymbol = registerSymbol({ key: "NewsletterProcess.SendNewsletter" });
}

export enum NewsletterState {
    Created = "Created" as any,
    Saved = "Saved" as any,
    Sent = "Sent" as any,
}
export const NewsletterState_Type = new EnumType<NewsletterState>("NewsletterState", NewsletterState);

export module Pop3ConfigurationAction {
    export const ReceiveAllActivePop3Configurations : Scheduler.SimpleTaskSymbol = registerSymbol({ key: "Pop3ConfigurationAction.ReceiveAllActivePop3Configurations" });
}

export const Pop3ConfigurationEntity_Type = new Type<Pop3ConfigurationEntity>("Pop3ConfigurationEntity");
export interface Pop3ConfigurationEntity extends Entities.Entity, Scheduler.ITaskEntity {
    active?: boolean;
    port?: number;
    host?: string;
    username?: string;
    password?: string;
    enableSSL?: boolean;
    readTimeout?: number;
    deleteMessagesAfter?: number;
    clientCertificationFiles?: Entities.MList<ClientCertificationFileEntity>;
}

export module Pop3ConfigurationOperation {
    export const Save : Entities.ExecuteSymbol<Pop3ConfigurationEntity> = registerSymbol({ key: "Pop3ConfigurationOperation.Save" });
    export const ReceiveEmails : Entities.ConstructSymbol_From<Pop3ReceptionEntity, Pop3ConfigurationEntity> = registerSymbol({ key: "Pop3ConfigurationOperation.ReceiveEmails" });
}

export const Pop3ReceptionEntity_Type = new Type<Pop3ReceptionEntity>("Pop3ReceptionEntity");
export interface Pop3ReceptionEntity extends Entities.Entity {
    pop3Configuration?: Entities.Lite<Pop3ConfigurationEntity>;
    startDate?: string;
    endDate?: string;
    newEmails?: number;
    exception?: Entities.Lite<Entities.Basics.ExceptionEntity>;
}

export const Pop3ReceptionExceptionEntity_Type = new Type<Pop3ReceptionExceptionEntity>("Pop3ReceptionExceptionEntity");
export interface Pop3ReceptionExceptionEntity extends Entities.Entity {
    reception?: Entities.Lite<Pop3ReceptionEntity>;
    exception?: Entities.Lite<Entities.Basics.ExceptionEntity>;
}

export const SmtpConfigurationEntity_Type = new Type<SmtpConfigurationEntity>("SmtpConfigurationEntity");
export interface SmtpConfigurationEntity extends Entities.Entity {
    name?: string;
    deliveryFormat?: External.SmtpDeliveryFormat;
    deliveryMethod?: External.SmtpDeliveryMethod;
    network?: SmtpNetworkDeliveryEntity;
    pickupDirectoryLocation?: string;
    defaultFrom?: EmailAddressEntity;
    additionalRecipients?: Entities.MList<EmailRecipientEntity>;
}

export module SmtpConfigurationOperation {
    export const Save : Entities.ExecuteSymbol<SmtpConfigurationEntity> = registerSymbol({ key: "SmtpConfigurationOperation.Save" });
}

export const SmtpNetworkDeliveryEntity_Type = new Type<SmtpNetworkDeliveryEntity>("SmtpNetworkDeliveryEntity");
export interface SmtpNetworkDeliveryEntity extends Entities.EmbeddedEntity {
    host?: string;
    port?: number;
    username?: string;
    password?: string;
    useDefaultCredentials?: boolean;
    enableSSL?: boolean;
    clientCertificationFiles?: Entities.MList<ClientCertificationFileEntity>;
}

export const SystemEmailEntity_Type = new Type<SystemEmailEntity>("SystemEmailEntity");
export interface SystemEmailEntity extends Entities.Entity {
    fullClassName?: string;
}

export namespace External {

    export enum SmtpDeliveryFormat {
        SevenBit = "SevenBit" as any,
        International = "International" as any,
    }
    export const SmtpDeliveryFormat_Type = new EnumType<SmtpDeliveryFormat>("SmtpDeliveryFormat", SmtpDeliveryFormat);
    
    export enum SmtpDeliveryMethod {
        Network = "Network" as any,
        SpecifiedPickupDirectory = "SpecifiedPickupDirectory" as any,
        PickupDirectoryFromIis = "PickupDirectoryFromIis" as any,
    }
    export const SmtpDeliveryMethod_Type = new EnumType<SmtpDeliveryMethod>("SmtpDeliveryMethod", SmtpDeliveryMethod);
    
}


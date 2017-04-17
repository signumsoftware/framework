//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Signum from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import * as Basics from '../Basics/Signum.Entities.Basics'
import * as Processes from '../Processes/Signum.Entities.Processes'


export const MessageLengthExceeded = new EnumType<MessageLengthExceeded>("MessageLengthExceeded");
export type MessageLengthExceeded =
    "NotAllowed" |
    "Allowed" |
    "TextPruning";

export const MultipleSMSModel = new Type<MultipleSMSModel>("MultipleSMSModel");
export interface MultipleSMSModel extends Entities.ModelEntity {
    Type: "MultipleSMSModel";
    message?: string | null;
    from?: string | null;
    certified?: boolean;
}

export module SMSCharactersMessage {
    export const Insert = new MessageKey("SMSCharactersMessage", "Insert");
    export const Message = new MessageKey("SMSCharactersMessage", "Message");
    export const RemainingCharacters = new MessageKey("SMSCharactersMessage", "RemainingCharacters");
    export const RemoveNonValidCharacters = new MessageKey("SMSCharactersMessage", "RemoveNonValidCharacters");
    export const StatusCanNotBeUpdatedForNonSentMessages = new MessageKey("SMSCharactersMessage", "StatusCanNotBeUpdatedForNonSentMessages");
    export const TheTemplateMustBeActiveToConstructSMSMessages = new MessageKey("SMSCharactersMessage", "TheTemplateMustBeActiveToConstructSMSMessages");
    export const TheTextForTheSMSMessageExceedsTheLengthLimit = new MessageKey("SMSCharactersMessage", "TheTextForTheSMSMessageExceedsTheLengthLimit");
    export const Language = new MessageKey("SMSCharactersMessage", "Language");
    export const Replacements = new MessageKey("SMSCharactersMessage", "Replacements");
}

export const SMSConfigurationEmbedded = new Type<SMSConfigurationEmbedded>("SMSConfigurationEmbedded");
export interface SMSConfigurationEmbedded extends Entities.EmbeddedEntity {
    Type: "SMSConfigurationEmbedded";
    defaultCulture?: Basics.CultureInfoEntity | null;
}

export const SMSMessageEntity = new Type<SMSMessageEntity>("SMSMessage");
export interface SMSMessageEntity extends Entities.Entity, Processes.IProcessLineDataEntity {
    Type: "SMSMessage";
    template?: Entities.Lite<SMSTemplateEntity> | null;
    message?: string | null;
    editableMessage?: boolean;
    from?: string | null;
    sendDate?: string | null;
    state?: SMSMessageState;
    destinationNumber?: string | null;
    messageID?: string | null;
    certified?: boolean;
    sendPackage?: Entities.Lite<SMSSendPackageEntity> | null;
    updatePackage?: Entities.Lite<SMSUpdatePackageEntity> | null;
    updatePackageProcessed?: boolean;
    referred?: Entities.Lite<Entities.Entity> | null;
    exception?: Entities.Lite<Signum.ExceptionEntity> | null;
}

export module SMSMessageOperation {
    export const Send : Entities.ExecuteSymbol<SMSMessageEntity> = registerSymbol("Operation", "SMSMessageOperation.Send");
    export const UpdateStatus : Entities.ExecuteSymbol<SMSMessageEntity> = registerSymbol("Operation", "SMSMessageOperation.UpdateStatus");
    export const CreateUpdateStatusPackage : Entities.ConstructSymbol_FromMany<Processes.ProcessEntity, SMSMessageEntity> = registerSymbol("Operation", "SMSMessageOperation.CreateUpdateStatusPackage");
    export const CreateSMSFromSMSTemplate : Entities.ConstructSymbol_From<SMSMessageEntity, SMSTemplateEntity> = registerSymbol("Operation", "SMSMessageOperation.CreateSMSFromSMSTemplate");
    export const CreateSMSWithTemplateFromEntity : Entities.ConstructSymbol_From<SMSMessageEntity, Entities.Entity> = registerSymbol("Operation", "SMSMessageOperation.CreateSMSWithTemplateFromEntity");
    export const CreateSMSFromEntity : Entities.ConstructSymbol_From<SMSMessageEntity, Entities.Entity> = registerSymbol("Operation", "SMSMessageOperation.CreateSMSFromEntity");
    export const SendSMSMessages : Entities.ConstructSymbol_FromMany<Processes.ProcessEntity, Entities.Entity> = registerSymbol("Operation", "SMSMessageOperation.SendSMSMessages");
    export const SendSMSMessagesFromTemplate : Entities.ConstructSymbol_FromMany<Processes.ProcessEntity, Entities.Entity> = registerSymbol("Operation", "SMSMessageOperation.SendSMSMessagesFromTemplate");
}

export module SMSMessageProcess {
    export const Send : Processes.ProcessAlgorithmSymbol = registerSymbol("ProcessAlgorithm", "SMSMessageProcess.Send");
    export const UpdateStatus : Processes.ProcessAlgorithmSymbol = registerSymbol("ProcessAlgorithm", "SMSMessageProcess.UpdateStatus");
}

export const SMSMessageState = new EnumType<SMSMessageState>("SMSMessageState");
export type SMSMessageState =
    "Created" |
    "Sent" |
    "Delivered" |
    "Failed";

export interface SMSPackageEntity extends Entities.Entity, Processes.IProcessDataEntity {
    name?: string | null;
}

export const SMSSendPackageEntity = new Type<SMSSendPackageEntity>("SMSSendPackage");
export interface SMSSendPackageEntity extends SMSPackageEntity {
    Type: "SMSSendPackage";
}

export const SMSTemplateEntity = new Type<SMSTemplateEntity>("SMSTemplate");
export interface SMSTemplateEntity extends Entities.Entity {
    Type: "SMSTemplate";
    name?: string | null;
    certified?: boolean;
    editableMessage?: boolean;
    associatedType?: Signum.TypeEntity | null;
    messages: Entities.MList<SMSTemplateMessageEmbedded>;
    from?: string | null;
    messageLengthExceeded?: MessageLengthExceeded;
    removeNoSMSCharacters?: boolean;
    active?: boolean;
    startDate?: string;
    endDate?: string | null;
}

export module SMSTemplateMessage {
    export const EndDateMustBeHigherThanStartDate = new MessageKey("SMSTemplateMessage", "EndDateMustBeHigherThanStartDate");
    export const ThereAreNoMessagesForTheTemplate = new MessageKey("SMSTemplateMessage", "ThereAreNoMessagesForTheTemplate");
    export const ThereMustBeAMessageFor0 = new MessageKey("SMSTemplateMessage", "ThereMustBeAMessageFor0");
    export const TheresMoreThanOneMessageForTheSameLanguage = new MessageKey("SMSTemplateMessage", "TheresMoreThanOneMessageForTheSameLanguage");
    export const NewCulture = new MessageKey("SMSTemplateMessage", "NewCulture");
}

export const SMSTemplateMessageEmbedded = new Type<SMSTemplateMessageEmbedded>("SMSTemplateMessageEmbedded");
export interface SMSTemplateMessageEmbedded extends Entities.EmbeddedEntity {
    Type: "SMSTemplateMessageEmbedded";
    template?: SMSTemplateEntity | null;
    cultureInfo?: Basics.CultureInfoEntity | null;
    message?: string | null;
}

export module SMSTemplateOperation {
    export const Create : Entities.ConstructSymbol_Simple<SMSTemplateEntity> = registerSymbol("Operation", "SMSTemplateOperation.Create");
    export const Save : Entities.ExecuteSymbol<SMSTemplateEntity> = registerSymbol("Operation", "SMSTemplateOperation.Save");
}

export const SMSUpdatePackageEntity = new Type<SMSUpdatePackageEntity>("SMSUpdatePackage");
export interface SMSUpdatePackageEntity extends SMSPackageEntity {
    Type: "SMSUpdatePackage";
}



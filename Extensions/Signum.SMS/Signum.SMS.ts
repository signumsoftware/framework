//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as Processes from '../Signum.Processes/Signum.Processes'
import * as Queries from '../Signum.UserAssets/Signum.UserAssets.Queries'
import * as Scheduler from '../Signum.Scheduler/Signum.Scheduler'


export interface ISMSOwnerEntity extends Entities.Entity {
}

export const MessageLengthExceeded = new EnumType<MessageLengthExceeded>("MessageLengthExceeded");
export type MessageLengthExceeded =
  "NotAllowed" |
  "Allowed" |
  "TextPruning";

export const MultipleSMSModel = new Type<MultipleSMSModel>("MultipleSMSModel");
export interface MultipleSMSModel extends Entities.ModelEntity {
  Type: "MultipleSMSModel";
  message: string;
  from: string;
  certified: boolean;
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
  defaultCulture: Basics.CultureInfoEntity;
}

export const SMSMessageEntity = new Type<SMSMessageEntity>("SMSMessage");
export interface SMSMessageEntity extends Entities.Entity {
  Type: "SMSMessage";
  template: Entities.Lite<SMSTemplateEntity> | null;
  message: string;
  editableMessage: boolean;
  from: string | null;
  sendDate: string /*DateTime*/ | null;
  state: SMSMessageState;
  destinationNumber: string;
  messageID: string | null;
  certified: boolean;
  sendPackage: Entities.Lite<SMSSendPackageEntity> | null;
  updatePackage: Entities.Lite<SMSUpdatePackageEntity> | null;
  updatePackageProcessed: boolean;
  referred: Entities.Lite<ISMSOwnerEntity> | null;
  exception: Entities.Lite<Basics.ExceptionEntity> | null;
}

export module SMSMessageOperation {
  export const Send : Operations.ExecuteSymbol<SMSMessageEntity> = registerSymbol("Operation", "SMSMessageOperation.Send");
  export const UpdateStatus : Operations.ExecuteSymbol<SMSMessageEntity> = registerSymbol("Operation", "SMSMessageOperation.UpdateStatus");
  export const CreateUpdateStatusPackage : Operations.ConstructSymbol_FromMany<Processes.ProcessEntity, SMSMessageEntity> = registerSymbol("Operation", "SMSMessageOperation.CreateUpdateStatusPackage");
  export const CreateSMSFromTemplate : Operations.ConstructSymbol_From<SMSMessageEntity, SMSTemplateEntity> = registerSymbol("Operation", "SMSMessageOperation.CreateSMSFromTemplate");
  export const SendMultipleSMSMessages : Operations.ConstructSymbol_FromMany<Processes.ProcessEntity, Entities.Entity> = registerSymbol("Operation", "SMSMessageOperation.SendMultipleSMSMessages");
}

export module SMSMessageProcess {
  export const Send : Processes.ProcessAlgorithmSymbol = registerSymbol("ProcessAlgorithm", "SMSMessageProcess.Send");
  export const UpdateStatus : Processes.ProcessAlgorithmSymbol = registerSymbol("ProcessAlgorithm", "SMSMessageProcess.UpdateStatus");
}

export const SMSMessageState = new EnumType<SMSMessageState>("SMSMessageState");
export type SMSMessageState =
  "Created" |
  "Sent" |
  "SendFailed" |
  "Delivered" |
  "DeliveryFailed";

export module SMSMessageTask {
  export const UpdateSMSStatus : Scheduler.SimpleTaskSymbol = registerSymbol("SimpleTask", "SMSMessageTask.UpdateSMSStatus");
}

export const SMSModelEntity = new Type<SMSModelEntity>("SMSModel");
export interface SMSModelEntity extends Entities.Entity {
  Type: "SMSModel";
  fullClassName: string;
}

export interface SMSPackageEntity extends Entities.Entity, Processes.IProcessDataEntity {
  name: string | null;
}

export const SMSSendPackageEntity = new Type<SMSSendPackageEntity>("SMSSendPackage");
export interface SMSSendPackageEntity extends SMSPackageEntity {
  Type: "SMSSendPackage";
}

export const SMSTemplateEntity = new Type<SMSTemplateEntity>("SMSTemplate");
export interface SMSTemplateEntity extends Entities.Entity {
  Type: "SMSTemplate";
  name: string;
  certified: boolean;
  editableMessage: boolean;
  disableAuthorization: boolean;
  query: Basics.QueryEntity | null;
  model: SMSModelEntity | null;
  messages: Entities.MList<SMSTemplateMessageEmbedded>;
  from: string | null;
  to: Queries.QueryTokenEmbedded | null;
  messageLengthExceeded: MessageLengthExceeded;
  removeNoSMSCharacters: boolean;
  isActive: boolean;
}

export module SMSTemplateMessage {
  export const ThereAreNoMessagesForTheTemplate = new MessageKey("SMSTemplateMessage", "ThereAreNoMessagesForTheTemplate");
  export const ThereMustBeAMessageFor0 = new MessageKey("SMSTemplateMessage", "ThereMustBeAMessageFor0");
  export const TheresMoreThanOneMessageForTheSameLanguage = new MessageKey("SMSTemplateMessage", "TheresMoreThanOneMessageForTheSameLanguage");
  export const NewCulture = new MessageKey("SMSTemplateMessage", "NewCulture");
  export const _0CharactersRemainingBeforeReplacements = new MessageKey("SMSTemplateMessage", "_0CharactersRemainingBeforeReplacements");
  export const ToMustBeSetInTheTemplate = new MessageKey("SMSTemplateMessage", "ToMustBeSetInTheTemplate");
}

export const SMSTemplateMessageEmbedded = new Type<SMSTemplateMessageEmbedded>("SMSTemplateMessageEmbedded");
export interface SMSTemplateMessageEmbedded extends Entities.EmbeddedEntity {
  Type: "SMSTemplateMessageEmbedded";
  cultureInfo: Basics.CultureInfoEntity;
  message: string;
}

export module SMSTemplateOperation {
  export const CreateSMSTemplateFromModel : Operations.ConstructSymbol_From<SMSTemplateEntity, SMSModelEntity> = registerSymbol("Operation", "SMSTemplateOperation.CreateSMSTemplateFromModel");
  export const Create : Operations.ConstructSymbol_Simple<SMSTemplateEntity> = registerSymbol("Operation", "SMSTemplateOperation.Create");
  export const Save : Operations.ExecuteSymbol<SMSTemplateEntity> = registerSymbol("Operation", "SMSTemplateOperation.Save");
}

export const SMSUpdatePackageEntity = new Type<SMSUpdatePackageEntity>("SMSUpdatePackage");
export interface SMSUpdatePackageEntity extends SMSPackageEntity {
  Type: "SMSUpdatePackage";
}


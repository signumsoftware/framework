//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Signum/React/Reflection'
import * as Entities from '../../../Signum/React/Signum.Entities'
import * as Basics from '../../../Signum/React/Signum.Basics'
import * as Operations from '../../../Signum/React/Signum.Operations'
import * as Scheduler from '../../Signum.Scheduler/Signum.Scheduler'


export const CompareInbox: EnumType<CompareInbox> = new EnumType<CompareInbox>("CompareInbox");
export type CompareInbox =
  "Full" |
  "LastNEmails";

export namespace EmailReceptionAction {
  export const ReceiveAllActiveEmailConfigurations : Scheduler.SimpleTaskSymbol = registerSymbol("SimpleTask", "EmailReceptionAction.ReceiveAllActiveEmailConfigurations");
}

export const EmailReceptionConfigurationEntity: Type<EmailReceptionConfigurationEntity> = new Type<EmailReceptionConfigurationEntity>("EmailReceptionConfiguration");
export interface EmailReceptionConfigurationEntity extends Entities.Entity, Scheduler.ITaskEntity {
  Type: "EmailReceptionConfiguration";
  active: boolean;
  emailAddress: string;
  deleteMessagesAfter: number | null;
  compareInbox: CompareInbox;
  service: EmailReceptionServiceEntity;
}

export namespace EmailReceptionConfigurationOperation {
  export const Save : Operations.ExecuteSymbol<EmailReceptionConfigurationEntity> = registerSymbol("Operation", "EmailReceptionConfigurationOperation.Save");
  export const ReceiveEmails : Operations.ConstructSymbol_From<EmailReceptionEntity, EmailReceptionConfigurationEntity> = registerSymbol("Operation", "EmailReceptionConfigurationOperation.ReceiveEmails");
  export const ReceiveLastEmails : Operations.ConstructSymbol_From<EmailReceptionEntity, EmailReceptionConfigurationEntity> = registerSymbol("Operation", "EmailReceptionConfigurationOperation.ReceiveLastEmails");
}

export const EmailReceptionEntity: Type<EmailReceptionEntity> = new Type<EmailReceptionEntity>("EmailReception");
export interface EmailReceptionEntity extends Entities.Entity {
  Type: "EmailReception";
  emailReceptionConfiguration: Entities.Lite<EmailReceptionConfigurationEntity>;
  startDate: string /*DateTime*/;
  endDate: string /*DateTime*/ | null;
  newEmails: number;
  serverEmails: number;
  lastServerMessageUID: string | null;
  mailsFromDifferentAccounts: boolean;
  exception: Entities.Lite<Basics.ExceptionEntity> | null;
}

export const EmailReceptionExceptionEntity: Type<EmailReceptionExceptionEntity> = new Type<EmailReceptionExceptionEntity>("EmailReceptionException");
export interface EmailReceptionExceptionEntity extends Entities.Entity {
  Type: "EmailReceptionException";
  reception: Entities.Lite<EmailReceptionEntity>;
  exception: Entities.Lite<Basics.ExceptionEntity>;
}

export const EmailReceptionInfoEmbedded: Type<EmailReceptionInfoEmbedded> = new Type<EmailReceptionInfoEmbedded>("EmailReceptionInfoEmbedded");
export interface EmailReceptionInfoEmbedded extends Entities.EmbeddedEntity {
  Type: "EmailReceptionInfoEmbedded";
  uniqueId: string;
  reception: Entities.Lite<EmailReceptionEntity>;
  rawContent: Entities.BigStringEmbedded;
  sentDate: string /*DateTime*/;
  receivedDate: string /*DateTime*/;
  deletionDate: string /*DateTime*/ | null;
}

export const EmailReceptionMixin: Type<EmailReceptionMixin> = new Type<EmailReceptionMixin>("EmailReceptionMixin");
export interface EmailReceptionMixin extends Entities.MixinEntity {
  Type: "EmailReceptionMixin";
  receptionInfo: EmailReceptionInfoEmbedded | null;
}

export interface EmailReceptionServiceEntity extends Entities.Entity {
}


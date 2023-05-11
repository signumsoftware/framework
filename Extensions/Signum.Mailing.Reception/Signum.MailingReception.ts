//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as Scheduler from '../Signum.Scheduler/Signum.Scheduler'
import * as Mailing from '../Signum.Mailing/Signum.Mailing'


export const EmailReceptionInfoEmbedded = new Type<EmailReceptionInfoEmbedded>("EmailReceptionInfoEmbedded");
export interface EmailReceptionInfoEmbedded extends Entities.EmbeddedEntity {
  Type: "EmailReceptionInfoEmbedded";
  uniqueId: string;
  reception: Entities.Lite<Pop3ReceptionEntity>;
  rawContent: Entities.BigStringEmbedded;
  sentDate: string /*DateTime*/;
  receivedDate: string /*DateTime*/;
  deletionDate: string /*DateTime*/ | null;
}

export const EmailReceptionMixin = new Type<EmailReceptionMixin>("EmailReceptionMixin");
export interface EmailReceptionMixin extends Entities.MixinEntity {
  Type: "EmailReceptionMixin";
  receptionInfo: EmailReceptionInfoEmbedded | null;
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
  clientCertificationFiles: Entities.MList<Mailing.ClientCertificationFileEmbedded>;
}

export module Pop3ConfigurationOperation {
  export const Save : Operations.ExecuteSymbol<Pop3ConfigurationEntity> = registerSymbol("Operation", "Pop3ConfigurationOperation.Save");
  export const ReceiveEmails : Operations.ConstructSymbol_From<Pop3ReceptionEntity, Pop3ConfigurationEntity> = registerSymbol("Operation", "Pop3ConfigurationOperation.ReceiveEmails");
  export const ReceiveLastEmails : Operations.ConstructSymbol_From<Pop3ReceptionEntity, Pop3ConfigurationEntity> = registerSymbol("Operation", "Pop3ConfigurationOperation.ReceiveLastEmails");
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
  exception: Entities.Lite<Basics.ExceptionEntity> | null;
}

export const Pop3ReceptionExceptionEntity = new Type<Pop3ReceptionExceptionEntity>("Pop3ReceptionException");
export interface Pop3ReceptionExceptionEntity extends Entities.Entity {
  Type: "Pop3ReceptionException";
  reception: Entities.Lite<Pop3ReceptionEntity>;
  exception: Entities.Lite<Basics.ExceptionEntity>;
}


//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Signum/React/Reflection'
import * as Entities from '../../../Signum/React/Signum.Entities'
import * as Operations from '../../../Signum/React/Signum.Operations'
import * as Processes from '../../Signum.Processes/Signum.Processes'
import * as Scheduler from '../../Signum.Scheduler/Signum.Scheduler'
import * as Templates from '../Signum.Mailing.Templates'
import * as Mailing from '../Signum.Mailing'
import * as UserQueries from '../../Signum.UserQueries/Signum.UserQueries'
import * as Templating from '../../Signum.Templating/Signum.Templating'


export const EmailMessagePackageMixin: Type<EmailMessagePackageMixin> = new Type<EmailMessagePackageMixin>("EmailMessagePackageMixin");
export interface EmailMessagePackageMixin extends Entities.MixinEntity {
  Type: "EmailMessagePackageMixin";
  package: Entities.Lite<EmailPackageEntity> | null;
}

export namespace EmailMessagePackageOperation {
  export const ReSendEmails : Operations.ConstructSymbol_FromMany<Processes.ProcessEntity, Mailing.EmailMessageEntity> = registerSymbol("Operation", "EmailMessagePackageOperation.ReSendEmails");
}

export namespace EmailMessageProcess {
  export const CreateEmailsSendAsync : Processes.ProcessAlgorithmSymbol = registerSymbol("ProcessAlgorithm", "EmailMessageProcess.CreateEmailsSendAsync");
  export const SendEmails : Processes.ProcessAlgorithmSymbol = registerSymbol("ProcessAlgorithm", "EmailMessageProcess.SendEmails");
}

export const EmailPackageEntity: Type<EmailPackageEntity> = new Type<EmailPackageEntity>("EmailPackage");
export interface EmailPackageEntity extends Entities.Entity, Processes.IProcessDataEntity {
  Type: "EmailPackage";
  name: string | null;
}

export const EmaiTemplateTargetFrom: EnumType<EmaiTemplateTargetFrom> = new EnumType<EmaiTemplateTargetFrom>("EmaiTemplateTargetFrom");
export type EmaiTemplateTargetFrom =
  "NoTarget" |
  "Unique" |
  "UserQuery";

export const SendEmailTaskEntity: Type<SendEmailTaskEntity> = new Type<SendEmailTaskEntity>("SendEmailTask");
export interface SendEmailTaskEntity extends Entities.Entity, Scheduler.ITaskEntity {
  Type: "SendEmailTask";
  name: string;
  emailTemplate: Entities.Lite<Templates.EmailTemplateEntity>;
  targetFrom: EmaiTemplateTargetFrom;
  uniqueTarget: Entities.Lite<Entities.Entity> | null;
  targetsFromUserQuery: Entities.Lite<UserQueries.UserQueryEntity> | null;
  modelConverter: Templating.ModelConverterSymbol | null;
}

export namespace SendEmailTaskOperation {
  export const Save : Operations.ExecuteSymbol<SendEmailTaskEntity> = registerSymbol("Operation", "SendEmailTaskOperation.Save");
}


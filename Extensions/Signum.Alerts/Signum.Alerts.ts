//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Security from '../../Signum/React/Signum.Security'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as Scheduler from '../Signum.Scheduler/Signum.Scheduler'


export const AlertCurrentState: EnumType<AlertCurrentState> = new EnumType<AlertCurrentState>("AlertCurrentState");
export type AlertCurrentState =
  "Attended" |
  "Alerted" |
  "Future";

export const AlertDropDownGroup: EnumType<AlertDropDownGroup> = new EnumType<AlertDropDownGroup>("AlertDropDownGroup");
export type AlertDropDownGroup =
  "ByType" |
  "ByUser" |
  "ByTypeAndUser";

export const AlertEntity: Type<AlertEntity> = new Type<AlertEntity>("Alert");
export interface AlertEntity extends Entities.Entity {
  Type: "Alert";
  target: Entities.Lite<Entities.Entity> | null;
  linkTarget: Entities.Lite<Entities.Entity> | null;
  groupTarget: Entities.Lite<Entities.Entity> | null;
  creationDate: string /*DateTime*/;
  alertDate: string /*DateTime*/ | null;
  attendedDate: string /*DateTime*/ | null;
  titleField: string | null;
  textArguments: string | null;
  textField: string | null;
  textFromAlertType: string | null;
  createdBy: Entities.Lite<Security.IUserEntity> | null;
  recipient: Entities.Lite<Security.IUserEntity> | null;
  attendedBy: Entities.Lite<Security.IUserEntity> | null;
  alertType: AlertTypeSymbol | null;
  state: AlertState;
  emailNotificationsSent: boolean;
  avoidSendMail: boolean;
}

export namespace AlertMessage {
  export const Alert: MessageKey = new MessageKey("AlertMessage", "Alert");
  export const NewAlert: MessageKey = new MessageKey("AlertMessage", "NewAlert");
  export const Alerts: MessageKey = new MessageKey("AlertMessage", "Alerts");
  export const Alerts_Attended: MessageKey = new MessageKey("AlertMessage", "Alerts_Attended");
  export const Alerts_Future: MessageKey = new MessageKey("AlertMessage", "Alerts_Future");
  export const Alerts_NotAttended: MessageKey = new MessageKey("AlertMessage", "Alerts_NotAttended");
  export const CheckedAlerts: MessageKey = new MessageKey("AlertMessage", "CheckedAlerts");
  export const CreateAlert: MessageKey = new MessageKey("AlertMessage", "CreateAlert");
  export const FutureAlerts: MessageKey = new MessageKey("AlertMessage", "FutureAlerts");
  export const WarnedAlerts: MessageKey = new MessageKey("AlertMessage", "WarnedAlerts");
  export const CustomDelay: MessageKey = new MessageKey("AlertMessage", "CustomDelay");
  export const DelayDuration: MessageKey = new MessageKey("AlertMessage", "DelayDuration");
  export const MyActiveAlerts: MessageKey = new MessageKey("AlertMessage", "MyActiveAlerts");
  export const YouDoNotHaveAnyActiveAlert: MessageKey = new MessageKey("AlertMessage", "YouDoNotHaveAnyActiveAlert");
  export const _0SimilarAlerts: MessageKey = new MessageKey("AlertMessage", "_0SimilarAlerts");
  export const _0HiddenAlerts: MessageKey = new MessageKey("AlertMessage", "_0HiddenAlerts");
  export const ViewMore: MessageKey = new MessageKey("AlertMessage", "ViewMore");
  export const CloseAll: MessageKey = new MessageKey("AlertMessage", "CloseAll");
  export const AllMyAlerts: MessageKey = new MessageKey("AlertMessage", "AllMyAlerts");
  export const NewUnreadNotifications: MessageKey = new MessageKey("AlertMessage", "NewUnreadNotifications");
  export const Title: MessageKey = new MessageKey("AlertMessage", "Title");
  export const Text: MessageKey = new MessageKey("AlertMessage", "Text");
  export const Hi0: MessageKey = new MessageKey("AlertMessage", "Hi0");
  export const YouHaveSomePendingAlerts: MessageKey = new MessageKey("AlertMessage", "YouHaveSomePendingAlerts");
  export const PleaseVisit0: MessageKey = new MessageKey("AlertMessage", "PleaseVisit0");
  export const OtherNotifications: MessageKey = new MessageKey("AlertMessage", "OtherNotifications");
  export const Expand: MessageKey = new MessageKey("AlertMessage", "Expand");
  export const Collapse: MessageKey = new MessageKey("AlertMessage", "Collapse");
  export const Show0AlertsMore: MessageKey = new MessageKey("AlertMessage", "Show0AlertsMore");
  export const Show0GroupsMore1Remaining: MessageKey = new MessageKey("AlertMessage", "Show0GroupsMore1Remaining");
  export const Ringing: MessageKey = new MessageKey("AlertMessage", "Ringing");
}

export namespace AlertOperation {
  export const CreateAlertFromEntity : Operations.ConstructSymbol_From<AlertEntity, Entities.Entity> = registerSymbol("Operation", "AlertOperation.CreateAlertFromEntity");
  export const Create : Operations.ConstructSymbol_Simple<AlertEntity> = registerSymbol("Operation", "AlertOperation.Create");
  export const Save : Operations.ExecuteSymbol<AlertEntity> = registerSymbol("Operation", "AlertOperation.Save");
  export const Delay : Operations.ExecuteSymbol<AlertEntity> = registerSymbol("Operation", "AlertOperation.Delay");
  export const Attend : Operations.ExecuteSymbol<AlertEntity> = registerSymbol("Operation", "AlertOperation.Attend");
  export const Unattend : Operations.ExecuteSymbol<AlertEntity> = registerSymbol("Operation", "AlertOperation.Unattend");
}

export const AlertState: EnumType<AlertState> = new EnumType<AlertState>("AlertState");
export type AlertState =
  "New" |
  "Saved" |
  "Attended";

export namespace AlertTypeOperation {
  export const Save : Operations.ExecuteSymbol<AlertTypeSymbol> = registerSymbol("Operation", "AlertTypeOperation.Save");
  export const Delete : Operations.DeleteSymbol<AlertTypeSymbol> = registerSymbol("Operation", "AlertTypeOperation.Delete");
}

export const AlertTypeSymbol: Type<AlertTypeSymbol> = new Type<AlertTypeSymbol>("AlertType");
export interface AlertTypeSymbol extends Basics.SemiSymbol {
  Type: "AlertType";
}

export const DelayOption: EnumType<DelayOption> = new EnumType<DelayOption>("DelayOption");
export type DelayOption =
  "_5Mins" |
  "_15Mins" |
  "_30Mins" |
  "_1Hour" |
  "_2Hours" |
  "_1Day" |
  "Custom";

export const SendAlertTypeBehavior: EnumType<SendAlertTypeBehavior> = new EnumType<SendAlertTypeBehavior>("SendAlertTypeBehavior");
export type SendAlertTypeBehavior =
  "All" |
  "Include" |
  "Exclude";

export const SendNotificationEmailTaskEntity: Type<SendNotificationEmailTaskEntity> = new Type<SendNotificationEmailTaskEntity>("SendNotificationEmailTask");
export interface SendNotificationEmailTaskEntity extends Entities.Entity, Scheduler.ITaskEntity {
  Type: "SendNotificationEmailTask";
  sendNotificationsOlderThan: number;
  ignoreNotificationsOlderThan: number | null;
  sendBehavior: SendAlertTypeBehavior;
  alertTypes: Entities.MList<AlertTypeSymbol>;
}

export namespace SendNotificationEmailTaskOperation {
  export const Save : Operations.ExecuteSymbol<SendNotificationEmailTaskEntity> = registerSymbol("Operation", "SendNotificationEmailTaskOperation.Save");
}


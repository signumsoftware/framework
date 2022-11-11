//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum.React/Scripts/Reflection'
import * as Entities from '../../Signum.React/Scripts/Signum.Entities'
import * as Basics from '../../Signum.React/Scripts/Signum.Entities.Basics'
import * as Scheduler from '../Scheduler/Signum.Entities.Scheduler'


export const AlertCurrentState = new EnumType<AlertCurrentState>("AlertCurrentState");
export type AlertCurrentState =
  "Attended" |
  "Alerted" |
  "Future";

export const AlertDropDownGroup = new EnumType<AlertDropDownGroup>("AlertDropDownGroup");
export type AlertDropDownGroup =
  "ByType" |
  "ByUser" |
  "ByTypeAndUser";

export const AlertEntity = new Type<AlertEntity>("Alert");
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
  createdBy: Entities.Lite<Basics.IUserEntity> | null;
  recipient: Entities.Lite<Basics.IUserEntity> | null;
  attendedBy: Entities.Lite<Basics.IUserEntity> | null;
  alertType: AlertTypeSymbol | null;
  state: AlertState;
  emailNotificationsSent: boolean;
}

export module AlertMessage {
  export const Alert = new MessageKey("AlertMessage", "Alert");
  export const NewAlert = new MessageKey("AlertMessage", "NewAlert");
  export const Alerts = new MessageKey("AlertMessage", "Alerts");
  export const Alerts_Attended = new MessageKey("AlertMessage", "Alerts_Attended");
  export const Alerts_Future = new MessageKey("AlertMessage", "Alerts_Future");
  export const Alerts_NotAttended = new MessageKey("AlertMessage", "Alerts_NotAttended");
  export const CheckedAlerts = new MessageKey("AlertMessage", "CheckedAlerts");
  export const CreateAlert = new MessageKey("AlertMessage", "CreateAlert");
  export const FutureAlerts = new MessageKey("AlertMessage", "FutureAlerts");
  export const WarnedAlerts = new MessageKey("AlertMessage", "WarnedAlerts");
  export const CustomDelay = new MessageKey("AlertMessage", "CustomDelay");
  export const DelayDuration = new MessageKey("AlertMessage", "DelayDuration");
  export const MyActiveAlerts = new MessageKey("AlertMessage", "MyActiveAlerts");
  export const YouDoNotHaveAnyActiveAlert = new MessageKey("AlertMessage", "YouDoNotHaveAnyActiveAlert");
  export const _0SimilarAlerts = new MessageKey("AlertMessage", "_0SimilarAlerts");
  export const _0HiddenAlerts = new MessageKey("AlertMessage", "_0HiddenAlerts");
  export const ViewMore = new MessageKey("AlertMessage", "ViewMore");
  export const CloseAll = new MessageKey("AlertMessage", "CloseAll");
  export const AllMyAlerts = new MessageKey("AlertMessage", "AllMyAlerts");
  export const NewUnreadNotifications = new MessageKey("AlertMessage", "NewUnreadNotifications");
  export const Title = new MessageKey("AlertMessage", "Title");
  export const Text = new MessageKey("AlertMessage", "Text");
  export const Hi0 = new MessageKey("AlertMessage", "Hi0");
  export const YouHaveSomePendingAlerts = new MessageKey("AlertMessage", "YouHaveSomePendingAlerts");
  export const PleaseVisit0 = new MessageKey("AlertMessage", "PleaseVisit0");
  export const OtherNotifications = new MessageKey("AlertMessage", "OtherNotifications");
  export const Expand = new MessageKey("AlertMessage", "Expand");
  export const Collapse = new MessageKey("AlertMessage", "Collapse");
  export const Show0AlertsMore = new MessageKey("AlertMessage", "Show0AlertsMore");
  export const Show0GroupsMore1Remaining = new MessageKey("AlertMessage", "Show0GroupsMore1Remaining");
}

export module AlertOperation {
  export const CreateAlertFromEntity : Entities.ConstructSymbol_From<AlertEntity, Entities.Entity> = registerSymbol("Operation", "AlertOperation.CreateAlertFromEntity");
  export const Create : Entities.ConstructSymbol_Simple<AlertEntity> = registerSymbol("Operation", "AlertOperation.Create");
  export const Save : Entities.ExecuteSymbol<AlertEntity> = registerSymbol("Operation", "AlertOperation.Save");
  export const Delay : Entities.ExecuteSymbol<AlertEntity> = registerSymbol("Operation", "AlertOperation.Delay");
  export const Attend : Entities.ExecuteSymbol<AlertEntity> = registerSymbol("Operation", "AlertOperation.Attend");
  export const Unattend : Entities.ExecuteSymbol<AlertEntity> = registerSymbol("Operation", "AlertOperation.Unattend");
}

export const AlertState = new EnumType<AlertState>("AlertState");
export type AlertState =
  "New" |
  "Saved" |
  "Attended";

export module AlertTypeOperation {
  export const Save : Entities.ExecuteSymbol<AlertTypeSymbol> = registerSymbol("Operation", "AlertTypeOperation.Save");
  export const Delete : Entities.DeleteSymbol<AlertTypeSymbol> = registerSymbol("Operation", "AlertTypeOperation.Delete");
}

export const AlertTypeSymbol = new Type<AlertTypeSymbol>("AlertType");
export interface AlertTypeSymbol extends Basics.SemiSymbol {
  Type: "AlertType";
}

export const DelayOption = new EnumType<DelayOption>("DelayOption");
export type DelayOption =
  "_5Mins" |
  "_15Mins" |
  "_30Mins" |
  "_1Hour" |
  "_2Hours" |
  "_1Day" |
  "Custom";

export const SendAlertTypeBehavior = new EnumType<SendAlertTypeBehavior>("SendAlertTypeBehavior");
export type SendAlertTypeBehavior =
  "All" |
  "Include" |
  "Exclude";

export const SendNotificationEmailTaskEntity = new Type<SendNotificationEmailTaskEntity>("SendNotificationEmailTask");
export interface SendNotificationEmailTaskEntity extends Entities.Entity, Scheduler.ITaskEntity {
  Type: "SendNotificationEmailTask";
  sendNotificationsOlderThan: number;
  ignoreNotificationsOlderThan: number | null;
  sendBehavior: SendAlertTypeBehavior;
  alertTypes: Entities.MList<AlertTypeSymbol>;
}

export module SendNotificationEmailTaskOperation {
  export const Save : Entities.ExecuteSymbol<SendNotificationEmailTaskEntity> = registerSymbol("Operation", "SendNotificationEmailTaskOperation.Save");
}



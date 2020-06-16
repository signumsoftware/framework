//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Basics from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'


export const AlertCurrentState = new EnumType<AlertCurrentState>("AlertCurrentState");
export type AlertCurrentState =
  "Attended" |
  "Alerted" |
  "Future";

export const AlertEntity = new Type<AlertEntity>("Alert");
export interface AlertEntity extends Entities.Entity {
  Type: "Alert";
  target: Entities.Lite<Entities.Entity> | null;
  creationDate: string;
  alertDate: string | null;
  attendedDate: string | null;
  title: string | null;
  text: string;
  createdBy: Entities.Lite<Basics.IUserEntity> | null;
  recipient: Entities.Lite<Basics.IUserEntity> | null;
  attendedBy: Entities.Lite<Basics.IUserEntity> | null;
  alertType: AlertTypeEntity | null;
  state: AlertState;
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

export const AlertTypeEntity = new Type<AlertTypeEntity>("AlertType");
export interface AlertTypeEntity extends Basics.SemiSymbol {
  Type: "AlertType";
}

export module AlertTypeOperation {
  export const Save : Entities.ExecuteSymbol<AlertTypeEntity> = registerSymbol("Operation", "AlertTypeOperation.Save");
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



//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Basics from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'


export const AlertEntity = new Type<AlertEntity>("Alert");
export interface AlertEntity extends Entities.Entity {
    Type: "Alert";
    target?: Entities.Lite<Entities.Entity> | null;
    creationDate?: string;
    alertDate?: string | null;
    attendedDate?: string | null;
    title?: string | null;
    text?: string | null;
    createdBy?: Entities.Lite<Basics.IUserEntity> | null;
    attendedBy?: Entities.Lite<Basics.IUserEntity> | null;
    alertType?: AlertTypeEntity | null;
    state?: AlertState;
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
}

export module AlertOperation {
    export const CreateAlertFromEntity : Entities.ConstructSymbol_From<AlertEntity, Entities.Entity> = registerSymbol({ Type: "Operation", key: "AlertOperation.CreateAlertFromEntity" });
    export const SaveNew : Entities.ExecuteSymbol<AlertEntity> = registerSymbol({ Type: "Operation", key: "AlertOperation.SaveNew" });
    export const Save : Entities.ExecuteSymbol<AlertEntity> = registerSymbol({ Type: "Operation", key: "AlertOperation.Save" });
    export const Attend : Entities.ExecuteSymbol<AlertEntity> = registerSymbol({ Type: "Operation", key: "AlertOperation.Attend" });
    export const Unattend : Entities.ExecuteSymbol<AlertEntity> = registerSymbol({ Type: "Operation", key: "AlertOperation.Unattend" });
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
    export const Save : Entities.ExecuteSymbol<AlertTypeEntity> = registerSymbol({ Type: "Operation", key: "AlertTypeOperation.Save" });
}



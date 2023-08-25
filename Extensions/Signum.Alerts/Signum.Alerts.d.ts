import { MessageKey, Type, EnumType } from '../../Signum/React/Reflection';
import * as Entities from '../../Signum/React/Signum.Entities';
import * as Security from '../../Signum/React/Signum.Security';
import * as Basics from '../../Signum/React/Signum.Basics';
import * as Operations from '../../Signum/React/Signum.Operations';
import * as Scheduler from '../Signum.Scheduler/Signum.Scheduler';
export declare const AlertCurrentState: EnumType<AlertCurrentState>;
export type AlertCurrentState = "Attended" | "Alerted" | "Future";
export declare const AlertDropDownGroup: EnumType<AlertDropDownGroup>;
export type AlertDropDownGroup = "ByType" | "ByUser" | "ByTypeAndUser";
export declare const AlertEntity: Type<AlertEntity>;
export interface AlertEntity extends Entities.Entity {
    Type: "Alert";
    target: Entities.Lite<Entities.Entity> | null;
    linkTarget: Entities.Lite<Entities.Entity> | null;
    groupTarget: Entities.Lite<Entities.Entity> | null;
    creationDate: string;
    alertDate: string | null;
    attendedDate: string | null;
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
}
export declare module AlertMessage {
    const Alert: MessageKey;
    const NewAlert: MessageKey;
    const Alerts: MessageKey;
    const Alerts_Attended: MessageKey;
    const Alerts_Future: MessageKey;
    const Alerts_NotAttended: MessageKey;
    const CheckedAlerts: MessageKey;
    const CreateAlert: MessageKey;
    const FutureAlerts: MessageKey;
    const WarnedAlerts: MessageKey;
    const CustomDelay: MessageKey;
    const DelayDuration: MessageKey;
    const MyActiveAlerts: MessageKey;
    const YouDoNotHaveAnyActiveAlert: MessageKey;
    const _0SimilarAlerts: MessageKey;
    const _0HiddenAlerts: MessageKey;
    const ViewMore: MessageKey;
    const CloseAll: MessageKey;
    const AllMyAlerts: MessageKey;
    const NewUnreadNotifications: MessageKey;
    const Title: MessageKey;
    const Text: MessageKey;
    const Hi0: MessageKey;
    const YouHaveSomePendingAlerts: MessageKey;
    const PleaseVisit0: MessageKey;
    const OtherNotifications: MessageKey;
    const Expand: MessageKey;
    const Collapse: MessageKey;
    const Show0AlertsMore: MessageKey;
    const Show0GroupsMore1Remaining: MessageKey;
    const Ringing: MessageKey;
}
export declare module AlertOperation {
    const CreateAlertFromEntity: Operations.ConstructSymbol_From<AlertEntity, Entities.Entity>;
    const Create: Operations.ConstructSymbol_Simple<AlertEntity>;
    const Save: Operations.ExecuteSymbol<AlertEntity>;
    const Delay: Operations.ExecuteSymbol<AlertEntity>;
    const Attend: Operations.ExecuteSymbol<AlertEntity>;
    const Unattend: Operations.ExecuteSymbol<AlertEntity>;
}
export declare const AlertState: EnumType<AlertState>;
export type AlertState = "New" | "Saved" | "Attended";
export declare module AlertTypeOperation {
    const Save: Operations.ExecuteSymbol<AlertTypeSymbol>;
    const Delete: Operations.DeleteSymbol<AlertTypeSymbol>;
}
export declare const AlertTypeSymbol: Type<AlertTypeSymbol>;
export interface AlertTypeSymbol extends Basics.SemiSymbol {
    Type: "AlertType";
}
export declare const DelayOption: EnumType<DelayOption>;
export type DelayOption = "_5Mins" | "_15Mins" | "_30Mins" | "_1Hour" | "_2Hours" | "_1Day" | "Custom";
export declare const SendAlertTypeBehavior: EnumType<SendAlertTypeBehavior>;
export type SendAlertTypeBehavior = "All" | "Include" | "Exclude";
export declare const SendNotificationEmailTaskEntity: Type<SendNotificationEmailTaskEntity>;
export interface SendNotificationEmailTaskEntity extends Entities.Entity, Scheduler.ITaskEntity {
    Type: "SendNotificationEmailTask";
    sendNotificationsOlderThan: number;
    ignoreNotificationsOlderThan: number | null;
    sendBehavior: SendAlertTypeBehavior;
    alertTypes: Entities.MList<AlertTypeSymbol>;
}
export declare module SendNotificationEmailTaskOperation {
    const Save: Operations.ExecuteSymbol<SendNotificationEmailTaskEntity>;
}

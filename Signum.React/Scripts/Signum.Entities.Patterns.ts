//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////
import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from './Reflection' 

import * as Entities from './Signum.Entities' 
export module EntityMessage {
    export const AttemptToSet0InLockedEntity1 = new MessageKey("EntityMessage", "AttemptToSet0InLockedEntity1");
    export const AttemptToAddRemove0InLockedEntity1 = new MessageKey("EntityMessage", "AttemptToAddRemove0InLockedEntity1");
}

export interface LockableEntity extends Entities.Entity {
    locked?: boolean;
}

export namespace External {

    export module CollectionMessage {
        export const And = new MessageKey("CollectionMessage", "And");
        export const Or = new MessageKey("CollectionMessage", "Or");
        export const No0Found = new MessageKey("CollectionMessage", "No0Found");
        export const MoreThanOne0Found = new MessageKey("CollectionMessage", "MoreThanOne0Found");
    }
    
    export enum DayOfWeek {
        Sunday = "Sunday" as any,
        Monday = "Monday" as any,
        Tuesday = "Tuesday" as any,
        Wednesday = "Wednesday" as any,
        Thursday = "Thursday" as any,
        Friday = "Friday" as any,
        Saturday = "Saturday" as any,
    }
    export const DayOfWeek_Type = new EnumType<DayOfWeek>("DayOfWeek", DayOfWeek);
    
}


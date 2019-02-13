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
  locked: boolean;
}



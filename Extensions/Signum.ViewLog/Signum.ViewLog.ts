//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Security from '../../Signum/React/Signum.Security'


export const ViewLogEntity: Type<ViewLogEntity> = new Type<ViewLogEntity>("ViewLog");
export interface ViewLogEntity extends Entities.Entity {
  Type: "ViewLog";
  target: Entities.Lite<Entities.Entity>;
  user: Entities.Lite<Security.IUserEntity>;
  viewAction: string;
  startDate: string /*DateTime*/;
  endDate: string /*DateTime*/;
  data: Entities.BigStringEmbedded;
}

export namespace ViewLogMessage {
  export const ViewLogMyLast: MessageKey = new MessageKey("ViewLogMessage", "ViewLogMyLast");
}


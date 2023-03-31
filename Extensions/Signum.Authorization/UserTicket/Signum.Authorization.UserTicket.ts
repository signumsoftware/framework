//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Signum/React/Reflection'
import * as Entities from '../../../Signum/React/Signum.Entities'
import * as Authorization from '../Signum.Authorization'


export const UserTicketEntity = new Type<UserTicketEntity>("UserTicket");
export interface UserTicketEntity extends Entities.Entity {
  Type: "UserTicket";
  user: Entities.Lite<Authorization.UserEntity>;
  ticket: string;
  connectionDate: string /*DateTime*/;
  device: string;
}


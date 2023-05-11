//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Mailing from '../Signum.Mailing/Signum.Mailing'

import * as External from './Signum.Mailing.ExchangeWS.External'

export const ExchangeWebServiceEmailServiceEntity = new Type<ExchangeWebServiceEmailServiceEntity>("ExchangeWebServiceEmailService");
export interface ExchangeWebServiceEmailServiceEntity extends Mailing.EmailServiceEntity {
  Type: "ExchangeWebServiceEmailService";
  exchangeVersion: External.ExchangeVersion;
  url: string | null;
  username: string | null;
  password: string | null;
  useDefaultCredentials: boolean;
}


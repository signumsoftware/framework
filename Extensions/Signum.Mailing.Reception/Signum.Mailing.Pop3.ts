//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as MailingReception from '../Signum.Mailing/Signum.MailingReception'
import * as Mailing from '../Signum.Mailing/Signum.Mailing'


export const Pop3EmailReceptionServiceEntity = new Type<Pop3EmailReceptionServiceEntity>("Pop3EmailReceptionService");
export interface Pop3EmailReceptionServiceEntity extends MailingReception.EmailReceptionServiceEntity {
  Type: "Pop3EmailReceptionService";
  port: number;
  host: string;
  username: string | null;
  password: string | null;
  enableSSL: boolean;
  readTimeout: number;
  clientCertificationFiles: Entities.MList<Mailing.ClientCertificationFileEmbedded>;
}


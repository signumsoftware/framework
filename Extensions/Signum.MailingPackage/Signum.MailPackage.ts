//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum.React/Reflection'
import * as Entities from '../../Signum.React/Signum.Entities'
import * as Mailing from './Signum.Mailing'


export const EmailMessagePackageMixin = new Type<EmailMessagePackageMixin>("EmailMessagePackageMixin");
export interface EmailMessagePackageMixin extends Entities.MixinEntity {
  Type: "EmailMessagePackageMixin";
  package: Entities.Lite<Mailing.EmailPackageEntity> | null;
}


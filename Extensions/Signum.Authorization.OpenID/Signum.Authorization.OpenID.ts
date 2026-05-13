//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as BaseAD from '../Signum.Authorization/Signum.Authorization.BaseAD'

export namespace OpenIDMessage {
  export const SignInWithOpenID: MessageKey = new MessageKey("OpenIDMessage", "SignInWithOpenID");
}

export const OpenIDConfigurationEmbedded: Type<OpenIDConfigurationEmbedded> = new Type<OpenIDConfigurationEmbedded>("OpenIDConfigurationEmbedded");
export interface OpenIDConfigurationEmbedded extends BaseAD.BaseADConfigurationEmbedded {
  Type: "OpenIDConfigurationEmbedded";
  enabled: boolean;
  authority: string | null;
  clientId: string | null;
  clientSecret: string | null;
  roleClaimPath: string | null;
  scopes: string | null;
}

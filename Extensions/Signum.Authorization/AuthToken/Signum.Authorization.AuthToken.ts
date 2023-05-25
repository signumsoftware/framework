//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Signum/React/Reflection'
import * as Entities from '../../../Signum/React/Signum.Entities'


export const AuthTokenConfigurationEmbedded = new Type<AuthTokenConfigurationEmbedded>("AuthTokenConfigurationEmbedded");
export interface AuthTokenConfigurationEmbedded extends Entities.EmbeddedEntity {
  Type: "AuthTokenConfigurationEmbedded";
  refreshTokenEvery: number;
  refreshAnyTokenPreviousTo: string /*DateTime*/ | null;
}


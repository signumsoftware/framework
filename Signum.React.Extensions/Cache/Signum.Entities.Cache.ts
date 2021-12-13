//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum.React/Scripts/Reflection'
import * as Entities from '../../Signum.React/Scripts/Signum.Entities'
import * as Authorization from '../Authorization/Signum.Entities.Authorization'


export const CacheConfigurationEmbedded = new Type<CacheConfigurationEmbedded>("CacheConfigurationEmbedded");
export interface CacheConfigurationEmbedded extends Entities.EmbeddedEntity {
  Type: "CacheConfigurationEmbedded";
  commonSecret: string;
  serverInstances: Entities.MList<ServereInstanceEmbedded>;
}

export module CachePermission {
  export const ViewCache : Authorization.PermissionSymbol = registerSymbol("Permission", "CachePermission.ViewCache");
  export const InvalidateCache : Authorization.PermissionSymbol = registerSymbol("Permission", "CachePermission.InvalidateCache");
}

export const ServereInstanceEmbedded = new Type<ServereInstanceEmbedded>("ServereInstanceEmbedded");
export interface ServereInstanceEmbedded extends Entities.EmbeddedEntity {
  Type: "ServereInstanceEmbedded";
  url: string;
}



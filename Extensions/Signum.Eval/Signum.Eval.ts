//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'


export interface EvalEmbedded<T> extends Entities.EmbeddedEntity {
  script: string;
}

export module EvalPanelMessage {
  export const OpenErrors = new MessageKey("EvalPanelMessage", "OpenErrors");
}

export module EvalPanelPermission {
  export const ViewDynamicPanel : Basics.PermissionSymbol = registerSymbol("Permission", "EvalPanelPermission.ViewDynamicPanel");
}


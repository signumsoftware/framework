//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'


export interface EvalEmbedded<T> extends Entities.EmbeddedEntity {
  script: string;
}

export namespace EvalPanelMessage {
  export const OpenErrors: MessageKey = new MessageKey("EvalPanelMessage", "OpenErrors");
  export const DynamicPanel: MessageKey = new MessageKey("EvalPanelMessage", "DynamicPanel");
  export const Search: MessageKey = new MessageKey("EvalPanelMessage", "Search");
  export const CheckEvals: MessageKey = new MessageKey("EvalPanelMessage", "CheckEvals");
  export const RefreshAll: MessageKey = new MessageKey("EvalPanelMessage", "RefreshAll");
  export const NoErrorsFound: MessageKey = new MessageKey("EvalPanelMessage", "NoErrorsFound");
  export const _0Found: MessageKey = new MessageKey("EvalPanelMessage", "_0Found");
  export const ExceptionChecking0_: MessageKey = new MessageKey("EvalPanelMessage", "ExceptionChecking0_");
  export const YouNeedToRefreshManually: MessageKey = new MessageKey("EvalPanelMessage", "YouNeedToRefreshManually");
  export const RefreshThisClient: MessageKey = new MessageKey("EvalPanelMessage", "RefreshThisClient");
}

export namespace EvalPanelPermission {
  export const ViewDynamicPanel : Basics.PermissionSymbol = registerSymbol("Permission", "EvalPanelPermission.ViewDynamicPanel");
}


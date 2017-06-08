//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Basics from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'


export const ViewLogEntity = new Type<ViewLogEntity>("ViewLog");
export interface ViewLogEntity extends Entities.Entity {
    Type: "ViewLog";
    target?: Entities.Lite<Entities.Entity> | null;
    user?: Entities.Lite<Basics.IUserEntity> | null;
    viewAction?: string | null;
    startDate?: string;
    endDate?: string;
    data?: string | null;
}

export module ViewLogMessage {
    export const ViewLogMyLast = new MessageKey("ViewLogMessage", "ViewLogMyLast");
}



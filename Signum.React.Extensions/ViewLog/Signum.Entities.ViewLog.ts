//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////
import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection' 

import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities' 

export const ViewLogEntity_Type = new Type<ViewLogEntity>("ViewLog");
export interface ViewLogEntity extends Entities.Entity {
    target?: Entities.Lite<Entities.Entity>;
    user?: Entities.Lite<Entities.Basics.IUserEntity>;
    viewAction?: string;
    startDate?: string;
    endDate?: string;
    data?: string;
}


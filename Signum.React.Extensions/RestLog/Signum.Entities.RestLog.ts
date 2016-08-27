//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Basics from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'


export const QueryStringValue = new Type<QueryStringValue>("QueryStringValue");
export interface QueryStringValue extends Entities.EmbeddedEntity {
    Type: "QueryStringValue";
    key?: string | null;
    value?: string | null;
}

export const RestLogEntity = new Type<RestLogEntity>("RestLog");
export interface RestLogEntity extends Entities.Entity {
    Type: "RestLog";
    url: string;
    startDate: string;
    requestBody: string;
    queryString: Entities.MList<QueryStringValue>;
    user: Entities.Lite<Basics.IUserEntity> | null;
    controller: string;
    action: string;
    exception: Entities.Lite<Basics.ExceptionEntity> | null;
    responseBody: string | null;
    endDate: string;
}



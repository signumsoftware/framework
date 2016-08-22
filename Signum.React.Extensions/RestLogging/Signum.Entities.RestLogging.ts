//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Basics from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'


export const RequestValueEntity = new Type<RequestValueEntity>("RequestValueEntity");
export interface RequestValueEntity extends Entities.EmbeddedEntity {
    key: string;
    value: string;
}

export const RestRequestEntity = new Type<RestRequestEntity>("RestRequest");
export interface RestRequestEntity extends Entities.Entity {
    uRL: string;
    response: string;
    creationDate: string;
    startDate: string;
    endDate: string;
    queryString: Entities.MList<RequestValueEntity>;
    user: Entities.Lite<Basics.IUserEntity>;
    controller: string;
    action: string;
    exception: Entities.Lite<Basics.ExceptionEntity>;
}



//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'


export const RequestEntity = new Type<RequestEntity>("Request");
export interface RequestEntity extends Entities.Entity {
    uRL: string;
    response: string;
    creationDate: string;
    values: Entities.MList<RequestValueEntity>;
}

export const RequestValueEntity = new Type<RequestValueEntity>("RequestValueEntity");
export interface RequestValueEntity extends Entities.EmbeddedEntity {
    key: string;
    value: string;
}



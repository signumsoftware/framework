//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////
import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from 'Framework/Signum.React/Scripts/Reflection' 

import * as Entities from 'Framework/Signum.React/Scripts/Signum.Entities' 

import * as Basics from 'Extensions/Signum.React.Extensions/Basics/Signum.Entities.Basics' 

import * as Files from 'Extensions/Signum.React.Extensions/Files/Signum.Entities.Files' 

import * as Authorization from 'Extensions/Signum.React.Extensions/Authorization/Signum.Entities.Authorization' 


export const SystemWordTemplateEntity_Type = new Type<SystemWordTemplateEntity>("SystemWordTemplate");
export interface SystemWordTemplateEntity extends Entities.Entity {
    fullClassName?: string;
}

export const WordConverterSymbol_Type = new Type<WordConverterSymbol>("WordConverter");
export interface WordConverterSymbol extends Entities.Symbol {
}

export const WordTemplateEntity_Type = new Type<WordTemplateEntity>("WordTemplate");
export interface WordTemplateEntity extends Entities.Entity {
    name?: string;
    query?: Entities.Basics.QueryEntity;
    systemWordTemplate?: SystemWordTemplateEntity;
    culture?: Basics.CultureInfoEntity;
    active?: boolean;
    startDate?: string;
    endDate?: string;
    disableAuthorization?: boolean;
    template?: Entities.Lite<Files.FileEntity>;
    fileName?: string;
    wordTransformer?: WordTransformerSymbol;
    wordConverter?: WordConverterSymbol;
}

export module WordTemplateMessage {
    export const ModelShouldBeSetToUseModel0 = new MessageKey("WordTemplateMessage", "ModelShouldBeSetToUseModel0");
    export const Type0DoesNotHaveAPropertyWithName1 = new MessageKey("WordTemplateMessage", "Type0DoesNotHaveAPropertyWithName1");
    export const ChooseAReportTemplate = new MessageKey("WordTemplateMessage", "ChooseAReportTemplate");
    export const _01RequiresExtraParameters = new MessageKey("WordTemplateMessage", "_01RequiresExtraParameters");
}

export module WordTemplateOperation {
    export const Save : Entities.ExecuteSymbol<WordTemplateEntity> = registerSymbol({ Type: "Operation", key: "WordTemplateOperation.Save" });
    export const CreateWordReport : Entities.ExecuteSymbol<WordTemplateEntity> = registerSymbol({ Type: "Operation", key: "WordTemplateOperation.CreateWordReport" });
    export const CreateWordTemplateFromSystemWordTemplate : Entities.ConstructSymbol_From<WordTemplateEntity, SystemWordTemplateEntity> = registerSymbol({ Type: "Operation", key: "WordTemplateOperation.CreateWordTemplateFromSystemWordTemplate" });
}

export module WordTemplatePermission {
    export const GenerateReport : Authorization.PermissionSymbol = registerSymbol({ Type: "Permission", key: "WordTemplatePermission.GenerateReport" });
}

export const WordTransformerSymbol_Type = new Type<WordTransformerSymbol>("WordTransformer");
export interface WordTransformerSymbol extends Entities.Symbol {
}


//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Signum from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import * as Basics from '../Basics/Signum.Entities.Basics'
import * as Authorization from '../Authorization/Signum.Entities.Authorization'


export const TranslatedInstanceEntity = new Type<TranslatedInstanceEntity>("TranslatedInstance");
export interface TranslatedInstanceEntity extends Entities.Entity {
  Type: "TranslatedInstance";
  culture: Basics.CultureInfoEntity;
  instance: Entities.Lite<Entities.Entity>;
  propertyRoute: Signum.PropertyRouteEntity;
  rowId: string | null;
  translatedText: string;
  originalText: string;
}

export module TranslationJavascriptMessage {
  export const WrongTranslationToSubstitute = new MessageKey("TranslationJavascriptMessage", "WrongTranslationToSubstitute");
  export const RightTranslation = new MessageKey("TranslationJavascriptMessage", "RightTranslation");
  export const RememberChange = new MessageKey("TranslationJavascriptMessage", "RememberChange");
}

export module TranslationMessage {
  export const RepeatedCultures0 = new MessageKey("TranslationMessage", "RepeatedCultures0");
  export const CodeTranslations = new MessageKey("TranslationMessage", "CodeTranslations");
  export const InstanceTranslations = new MessageKey("TranslationMessage", "InstanceTranslations");
  export const Synchronize0In1 = new MessageKey("TranslationMessage", "Synchronize0In1");
  export const View0In1 = new MessageKey("TranslationMessage", "View0In1");
  export const AllLanguages = new MessageKey("TranslationMessage", "AllLanguages");
  export const _0AlreadySynchronized = new MessageKey("TranslationMessage", "_0AlreadySynchronized");
  export const NothingToTranslate = new MessageKey("TranslationMessage", "NothingToTranslate");
  export const All = new MessageKey("TranslationMessage", "All");
  export const NothingToTranslateIn0 = new MessageKey("TranslationMessage", "NothingToTranslateIn0");
  export const Sync = new MessageKey("TranslationMessage", "Sync");
  export const View = new MessageKey("TranslationMessage", "View");
  export const None = new MessageKey("TranslationMessage", "None");
  export const Edit = new MessageKey("TranslationMessage", "Edit");
  export const Member = new MessageKey("TranslationMessage", "Member");
  export const Type = new MessageKey("TranslationMessage", "Type");
  export const Instance = new MessageKey("TranslationMessage", "Instance");
  export const Property = new MessageKey("TranslationMessage", "Property");
  export const Save = new MessageKey("TranslationMessage", "Save");
  export const Search = new MessageKey("TranslationMessage", "Search");
  export const PressSearchForResults = new MessageKey("TranslationMessage", "PressSearchForResults");
  export const NoResultsFound = new MessageKey("TranslationMessage", "NoResultsFound");
  export const Namespace = new MessageKey("TranslationMessage", "Namespace");
  export const NewTypes = new MessageKey("TranslationMessage", "NewTypes");
  export const NewTranslations = new MessageKey("TranslationMessage", "NewTranslations");
  export const BackToTranslationStatus = new MessageKey("TranslationMessage", "BackToTranslationStatus");
}

export module TranslationPermission {
  export const TranslateCode : Authorization.PermissionSymbol = registerSymbol("Permission", "TranslationPermission.TranslateCode");
  export const TranslateInstances : Authorization.PermissionSymbol = registerSymbol("Permission", "TranslationPermission.TranslateInstances");
}

export const TranslationReplacementEntity = new Type<TranslationReplacementEntity>("TranslationReplacement");
export interface TranslationReplacementEntity extends Entities.Entity {
  Type: "TranslationReplacement";
  cultureInfo: Basics.CultureInfoEntity;
  wrongTranslation: string;
  rightTranslation: string;
}

export module TranslationReplacementOperation {
  export const Save : Entities.ExecuteSymbol<TranslationReplacementEntity> = registerSymbol("Operation", "TranslationReplacementOperation.Save");
  export const Delete : Entities.DeleteSymbol<TranslationReplacementEntity> = registerSymbol("Operation", "TranslationReplacementOperation.Delete");
}



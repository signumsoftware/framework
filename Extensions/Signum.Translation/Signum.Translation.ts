//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'


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
  export const AutoSync = new MessageKey("TranslationMessage", "AutoSync");
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
  export const BackToSyncAssembly0 = new MessageKey("TranslationMessage", "BackToSyncAssembly0");
  export const ThisFieldIsTranslatable = new MessageKey("TranslationMessage", "ThisFieldIsTranslatable");
  export const _0OutdatedTranslationsFor1HaveBeenDeleted = new MessageKey("TranslationMessage", "_0OutdatedTranslationsFor1HaveBeenDeleted");
  export const DownloadView = new MessageKey("TranslationMessage", "DownloadView");
  export const DownloadSync = new MessageKey("TranslationMessage", "DownloadSync");
  export const Download = new MessageKey("TranslationMessage", "Download");
  export const AreYouSureToContinueAutoTranslation0For1WithoutRevision = new MessageKey("TranslationMessage", "AreYouSureToContinueAutoTranslation0For1WithoutRevision");
  export const AreYouSureToContinueAutoTranslationAllTypesFor0WithoutRevision = new MessageKey("TranslationMessage", "AreYouSureToContinueAutoTranslationAllTypesFor0WithoutRevision");
  export const AreYouSureToContinueAutoTranslationAllAssembliesFor0WithoutRevision = new MessageKey("TranslationMessage", "AreYouSureToContinueAutoTranslationAllAssembliesFor0WithoutRevision");
}

export module TranslationPermission {
  export const TranslateCode : Basics.PermissionSymbol = registerSymbol("Permission", "TranslationPermission.TranslateCode");
  export const TranslateInstances : Basics.PermissionSymbol = registerSymbol("Permission", "TranslationPermission.TranslateInstances");
}

export const TranslationReplacementEntity = new Type<TranslationReplacementEntity>("TranslationReplacement");
export interface TranslationReplacementEntity extends Entities.Entity {
  Type: "TranslationReplacement";
  cultureInfo: Basics.CultureInfoEntity;
  wrongTranslation: string;
  rightTranslation: string;
}

export module TranslationReplacementOperation {
  export const Save : Operations.ExecuteSymbol<TranslationReplacementEntity> = registerSymbol("Operation", "TranslationReplacementOperation.Save");
  export const Delete : Operations.DeleteSymbol<TranslationReplacementEntity> = registerSymbol("Operation", "TranslationReplacementOperation.Delete");
}


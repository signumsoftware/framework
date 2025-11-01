//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'


export namespace TranslationJavascriptMessage {
  export const WrongTranslationToSubstitute: MessageKey = new MessageKey("TranslationJavascriptMessage", "WrongTranslationToSubstitute");
  export const RightTranslation: MessageKey = new MessageKey("TranslationJavascriptMessage", "RightTranslation");
  export const RememberChange: MessageKey = new MessageKey("TranslationJavascriptMessage", "RememberChange");
}

export namespace TranslationMessage {
  export const RepeatedCultures0: MessageKey = new MessageKey("TranslationMessage", "RepeatedCultures0");
  export const CodeTranslations: MessageKey = new MessageKey("TranslationMessage", "CodeTranslations");
  export const InstanceTranslations: MessageKey = new MessageKey("TranslationMessage", "InstanceTranslations");
  export const Synchronize0In1: MessageKey = new MessageKey("TranslationMessage", "Synchronize0In1");
  export const View0In1: MessageKey = new MessageKey("TranslationMessage", "View0In1");
  export const AllLanguages: MessageKey = new MessageKey("TranslationMessage", "AllLanguages");
  export const _0AlreadySynchronized: MessageKey = new MessageKey("TranslationMessage", "_0AlreadySynchronized");
  export const NothingToTranslate: MessageKey = new MessageKey("TranslationMessage", "NothingToTranslate");
  export const All: MessageKey = new MessageKey("TranslationMessage", "All");
  export const NothingToTranslateIn0: MessageKey = new MessageKey("TranslationMessage", "NothingToTranslateIn0");
  export const Sync: MessageKey = new MessageKey("TranslationMessage", "Sync");
  export const View: MessageKey = new MessageKey("TranslationMessage", "View");
  export const None: MessageKey = new MessageKey("TranslationMessage", "None");
  export const Edit: MessageKey = new MessageKey("TranslationMessage", "Edit");
  export const AutoSync: MessageKey = new MessageKey("TranslationMessage", "AutoSync");
  export const Member: MessageKey = new MessageKey("TranslationMessage", "Member");
  export const Type: MessageKey = new MessageKey("TranslationMessage", "Type");
  export const Instance: MessageKey = new MessageKey("TranslationMessage", "Instance");
  export const Property: MessageKey = new MessageKey("TranslationMessage", "Property");
  export const Save: MessageKey = new MessageKey("TranslationMessage", "Save");
  export const Search: MessageKey = new MessageKey("TranslationMessage", "Search");
  export const PressSearchForResults: MessageKey = new MessageKey("TranslationMessage", "PressSearchForResults");
  export const NoResultsFound: MessageKey = new MessageKey("TranslationMessage", "NoResultsFound");
  export const Namespace: MessageKey = new MessageKey("TranslationMessage", "Namespace");
  export const NewTypes: MessageKey = new MessageKey("TranslationMessage", "NewTypes");
  export const NewTranslations: MessageKey = new MessageKey("TranslationMessage", "NewTranslations");
  export const BackToTranslationStatus: MessageKey = new MessageKey("TranslationMessage", "BackToTranslationStatus");
  export const BackToSyncAssembly0: MessageKey = new MessageKey("TranslationMessage", "BackToSyncAssembly0");
  export const ThisFieldIsTranslatable: MessageKey = new MessageKey("TranslationMessage", "ThisFieldIsTranslatable");
  export const _0OutdatedTranslationsFor1HaveBeenDeleted: MessageKey = new MessageKey("TranslationMessage", "_0OutdatedTranslationsFor1HaveBeenDeleted");
  export const DownloadView: MessageKey = new MessageKey("TranslationMessage", "DownloadView");
  export const DownloadSync: MessageKey = new MessageKey("TranslationMessage", "DownloadSync");
  export const Download: MessageKey = new MessageKey("TranslationMessage", "Download");
  export const AreYouSureToContinueAutoTranslation0For1WithoutRevision: MessageKey = new MessageKey("TranslationMessage", "AreYouSureToContinueAutoTranslation0For1WithoutRevision");
  export const AreYouSureToContinueAutoTranslationAllTypesFor0WithoutRevision: MessageKey = new MessageKey("TranslationMessage", "AreYouSureToContinueAutoTranslationAllTypesFor0WithoutRevision");
  export const AreYouSureToContinueAutoTranslationAllAssembliesFor0WithoutRevision: MessageKey = new MessageKey("TranslationMessage", "AreYouSureToContinueAutoTranslationAllAssembliesFor0WithoutRevision");
  export const TranslationStatus: MessageKey = new MessageKey("TranslationMessage", "TranslationStatus");
  export const Singular: MessageKey = new MessageKey("TranslationMessage", "Singular");
  export const Plural: MessageKey = new MessageKey("TranslationMessage", "Plural");
  export const PluralDescription: MessageKey = new MessageKey("TranslationMessage", "PluralDescription");
  export const Description: MessageKey = new MessageKey("TranslationMessage", "Description");
  export const Gender: MessageKey = new MessageKey("TranslationMessage", "Gender");
  export const Culture: MessageKey = new MessageKey("TranslationMessage", "Culture");
  export const TranslationsOverview: MessageKey = new MessageKey("TranslationMessage", "TranslationsOverview");
  export const InstanceRouteConflictsOverview: MessageKey = new MessageKey("TranslationMessage", "InstanceRouteConflictsOverview");
  export const TranslationFor0_: MessageKey = new MessageKey("TranslationMessage", "TranslationFor0_");
  export const OnlyNeutralCultures: MessageKey = new MessageKey("TranslationMessage", "OnlyNeutralCultures");
  export const From0using1_: MessageKey = new MessageKey("TranslationMessage", "From0using1_");
  export const SelectAxlsxFileWithTheTranslations: MessageKey = new MessageKey("TranslationMessage", "SelectAxlsxFileWithTheTranslations");
  export const NoRoutesMarkedForTranslationConsiderUsing: MessageKey = new MessageKey("TranslationMessage", "NoRoutesMarkedForTranslationConsiderUsing");
}

export namespace TranslationPermission {
  export const TranslateCode : Basics.PermissionSymbol = registerSymbol("Permission", "TranslationPermission.TranslateCode");
  export const TranslateInstances : Basics.PermissionSymbol = registerSymbol("Permission", "TranslationPermission.TranslateInstances");
}

export const TranslationReplacementEntity: Type<TranslationReplacementEntity> = new Type<TranslationReplacementEntity>("TranslationReplacement");
export interface TranslationReplacementEntity extends Entities.Entity {
  Type: "TranslationReplacement";
  cultureInfo: Basics.CultureInfoEntity;
  wrongTranslation: string;
  rightTranslation: string;
}

export namespace TranslationReplacementOperation {
  export const Save : Operations.ExecuteSymbol<TranslationReplacementEntity> = registerSymbol("Operation", "TranslationReplacementOperation.Save");
  export const Delete : Operations.DeleteSymbol<TranslationReplacementEntity> = registerSymbol("Operation", "TranslationReplacementOperation.Delete");
}


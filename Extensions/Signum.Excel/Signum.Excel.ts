//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as Templates from '../Signum.Mailing/Signum.Mailing.Templates'
import * as UserQueries from '../Signum.UserQueries/Signum.UserQueries'
import * as Files from '../Signum.Files/Signum.Files'


export const CollectionElementEmbedded: Type<CollectionElementEmbedded> = new Type<CollectionElementEmbedded>("CollectionElementEmbedded");
export interface CollectionElementEmbedded extends Entities.EmbeddedEntity {
  Type: "CollectionElementEmbedded";
  collectionElement: string;
  matchByColumn: string | null;
}

export const ExcelAttachmentEntity: Type<ExcelAttachmentEntity> = new Type<ExcelAttachmentEntity>("ExcelAttachment");
export interface ExcelAttachmentEntity extends Entities.Entity, Templates.IAttachmentGeneratorEntity {
  Type: "ExcelAttachment";
  fileName: string;
  title: string | null;
  userQuery: Entities.Lite<UserQueries.UserQueryEntity>;
  related: Entities.Lite<Entities.Entity> | null;
}

export namespace ExcelMessage {
  export const Data: MessageKey = new MessageKey("ExcelMessage", "Data");
  export const Download: MessageKey = new MessageKey("ExcelMessage", "Download");
  export const Excel2007Spreadsheet: MessageKey = new MessageKey("ExcelMessage", "Excel2007Spreadsheet");
  export const Administer: MessageKey = new MessageKey("ExcelMessage", "Administer");
  export const ExcelReport: MessageKey = new MessageKey("ExcelMessage", "ExcelReport");
  export const ExcelTemplateMustHaveExtensionXLSXandCurrentOneHas0: MessageKey = new MessageKey("ExcelMessage", "ExcelTemplateMustHaveExtensionXLSXandCurrentOneHas0");
  export const FindLocationFoExcelReport: MessageKey = new MessageKey("ExcelMessage", "FindLocationFoExcelReport");
  export const Reports: MessageKey = new MessageKey("ExcelMessage", "Reports");
  export const TheExcelTemplateHasAColumn0NotPresentInTheFindWindow: MessageKey = new MessageKey("ExcelMessage", "TheExcelTemplateHasAColumn0NotPresentInTheFindWindow");
  export const ThereAreNoResultsToWrite: MessageKey = new MessageKey("ExcelMessage", "ThereAreNoResultsToWrite");
  export const CreateNew: MessageKey = new MessageKey("ExcelMessage", "CreateNew");
}

export namespace ExcelPermission {
  export const PlainExcel : Basics.PermissionSymbol = registerSymbol("Permission", "ExcelPermission.PlainExcel");
  export const ImportFromExcel : Basics.PermissionSymbol = registerSymbol("Permission", "ExcelPermission.ImportFromExcel");
}

export const ExcelReportEntity: Type<ExcelReportEntity> = new Type<ExcelReportEntity>("ExcelReport");
export interface ExcelReportEntity extends Entities.Entity {
  Type: "ExcelReport";
  query: Basics.QueryEntity;
  displayName: string;
  file: Files.FileEmbedded;
}

export namespace ExcelReportOperation {
  export const Save : Operations.ExecuteSymbol<ExcelReportEntity> = registerSymbol("Operation", "ExcelReportOperation.Save");
  export const Delete : Operations.DeleteSymbol<ExcelReportEntity> = registerSymbol("Operation", "ExcelReportOperation.Delete");
}

export const ImportExcelMode: EnumType<ImportExcelMode> = new EnumType<ImportExcelMode>("ImportExcelMode");
export type ImportExcelMode =
  "Insert" |
  "Update" |
  "InsertOrUpdate";

export const ImportExcelModel: Type<ImportExcelModel> = new Type<ImportExcelModel>("ImportExcelModel");
export interface ImportExcelModel extends Entities.ModelEntity {
  Type: "ImportExcelModel";
  typeName: string;
  excelFile: Files.FileEmbedded;
  operationKey: string;
  transactional: boolean;
  identityInsert: boolean;
  mode: ImportExcelMode;
  matchByColumn: string | null;
  collections: Entities.MList<CollectionElementEmbedded>;
}

export namespace ImportFromExcelMessage {
  export const ImportFromExcel: MessageKey = new MessageKey("ImportFromExcelMessage", "ImportFromExcel");
  export const _0Errors: MessageKey = new MessageKey("ImportFromExcelMessage", "_0Errors");
  export const Importing0: MessageKey = new MessageKey("ImportFromExcelMessage", "Importing0");
  export const Import0FromExcel: MessageKey = new MessageKey("ImportFromExcelMessage", "Import0FromExcel");
  export const DownloadTemplate: MessageKey = new MessageKey("ImportFromExcelMessage", "DownloadTemplate");
  export const Columns0AlreadyHaveConstanValuesFromFilters: MessageKey = new MessageKey("ImportFromExcelMessage", "Columns0AlreadyHaveConstanValuesFromFilters");
  export const ThisQueryHasMultipleImplementations0: MessageKey = new MessageKey("ImportFromExcelMessage", "ThisQueryHasMultipleImplementations0");
  export const SomeColumnsAreIncompatibleWithImportingFromExcel: MessageKey = new MessageKey("ImportFromExcelMessage", "SomeColumnsAreIncompatibleWithImportingFromExcel");
  export const Operation0IsNotSupported: MessageKey = new MessageKey("ImportFromExcelMessage", "Operation0IsNotSupported");
  export const ManyFiltersTryToAssignTheSameProperty0WithDifferentValues1: MessageKey = new MessageKey("ImportFromExcelMessage", "ManyFiltersTryToAssignTheSameProperty0WithDifferentValues1");
  export const _0IsNotSupported: MessageKey = new MessageKey("ImportFromExcelMessage", "_0IsNotSupported");
  export const _01CanNotBeAssignedDirectylEachNestedFieldShouldBeAssignedIndependently: MessageKey = new MessageKey("ImportFromExcelMessage", "_01CanNotBeAssignedDirectylEachNestedFieldShouldBeAssignedIndependently");
  export const _01CanAlsoBeUsed: MessageKey = new MessageKey("ImportFromExcelMessage", "_01CanAlsoBeUsed");
  export const _0IsReadOnly: MessageKey = new MessageKey("ImportFromExcelMessage", "_0IsReadOnly");
  export const _01IsIncompatible: MessageKey = new MessageKey("ImportFromExcelMessage", "_01IsIncompatible");
  export const ErrorsIn0Rows_N: MessageKey = new MessageKey("ImportFromExcelMessage", "ErrorsIn0Rows_N");
  export const No0FoundInThisQueryWith1EqualsTo2: MessageKey = new MessageKey("ImportFromExcelMessage", "No0FoundInThisQueryWith1EqualsTo2");
  export const UnableToAssignMoreThanOneUnrelatedCollections0: MessageKey = new MessageKey("ImportFromExcelMessage", "UnableToAssignMoreThanOneUnrelatedCollections0");
  export const DuplicatedNonConsecutive0Found1: MessageKey = new MessageKey("ImportFromExcelMessage", "DuplicatedNonConsecutive0Found1");
  export const ColumnsDoNotMatchExcelColumns0QueryColumns1: MessageKey = new MessageKey("ImportFromExcelMessage", "ColumnsDoNotMatchExcelColumns0QueryColumns1");
}


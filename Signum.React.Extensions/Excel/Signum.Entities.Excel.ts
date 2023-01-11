//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum.React/Scripts/Reflection'
import * as Entities from '../../Signum.React/Scripts/Signum.Entities'
import * as Basics from '../../Signum.React/Scripts/Signum.Entities.Basics'
import * as Mailing from '../Mailing/Signum.Entities.Mailing'
import * as UserQueries from '../UserQueries/Signum.Entities.UserQueries'
import * as Files from '../Files/Signum.Entities.Files'
import * as Authorization from '../Authorization/Signum.Entities.Authorization'


export const CollectionElementEmbedded = new Type<CollectionElementEmbedded>("CollectionElementEmbedded");
export interface CollectionElementEmbedded extends Entities.EmbeddedEntity {
  Type: "CollectionElementEmbedded";
  collectionElement: string;
  matchByColumn: string | null;
}

export const ExcelAttachmentEntity = new Type<ExcelAttachmentEntity>("ExcelAttachment");
export interface ExcelAttachmentEntity extends Entities.Entity, Mailing.IAttachmentGeneratorEntity {
  Type: "ExcelAttachment";
  fileName: string;
  title: string | null;
  userQuery: Entities.Lite<UserQueries.UserQueryEntity>;
  related: Entities.Lite<Entities.Entity> | null;
}

export module ExcelMessage {
  export const Data = new MessageKey("ExcelMessage", "Data");
  export const Download = new MessageKey("ExcelMessage", "Download");
  export const Excel2007Spreadsheet = new MessageKey("ExcelMessage", "Excel2007Spreadsheet");
  export const Administer = new MessageKey("ExcelMessage", "Administer");
  export const ExcelReport = new MessageKey("ExcelMessage", "ExcelReport");
  export const ExcelTemplateMustHaveExtensionXLSXandCurrentOneHas0 = new MessageKey("ExcelMessage", "ExcelTemplateMustHaveExtensionXLSXandCurrentOneHas0");
  export const FindLocationFoExcelReport = new MessageKey("ExcelMessage", "FindLocationFoExcelReport");
  export const Reports = new MessageKey("ExcelMessage", "Reports");
  export const TheExcelTemplateHasAColumn0NotPresentInTheFindWindow = new MessageKey("ExcelMessage", "TheExcelTemplateHasAColumn0NotPresentInTheFindWindow");
  export const ThereAreNoResultsToWrite = new MessageKey("ExcelMessage", "ThereAreNoResultsToWrite");
  export const CreateNew = new MessageKey("ExcelMessage", "CreateNew");
}

export module ExcelPermission {
  export const PlainExcel : Authorization.PermissionSymbol = registerSymbol("Permission", "ExcelPermission.PlainExcel");
  export const ImportFromExcel : Authorization.PermissionSymbol = registerSymbol("Permission", "ExcelPermission.ImportFromExcel");
}

export const ExcelReportEntity = new Type<ExcelReportEntity>("ExcelReport");
export interface ExcelReportEntity extends Entities.Entity {
  Type: "ExcelReport";
  query: Basics.QueryEntity;
  displayName: string;
  file: Files.FileEmbedded;
}

export module ExcelReportOperation {
  export const Save : Entities.ExecuteSymbol<ExcelReportEntity> = registerSymbol("Operation", "ExcelReportOperation.Save");
  export const Delete : Entities.DeleteSymbol<ExcelReportEntity> = registerSymbol("Operation", "ExcelReportOperation.Delete");
}

export const ImportExcelMode = new EnumType<ImportExcelMode>("ImportExcelMode");
export type ImportExcelMode =
  "Insert" |
  "Update" |
  "InsertOrUpdate";

export const ImportExcelModel = new Type<ImportExcelModel>("ImportExcelModel");
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

export module ImportFromExcelMessage {
  export const ImportFromExcel = new MessageKey("ImportFromExcelMessage", "ImportFromExcel");
  export const _0Errors = new MessageKey("ImportFromExcelMessage", "_0Errors");
  export const Importing0 = new MessageKey("ImportFromExcelMessage", "Importing0");
  export const Import0FromExcel = new MessageKey("ImportFromExcelMessage", "Import0FromExcel");
  export const DownloadTemplate = new MessageKey("ImportFromExcelMessage", "DownloadTemplate");
  export const Columns0AlreadyHaveConstanValuesFromFilters = new MessageKey("ImportFromExcelMessage", "Columns0AlreadyHaveConstanValuesFromFilters");
  export const ThisQueryHasMultipleImplementations0 = new MessageKey("ImportFromExcelMessage", "ThisQueryHasMultipleImplementations0");
  export const SomeColumnsAreIncompatibleWithImportingFromExcel = new MessageKey("ImportFromExcelMessage", "SomeColumnsAreIncompatibleWithImportingFromExcel");
  export const Operation0IsNotSupported = new MessageKey("ImportFromExcelMessage", "Operation0IsNotSupported");
  export const ManyFiltersTryToAssignTheSameProperty0WithDifferentValues1 = new MessageKey("ImportFromExcelMessage", "ManyFiltersTryToAssignTheSameProperty0WithDifferentValues1");
  export const _0IsNotSupported = new MessageKey("ImportFromExcelMessage", "_0IsNotSupported");
  export const _01CanNotBeAssignedDirectylEachNestedFieldShouldBeAssignedIndependently = new MessageKey("ImportFromExcelMessage", "_01CanNotBeAssignedDirectylEachNestedFieldShouldBeAssignedIndependently");
  export const _01CanAlsoBeUsed = new MessageKey("ImportFromExcelMessage", "_01CanAlsoBeUsed");
  export const _0IsReadOnly = new MessageKey("ImportFromExcelMessage", "_0IsReadOnly");
  export const _01IsIncompatible = new MessageKey("ImportFromExcelMessage", "_01IsIncompatible");
  export const ErrorsIn0Rows_N = new MessageKey("ImportFromExcelMessage", "ErrorsIn0Rows_N");
  export const No0FoundInThisQueryWith1EqualsTo2 = new MessageKey("ImportFromExcelMessage", "No0FoundInThisQueryWith1EqualsTo2");
  export const UnableToAssignMoreThanOneUnrelatedCollections0 = new MessageKey("ImportFromExcelMessage", "UnableToAssignMoreThanOneUnrelatedCollections0");
  export const DuplicatedNonConsecutive0Found1 = new MessageKey("ImportFromExcelMessage", "DuplicatedNonConsecutive0Found1");
  export const ColumnsDoNotMatchExcelColumns0QueryColumns1 = new MessageKey("ImportFromExcelMessage", "ColumnsDoNotMatchExcelColumns0QueryColumns1");
}



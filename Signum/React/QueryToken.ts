import { getTypeInfo, IsByAll, isTypeEnum, isTypeModel, QueryTokenString, tryGetTypeInfos, TypeReference } from './Reflection';
import { FilterType } from './Signum.DynamicQuery';
import { QueryTokenDateMessage, QueryTokenMessage } from './Signum.DynamicQuery.Tokens';
import { Entity, Lite } from './Signum.Entities';
import { CollectionMessage } from './Signum.External';

export interface QueryToken {
    readonly toStr: string;
    readonly niceName: string;
    readonly key: string;
    readonly format?: string;
    readonly unit?: string;
    readonly type: TypeReference;
    readonly niceTypeName: string;
    readonly isGroupable: boolean;
    readonly queryTokenColor: string;
    readonly hasOrderAdapter?: boolean;
    readonly tsVectorFor?: string[];
    readonly preferEquals?: boolean;
    readonly filterType?: FilterType;
    readonly autoExpand?: boolean;
    readonly hideInAutoExpand?: boolean;
    readonly fullKey: string;
    readonly queryTokenType?: QueryTokenType;
    readonly parent?: QueryToken;
    readonly propertyRoute?: string;
    readonly __isCached__?: true;
}

export type QueryTokenType = "Aggregate" | "Element" | "AnyOrAll" | "OperationContainer" | "ToArray" | "Manual" | "Nested" | "Snippet" | "TimeSeries" | "IndexerContainer";

export enum SubTokensOptions {
  CanAggregate = 1,
  CanAnyAll = 2,
  CanElement = 4,
  CanOperation = 8,
  CanToArray = 16,
  CanSnippet = 32,
  CanManual = 64,
  CanTimeSeries = 128,
  CanNested = 256,
}

export type Writable<T> = {
  -readonly [P in keyof T]: T[P];
};

export function completeToken(token: QueryToken): QueryToken {
  
  var t = token as Writable<QueryToken>;

  if (t.fullKey == null)
    t.fullKey = t.parent == null ? t.key : t.parent.fullKey + "." + t.key;

  if (t.toStr == null)
    t.toStr = t.key;

  if (t.niceName == null)
    t.niceName = t.toStr;

  t.queryTokenColor = getQueryTokenColor(t);
  t.filterType = getFilterType(t.type);
  t.niceTypeName =
    t.type.isCollection ? QueryTokenMessage.ListOf0.niceToString(getNiceTypeName({ ...t.type, isCollection: false })) :
      getNiceTypeName(token.type, token.filterType);

  return t;
}

export function getFilterType(tr: TypeReference): FilterType | undefined {

  if (tr.isCollection)
    return undefined;

  if (tr.isLite)
    return "Lite";

  switch (tr.name) {
    case "boolean":
      return "Boolean";

    case "double":
    case "decimal":
    case "float":
      return "Decimal";

    case "byte":
    case "sbyte":
    case "short":
    case "int":
    case "long":
    case "ushort":
    case "uint":
    case "ulong":
      return "Integer";

    case "char":
    case "string":
      return "String";

    case "Guid":
      return "Guid";

    case "DateOnly":
    case "DateTime":
    case "DateTimeOffset":
      return "DateTime";

    case "TimeSpan":
    case "TimeOnly":
      return "Time";

    case "NpgsqlTsVector":
      return "TsVector";
  }

  if (isTypeEnum(tr.name))
    return "Enum";

  if (tr.isLite || tryGetTypeInfos(tr)[0]?.name)
    return "Lite";

  if (tr.isEmbedded)
    return isTypeModel(tr.name) ? "Model" : "Embedded";

  return undefined;
}

function getNiceTypeName(tr: TypeReference, filterType?: FilterType): string {

  filterType = filterType ?? getFilterType(tr);

  if (tr.name == "CellOperationDTO")
    return QueryTokenMessage.CellOperation.niceToString();

  if (tr.name == "OperationsContainerToken")
    return QueryTokenMessage.ContainerOfCellOperations.niceToString();

  if (tr.name == "IndexerContainerToken")
    return QueryTokenMessage.IndexerContainer.niceToString();

  switch (filterType) {
    case "Integer": return QueryTokenMessage.Number.niceToString();
    case "Decimal": return QueryTokenMessage.DecimalNumber.niceToString();
    case "String": return QueryTokenMessage.Text.niceToString();
    case "Time": return QueryTokenDateMessage.TimeOfDay.niceToString();
    case "DateTime":
      if (tr.name == "DateOnly")
        return QueryTokenDateMessage.Date.niceToString();

      return QueryTokenMessage.DateTime.niceToString();

    case "Boolean": return QueryTokenMessage.Check.niceToString();
    case "Guid": return QueryTokenMessage.GlobalUniqueIdentifier.niceToString();
    case "Enum": return getTypeInfo(tr.name).niceName!;

    case "Lite": {
      if (tr.name == IsByAll)
        return QueryTokenMessage.AnyEntity.niceToString();

      return tryGetTypeInfos(tr).map(a => a?.niceName).joinComma(CollectionMessage.Or.niceToString());
    }

    case "Embedded":
      return tr.typeNiceName!;

    default:
      return tr.name;
  }
}

function getQueryTokenColor(token: QueryToken): string {
  switch (token.queryTokenType) {
    case "Aggregate":
    case "AnyOrAll":
    case "Element":
    case "ToArray":
    case "Nested":
      return "var(--qt-keyword)" /*#0000FF*/;

    case "IndexerContainer":
    case "Manual":
    case "OperationContainer":
    case "Snippet":
    case "TimeSeries":
      return "var(--qt-exotic)"; /*#7D7D7D */
  }

  if (token.type.isCollection)
    return "var(--qt-collection)"; /*#CE6700*/


  if (token.parent == null && token.key == "Entity")
    return "var(--qt-main-entity)" /*#2B78AF*/;

  switch (token.filterType) {
    case "Integer":
    case "Decimal":
    case "String":
    case "Guid":
    case "Boolean":
      return "var(--qt-value)"; //"var(--bs-body-color)"

    case "DateTime":
      return "var(--qt-date)" /*#5100A1*/;
    case "Time":
      return "var(--qt-time)" /*#9956db*/;
    case "Enum":
      return "var(--qt-enum)" /*#800046*/;
    case "Lite":
      return "var(--qt-lite)" /* #2B91AF*/;
    case "Embedded":
      return "var(--qt-embedded)" /* #156F8A*/;
    default:
      return "var(--qt-exotic)" /*  #7D7D7D */;
  }
}


export interface ManualToken {
  toStr: string;
  niceName: string;
  key: string;
  typeColor?: string;
  niceTypeName: string;
  subToken?: Promise<ManualToken[]>;
}

export interface ManualCellDto {
  lite: Lite<Entity>;
  manualContainerTokenKey: string;
  manualTokenKey: string;
}

function getFullKey(token: QueryToken | QueryTokenString<any> | string): string {
  if (token instanceof QueryTokenString)
    return token.token;

  if (typeof token == "object")
    return token.fullKey;

  return token;
}

export function tokenStartsWith(token: QueryToken | QueryTokenString<any> | string, tokenStart: QueryToken | QueryTokenString<any> | string): boolean {

  token = getFullKey(token);
  tokenStart = getFullKey(token);

  return token == tokenStart || token.startsWith(tokenStart + ".");
}

export function getTokenParents(token: QueryToken | null | undefined): QueryToken[] {
  const result: QueryToken[] = [];
  while (token) {
    result.insertAt(0, token);
    token = token.parent;
  }
  return result;
}



export function hasAnyOrAll(token: QueryToken | undefined, recursive: boolean = true): boolean {
  if (token == undefined)
    return false;

  if (token.queryTokenType == "AnyOrAll")
    return true;

  return recursive && hasAnyOrAll(token.parent);
}

export function hasAny(token: QueryToken | undefined): boolean {
  if (token == undefined)
    return false;

  if (token.queryTokenType == "AnyOrAll" && token.key == "Any")
    return true;

  return hasAny(token.parent);
}

export function isPrefix(prefix: QueryToken, token: QueryToken): boolean {
  return prefix.fullKey == token.fullKey || token.fullKey.startsWith(prefix.fullKey + ".");
}

export function hasAggregate(token: QueryToken | undefined): boolean {
  if (token == undefined)
    return false;

  if (token.queryTokenType == "Aggregate")
    return true;

  return false;
}

export function hasElement(token: QueryToken | undefined): boolean {
  if (token == undefined)
    return false;

  if (token.queryTokenType == "Element")
    return true;

  return hasElement(token.parent);
}

export function hasOperation(token: QueryToken | undefined): boolean {
  if (token == undefined)
    return false;

  if (token.queryTokenType == "OperationContainer")
    return true;

  return false;
}

export function hasManual(token: QueryToken | undefined): boolean {
  if (token == undefined)
    return false;

  if (token.queryTokenType == "Manual")
    return true;

  return false;
}

export function hasNested(token: QueryToken | undefined): boolean {
  if (token == undefined)
    return false;

  if (token.queryTokenType == "Nested")
    return true;

  return hasNested(token.parent);
}

export function hasTimeSeries(token: QueryToken | undefined): boolean {
  if (token == undefined)
    return false;

  if (token.queryTokenType == "TimeSeries")
    return true;

  return hasTimeSeries(token.parent);
}

export function hasSnippet(token: QueryToken | undefined): boolean {
  if (token == undefined)
    return false;

  if (token.queryTokenType == "Snippet")
    return true;

  return false;
}

export function hasToArray(token: QueryToken | undefined): QueryToken | undefined {
  if (token == undefined)
    return undefined;

  if (token.queryTokenType == "ToArray")
    return token;

  return hasToArray(token.parent);
}

//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as Files from '../Signum.Files/Signum.Files'


export const AppendixHelpEntity: Type<AppendixHelpEntity> = new Type<AppendixHelpEntity>("AppendixHelp");
export interface AppendixHelpEntity extends Entities.Entity, IHelpImageTarget {
  Type: "AppendixHelp";
  uniqueName: string;
  culture: Basics.CultureInfoEntity;
  title: string;
  description: string | null;
}

export namespace AppendixHelpOperation {
  export const Save : Operations.ExecuteSymbol<AppendixHelpEntity> = registerSymbol("Operation", "AppendixHelpOperation.Save");
  export const Delete : Operations.DeleteSymbol<AppendixHelpEntity> = registerSymbol("Operation", "AppendixHelpOperation.Delete");
}

export const HelpImageEntity: Type<HelpImageEntity> = new Type<HelpImageEntity>("HelpImage");
export interface HelpImageEntity extends Entities.Entity {
  Type: "HelpImage";
  target: Entities.Lite<IHelpImageTarget>;
  creationDate: string /*DateTime*/;
  file: Files.FilePathEmbedded;
}

export namespace HelpImageFileType {
  export const Image : Files.FileTypeSymbol = registerSymbol("FileType", "HelpImageFileType.Image");
}

export namespace HelpKindMessage {
  export const HisMainFunctionIsTo0: MessageKey = new MessageKey("HelpKindMessage", "HisMainFunctionIsTo0");
  export const RelateOtherEntities: MessageKey = new MessageKey("HelpKindMessage", "RelateOtherEntities");
  export const ClassifyOtherEntities: MessageKey = new MessageKey("HelpKindMessage", "ClassifyOtherEntities");
  export const StoreInformationSharedByOtherEntities: MessageKey = new MessageKey("HelpKindMessage", "StoreInformationSharedByOtherEntities");
  export const StoreInformationOnItsOwn: MessageKey = new MessageKey("HelpKindMessage", "StoreInformationOnItsOwn");
  export const StorePartOfTheInformationOfAnotherEntity: MessageKey = new MessageKey("HelpKindMessage", "StorePartOfTheInformationOfAnotherEntity");
  export const StorePartsOfInformationSharedByDifferentEntities: MessageKey = new MessageKey("HelpKindMessage", "StorePartsOfInformationSharedByDifferentEntities");
  export const AutomaticallyByTheSystem: MessageKey = new MessageKey("HelpKindMessage", "AutomaticallyByTheSystem");
  export const AndIsMasterDataRarelyChanges: MessageKey = new MessageKey("HelpKindMessage", "AndIsMasterDataRarelyChanges");
  export const andIsTransactionalDataCreatedRegularly: MessageKey = new MessageKey("HelpKindMessage", "andIsTransactionalDataCreatedRegularly");
}

export namespace HelpMessage {
  export const _0IsA1_G: MessageKey = new MessageKey("HelpMessage", "_0IsA1_G");
  export const AnEmbeddedEntityOfType0: MessageKey = new MessageKey("HelpMessage", "AnEmbeddedEntityOfType0");
  export const AReference1ToA2_G: MessageKey = new MessageKey("HelpMessage", "AReference1ToA2_G");
  export const lite: MessageKey = new MessageKey("HelpMessage", "lite");
  export const full: MessageKey = new MessageKey("HelpMessage", "full");
  export const _0IsA1AndShows2: MessageKey = new MessageKey("HelpMessage", "_0IsA1AndShows2");
  export const _0IsACalculated1: MessageKey = new MessageKey("HelpMessage", "_0IsACalculated1");
  export const _0IsACollectionOfElements1: MessageKey = new MessageKey("HelpMessage", "_0IsACollectionOfElements1");
  export const Amount: MessageKey = new MessageKey("HelpMessage", "Amount");
  export const Any: MessageKey = new MessageKey("HelpMessage", "Any");
  export const Appendices: MessageKey = new MessageKey("HelpMessage", "Appendices");
  export const Buscador: MessageKey = new MessageKey("HelpMessage", "Buscador");
  export const Call0Over1OfThe2: MessageKey = new MessageKey("HelpMessage", "Call0Over1OfThe2");
  export const Character: MessageKey = new MessageKey("HelpMessage", "Character");
  export const BooleanValue: MessageKey = new MessageKey("HelpMessage", "BooleanValue");
  export const ConstructsANew0: MessageKey = new MessageKey("HelpMessage", "ConstructsANew0");
  export const Date: MessageKey = new MessageKey("HelpMessage", "Date");
  export const DateTime: MessageKey = new MessageKey("HelpMessage", "DateTime");
  export const ExpressedIn: MessageKey = new MessageKey("HelpMessage", "ExpressedIn");
  export const From0OfThe1: MessageKey = new MessageKey("HelpMessage", "From0OfThe1");
  export const FromMany0: MessageKey = new MessageKey("HelpMessage", "FromMany0");
  export const Help: MessageKey = new MessageKey("HelpMessage", "Help");
  export const HelpNotLoaded: MessageKey = new MessageKey("HelpMessage", "HelpNotLoaded");
  export const Integer: MessageKey = new MessageKey("HelpMessage", "Integer");
  export const Key0NotFound: MessageKey = new MessageKey("HelpMessage", "Key0NotFound");
  export const Optional: MessageKey = new MessageKey("HelpMessage", "Optional");
  export const Property0NotExistsInType1: MessageKey = new MessageKey("HelpMessage", "Property0NotExistsInType1");
  export const QueryOf0: MessageKey = new MessageKey("HelpMessage", "QueryOf0");
  export const RemovesThe0FromTheDatabase: MessageKey = new MessageKey("HelpMessage", "RemovesThe0FromTheDatabase");
  export const Should: MessageKey = new MessageKey("HelpMessage", "Should");
  export const String: MessageKey = new MessageKey("HelpMessage", "String");
  export const TheDatabaseVersion: MessageKey = new MessageKey("HelpMessage", "TheDatabaseVersion");
  export const TheProperty0: MessageKey = new MessageKey("HelpMessage", "TheProperty0");
  export const Value: MessageKey = new MessageKey("HelpMessage", "Value");
  export const ValueLike0: MessageKey = new MessageKey("HelpMessage", "ValueLike0");
  export const YourVersion: MessageKey = new MessageKey("HelpMessage", "YourVersion");
  export const _0IsThePrimaryKeyOf1OfType2: MessageKey = new MessageKey("HelpMessage", "_0IsThePrimaryKeyOf1OfType2");
  export const In0: MessageKey = new MessageKey("HelpMessage", "In0");
  export const Entities: MessageKey = new MessageKey("HelpMessage", "Entities");
  export const SearchText: MessageKey = new MessageKey("HelpMessage", "SearchText");
  export const Previous: MessageKey = new MessageKey("HelpMessage", "Previous");
  export const Next: MessageKey = new MessageKey("HelpMessage", "Next");
  export const Edit: MessageKey = new MessageKey("HelpMessage", "Edit");
  export const Close: MessageKey = new MessageKey("HelpMessage", "Close");
  export const ViewMore: MessageKey = new MessageKey("HelpMessage", "ViewMore");
  export const JumpToViewMore: MessageKey = new MessageKey("HelpMessage", "JumpToViewMore");
}

export namespace HelpPermissions {
  export const ViewHelp : Basics.PermissionSymbol = registerSymbol("Permission", "HelpPermissions.ViewHelp");
  export const DownloadHelp : Basics.PermissionSymbol = registerSymbol("Permission", "HelpPermissions.DownloadHelp");
}

export namespace HelpSearchMessage {
  export const Search: MessageKey = new MessageKey("HelpSearchMessage", "Search");
  export const _0ResultsFor1In2: MessageKey = new MessageKey("HelpSearchMessage", "_0ResultsFor1In2");
  export const Results: MessageKey = new MessageKey("HelpSearchMessage", "Results");
}

export namespace HelpSyntaxMessage {
  export const BoldText: MessageKey = new MessageKey("HelpSyntaxMessage", "BoldText");
  export const ItalicText: MessageKey = new MessageKey("HelpSyntaxMessage", "ItalicText");
  export const UnderlineText: MessageKey = new MessageKey("HelpSyntaxMessage", "UnderlineText");
  export const StriketroughText: MessageKey = new MessageKey("HelpSyntaxMessage", "StriketroughText");
  export const LinkToEntity: MessageKey = new MessageKey("HelpSyntaxMessage", "LinkToEntity");
  export const LinkToProperty: MessageKey = new MessageKey("HelpSyntaxMessage", "LinkToProperty");
  export const LinkToQuery: MessageKey = new MessageKey("HelpSyntaxMessage", "LinkToQuery");
  export const LinkToOperation: MessageKey = new MessageKey("HelpSyntaxMessage", "LinkToOperation");
  export const LinkToNamespace: MessageKey = new MessageKey("HelpSyntaxMessage", "LinkToNamespace");
  export const ExernalLink: MessageKey = new MessageKey("HelpSyntaxMessage", "ExernalLink");
  export const LinksAllowAnExtraParameterForTheText: MessageKey = new MessageKey("HelpSyntaxMessage", "LinksAllowAnExtraParameterForTheText");
  export const Example: MessageKey = new MessageKey("HelpSyntaxMessage", "Example");
  export const UnorderedListItem: MessageKey = new MessageKey("HelpSyntaxMessage", "UnorderedListItem");
  export const OtherItem: MessageKey = new MessageKey("HelpSyntaxMessage", "OtherItem");
  export const OrderedListItem: MessageKey = new MessageKey("HelpSyntaxMessage", "OrderedListItem");
  export const TitleLevel: MessageKey = new MessageKey("HelpSyntaxMessage", "TitleLevel");
  export const Title: MessageKey = new MessageKey("HelpSyntaxMessage", "Title");
  export const Images: MessageKey = new MessageKey("HelpSyntaxMessage", "Images");
  export const Texts: MessageKey = new MessageKey("HelpSyntaxMessage", "Texts");
  export const Links: MessageKey = new MessageKey("HelpSyntaxMessage", "Links");
  export const Lists: MessageKey = new MessageKey("HelpSyntaxMessage", "Lists");
  export const InsertImage: MessageKey = new MessageKey("HelpSyntaxMessage", "InsertImage");
  export const Options: MessageKey = new MessageKey("HelpSyntaxMessage", "Options");
  export const Edit: MessageKey = new MessageKey("HelpSyntaxMessage", "Edit");
  export const Save: MessageKey = new MessageKey("HelpSyntaxMessage", "Save");
  export const Syntax: MessageKey = new MessageKey("HelpSyntaxMessage", "Syntax");
  export const TranslateFrom: MessageKey = new MessageKey("HelpSyntaxMessage", "TranslateFrom");
}

export interface IHelpImageTarget extends Entities.Entity {
}

export const NamespaceHelpEntity: Type<NamespaceHelpEntity> = new Type<NamespaceHelpEntity>("NamespaceHelp");
export interface NamespaceHelpEntity extends Entities.Entity, IHelpImageTarget {
  Type: "NamespaceHelp";
  name: string;
  culture: Basics.CultureInfoEntity;
  title: string | null;
  description: string | null;
}

export namespace NamespaceHelpOperation {
  export const Save : Operations.ExecuteSymbol<NamespaceHelpEntity> = registerSymbol("Operation", "NamespaceHelpOperation.Save");
  export const Delete : Operations.DeleteSymbol<NamespaceHelpEntity> = registerSymbol("Operation", "NamespaceHelpOperation.Delete");
}

export const OperationHelpEmbedded: Type<OperationHelpEmbedded> = new Type<OperationHelpEmbedded>("OperationHelpEmbedded");
export interface OperationHelpEmbedded extends Entities.EmbeddedEntity {
  Type: "OperationHelpEmbedded";
  operation: Operations.OperationSymbol;
  info: string | null;
  description: string | null;
}

export const PropertyRouteHelpEmbedded: Type<PropertyRouteHelpEmbedded> = new Type<PropertyRouteHelpEmbedded>("PropertyRouteHelpEmbedded");
export interface PropertyRouteHelpEmbedded extends Entities.EmbeddedEntity {
  Type: "PropertyRouteHelpEmbedded";
  property: Basics.PropertyRouteEntity;
  info: string | null;
  description: string | null;
}

export const QueryColumnHelpEmbedded: Type<QueryColumnHelpEmbedded> = new Type<QueryColumnHelpEmbedded>("QueryColumnHelpEmbedded");
export interface QueryColumnHelpEmbedded extends Entities.EmbeddedEntity {
  Type: "QueryColumnHelpEmbedded";
  columnName: string;
  description: string | null;
  niceName: string | null;
  info: string | null;
}

export const QueryHelpEntity: Type<QueryHelpEntity> = new Type<QueryHelpEntity>("QueryHelp");
export interface QueryHelpEntity extends Entities.Entity, IHelpImageTarget {
  Type: "QueryHelp";
  query: Basics.QueryEntity;
  culture: Basics.CultureInfoEntity;
  info: string | null;
  description: string | null;
  columns: Entities.MList<QueryColumnHelpEmbedded>;
  isEmpty: boolean;
}

export namespace QueryHelpOperation {
  export const Save : Operations.ExecuteSymbol<QueryHelpEntity> = registerSymbol("Operation", "QueryHelpOperation.Save");
  export const Delete : Operations.DeleteSymbol<QueryHelpEntity> = registerSymbol("Operation", "QueryHelpOperation.Delete");
}

export const TypeHelpEntity: Type<TypeHelpEntity> = new Type<TypeHelpEntity>("TypeHelp");
export interface TypeHelpEntity extends Entities.Entity, IHelpImageTarget {
  Type: "TypeHelp";
  type: Basics.TypeEntity;
  culture: Basics.CultureInfoEntity;
  description: string | null;
  properties: Entities.MList<PropertyRouteHelpEmbedded>;
  operations: Entities.MList<OperationHelpEmbedded>;
  queries: Entities.MList<QueryHelpEntity>;
  isEmpty: boolean;
  info: string | null;
}

export namespace TypeHelpOperation {
  export const Save : Operations.ExecuteSymbol<TypeHelpEntity> = registerSymbol("Operation", "TypeHelpOperation.Save");
  export const Delete : Operations.DeleteSymbol<TypeHelpEntity> = registerSymbol("Operation", "TypeHelpOperation.Delete");
}


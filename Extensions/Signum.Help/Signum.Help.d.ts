import { MessageKey, Type } from '../../Signum/React/Reflection';
import * as Entities from '../../Signum/React/Signum.Entities';
import * as Basics from '../../Signum/React/Signum.Basics';
import * as Operations from '../../Signum/React/Signum.Operations';
import * as Files from '../Signum.Files/Signum.Files';
export declare const AppendixHelpEntity: Type<AppendixHelpEntity>;
export interface AppendixHelpEntity extends Entities.Entity, IHelpImageTarget {
    Type: "AppendixHelp";
    uniqueName: string;
    culture: Basics.CultureInfoEntity;
    title: string;
    description: string | null;
}
export declare namespace AppendixHelpOperation {
    const Save: Operations.ExecuteSymbol<AppendixHelpEntity>;
    const Delete: Operations.DeleteSymbol<AppendixHelpEntity>;
}
export declare const HelpImageEntity: Type<HelpImageEntity>;
export interface HelpImageEntity extends Entities.Entity {
    Type: "HelpImage";
    target: Entities.Lite<IHelpImageTarget>;
    creationDate: string;
    file: Files.FilePathEmbedded;
}
export declare namespace HelpImageFileType {
    const Image: Files.FileTypeSymbol;
}
export declare namespace HelpKindMessage {
    const HisMainFunctionIsTo0: MessageKey;
    const RelateOtherEntities: MessageKey;
    const ClassifyOtherEntities: MessageKey;
    const StoreInformationSharedByOtherEntities: MessageKey;
    const StoreInformationOnItsOwn: MessageKey;
    const StorePartOfTheInformationOfAnotherEntity: MessageKey;
    const StorePartsOfInformationSharedByDifferentEntities: MessageKey;
    const AutomaticallyByTheSystem: MessageKey;
    const AndIsMasterDataRarelyChanges: MessageKey;
    const andIsTransactionalDataCreatedRegularly: MessageKey;
}
export declare namespace HelpMessage {
    const _0IsA1_G: MessageKey;
    const AnEmbeddedEntityOfType0: MessageKey;
    const AReference1ToA2_G: MessageKey;
    const lite: MessageKey;
    const full: MessageKey;
    const _0IsA1AndShows2: MessageKey;
    const _0IsACalculated1: MessageKey;
    const _0IsACollectionOfElements1: MessageKey;
    const Amount: MessageKey;
    const Any: MessageKey;
    const Appendices: MessageKey;
    const Buscador: MessageKey;
    const Call0Over1OfThe2: MessageKey;
    const Character: MessageKey;
    const BooleanValue: MessageKey;
    const ConstructsANew0: MessageKey;
    const Date: MessageKey;
    const DateTime: MessageKey;
    const ExpressedIn: MessageKey;
    const From0OfThe1: MessageKey;
    const FromMany0: MessageKey;
    const Help: MessageKey;
    const HelpNotLoaded: MessageKey;
    const Integer: MessageKey;
    const Key0NotFound: MessageKey;
    const Optional: MessageKey;
    const Property0NotExistsInType1: MessageKey;
    const QueryOf0: MessageKey;
    const RemovesThe0FromTheDatabase: MessageKey;
    const Should: MessageKey;
    const String: MessageKey;
    const TheDatabaseVersion: MessageKey;
    const TheProperty0: MessageKey;
    const Value: MessageKey;
    const ValueLike0: MessageKey;
    const YourVersion: MessageKey;
    const _0IsThePrimaryKeyOf1OfType2: MessageKey;
    const In0: MessageKey;
    const Entities: MessageKey;
    const SearchText: MessageKey;
    const Previous: MessageKey;
    const Next: MessageKey;
    const Edit: MessageKey;
    const Close: MessageKey;
    const ViewMore: MessageKey;
}
export declare namespace HelpPermissions {
    const ViewHelp: Basics.PermissionSymbol;
    const DownloadHelp: Basics.PermissionSymbol;
}
export declare namespace HelpSearchMessage {
    const Search: MessageKey;
    const _0ResultsFor1In2: MessageKey;
    const Results: MessageKey;
}
export declare namespace HelpSyntaxMessage {
    const BoldText: MessageKey;
    const ItalicText: MessageKey;
    const UnderlineText: MessageKey;
    const StriketroughText: MessageKey;
    const LinkToEntity: MessageKey;
    const LinkToProperty: MessageKey;
    const LinkToQuery: MessageKey;
    const LinkToOperation: MessageKey;
    const LinkToNamespace: MessageKey;
    const ExernalLink: MessageKey;
    const LinksAllowAnExtraParameterForTheText: MessageKey;
    const Example: MessageKey;
    const UnorderedListItem: MessageKey;
    const OtherItem: MessageKey;
    const OrderedListItem: MessageKey;
    const TitleLevel: MessageKey;
    const Title: MessageKey;
    const Images: MessageKey;
    const Texts: MessageKey;
    const Links: MessageKey;
    const Lists: MessageKey;
    const InsertImage: MessageKey;
    const Options: MessageKey;
    const Edit: MessageKey;
    const Save: MessageKey;
    const Syntax: MessageKey;
    const TranslateFrom: MessageKey;
}
export interface IHelpImageTarget extends Entities.Entity {
}
export declare const NamespaceHelpEntity: Type<NamespaceHelpEntity>;
export interface NamespaceHelpEntity extends Entities.Entity, IHelpImageTarget {
    Type: "NamespaceHelp";
    name: string;
    culture: Basics.CultureInfoEntity;
    title: string | null;
    description: string | null;
}
export declare namespace NamespaceHelpOperation {
    const Save: Operations.ExecuteSymbol<NamespaceHelpEntity>;
    const Delete: Operations.DeleteSymbol<NamespaceHelpEntity>;
}
export declare const OperationHelpEmbedded: Type<OperationHelpEmbedded>;
export interface OperationHelpEmbedded extends Entities.EmbeddedEntity {
    Type: "OperationHelpEmbedded";
    operation: Operations.OperationSymbol;
    info: string;
    description: string | null;
}
export declare const PropertyRouteHelpEmbedded: Type<PropertyRouteHelpEmbedded>;
export interface PropertyRouteHelpEmbedded extends Entities.EmbeddedEntity {
    Type: "PropertyRouteHelpEmbedded";
    property: Basics.PropertyRouteEntity;
    info: string | null;
    description: string | null;
}
export declare const QueryColumnHelpEmbedded: Type<QueryColumnHelpEmbedded>;
export interface QueryColumnHelpEmbedded extends Entities.EmbeddedEntity {
    Type: "QueryColumnHelpEmbedded";
    columnName: string;
    description: string | null;
    niceName: string;
    info: string;
}
export declare const QueryHelpEntity: Type<QueryHelpEntity>;
export interface QueryHelpEntity extends Entities.Entity, IHelpImageTarget {
    Type: "QueryHelp";
    query: Basics.QueryEntity;
    culture: Basics.CultureInfoEntity;
    info: string;
    description: string | null;
    columns: Entities.MList<QueryColumnHelpEmbedded>;
    isEmpty: boolean;
}
export declare namespace QueryHelpOperation {
    const Save: Operations.ExecuteSymbol<QueryHelpEntity>;
    const Delete: Operations.DeleteSymbol<QueryHelpEntity>;
}
export declare const TypeHelpEntity: Type<TypeHelpEntity>;
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
export declare namespace TypeHelpOperation {
    const Save: Operations.ExecuteSymbol<TypeHelpEntity>;
    const Delete: Operations.DeleteSymbol<TypeHelpEntity>;
}

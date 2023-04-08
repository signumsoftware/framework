import { MessageKey, Type, EnumType } from '../../Signum/React/Reflection';
import * as Entities from '../../Signum/React/Signum.Entities';
import * as Basics from '../../Signum/React/Signum.Basics';
export declare const EntityAction: EnumType<EntityAction>;
export type EntityAction = "Identical" | "Different" | "New";
export interface IUserAssetEntity extends Entities.Entity {
    guid: string;
}
export declare const LiteConflictEmbedded: Type<LiteConflictEmbedded>;
export interface LiteConflictEmbedded extends Entities.EmbeddedEntity {
    Type: "LiteConflictEmbedded";
    propertyRoute: string;
    from: Entities.Lite<Entities.Entity>;
    to: Entities.Lite<Entities.Entity> | null;
}
export declare module UserAssetMessage {
    const ExportToXml: MessageKey;
    const ImportUserAssets: MessageKey;
    const ImportPreview: MessageKey;
    const SelectTheXmlFileWithTheUserAssetsThatYouWantToImport: MessageKey;
    const SelectTheEntitiesToOverride: MessageKey;
    const SucessfullyImported: MessageKey;
    const SwitchToValue: MessageKey;
    const SwitchToExpression: MessageKey;
    const LooksLikeSomeEntitiesIn0DoNotExistsOrHaveADifferentMeaningInThisDatabase: MessageKey;
    const SameSelectionForAllConflictsOf0: MessageKey;
    const _0IsNotFilterable: MessageKey;
    const TheFilterOperation0isNotCompatibleWith1: MessageKey;
}
export declare module UserAssetPermission {
    const UserAssetsToXML: Basics.PermissionSymbol;
}
export declare const UserAssetPreviewLineEmbedded: Type<UserAssetPreviewLineEmbedded>;
export interface UserAssetPreviewLineEmbedded extends Entities.EmbeddedEntity {
    Type: "UserAssetPreviewLineEmbedded";
    type: Basics.TypeEntity | null;
    text: string;
    action: EntityAction;
    overrideEntity: boolean;
    guid: string;
    customResolution: Entities.ModelEntity | null;
    liteConflicts: Entities.MList<LiteConflictEmbedded>;
}
export declare const UserAssetPreviewModel: Type<UserAssetPreviewModel>;
export interface UserAssetPreviewModel extends Entities.ModelEntity {
    Type: "UserAssetPreviewModel";
    lines: Entities.MList<UserAssetPreviewLineEmbedded>;
}
//# sourceMappingURL=Signum.Entities.UserAssets.d.ts.map
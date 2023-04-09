import { MessageKey, Type, EnumType } from '../../../Signum/React/Reflection';
import * as Entities from '../../../Signum/React/Signum.Entities';
import * as Basics from '../../../Signum/React/Signum.Basics';
import * as DynamicQuery from '../../../Signum/React/Signum.DynamicQuery';
import * as Operations from '../../../Signum/React/Signum.Operations';
import * as Authorization from '../Signum.Authorization';
export interface AllowedRule<R, A> extends Entities.ModelEntity {
    allowedBase: A;
    allowed: A;
    resource: R;
}
export interface AllowedRuleCoerced<R, A> extends AllowedRule<R, A> {
    coercedValues: A[];
}
export declare module AuthAdminMessage {
    const _0of1: MessageKey;
    const TypeRules: MessageKey;
    const PermissionRules: MessageKey;
    const Allow: MessageKey;
    const Deny: MessageKey;
    const Overriden: MessageKey;
    const Filter: MessageKey;
    const PleaseSaveChangesFirst: MessageKey;
    const ResetChanges: MessageKey;
    const SwitchTo: MessageKey;
    const OnlyActive: MessageKey;
    const _0InUI: MessageKey;
    const _0InDB: MessageKey;
    const CanNotBeModified: MessageKey;
    const CanNotBeModifiedBecauseIsInCondition0: MessageKey;
    const CanNotBeModifiedBecauseIsNotInCondition0: MessageKey;
    const CanNotBeReadBecauseIsInCondition0: MessageKey;
    const CanNotBeReadBecauseIsNotInCondition0: MessageKey;
    const _0RulesFor1: MessageKey;
    const TheUserStateMustBeDisabled: MessageKey;
    const _0CyclesHaveBeenFoundInTheGraphOfRolesDueToTheRelationships: MessageKey;
    const ConflictMergingTypeConditions: MessageKey;
    const Save: MessageKey;
    const DefaultAuthorization: MessageKey;
    const MaximumOfThe0: MessageKey;
    const MinumumOfThe0: MessageKey;
    const SameAs0: MessageKey;
    const Nothing: MessageKey;
    const Everything: MessageKey;
    const SelectTypeConditions: MessageKey;
    const ThereAre0TypeConditionsDefinedFor1: MessageKey;
    const SelectOneToOverrideTheAccessFor0ThatSatisfyThisCondition: MessageKey;
    const SelectMoreThanOneToOverrideAccessFor0ThatSatisfyAllTheConditionsAtTheSameTime: MessageKey;
    const RepeatedTypeCondition: MessageKey;
    const TheFollowingTypeConditionsHaveAlreadyBeenUsed: MessageKey;
    const Role0InheritsFromTrivialMergeRole1: MessageKey;
    const IncludeTrivialMerges: MessageKey;
    const Role0IsTrivialMerge: MessageKey;
    const Check: MessageKey;
    const Uncheck: MessageKey;
    const AddCondition: MessageKey;
    const RemoveCondition: MessageKey;
}
export declare const AuthThumbnail: EnumType<AuthThumbnail>;
export type AuthThumbnail = "All" | "Mix" | "None";
export interface BaseRulePack<T> extends Entities.ModelEntity {
    role: Entities.Lite<Authorization.RoleEntity>;
    strategy: string;
    rules: Entities.MList<T>;
}
export declare module BasicPermission {
    const AdminRules: Basics.PermissionSymbol;
    const AutomaticUpgradeOfProperties: Basics.PermissionSymbol;
    const AutomaticUpgradeOfQueries: Basics.PermissionSymbol;
    const AutomaticUpgradeOfOperations: Basics.PermissionSymbol;
}
export declare const OperationAllowed: EnumType<OperationAllowed>;
export type OperationAllowed = "None" | "DBOnly" | "Allow";
export declare const OperationAllowedRule: Type<OperationAllowedRule>;
export interface OperationAllowedRule extends AllowedRuleCoerced<OperationTypeEmbedded, OperationAllowed> {
    Type: "OperationAllowedRule";
}
export declare const OperationRulePack: Type<OperationRulePack>;
export interface OperationRulePack extends BaseRulePack<OperationAllowedRule> {
    Type: "OperationRulePack";
    type: Basics.TypeEntity;
}
export declare const OperationTypeEmbedded: Type<OperationTypeEmbedded>;
export interface OperationTypeEmbedded extends Entities.EmbeddedEntity {
    Type: "OperationTypeEmbedded";
    operation: Operations.OperationSymbol;
    type: Basics.TypeEntity;
}
export declare const PermissionAllowedRule: Type<PermissionAllowedRule>;
export interface PermissionAllowedRule extends AllowedRule<Basics.PermissionSymbol, boolean> {
    Type: "PermissionAllowedRule";
}
export declare const PermissionRulePack: Type<PermissionRulePack>;
export interface PermissionRulePack extends BaseRulePack<PermissionAllowedRule> {
    Type: "PermissionRulePack";
}
export declare const PropertyAllowed: EnumType<PropertyAllowed>;
export type PropertyAllowed = "None" | "Read" | "Write";
export declare const PropertyAllowedRule: Type<PropertyAllowedRule>;
export interface PropertyAllowedRule extends AllowedRuleCoerced<Basics.PropertyRouteEntity, PropertyAllowed> {
    Type: "PropertyAllowedRule";
}
export declare const PropertyRulePack: Type<PropertyRulePack>;
export interface PropertyRulePack extends BaseRulePack<PropertyAllowedRule> {
    Type: "PropertyRulePack";
    type: Basics.TypeEntity;
}
export declare const QueryAllowed: EnumType<QueryAllowed>;
export type QueryAllowed = "None" | "EmbeddedOnly" | "Allow";
export declare const QueryAllowedRule: Type<QueryAllowedRule>;
export interface QueryAllowedRule extends AllowedRuleCoerced<DynamicQuery.QueryEntity, QueryAllowed> {
    Type: "QueryAllowedRule";
}
export declare const QueryRulePack: Type<QueryRulePack>;
export interface QueryRulePack extends BaseRulePack<QueryAllowedRule> {
    Type: "QueryRulePack";
    type: Basics.TypeEntity;
}
export interface RuleEntity<R, A> extends Entities.Entity {
    role: Entities.Lite<Authorization.RoleEntity>;
    resource: R;
    allowed: A;
}
export declare const RuleOperationEntity: Type<RuleOperationEntity>;
export interface RuleOperationEntity extends RuleEntity<OperationTypeEmbedded, OperationAllowed> {
    Type: "RuleOperation";
}
export declare const RulePermissionEntity: Type<RulePermissionEntity>;
export interface RulePermissionEntity extends RuleEntity<Basics.PermissionSymbol, boolean> {
    Type: "RulePermission";
}
export declare const RulePropertyEntity: Type<RulePropertyEntity>;
export interface RulePropertyEntity extends RuleEntity<Basics.PropertyRouteEntity, PropertyAllowed> {
    Type: "RuleProperty";
}
export declare const RuleQueryEntity: Type<RuleQueryEntity>;
export interface RuleQueryEntity extends RuleEntity<DynamicQuery.QueryEntity, QueryAllowed> {
    Type: "RuleQuery";
}
export declare const RuleTypeConditionEntity: Type<RuleTypeConditionEntity>;
export interface RuleTypeConditionEntity extends Entities.Entity {
    Type: "RuleTypeCondition";
    ruleType: Entities.Lite<RuleTypeEntity>;
    conditions: Entities.MList<TypeConditionSymbol>;
    allowed: TypeAllowed;
    order: number;
}
export declare const RuleTypeEntity: Type<RuleTypeEntity>;
export interface RuleTypeEntity extends RuleEntity<Basics.TypeEntity, TypeAllowed> {
    Type: "RuleType";
    conditionRules: Entities.MList<RuleTypeConditionEntity>;
}
export declare const TypeAllowed: EnumType<TypeAllowed>;
export type TypeAllowed = "None" | "DBReadUINone" | "Read" | "DBWriteUINone" | "DBWriteUIRead" | "Write";
export declare const TypeAllowedAndConditions: Type<TypeAllowedAndConditions>;
export interface TypeAllowedAndConditions extends Entities.ModelEntity {
    Type: "TypeAllowedAndConditions";
    fallback: TypeAllowed;
    conditionRules: Entities.MList<TypeConditionRuleModel>;
}
export declare const TypeAllowedBasic: EnumType<TypeAllowedBasic>;
export type TypeAllowedBasic = "None" | "Read" | "Write";
export declare const TypeAllowedRule: Type<TypeAllowedRule>;
export interface TypeAllowedRule extends AllowedRule<Basics.TypeEntity, TypeAllowedAndConditions> {
    Type: "TypeAllowedRule";
    properties: AuthThumbnail | null;
    operations: AuthThumbnail | null;
    queries: AuthThumbnail | null;
    availableConditions: Array<TypeConditionSymbol>;
}
export declare const TypeConditionRuleModel: Type<TypeConditionRuleModel>;
export interface TypeConditionRuleModel extends Entities.ModelEntity {
    Type: "TypeConditionRuleModel";
    typeConditions: Entities.MList<TypeConditionSymbol>;
    allowed: TypeAllowed;
}
export declare const TypeConditionSymbol: Type<TypeConditionSymbol>;
export interface TypeConditionSymbol extends Basics.Symbol {
    Type: "TypeCondition";
}
export declare const TypeRulePack: Type<TypeRulePack>;
export interface TypeRulePack extends BaseRulePack<TypeAllowedRule> {
    Type: "TypeRulePack";
}
//# sourceMappingURL=Signum.Authorization.Rules.d.ts.map
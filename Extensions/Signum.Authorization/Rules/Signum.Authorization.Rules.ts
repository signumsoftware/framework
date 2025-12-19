//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Signum/React/Reflection'
import * as Entities from '../../../Signum/React/Signum.Entities'
import * as Basics from '../../../Signum/React/Signum.Basics'
import * as Operations from '../../../Signum/React/Signum.Operations'
import * as Authorization from '../Signum.Authorization'


export interface AllowedRule<R, A> extends Entities.ModelEntity {
  allowedBase: A;
  allowed: A;
  resource: R;
}

export interface AllowedRuleCoerced<R, A> extends AllowedRule<R, A> {
  coerced: A;
}

export namespace AuthAdminMessage {
  export const _0of1: MessageKey = new MessageKey("AuthAdminMessage", "_0of1");
  export const TypeRules: MessageKey = new MessageKey("AuthAdminMessage", "TypeRules");
  export const PermissionRules: MessageKey = new MessageKey("AuthAdminMessage", "PermissionRules");
  export const Allow: MessageKey = new MessageKey("AuthAdminMessage", "Allow");
  export const Deny: MessageKey = new MessageKey("AuthAdminMessage", "Deny");
  export const Overriden: MessageKey = new MessageKey("AuthAdminMessage", "Overriden");
  export const Filter: MessageKey = new MessageKey("AuthAdminMessage", "Filter");
  export const PleaseSaveChangesFirst: MessageKey = new MessageKey("AuthAdminMessage", "PleaseSaveChangesFirst");
  export const ResetChanges: MessageKey = new MessageKey("AuthAdminMessage", "ResetChanges");
  export const SwitchTo: MessageKey = new MessageKey("AuthAdminMessage", "SwitchTo");
  export const _0InUI: MessageKey = new MessageKey("AuthAdminMessage", "_0InUI");
  export const _0InDB: MessageKey = new MessageKey("AuthAdminMessage", "_0InDB");
  export const CanNotBeModified: MessageKey = new MessageKey("AuthAdminMessage", "CanNotBeModified");
  export const CanNotBeModifiedBecauseIsInCondition0: MessageKey = new MessageKey("AuthAdminMessage", "CanNotBeModifiedBecauseIsInCondition0");
  export const CanNotBeModifiedBecauseIsNotInCondition0: MessageKey = new MessageKey("AuthAdminMessage", "CanNotBeModifiedBecauseIsNotInCondition0");
  export const CanNotBeReadBecauseIsInCondition0: MessageKey = new MessageKey("AuthAdminMessage", "CanNotBeReadBecauseIsInCondition0");
  export const CanNotBeReadBecauseIsNotInCondition0: MessageKey = new MessageKey("AuthAdminMessage", "CanNotBeReadBecauseIsNotInCondition0");
  export const _0RulesFor1: MessageKey = new MessageKey("AuthAdminMessage", "_0RulesFor1");
  export const TheUserStateMustBeDisabled: MessageKey = new MessageKey("AuthAdminMessage", "TheUserStateMustBeDisabled");
  export const _0CyclesHaveBeenFoundInTheGraphOfRolesDueToTheRelationships: MessageKey = new MessageKey("AuthAdminMessage", "_0CyclesHaveBeenFoundInTheGraphOfRolesDueToTheRelationships");
  export const Save: MessageKey = new MessageKey("AuthAdminMessage", "Save");
  export const SelectTypeConditions: MessageKey = new MessageKey("AuthAdminMessage", "SelectTypeConditions");
  export const ThereAre0TypeConditionsDefinedFor1: MessageKey = new MessageKey("AuthAdminMessage", "ThereAre0TypeConditionsDefinedFor1");
  export const SelectOneToOverrideTheAccessFor0ThatSatisfyThisCondition: MessageKey = new MessageKey("AuthAdminMessage", "SelectOneToOverrideTheAccessFor0ThatSatisfyThisCondition");
  export const SelectMoreThanOneToOverrideAccessFor0ThatSatisfyAllTheConditionsAtTheSameTime: MessageKey = new MessageKey("AuthAdminMessage", "SelectMoreThanOneToOverrideAccessFor0ThatSatisfyAllTheConditionsAtTheSameTime");
  export const RepeatedTypeCondition: MessageKey = new MessageKey("AuthAdminMessage", "RepeatedTypeCondition");
  export const TheFollowingTypeConditionsHaveAlreadyBeenUsed: MessageKey = new MessageKey("AuthAdminMessage", "TheFollowingTypeConditionsHaveAlreadyBeenUsed");
  export const Role0InheritsFromTrivialMergeRole1: MessageKey = new MessageKey("AuthAdminMessage", "Role0InheritsFromTrivialMergeRole1");
  export const Role0IsTrivialMerge: MessageKey = new MessageKey("AuthAdminMessage", "Role0IsTrivialMerge");
  export const UsedByRoles: MessageKey = new MessageKey("AuthAdminMessage", "UsedByRoles");
  export const Check: MessageKey = new MessageKey("AuthAdminMessage", "Check");
  export const Uncheck: MessageKey = new MessageKey("AuthAdminMessage", "Uncheck");
  export const AddCondition: MessageKey = new MessageKey("AuthAdminMessage", "AddCondition");
  export const RemoveCondition: MessageKey = new MessageKey("AuthAdminMessage", "RemoveCondition");
  export const Fallback: MessageKey = new MessageKey("AuthAdminMessage", "Fallback");
  export const FirstRule: MessageKey = new MessageKey("AuthAdminMessage", "FirstRule");
  export const SecondRule: MessageKey = new MessageKey("AuthAdminMessage", "SecondRule");
  export const ThirdRule: MessageKey = new MessageKey("AuthAdminMessage", "ThirdRule");
  export const NthRule: MessageKey = new MessageKey("AuthAdminMessage", "NthRule");
  export const TypePermissionOverview: MessageKey = new MessageKey("AuthAdminMessage", "TypePermissionOverview");
  export const PropertyRuleOverview: MessageKey = new MessageKey("AuthAdminMessage", "PropertyRuleOverview");
  export const CopyFrom: MessageKey = new MessageKey("AuthAdminMessage", "CopyFrom");
  export const TypeConditions: MessageKey = new MessageKey("AuthAdminMessage", "TypeConditions");
  export const PermissionRulesOverview: MessageKey = new MessageKey("AuthAdminMessage", "PermissionRulesOverview");
  export const PermissionOverriden: MessageKey = new MessageKey("AuthAdminMessage", "PermissionOverriden");
  export const AuthRuleOverview: MessageKey = new MessageKey("AuthAdminMessage", "AuthRuleOverview");
  export const QueryPermissionsOverview: MessageKey = new MessageKey("AuthAdminMessage", "QueryPermissionsOverview");
  export const DownloadAuthRules: MessageKey = new MessageKey("AuthAdminMessage", "DownloadAuthRules");
}

export const AuthThumbnail: EnumType<AuthThumbnail> = new EnumType<AuthThumbnail>("AuthThumbnail");
export type AuthThumbnail =
  "All" |
  "Mix" |
  "None";

export interface BaseRulePack<T> extends Entities.ModelEntity {
  role: Entities.Lite<Authorization.RoleEntity>;
  strategy: string;
  rules: Entities.MList<T>;
}

export namespace BasicPermission {
  export const AdminRules : Basics.PermissionSymbol = registerSymbol("Permission", "BasicPermission.AdminRules");
  export const AutomaticUpgradeOfProperties : Basics.PermissionSymbol = registerSymbol("Permission", "BasicPermission.AutomaticUpgradeOfProperties");
  export const AutomaticUpgradeOfQueries : Basics.PermissionSymbol = registerSymbol("Permission", "BasicPermission.AutomaticUpgradeOfQueries");
  export const AutomaticUpgradeOfOperations : Basics.PermissionSymbol = registerSymbol("Permission", "BasicPermission.AutomaticUpgradeOfOperations");
}

export function ConditionRuleModel<A extends string>(a : EnumType<A>): Type<ConditionRuleModel<A>> {
    return new Type<ConditionRuleModel<A>>("ConditionRuleModel_" + a.typeName);
}
export interface ConditionRuleModel<A> extends Entities.ModelEntity {
  typeConditions: Entities.MList<TypeConditionSymbol>;
  allowed: A;
}

export const OperationAllowed: EnumType<OperationAllowed> = new EnumType<OperationAllowed>("OperationAllowed");
export type OperationAllowed =
  "None" |
  "DBOnly" |
  "Allow";

export const OperationAllowedRule: Type<OperationAllowedRule> = new Type<OperationAllowedRule>("OperationAllowedRule");
export interface OperationAllowedRule extends AllowedRuleCoerced<OperationTypeEmbedded, WithConditionsModel<OperationAllowed>> {
  Type: "OperationAllowedRule";
}

export const OperationRulePack: Type<OperationRulePack> = new Type<OperationRulePack>("OperationRulePack");
export interface OperationRulePack extends BaseRulePack<OperationAllowedRule> {
  Type: "OperationRulePack";
  type: Basics.TypeEntity;
  availableTypeConditions: Array<Array<TypeConditionSymbol>>;
}

export const OperationTypeEmbedded: Type<OperationTypeEmbedded> = new Type<OperationTypeEmbedded>("OperationTypeEmbedded");
export interface OperationTypeEmbedded extends Entities.EmbeddedEntity {
  Type: "OperationTypeEmbedded";
  operation: Operations.OperationSymbol;
  type: Basics.TypeEntity;
}

export const PermissionAllowedRule: Type<PermissionAllowedRule> = new Type<PermissionAllowedRule>("PermissionAllowedRule");
export interface PermissionAllowedRule extends AllowedRule<Basics.PermissionSymbol, boolean> {
  Type: "PermissionAllowedRule";
}

export const PermissionRulePack: Type<PermissionRulePack> = new Type<PermissionRulePack>("PermissionRulePack");
export interface PermissionRulePack extends BaseRulePack<PermissionAllowedRule> {
  Type: "PermissionRulePack";
}

export const PropertyAllowed: EnumType<PropertyAllowed> = new EnumType<PropertyAllowed>("PropertyAllowed");
export type PropertyAllowed =
  "None" |
  "Read" |
  "Write";

export const PropertyAllowedRule: Type<PropertyAllowedRule> = new Type<PropertyAllowedRule>("PropertyAllowedRule");
export interface PropertyAllowedRule extends AllowedRuleCoerced<Basics.PropertyRouteEntity, WithConditionsModel<PropertyAllowed>> {
  Type: "PropertyAllowedRule";
}

export const PropertyRulePack: Type<PropertyRulePack> = new Type<PropertyRulePack>("PropertyRulePack");
export interface PropertyRulePack extends BaseRulePack<PropertyAllowedRule> {
  Type: "PropertyRulePack";
  type: Basics.TypeEntity;
  availableTypeConditions: Array<Array<TypeConditionSymbol>>;
}

export const QueryAllowed: EnumType<QueryAllowed> = new EnumType<QueryAllowed>("QueryAllowed");
export type QueryAllowed =
  "None" |
  "EmbeddedOnly" |
  "Allow";

export const QueryAllowedRule: Type<QueryAllowedRule> = new Type<QueryAllowedRule>("QueryAllowedRule");
export interface QueryAllowedRule extends AllowedRuleCoerced<Basics.QueryEntity, QueryAllowed> {
  Type: "QueryAllowedRule";
}

export const QueryRulePack: Type<QueryRulePack> = new Type<QueryRulePack>("QueryRulePack");
export interface QueryRulePack extends BaseRulePack<QueryAllowedRule> {
  Type: "QueryRulePack";
  type: Basics.TypeEntity;
}

export interface RuleEntity<R> extends Entities.Entity {
  role: Entities.Lite<Authorization.RoleEntity>;
  resource: R;
}

export const RuleOperationConditionEntity: Type<RuleOperationConditionEntity> = new Type<RuleOperationConditionEntity>("RuleOperationCondition");
export interface RuleOperationConditionEntity extends Entities.Entity {
  Type: "RuleOperationCondition";
  ruleOperation: Entities.Lite<RuleOperationEntity>;
  conditions: Entities.MList<TypeConditionSymbol>;
  allowed: OperationAllowed;
  order: number;
}

export const RuleOperationEntity: Type<RuleOperationEntity> = new Type<RuleOperationEntity>("RuleOperation");
export interface RuleOperationEntity extends RuleEntity<OperationTypeEmbedded> {
  Type: "RuleOperation";
  fallback: OperationAllowed;
  conditionRules: Entities.MList<RuleOperationConditionEntity>;
}

export const RulePermissionEntity: Type<RulePermissionEntity> = new Type<RulePermissionEntity>("RulePermission");
export interface RulePermissionEntity extends RuleEntity<Basics.PermissionSymbol> {
  Type: "RulePermission";
  allowed: boolean;
}

export const RulePropertyConditionEntity: Type<RulePropertyConditionEntity> = new Type<RulePropertyConditionEntity>("RulePropertyCondition");
export interface RulePropertyConditionEntity extends Entities.Entity {
  Type: "RulePropertyCondition";
  ruleProperty: Entities.Lite<RulePropertyEntity>;
  conditions: Entities.MList<TypeConditionSymbol>;
  allowed: PropertyAllowed;
  order: number;
}

export const RulePropertyEntity: Type<RulePropertyEntity> = new Type<RulePropertyEntity>("RuleProperty");
export interface RulePropertyEntity extends RuleEntity<Basics.PropertyRouteEntity> {
  Type: "RuleProperty";
  fallback: PropertyAllowed;
  conditionRules: Entities.MList<RulePropertyConditionEntity>;
}

export const RuleQueryEntity: Type<RuleQueryEntity> = new Type<RuleQueryEntity>("RuleQuery");
export interface RuleQueryEntity extends RuleEntity<Basics.QueryEntity> {
  Type: "RuleQuery";
  allowed: QueryAllowed;
}

export const RuleTypeConditionEntity: Type<RuleTypeConditionEntity> = new Type<RuleTypeConditionEntity>("RuleTypeCondition");
export interface RuleTypeConditionEntity extends Entities.Entity {
  Type: "RuleTypeCondition";
  ruleType: Entities.Lite<RuleTypeEntity>;
  conditions: Entities.MList<TypeConditionSymbol>;
  allowed: TypeAllowed;
  order: number;
}

export const RuleTypeEntity: Type<RuleTypeEntity> = new Type<RuleTypeEntity>("RuleType");
export interface RuleTypeEntity extends RuleEntity<Basics.TypeEntity> {
  Type: "RuleType";
  fallback: TypeAllowed;
  conditionRules: Entities.MList<RuleTypeConditionEntity>;
}

export const TypeAllowed: EnumType<TypeAllowed> = new EnumType<TypeAllowed>("TypeAllowed");
export type TypeAllowed =
  "None" |
  "DBReadUINone" |
  "Read" |
  "DBWriteUINone" |
  "DBWriteUIRead" |
  "Write";

export const TypeAllowedBasic: EnumType<TypeAllowedBasic> = new EnumType<TypeAllowedBasic>("TypeAllowedBasic");
export type TypeAllowedBasic =
  "None" |
  "Read" |
  "Write";

export const TypeAllowedRule: Type<TypeAllowedRule> = new Type<TypeAllowedRule>("TypeAllowedRule");
export interface TypeAllowedRule extends AllowedRule<Basics.TypeEntity, WithConditionsModel<TypeAllowed>> {
  Type: "TypeAllowedRule";
  properties: WithConditionsModel<AuthThumbnail> | null;
  operations: WithConditionsModel<AuthThumbnail> | null;
  queries: AuthThumbnail | null;
  availableConditions: Array<TypeConditionSymbol>;
}

export const TypeConditionSymbol: Type<TypeConditionSymbol> = new Type<TypeConditionSymbol>("TypeCondition");
export interface TypeConditionSymbol extends Basics.Symbol {
  Type: "TypeCondition";
}

export const TypeRulePack: Type<TypeRulePack> = new Type<TypeRulePack>("TypeRulePack");
export interface TypeRulePack extends BaseRulePack<TypeAllowedRule> {
  Type: "TypeRulePack";
}

export function WithConditionsModel<A extends string>(a : EnumType<A>): Type<WithConditionsModel<A>> {
    return new Type<WithConditionsModel<A>>("WithConditionsModel_" + a.typeName);
}
export interface WithConditionsModel<A> extends Entities.ModelEntity {
  fallback: A;
  conditionRules: Entities.MList<ConditionRuleModel<A>>;
}


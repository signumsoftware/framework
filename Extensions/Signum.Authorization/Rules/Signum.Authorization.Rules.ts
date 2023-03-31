//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Signum/React/Reflection'
import * as Entities from '../../../Signum/React/Signum.Entities'
import * as Basics from '../../../Signum/React/Signum.Basics'
import * as DynamicQuery from '../../../Signum/React/Signum.DynamicQuery'
import * as Operations from '../../../Signum/React/Signum.Operations'
import * as Authorization from '../Signum.Authorization'


export interface AllowedRule<R, A> extends Entities.ModelEntity {
  allowedBase: A;
  allowed: A;
  resource: R;
}

export interface AllowedRuleCoerced<R, A> extends AllowedRule<R, A> {
  coercedValues: A[];
}

export module AuthAdminMessage {
  export const _0of1 = new MessageKey("AuthAdminMessage", "_0of1");
  export const TypeRules = new MessageKey("AuthAdminMessage", "TypeRules");
  export const PermissionRules = new MessageKey("AuthAdminMessage", "PermissionRules");
  export const Allow = new MessageKey("AuthAdminMessage", "Allow");
  export const Deny = new MessageKey("AuthAdminMessage", "Deny");
  export const Overriden = new MessageKey("AuthAdminMessage", "Overriden");
  export const Filter = new MessageKey("AuthAdminMessage", "Filter");
  export const PleaseSaveChangesFirst = new MessageKey("AuthAdminMessage", "PleaseSaveChangesFirst");
  export const ResetChanges = new MessageKey("AuthAdminMessage", "ResetChanges");
  export const SwitchTo = new MessageKey("AuthAdminMessage", "SwitchTo");
  export const OnlyActive = new MessageKey("AuthAdminMessage", "OnlyActive");
  export const _0InUI = new MessageKey("AuthAdminMessage", "_0InUI");
  export const _0InDB = new MessageKey("AuthAdminMessage", "_0InDB");
  export const CanNotBeModified = new MessageKey("AuthAdminMessage", "CanNotBeModified");
  export const CanNotBeModifiedBecauseIsInCondition0 = new MessageKey("AuthAdminMessage", "CanNotBeModifiedBecauseIsInCondition0");
  export const CanNotBeModifiedBecauseIsNotInCondition0 = new MessageKey("AuthAdminMessage", "CanNotBeModifiedBecauseIsNotInCondition0");
  export const CanNotBeReadBecauseIsInCondition0 = new MessageKey("AuthAdminMessage", "CanNotBeReadBecauseIsInCondition0");
  export const CanNotBeReadBecauseIsNotInCondition0 = new MessageKey("AuthAdminMessage", "CanNotBeReadBecauseIsNotInCondition0");
  export const _0RulesFor1 = new MessageKey("AuthAdminMessage", "_0RulesFor1");
  export const TheUserStateMustBeDisabled = new MessageKey("AuthAdminMessage", "TheUserStateMustBeDisabled");
  export const _0CyclesHaveBeenFoundInTheGraphOfRolesDueToTheRelationships = new MessageKey("AuthAdminMessage", "_0CyclesHaveBeenFoundInTheGraphOfRolesDueToTheRelationships");
  export const ConflictMergingTypeConditions = new MessageKey("AuthAdminMessage", "ConflictMergingTypeConditions");
  export const Save = new MessageKey("AuthAdminMessage", "Save");
  export const DefaultAuthorization = new MessageKey("AuthAdminMessage", "DefaultAuthorization");
  export const MaximumOfThe0 = new MessageKey("AuthAdminMessage", "MaximumOfThe0");
  export const MinumumOfThe0 = new MessageKey("AuthAdminMessage", "MinumumOfThe0");
  export const SameAs0 = new MessageKey("AuthAdminMessage", "SameAs0");
  export const Nothing = new MessageKey("AuthAdminMessage", "Nothing");
  export const Everything = new MessageKey("AuthAdminMessage", "Everything");
  export const SelectTypeConditions = new MessageKey("AuthAdminMessage", "SelectTypeConditions");
  export const ThereAre0TypeConditionsDefinedFor1 = new MessageKey("AuthAdminMessage", "ThereAre0TypeConditionsDefinedFor1");
  export const SelectOneToOverrideTheAccessFor0ThatSatisfyThisCondition = new MessageKey("AuthAdminMessage", "SelectOneToOverrideTheAccessFor0ThatSatisfyThisCondition");
  export const SelectMoreThanOneToOverrideAccessFor0ThatSatisfyAllTheConditionsAtTheSameTime = new MessageKey("AuthAdminMessage", "SelectMoreThanOneToOverrideAccessFor0ThatSatisfyAllTheConditionsAtTheSameTime");
  export const RepeatedTypeCondition = new MessageKey("AuthAdminMessage", "RepeatedTypeCondition");
  export const TheFollowingTypeConditionsHaveAlreadyBeenUsed = new MessageKey("AuthAdminMessage", "TheFollowingTypeConditionsHaveAlreadyBeenUsed");
  export const Role0InheritsFromTrivialMergeRole1 = new MessageKey("AuthAdminMessage", "Role0InheritsFromTrivialMergeRole1");
  export const IncludeTrivialMerges = new MessageKey("AuthAdminMessage", "IncludeTrivialMerges");
  export const Role0IsTrivialMerge = new MessageKey("AuthAdminMessage", "Role0IsTrivialMerge");
  export const Check = new MessageKey("AuthAdminMessage", "Check");
  export const Uncheck = new MessageKey("AuthAdminMessage", "Uncheck");
  export const AddCondition = new MessageKey("AuthAdminMessage", "AddCondition");
  export const RemoveCondition = new MessageKey("AuthAdminMessage", "RemoveCondition");
}

export const AuthThumbnail = new EnumType<AuthThumbnail>("AuthThumbnail");
export type AuthThumbnail =
  "All" |
  "Mix" |
  "None";

export interface BaseRulePack<T> extends Entities.ModelEntity {
  role: Entities.Lite<Authorization.RoleEntity>;
  strategy: string;
  rules: Entities.MList<T>;
}

export module BasicPermission {
  export const AdminRules : Basics.PermissionSymbol = registerSymbol("Permission", "BasicPermission.AdminRules");
  export const AutomaticUpgradeOfProperties : Basics.PermissionSymbol = registerSymbol("Permission", "BasicPermission.AutomaticUpgradeOfProperties");
  export const AutomaticUpgradeOfQueries : Basics.PermissionSymbol = registerSymbol("Permission", "BasicPermission.AutomaticUpgradeOfQueries");
  export const AutomaticUpgradeOfOperations : Basics.PermissionSymbol = registerSymbol("Permission", "BasicPermission.AutomaticUpgradeOfOperations");
}

export const OperationAllowed = new EnumType<OperationAllowed>("OperationAllowed");
export type OperationAllowed =
  "None" |
  "DBOnly" |
  "Allow";

export const OperationAllowedRule = new Type<OperationAllowedRule>("OperationAllowedRule");
export interface OperationAllowedRule extends AllowedRuleCoerced<OperationTypeEmbedded, OperationAllowed> {
  Type: "OperationAllowedRule";
}

export const OperationRulePack = new Type<OperationRulePack>("OperationRulePack");
export interface OperationRulePack extends BaseRulePack<OperationAllowedRule> {
  Type: "OperationRulePack";
  type: Basics.TypeEntity;
}

export const OperationTypeEmbedded = new Type<OperationTypeEmbedded>("OperationTypeEmbedded");
export interface OperationTypeEmbedded extends Entities.EmbeddedEntity {
  Type: "OperationTypeEmbedded";
  operation: Operations.OperationSymbol;
  type: Basics.TypeEntity;
}

export const PermissionAllowedRule = new Type<PermissionAllowedRule>("PermissionAllowedRule");
export interface PermissionAllowedRule extends AllowedRule<Basics.PermissionSymbol, boolean> {
  Type: "PermissionAllowedRule";
}

export const PermissionRulePack = new Type<PermissionRulePack>("PermissionRulePack");
export interface PermissionRulePack extends BaseRulePack<PermissionAllowedRule> {
  Type: "PermissionRulePack";
}

export const PropertyAllowed = new EnumType<PropertyAllowed>("PropertyAllowed");
export type PropertyAllowed =
  "None" |
  "Read" |
  "Write";

export const PropertyAllowedRule = new Type<PropertyAllowedRule>("PropertyAllowedRule");
export interface PropertyAllowedRule extends AllowedRuleCoerced<Basics.PropertyRouteEntity, PropertyAllowed> {
  Type: "PropertyAllowedRule";
}

export const PropertyRulePack = new Type<PropertyRulePack>("PropertyRulePack");
export interface PropertyRulePack extends BaseRulePack<PropertyAllowedRule> {
  Type: "PropertyRulePack";
  type: Basics.TypeEntity;
}

export const QueryAllowed = new EnumType<QueryAllowed>("QueryAllowed");
export type QueryAllowed =
  "None" |
  "EmbeddedOnly" |
  "Allow";

export const QueryAllowedRule = new Type<QueryAllowedRule>("QueryAllowedRule");
export interface QueryAllowedRule extends AllowedRuleCoerced<DynamicQuery.QueryEntity, QueryAllowed> {
  Type: "QueryAllowedRule";
}

export const QueryRulePack = new Type<QueryRulePack>("QueryRulePack");
export interface QueryRulePack extends BaseRulePack<QueryAllowedRule> {
  Type: "QueryRulePack";
  type: Basics.TypeEntity;
}

export interface RuleEntity<R, A> extends Entities.Entity {
  role: Entities.Lite<Authorization.RoleEntity>;
  resource: R;
  allowed: A;
}

export const RuleOperationEntity = new Type<RuleOperationEntity>("RuleOperation");
export interface RuleOperationEntity extends RuleEntity<OperationTypeEmbedded, OperationAllowed> {
  Type: "RuleOperation";
}

export const RulePermissionEntity = new Type<RulePermissionEntity>("RulePermission");
export interface RulePermissionEntity extends RuleEntity<Basics.PermissionSymbol, boolean> {
  Type: "RulePermission";
}

export const RulePropertyEntity = new Type<RulePropertyEntity>("RuleProperty");
export interface RulePropertyEntity extends RuleEntity<Basics.PropertyRouteEntity, PropertyAllowed> {
  Type: "RuleProperty";
}

export const RuleQueryEntity = new Type<RuleQueryEntity>("RuleQuery");
export interface RuleQueryEntity extends RuleEntity<DynamicQuery.QueryEntity, QueryAllowed> {
  Type: "RuleQuery";
}

export const RuleTypeConditionEntity = new Type<RuleTypeConditionEntity>("RuleTypeCondition");
export interface RuleTypeConditionEntity extends Entities.Entity {
  Type: "RuleTypeCondition";
  ruleType: Entities.Lite<RuleTypeEntity>;
  conditions: Entities.MList<TypeConditionSymbol>;
  allowed: TypeAllowed;
  order: number;
}

export const RuleTypeEntity = new Type<RuleTypeEntity>("RuleType");
export interface RuleTypeEntity extends RuleEntity<Basics.TypeEntity, TypeAllowed> {
  Type: "RuleType";
  conditionRules: Entities.MList<RuleTypeConditionEntity>;
}

export const TypeAllowed = new EnumType<TypeAllowed>("TypeAllowed");
export type TypeAllowed =
  "None" |
  "DBReadUINone" |
  "Read" |
  "DBWriteUINone" |
  "DBWriteUIRead" |
  "Write";

export const TypeAllowedAndConditions = new Type<TypeAllowedAndConditions>("TypeAllowedAndConditions");
export interface TypeAllowedAndConditions extends Entities.ModelEntity {
  Type: "TypeAllowedAndConditions";
  fallback: TypeAllowed;
  conditionRules: Entities.MList<TypeConditionRuleModel>;
}

export const TypeAllowedBasic = new EnumType<TypeAllowedBasic>("TypeAllowedBasic");
export type TypeAllowedBasic =
  "None" |
  "Read" |
  "Write";

export const TypeAllowedRule = new Type<TypeAllowedRule>("TypeAllowedRule");
export interface TypeAllowedRule extends AllowedRule<Basics.TypeEntity, TypeAllowedAndConditions> {
  Type: "TypeAllowedRule";
  properties: AuthThumbnail | null;
  operations: AuthThumbnail | null;
  queries: AuthThumbnail | null;
  availableConditions: Array<TypeConditionSymbol>;
}

export const TypeConditionRuleModel = new Type<TypeConditionRuleModel>("TypeConditionRuleModel");
export interface TypeConditionRuleModel extends Entities.ModelEntity {
  Type: "TypeConditionRuleModel";
  typeConditions: Entities.MList<TypeConditionSymbol>;
  allowed: TypeAllowed;
}

export const TypeConditionSymbol = new Type<TypeConditionSymbol>("TypeCondition");
export interface TypeConditionSymbol extends Basics.Symbol {
  Type: "TypeCondition";
}

export const TypeRulePack = new Type<TypeRulePack>("TypeRulePack");
export interface TypeRulePack extends BaseRulePack<TypeAllowedRule> {
  Type: "TypeRulePack";
}


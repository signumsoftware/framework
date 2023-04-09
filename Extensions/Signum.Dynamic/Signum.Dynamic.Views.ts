//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'


export const DynamicViewEntity = new Type<DynamicViewEntity>("DynamicView");
export interface DynamicViewEntity extends Entities.Entity {
  Type: "DynamicView";
  viewName: string;
  entityType: Basics.TypeEntity;
  props: Entities.MList<DynamicViewPropEmbedded>;
  locals: string | null;
  viewContent: string;
}

export module DynamicViewMessage {
  export const AddChild = new MessageKey("DynamicViewMessage", "AddChild");
  export const AddSibling = new MessageKey("DynamicViewMessage", "AddSibling");
  export const Remove = new MessageKey("DynamicViewMessage", "Remove");
  export const GenerateChildren = new MessageKey("DynamicViewMessage", "GenerateChildren");
  export const ClearChildren = new MessageKey("DynamicViewMessage", "ClearChildren");
  export const SelectATypeOfComponent = new MessageKey("DynamicViewMessage", "SelectATypeOfComponent");
  export const SelectANodeFirst = new MessageKey("DynamicViewMessage", "SelectANodeFirst");
  export const UseExpression = new MessageKey("DynamicViewMessage", "UseExpression");
  export const SuggestedFindOptions = new MessageKey("DynamicViewMessage", "SuggestedFindOptions");
  export const TheFollowingQueriesReference0 = new MessageKey("DynamicViewMessage", "TheFollowingQueriesReference0");
  export const ChooseAView = new MessageKey("DynamicViewMessage", "ChooseAView");
  export const SinceThereIsNoDynamicViewSelectorYouNeedToChooseAViewManually = new MessageKey("DynamicViewMessage", "SinceThereIsNoDynamicViewSelectorYouNeedToChooseAViewManually");
  export const ExampleEntity = new MessageKey("DynamicViewMessage", "ExampleEntity");
  export const ShowHelp = new MessageKey("DynamicViewMessage", "ShowHelp");
  export const HideHelp = new MessageKey("DynamicViewMessage", "HideHelp");
  export const ModulesHelp = new MessageKey("DynamicViewMessage", "ModulesHelp");
  export const PropsHelp = new MessageKey("DynamicViewMessage", "PropsHelp");
}

export module DynamicViewOperation {
  export const Create : Operations.ConstructSymbol_Simple<DynamicViewEntity> = registerSymbol("Operation", "DynamicViewOperation.Create");
  export const Clone : Operations.ConstructSymbol_From<DynamicViewEntity, DynamicViewEntity> = registerSymbol("Operation", "DynamicViewOperation.Clone");
  export const Save : Operations.ExecuteSymbol<DynamicViewEntity> = registerSymbol("Operation", "DynamicViewOperation.Save");
  export const Delete : Operations.DeleteSymbol<DynamicViewEntity> = registerSymbol("Operation", "DynamicViewOperation.Delete");
}

export const DynamicViewOverrideEntity = new Type<DynamicViewOverrideEntity>("DynamicViewOverride");
export interface DynamicViewOverrideEntity extends Entities.Entity {
  Type: "DynamicViewOverride";
  entityType: Basics.TypeEntity;
  viewName: string | null;
  script: string;
}

export module DynamicViewOverrideOperation {
  export const Save : Operations.ExecuteSymbol<DynamicViewOverrideEntity> = registerSymbol("Operation", "DynamicViewOverrideOperation.Save");
  export const Delete : Operations.DeleteSymbol<DynamicViewOverrideEntity> = registerSymbol("Operation", "DynamicViewOverrideOperation.Delete");
}

export const DynamicViewPropEmbedded = new Type<DynamicViewPropEmbedded>("DynamicViewPropEmbedded");
export interface DynamicViewPropEmbedded extends Entities.EmbeddedEntity {
  Type: "DynamicViewPropEmbedded";
  name: string;
  type: string;
}

export const DynamicViewSelectorEntity = new Type<DynamicViewSelectorEntity>("DynamicViewSelector");
export interface DynamicViewSelectorEntity extends Entities.Entity {
  Type: "DynamicViewSelector";
  entityType: Basics.TypeEntity;
  script: string;
}

export module DynamicViewSelectorOperation {
  export const Save : Operations.ExecuteSymbol<DynamicViewSelectorEntity> = registerSymbol("Operation", "DynamicViewSelectorOperation.Save");
  export const Delete : Operations.DeleteSymbol<DynamicViewSelectorEntity> = registerSymbol("Operation", "DynamicViewSelectorOperation.Delete");
}

export module DynamicViewValidationMessage {
  export const OnlyChildNodesOfType0Allowed = new MessageKey("DynamicViewValidationMessage", "OnlyChildNodesOfType0Allowed");
  export const Type0DoesNotContainsField1 = new MessageKey("DynamicViewValidationMessage", "Type0DoesNotContainsField1");
  export const Member0IsMandatoryFor1 = new MessageKey("DynamicViewValidationMessage", "Member0IsMandatoryFor1");
  export const _0RequiresA1 = new MessageKey("DynamicViewValidationMessage", "_0RequiresA1");
  export const Entity = new MessageKey("DynamicViewValidationMessage", "Entity");
  export const CollectionOfEntities = new MessageKey("DynamicViewValidationMessage", "CollectionOfEntities");
  export const Value = new MessageKey("DynamicViewValidationMessage", "Value");
  export const CollectionOfEnums = new MessageKey("DynamicViewValidationMessage", "CollectionOfEnums");
  export const EntityOrValue = new MessageKey("DynamicViewValidationMessage", "EntityOrValue");
  export const FilteringWithNew0ConsiderChangingVisibility = new MessageKey("DynamicViewValidationMessage", "FilteringWithNew0ConsiderChangingVisibility");
  export const AggregateIsMandatoryFor01 = new MessageKey("DynamicViewValidationMessage", "AggregateIsMandatoryFor01");
  export const ValueTokenCanNotBeUseFor0BecauseIsNotAnEntity = new MessageKey("DynamicViewValidationMessage", "ValueTokenCanNotBeUseFor0BecauseIsNotAnEntity");
  export const ViewNameIsNotAllowedWhileHavingChildren = new MessageKey("DynamicViewValidationMessage", "ViewNameIsNotAllowedWhileHavingChildren");
  export const _0ShouldStartByLowercase = new MessageKey("DynamicViewValidationMessage", "_0ShouldStartByLowercase");
  export const _0CanNotBe1 = new MessageKey("DynamicViewValidationMessage", "_0CanNotBe1");
}


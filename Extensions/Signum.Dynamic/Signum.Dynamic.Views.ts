//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'


export const DynamicViewEntity: Type<DynamicViewEntity> = new Type<DynamicViewEntity>("DynamicView");
export interface DynamicViewEntity extends Entities.Entity {
  Type: "DynamicView";
  viewName: string;
  entityType: Basics.TypeEntity;
  props: Entities.MList<DynamicViewPropEmbedded>;
  locals: string | null;
  viewContent: string;
}

export module DynamicViewMessage {
  export const AddChild: MessageKey = new MessageKey("DynamicViewMessage", "AddChild");
  export const AddSibling: MessageKey = new MessageKey("DynamicViewMessage", "AddSibling");
  export const Remove: MessageKey = new MessageKey("DynamicViewMessage", "Remove");
  export const GenerateChildren: MessageKey = new MessageKey("DynamicViewMessage", "GenerateChildren");
  export const ClearChildren: MessageKey = new MessageKey("DynamicViewMessage", "ClearChildren");
  export const SelectATypeOfComponent: MessageKey = new MessageKey("DynamicViewMessage", "SelectATypeOfComponent");
  export const SelectANodeFirst: MessageKey = new MessageKey("DynamicViewMessage", "SelectANodeFirst");
  export const UseExpression: MessageKey = new MessageKey("DynamicViewMessage", "UseExpression");
  export const SuggestedFindOptions: MessageKey = new MessageKey("DynamicViewMessage", "SuggestedFindOptions");
  export const TheFollowingQueriesReference0: MessageKey = new MessageKey("DynamicViewMessage", "TheFollowingQueriesReference0");
  export const ChooseAView: MessageKey = new MessageKey("DynamicViewMessage", "ChooseAView");
  export const SinceThereIsNoDynamicViewSelectorYouNeedToChooseAViewManually: MessageKey = new MessageKey("DynamicViewMessage", "SinceThereIsNoDynamicViewSelectorYouNeedToChooseAViewManually");
  export const ExampleEntity: MessageKey = new MessageKey("DynamicViewMessage", "ExampleEntity");
  export const ShowHelp: MessageKey = new MessageKey("DynamicViewMessage", "ShowHelp");
  export const HideHelp: MessageKey = new MessageKey("DynamicViewMessage", "HideHelp");
  export const ModulesHelp: MessageKey = new MessageKey("DynamicViewMessage", "ModulesHelp");
  export const PropsHelp: MessageKey = new MessageKey("DynamicViewMessage", "PropsHelp");
}

export module DynamicViewOperation {
  export const Create : Operations.ConstructSymbol_Simple<DynamicViewEntity> = registerSymbol("Operation", "DynamicViewOperation.Create");
  export const Clone : Operations.ConstructSymbol_From<DynamicViewEntity, DynamicViewEntity> = registerSymbol("Operation", "DynamicViewOperation.Clone");
  export const Save : Operations.ExecuteSymbol<DynamicViewEntity> = registerSymbol("Operation", "DynamicViewOperation.Save");
  export const Delete : Operations.DeleteSymbol<DynamicViewEntity> = registerSymbol("Operation", "DynamicViewOperation.Delete");
}

export const DynamicViewOverrideEntity: Type<DynamicViewOverrideEntity> = new Type<DynamicViewOverrideEntity>("DynamicViewOverride");
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

export const DynamicViewPropEmbedded: Type<DynamicViewPropEmbedded> = new Type<DynamicViewPropEmbedded>("DynamicViewPropEmbedded");
export interface DynamicViewPropEmbedded extends Entities.EmbeddedEntity {
  Type: "DynamicViewPropEmbedded";
  name: string;
  type: string;
}

export const DynamicViewSelectorEntity: Type<DynamicViewSelectorEntity> = new Type<DynamicViewSelectorEntity>("DynamicViewSelector");
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
  export const OnlyChildNodesOfType0Allowed: MessageKey = new MessageKey("DynamicViewValidationMessage", "OnlyChildNodesOfType0Allowed");
  export const Type0DoesNotContainsField1: MessageKey = new MessageKey("DynamicViewValidationMessage", "Type0DoesNotContainsField1");
  export const Member0IsMandatoryFor1: MessageKey = new MessageKey("DynamicViewValidationMessage", "Member0IsMandatoryFor1");
  export const _0RequiresA1: MessageKey = new MessageKey("DynamicViewValidationMessage", "_0RequiresA1");
  export const Entity: MessageKey = new MessageKey("DynamicViewValidationMessage", "Entity");
  export const CollectionOfEntities: MessageKey = new MessageKey("DynamicViewValidationMessage", "CollectionOfEntities");
  export const Value: MessageKey = new MessageKey("DynamicViewValidationMessage", "Value");
  export const CollectionOfEnums: MessageKey = new MessageKey("DynamicViewValidationMessage", "CollectionOfEnums");
  export const EntityOrValue: MessageKey = new MessageKey("DynamicViewValidationMessage", "EntityOrValue");
  export const FilteringWithNew0ConsiderChangingVisibility: MessageKey = new MessageKey("DynamicViewValidationMessage", "FilteringWithNew0ConsiderChangingVisibility");
  export const AggregateIsMandatoryFor01: MessageKey = new MessageKey("DynamicViewValidationMessage", "AggregateIsMandatoryFor01");
  export const ValueTokenCanNotBeUseFor0BecauseIsNotAnEntity: MessageKey = new MessageKey("DynamicViewValidationMessage", "ValueTokenCanNotBeUseFor0BecauseIsNotAnEntity");
  export const ViewNameIsNotAllowedWhileHavingChildren: MessageKey = new MessageKey("DynamicViewValidationMessage", "ViewNameIsNotAllowedWhileHavingChildren");
  export const _0ShouldStartByLowercase: MessageKey = new MessageKey("DynamicViewValidationMessage", "_0ShouldStartByLowercase");
  export const _0CanNotBe1: MessageKey = new MessageKey("DynamicViewValidationMessage", "_0CanNotBe1");
}


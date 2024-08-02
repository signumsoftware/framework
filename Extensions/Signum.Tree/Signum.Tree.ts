//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as Dashboard from '../Signum.Dashboard/Signum.Dashboard'
import * as UserQueries from '../Signum.UserQueries/Signum.UserQueries'


export const InsertPlace: EnumType<InsertPlace> = new EnumType<InsertPlace>("InsertPlace");
export type InsertPlace =
  "FirstNode" |
  "After" |
  "Before" |
  "LastNode";

export const MoveTreeModel: Type<MoveTreeModel> = new Type<MoveTreeModel>("MoveTreeModel");
export interface MoveTreeModel extends Entities.ModelEntity {
  Type: "MoveTreeModel";
  newParent: Entities.Lite<TreeEntity> | null;
  insertPlace: InsertPlace;
  sibling: Entities.Lite<TreeEntity> | null;
}

export interface TreeEntity extends Entities.Entity {
  parentRoute: string;
  level: number | null;
  parentOrSibling: Entities.Lite<TreeEntity> | null;
  isSibling: boolean;
  name: string;
  fullName: string;
}

export module TreeMessage {
  export const Tree: MessageKey = new MessageKey("TreeMessage", "Tree");
  export const Descendants: MessageKey = new MessageKey("TreeMessage", "Descendants");
  export const Parent: MessageKey = new MessageKey("TreeMessage", "Parent");
  export const Ascendants: MessageKey = new MessageKey("TreeMessage", "Ascendants");
  export const Children: MessageKey = new MessageKey("TreeMessage", "Children");
  export const Level: MessageKey = new MessageKey("TreeMessage", "Level");
  export const TreeInfo: MessageKey = new MessageKey("TreeMessage", "TreeInfo");
  export const TreeType: MessageKey = new MessageKey("TreeMessage", "TreeType");
  export const LevelShouldNotBeGreaterThan0: MessageKey = new MessageKey("TreeMessage", "LevelShouldNotBeGreaterThan0");
  export const ImpossibleToMove0InsideOf1: MessageKey = new MessageKey("TreeMessage", "ImpossibleToMove0InsideOf1");
  export const ImpossibleToMove01Of2: MessageKey = new MessageKey("TreeMessage", "ImpossibleToMove01Of2");
  export const Move0: MessageKey = new MessageKey("TreeMessage", "Move0");
  export const Copy0: MessageKey = new MessageKey("TreeMessage", "Copy0");
  export const ListView: MessageKey = new MessageKey("TreeMessage", "ListView");
}

export module TreeOperation {
  export const CreateRoot : Operations.ConstructSymbol_Simple<TreeEntity> = registerSymbol("Operation", "TreeOperation.CreateRoot");
  export const CreateChild : Operations.ConstructSymbol_From<TreeEntity, TreeEntity> = registerSymbol("Operation", "TreeOperation.CreateChild");
  export const CreateNextSibling : Operations.ConstructSymbol_From<TreeEntity, TreeEntity> = registerSymbol("Operation", "TreeOperation.CreateNextSibling");
  export const Save : Operations.ExecuteSymbol<TreeEntity> = registerSymbol("Operation", "TreeOperation.Save");
  export const Move : Operations.ExecuteSymbol<TreeEntity> = registerSymbol("Operation", "TreeOperation.Move");
  export const Copy : Operations.ConstructSymbol_From<TreeEntity, TreeEntity> = registerSymbol("Operation", "TreeOperation.Copy");
  export const Delete : Operations.DeleteSymbol<TreeEntity> = registerSymbol("Operation", "TreeOperation.Delete");
}

export module TreeViewerMessage {
  export const Search: MessageKey = new MessageKey("TreeViewerMessage", "Search");
  export const AddRoot: MessageKey = new MessageKey("TreeViewerMessage", "AddRoot");
  export const AddChild: MessageKey = new MessageKey("TreeViewerMessage", "AddChild");
  export const AddSibling: MessageKey = new MessageKey("TreeViewerMessage", "AddSibling");
  export const Remove: MessageKey = new MessageKey("TreeViewerMessage", "Remove");
  export const None: MessageKey = new MessageKey("TreeViewerMessage", "None");
  export const ExpandAll: MessageKey = new MessageKey("TreeViewerMessage", "ExpandAll");
  export const CollapseAll: MessageKey = new MessageKey("TreeViewerMessage", "CollapseAll");
}

export const UserTreePartEntity: Type<UserTreePartEntity> = new Type<UserTreePartEntity>("UserTreePart");
export interface UserTreePartEntity extends Entities.Entity, Dashboard.IPartEntity {
  Type: "UserTreePart";
  userQuery: UserQueries.UserQueryEntity;
  requiresTitle: boolean;
}


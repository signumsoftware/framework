//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'


export const InsertPlace = new EnumType<InsertPlace>("InsertPlace");
export type InsertPlace =
  "FirstNode" |
  "After" |
  "Before" |
  "LastNode";

export const MoveTreeModel = new Type<MoveTreeModel>("MoveTreeModel");
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
  export const Tree = new MessageKey("TreeMessage", "Tree");
  export const Descendants = new MessageKey("TreeMessage", "Descendants");
  export const Parent = new MessageKey("TreeMessage", "Parent");
  export const Ascendants = new MessageKey("TreeMessage", "Ascendants");
  export const Children = new MessageKey("TreeMessage", "Children");
  export const Level = new MessageKey("TreeMessage", "Level");
  export const TreeType = new MessageKey("TreeMessage", "TreeType");
  export const LevelShouldNotBeGreaterThan0 = new MessageKey("TreeMessage", "LevelShouldNotBeGreaterThan0");
  export const ImpossibleToMove0InsideOf1 = new MessageKey("TreeMessage", "ImpossibleToMove0InsideOf1");
  export const ImpossibleToMove01Of2 = new MessageKey("TreeMessage", "ImpossibleToMove01Of2");
  export const Move0 = new MessageKey("TreeMessage", "Move0");
  export const Copy0 = new MessageKey("TreeMessage", "Copy0");
}

export module TreeOperation {
  export const CreateRoot : Entities.ConstructSymbol_Simple<TreeEntity> = registerSymbol("Operation", "TreeOperation.CreateRoot");
  export const CreateChild : Entities.ConstructSymbol_From<TreeEntity, TreeEntity> = registerSymbol("Operation", "TreeOperation.CreateChild");
  export const CreateNextSibling : Entities.ConstructSymbol_From<TreeEntity, TreeEntity> = registerSymbol("Operation", "TreeOperation.CreateNextSibling");
  export const Save : Entities.ExecuteSymbol<TreeEntity> = registerSymbol("Operation", "TreeOperation.Save");
  export const Move : Entities.ExecuteSymbol<TreeEntity> = registerSymbol("Operation", "TreeOperation.Move");
  export const Copy : Entities.ConstructSymbol_From<TreeEntity, TreeEntity> = registerSymbol("Operation", "TreeOperation.Copy");
  export const Delete : Entities.DeleteSymbol<TreeEntity> = registerSymbol("Operation", "TreeOperation.Delete");
}

export module TreeViewerMessage {
  export const Search = new MessageKey("TreeViewerMessage", "Search");
  export const AddRoot = new MessageKey("TreeViewerMessage", "AddRoot");
  export const AddChild = new MessageKey("TreeViewerMessage", "AddChild");
  export const AddSibling = new MessageKey("TreeViewerMessage", "AddSibling");
  export const Remove = new MessageKey("TreeViewerMessage", "Remove");
  export const None = new MessageKey("TreeViewerMessage", "None");
}



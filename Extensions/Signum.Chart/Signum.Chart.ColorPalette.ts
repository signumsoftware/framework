//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'


export const ColorPaletteEntity = new Type<ColorPaletteEntity>("ColorPalette");
export interface ColorPaletteEntity extends Entities.Entity {
  Type: "ColorPalette";
  type: Basics.TypeEntity;
  categoryName: string;
  seed: number;
  specificColors: Entities.MList<SpecificColorEmbedded>;
}

export module ColorPaletteMessage {
  export const FillAutomatically = new MessageKey("ColorPaletteMessage", "FillAutomatically");
  export const Select0OnlyIfYouWantToOverrideTheAutomaticColor = new MessageKey("ColorPaletteMessage", "Select0OnlyIfYouWantToOverrideTheAutomaticColor");
  export const ShowPalette = new MessageKey("ColorPaletteMessage", "ShowPalette");
  export const ShowList = new MessageKey("ColorPaletteMessage", "ShowList");
}

export module ColorPaletteOperation {
  export const Save : Operations.ExecuteSymbol<ColorPaletteEntity> = registerSymbol("Operation", "ColorPaletteOperation.Save");
  export const Delete : Operations.DeleteSymbol<ColorPaletteEntity> = registerSymbol("Operation", "ColorPaletteOperation.Delete");
}

export const SpecificColorEmbedded = new Type<SpecificColorEmbedded>("SpecificColorEmbedded");
export interface SpecificColorEmbedded extends Entities.EmbeddedEntity {
  Type: "SpecificColorEmbedded";
  entity: Entities.Lite<Entities.Entity>;
  color: string;
}


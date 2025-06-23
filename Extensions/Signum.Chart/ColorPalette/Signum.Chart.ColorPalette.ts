//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Signum/React/Reflection'
import * as Entities from '../../../Signum/React/Signum.Entities'
import * as Basics from '../../../Signum/React/Signum.Basics'
import * as Operations from '../../../Signum/React/Signum.Operations'


export const ColorPaletteEntity: Type<ColorPaletteEntity> = new Type<ColorPaletteEntity>("ColorPalette");
export interface ColorPaletteEntity extends Entities.Entity {
  Type: "ColorPalette";
  type: Basics.TypeEntity;
  categoryName: string;
  seed: number;
  specificColors: Entities.MList<SpecificColorEmbedded>;
}

export namespace ColorPaletteMessage {
  export const FillAutomatically: MessageKey = new MessageKey("ColorPaletteMessage", "FillAutomatically");
  export const Select0OnlyIfYouWantToOverrideTheAutomaticColor: MessageKey = new MessageKey("ColorPaletteMessage", "Select0OnlyIfYouWantToOverrideTheAutomaticColor");
  export const ShowPalette: MessageKey = new MessageKey("ColorPaletteMessage", "ShowPalette");
  export const ShowList: MessageKey = new MessageKey("ColorPaletteMessage", "ShowList");
}

export namespace ColorPaletteOperation {
  export const Save : Operations.ExecuteSymbol<ColorPaletteEntity> = registerSymbol("Operation", "ColorPaletteOperation.Save");
  export const Delete : Operations.DeleteSymbol<ColorPaletteEntity> = registerSymbol("Operation", "ColorPaletteOperation.Delete");
}

export const SpecificColorEmbedded: Type<SpecificColorEmbedded> = new Type<SpecificColorEmbedded>("SpecificColorEmbedded");
export interface SpecificColorEmbedded extends Entities.EmbeddedEntity {
  Type: "SpecificColorEmbedded";
  entity: Entities.Lite<Entities.Entity>;
  color: string;
}


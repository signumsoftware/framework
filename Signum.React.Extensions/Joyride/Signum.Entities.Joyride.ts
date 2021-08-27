//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as UserAssets from '../UserAssets/Signum.Entities.UserAssets'
import * as Basics from '../Basics/Signum.Entities.Basics'


export const JoyrideEntity = new Type<JoyrideEntity>("Joyride");
export interface JoyrideEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
  Type: "Joyride";
  name: string;
  culture: Entities.Lite<Basics.CultureInfoEntity> | null;
  type: JoyrideType;
  steps: Entities.MList<JoyrideStepEntity>;
  showSkipButton: boolean;
  showStepsProgress: boolean;
  keyboardNavigation: boolean;
  debug: boolean;
  guid: string;
}

export module JoyrideMessage {
  export const Back = new MessageKey("JoyrideMessage", "Back");
  export const Close = new MessageKey("JoyrideMessage", "Close");
  export const Last = new MessageKey("JoyrideMessage", "Last");
  export const Next = new MessageKey("JoyrideMessage", "Next");
  export const Skip = new MessageKey("JoyrideMessage", "Skip");
}

export module JoyrideOperation {
  export const Save : Entities.ExecuteSymbol<JoyrideEntity> = registerSymbol("Operation", "JoyrideOperation.Save");
}

export const JoyrideStepEntity = new Type<JoyrideStepEntity>("JoyrideStep");
export interface JoyrideStepEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
  Type: "JoyrideStep";
  guid: string;
  culture: Entities.Lite<Basics.CultureInfoEntity> | null;
  title: string;
  text: string;
  style: JoyrideStepStyleEntity | null;
  selector: string;
  position: JoyrideStepPosition;
  type: JoyrideStepType;
  allowClicksThruHole: boolean;
  isFixed: boolean;
}

export module JoyrideStepOperation {
  export const Save : Entities.ExecuteSymbol<JoyrideStepEntity> = registerSymbol("Operation", "JoyrideStepOperation.Save");
}

export const JoyrideStepPosition = new EnumType<JoyrideStepPosition>("JoyrideStepPosition");
export type JoyrideStepPosition =
  "Top" |
  "TopLeft" |
  "TopRight" |
  "Bottom" |
  "BottomLeft" |
  "BottomRight";

export const JoyrideStepStyleEntity = new Type<JoyrideStepStyleEntity>("JoyrideStepStyle");
export interface JoyrideStepStyleEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
  Type: "JoyrideStepStyle";
  name: string;
  backgroundColor: string | null;
  color: string | null;
  mainColor: string | null;
  borderRadius: string | null;
  textAlign: string | null;
  width: string | null;
  guid: string;
}

export module JoyrideStepStyleOperation {
  export const Save : Entities.ExecuteSymbol<JoyrideStepStyleEntity> = registerSymbol("Operation", "JoyrideStepStyleOperation.Save");
}

export const JoyrideStepType = new EnumType<JoyrideStepType>("JoyrideStepType");
export type JoyrideStepType =
  "Click" |
  "Hover";

export const JoyrideType = new EnumType<JoyrideType>("JoyrideType");
export type JoyrideType =
  "Continuous" |
  "Single";



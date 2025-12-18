//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from './Reflection'
import * as Entities from './Signum.Entities'


export const ComparisonType: EnumType<ComparisonType> = new EnumType<ComparisonType>("ComparisonType");
export type ComparisonType =
  "EqualTo" |
  "DistinctTo" |
  "GreaterThan" |
  "GreaterThanOrEqualTo" |
  "LessThan" |
  "LessThanOrEqualTo";

export namespace ValidationMessage {
  export const _0DoesNotHaveAValid1Format: MessageKey = new MessageKey("ValidationMessage", "_0DoesNotHaveAValid1Format");
  export const _0DoesNotHaveAValid1IdentifierFormat: MessageKey = new MessageKey("ValidationMessage", "_0DoesNotHaveAValid1IdentifierFormat");
  export const _0HasAnInvalidFormat: MessageKey = new MessageKey("ValidationMessage", "_0HasAnInvalidFormat");
  export const _0HasMoreThan1DecimalPlaces: MessageKey = new MessageKey("ValidationMessage", "_0HasMoreThan1DecimalPlaces");
  export const _0HasSomeRepeatedElements1: MessageKey = new MessageKey("ValidationMessage", "_0HasSomeRepeatedElements1");
  export const _0ShouldBe12: MessageKey = new MessageKey("ValidationMessage", "_0ShouldBe12");
  export const _0ShouldBe1: MessageKey = new MessageKey("ValidationMessage", "_0ShouldBe1");
  export const _0ShouldBe1InsteadOf2: MessageKey = new MessageKey("ValidationMessage", "_0ShouldBe1InsteadOf2");
  export const _0HasToBeBetween1And2: MessageKey = new MessageKey("ValidationMessage", "_0HasToBeBetween1And2");
  export const _0HasToBeLowercase: MessageKey = new MessageKey("ValidationMessage", "_0HasToBeLowercase");
  export const _0HasToBeUppercase: MessageKey = new MessageKey("ValidationMessage", "_0HasToBeUppercase");
  export const _0IsNecessary: MessageKey = new MessageKey("ValidationMessage", "_0IsNecessary");
  export const _0IsNecessaryOnState1: MessageKey = new MessageKey("ValidationMessage", "_0IsNecessaryOnState1");
  export const _0IsNotAllowed: MessageKey = new MessageKey("ValidationMessage", "_0IsNotAllowed");
  export const _0IsNotAllowedOnState1: MessageKey = new MessageKey("ValidationMessage", "_0IsNotAllowedOnState1");
  export const _0IsNotSet: MessageKey = new MessageKey("ValidationMessage", "_0IsNotSet");
  export const _0IsNotSetIn1: MessageKey = new MessageKey("ValidationMessage", "_0IsNotSetIn1");
  export const _0AreNotSet: MessageKey = new MessageKey("ValidationMessage", "_0AreNotSet");
  export const _0IsSet: MessageKey = new MessageKey("ValidationMessage", "_0IsSet");
  export const _0IsNotA1_G: MessageKey = new MessageKey("ValidationMessage", "_0IsNotA1_G");
  export const BeA0_G: MessageKey = new MessageKey("ValidationMessage", "BeA0_G");
  export const Be0: MessageKey = new MessageKey("ValidationMessage", "Be0");
  export const BeBetween0And1: MessageKey = new MessageKey("ValidationMessage", "BeBetween0And1");
  export const BeNotNull: MessageKey = new MessageKey("ValidationMessage", "BeNotNull");
  export const FileName: MessageKey = new MessageKey("ValidationMessage", "FileName");
  export const Have0Decimals: MessageKey = new MessageKey("ValidationMessage", "Have0Decimals");
  export const HaveANumberOfElements01: MessageKey = new MessageKey("ValidationMessage", "HaveANumberOfElements01");
  export const HaveAPrecisionOf0: MessageKey = new MessageKey("ValidationMessage", "HaveAPrecisionOf0");
  export const HaveBetween0And1Characters: MessageKey = new MessageKey("ValidationMessage", "HaveBetween0And1Characters");
  export const HaveMaximum0Characters: MessageKey = new MessageKey("ValidationMessage", "HaveMaximum0Characters");
  export const HaveMinimum0Characters: MessageKey = new MessageKey("ValidationMessage", "HaveMinimum0Characters");
  export const HaveNoRepeatedElements: MessageKey = new MessageKey("ValidationMessage", "HaveNoRepeatedElements");
  export const HaveValid0Format: MessageKey = new MessageKey("ValidationMessage", "HaveValid0Format");
  export const InvalidDateFormat: MessageKey = new MessageKey("ValidationMessage", "InvalidDateFormat");
  export const InvalidFormat: MessageKey = new MessageKey("ValidationMessage", "InvalidFormat");
  export const NotPossibleToaAssign0: MessageKey = new MessageKey("ValidationMessage", "NotPossibleToaAssign0");
  export const Numeric: MessageKey = new MessageKey("ValidationMessage", "Numeric");
  export const OrBeNull: MessageKey = new MessageKey("ValidationMessage", "OrBeNull");
  export const Telephone: MessageKey = new MessageKey("ValidationMessage", "Telephone");
  export const _0ShouldHaveJustOneLine: MessageKey = new MessageKey("ValidationMessage", "_0ShouldHaveJustOneLine");
  export const _0ShouldNotHaveInitialSpaces: MessageKey = new MessageKey("ValidationMessage", "_0ShouldNotHaveInitialSpaces");
  export const _0ShouldNotHaveFinalSpaces: MessageKey = new MessageKey("ValidationMessage", "_0ShouldNotHaveFinalSpaces");
  export const TheLenghtOf0HasToBeEqualTo1: MessageKey = new MessageKey("ValidationMessage", "TheLenghtOf0HasToBeEqualTo1");
  export const TheLengthOf0HasToBeGreaterOrEqualTo1: MessageKey = new MessageKey("ValidationMessage", "TheLengthOf0HasToBeGreaterOrEqualTo1");
  export const TheLengthOf0HasToBeLesserOrEqualTo1: MessageKey = new MessageKey("ValidationMessage", "TheLengthOf0HasToBeLesserOrEqualTo1");
  export const TheNumberOf0IsBeingMultipliedBy1: MessageKey = new MessageKey("ValidationMessage", "TheNumberOf0IsBeingMultipliedBy1");
  export const TheRowsAreBeingGroupedBy0: MessageKey = new MessageKey("ValidationMessage", "TheRowsAreBeingGroupedBy0");
  export const EachRowRepresentsAGroupOf0WithSame1: MessageKey = new MessageKey("ValidationMessage", "EachRowRepresentsAGroupOf0WithSame1");
  export const TheNumberOfElementsOf0HasToBe12: MessageKey = new MessageKey("ValidationMessage", "TheNumberOfElementsOf0HasToBe12");
  export const Type0NotAllowed: MessageKey = new MessageKey("ValidationMessage", "Type0NotAllowed");
  export const _0IsMandatoryWhen1IsNotSet: MessageKey = new MessageKey("ValidationMessage", "_0IsMandatoryWhen1IsNotSet");
  export const _0IsMandatoryWhen1IsNotSetTo2: MessageKey = new MessageKey("ValidationMessage", "_0IsMandatoryWhen1IsNotSetTo2");
  export const _0IsMandatoryWhen1IsSet: MessageKey = new MessageKey("ValidationMessage", "_0IsMandatoryWhen1IsSet");
  export const _0IsMandatoryWhen1IsSetTo2: MessageKey = new MessageKey("ValidationMessage", "_0IsMandatoryWhen1IsSetTo2");
  export const _0ShouldBeNullWhen1IsNotSet: MessageKey = new MessageKey("ValidationMessage", "_0ShouldBeNullWhen1IsNotSet");
  export const _0ShouldBeNullWhen1IsNotSetTo2: MessageKey = new MessageKey("ValidationMessage", "_0ShouldBeNullWhen1IsNotSetTo2");
  export const _0ShouldBeNullWhen1IsSet: MessageKey = new MessageKey("ValidationMessage", "_0ShouldBeNullWhen1IsSet");
  export const _0ShouldBeNullWhen1IsSetTo2: MessageKey = new MessageKey("ValidationMessage", "_0ShouldBeNullWhen1IsSetTo2");
  export const _0ShouldBeNull: MessageKey = new MessageKey("ValidationMessage", "_0ShouldBeNull");
  export const _0ShouldBe1When2Is3: MessageKey = new MessageKey("ValidationMessage", "_0ShouldBe1When2Is3");
  export const _0ShouldBeADateInThePast: MessageKey = new MessageKey("ValidationMessage", "_0ShouldBeADateInThePast");
  export const _0ShouldBeADateInTheFuture: MessageKey = new MessageKey("ValidationMessage", "_0ShouldBeADateInTheFuture");
  export const BeInThePast: MessageKey = new MessageKey("ValidationMessage", "BeInThePast");
  export const _0ShouldBeGreaterThan1: MessageKey = new MessageKey("ValidationMessage", "_0ShouldBeGreaterThan1");
  export const _0ShouldBeGreaterThanOrEqual1: MessageKey = new MessageKey("ValidationMessage", "_0ShouldBeGreaterThanOrEqual1");
  export const _0ShouldBeLessThan1: MessageKey = new MessageKey("ValidationMessage", "_0ShouldBeLessThan1");
  export const _0ShouldBeLessThanOrEqual1: MessageKey = new MessageKey("ValidationMessage", "_0ShouldBeLessThanOrEqual1");
  export const _0HasAPrecisionOf1InsteadOf2: MessageKey = new MessageKey("ValidationMessage", "_0HasAPrecisionOf1InsteadOf2");
  export const _0ShouldBeOfType1: MessageKey = new MessageKey("ValidationMessage", "_0ShouldBeOfType1");
  export const _0ShouldNotBeOfType1: MessageKey = new MessageKey("ValidationMessage", "_0ShouldNotBeOfType1");
  export const _0And1CanNotBeSetAtTheSameTime: MessageKey = new MessageKey("ValidationMessage", "_0And1CanNotBeSetAtTheSameTime");
  export const _0Or1ShouldBeSet: MessageKey = new MessageKey("ValidationMessage", "_0Or1ShouldBeSet");
  export const _0And1And2CanNotBeSetAtTheSameTime: MessageKey = new MessageKey("ValidationMessage", "_0And1And2CanNotBeSetAtTheSameTime");
  export const _0Have1ElementsButAllowedOnly2: MessageKey = new MessageKey("ValidationMessage", "_0Have1ElementsButAllowedOnly2");
  export const _0IsEmpty: MessageKey = new MessageKey("ValidationMessage", "_0IsEmpty");
  export const _0ShouldBeEmpty: MessageKey = new MessageKey("ValidationMessage", "_0ShouldBeEmpty");
  export const _AtLeastOneValueIsNeeded: MessageKey = new MessageKey("ValidationMessage", "_AtLeastOneValueIsNeeded");
  export const PowerOf: MessageKey = new MessageKey("ValidationMessage", "PowerOf");
  export const BeAString: MessageKey = new MessageKey("ValidationMessage", "BeAString");
  export const BeAMultilineString: MessageKey = new MessageKey("ValidationMessage", "BeAMultilineString");
  export const IsATimeOfTheDay: MessageKey = new MessageKey("ValidationMessage", "IsATimeOfTheDay");
  export const ThereAre0InState1: MessageKey = new MessageKey("ValidationMessage", "ThereAre0InState1");
  export const ThereAre0ThatReferenceThis1: MessageKey = new MessageKey("ValidationMessage", "ThereAre0ThatReferenceThis1");
  export const _0IsNotCompatibleWith1: MessageKey = new MessageKey("ValidationMessage", "_0IsNotCompatibleWith1");
  export const _0IsRepeated: MessageKey = new MessageKey("ValidationMessage", "_0IsRepeated");
  export const NumberIsTooBig: MessageKey = new MessageKey("ValidationMessage", "NumberIsTooBig");
  export const NumberIsTooSmall: MessageKey = new MessageKey("ValidationMessage", "NumberIsTooSmall");
  export const Either0Or1ShouldBeSet: MessageKey = new MessageKey("ValidationMessage", "Either0Or1ShouldBeSet");
}


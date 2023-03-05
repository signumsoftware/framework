//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from './Reflection'
import * as Entities from './Signum.Entities'


export const ComparisonType = new EnumType<ComparisonType>("ComparisonType");
export type ComparisonType =
  "EqualTo" |
  "DistinctTo" |
  "GreaterThan" |
  "GreaterThanOrEqualTo" |
  "LessThan" |
  "LessThanOrEqualTo";

export module ValidationMessage {
  export const _0DoesNotHaveAValid1Format = new MessageKey("ValidationMessage", "_0DoesNotHaveAValid1Format");
  export const _0DoesNotHaveAValid1IdentifierFormat = new MessageKey("ValidationMessage", "_0DoesNotHaveAValid1IdentifierFormat");
  export const _0HasAnInvalidFormat = new MessageKey("ValidationMessage", "_0HasAnInvalidFormat");
  export const _0HasMoreThan1DecimalPlaces = new MessageKey("ValidationMessage", "_0HasMoreThan1DecimalPlaces");
  export const _0HasSomeRepeatedElements1 = new MessageKey("ValidationMessage", "_0HasSomeRepeatedElements1");
  export const _0ShouldBe12 = new MessageKey("ValidationMessage", "_0ShouldBe12");
  export const _0ShouldBe1 = new MessageKey("ValidationMessage", "_0ShouldBe1");
  export const _0ShouldBe1InsteadOf2 = new MessageKey("ValidationMessage", "_0ShouldBe1InsteadOf2");
  export const _0HasToBeBetween1And2 = new MessageKey("ValidationMessage", "_0HasToBeBetween1And2");
  export const _0HasToBeLowercase = new MessageKey("ValidationMessage", "_0HasToBeLowercase");
  export const _0HasToBeUppercase = new MessageKey("ValidationMessage", "_0HasToBeUppercase");
  export const _0IsNecessary = new MessageKey("ValidationMessage", "_0IsNecessary");
  export const _0IsNecessaryOnState1 = new MessageKey("ValidationMessage", "_0IsNecessaryOnState1");
  export const _0IsNotAllowed = new MessageKey("ValidationMessage", "_0IsNotAllowed");
  export const _0IsNotAllowedOnState1 = new MessageKey("ValidationMessage", "_0IsNotAllowedOnState1");
  export const _0IsNotSet = new MessageKey("ValidationMessage", "_0IsNotSet");
  export const _0IsNotSetIn1 = new MessageKey("ValidationMessage", "_0IsNotSetIn1");
  export const _0AreNotSet = new MessageKey("ValidationMessage", "_0AreNotSet");
  export const _0IsSet = new MessageKey("ValidationMessage", "_0IsSet");
  export const _0IsNotA1_G = new MessageKey("ValidationMessage", "_0IsNotA1_G");
  export const BeA0_G = new MessageKey("ValidationMessage", "BeA0_G");
  export const Be0 = new MessageKey("ValidationMessage", "Be0");
  export const BeBetween0And1 = new MessageKey("ValidationMessage", "BeBetween0And1");
  export const BeNotNull = new MessageKey("ValidationMessage", "BeNotNull");
  export const FileName = new MessageKey("ValidationMessage", "FileName");
  export const Have0Decimals = new MessageKey("ValidationMessage", "Have0Decimals");
  export const HaveANumberOfElements01 = new MessageKey("ValidationMessage", "HaveANumberOfElements01");
  export const HaveAPrecisionOf0 = new MessageKey("ValidationMessage", "HaveAPrecisionOf0");
  export const HaveBetween0And1Characters = new MessageKey("ValidationMessage", "HaveBetween0And1Characters");
  export const HaveMaximum0Characters = new MessageKey("ValidationMessage", "HaveMaximum0Characters");
  export const HaveMinimum0Characters = new MessageKey("ValidationMessage", "HaveMinimum0Characters");
  export const HaveNoRepeatedElements = new MessageKey("ValidationMessage", "HaveNoRepeatedElements");
  export const HaveValid0Format = new MessageKey("ValidationMessage", "HaveValid0Format");
  export const InvalidDateFormat = new MessageKey("ValidationMessage", "InvalidDateFormat");
  export const InvalidFormat = new MessageKey("ValidationMessage", "InvalidFormat");
  export const NotPossibleToaAssign0 = new MessageKey("ValidationMessage", "NotPossibleToaAssign0");
  export const Numeric = new MessageKey("ValidationMessage", "Numeric");
  export const OrBeNull = new MessageKey("ValidationMessage", "OrBeNull");
  export const Telephone = new MessageKey("ValidationMessage", "Telephone");
  export const _0ShouldHaveJustOneLine = new MessageKey("ValidationMessage", "_0ShouldHaveJustOneLine");
  export const _0ShouldNotHaveInitialSpaces = new MessageKey("ValidationMessage", "_0ShouldNotHaveInitialSpaces");
  export const _0ShouldNotHaveFinalSpaces = new MessageKey("ValidationMessage", "_0ShouldNotHaveFinalSpaces");
  export const TheLenghtOf0HasToBeEqualTo1 = new MessageKey("ValidationMessage", "TheLenghtOf0HasToBeEqualTo1");
  export const TheLengthOf0HasToBeGreaterOrEqualTo1 = new MessageKey("ValidationMessage", "TheLengthOf0HasToBeGreaterOrEqualTo1");
  export const TheLengthOf0HasToBeLesserOrEqualTo1 = new MessageKey("ValidationMessage", "TheLengthOf0HasToBeLesserOrEqualTo1");
  export const TheNumberOf0IsBeingMultipliedBy1 = new MessageKey("ValidationMessage", "TheNumberOf0IsBeingMultipliedBy1");
  export const TheRowsAreBeingGroupedBy0 = new MessageKey("ValidationMessage", "TheRowsAreBeingGroupedBy0");
  export const TheNumberOfElementsOf0HasToBe12 = new MessageKey("ValidationMessage", "TheNumberOfElementsOf0HasToBe12");
  export const Type0NotAllowed = new MessageKey("ValidationMessage", "Type0NotAllowed");
  export const _0IsMandatoryWhen1IsNotSet = new MessageKey("ValidationMessage", "_0IsMandatoryWhen1IsNotSet");
  export const _0IsMandatoryWhen1IsNotSetTo2 = new MessageKey("ValidationMessage", "_0IsMandatoryWhen1IsNotSetTo2");
  export const _0IsMandatoryWhen1IsSet = new MessageKey("ValidationMessage", "_0IsMandatoryWhen1IsSet");
  export const _0IsMandatoryWhen1IsSetTo2 = new MessageKey("ValidationMessage", "_0IsMandatoryWhen1IsSetTo2");
  export const _0ShouldBeNullWhen1IsNotSet = new MessageKey("ValidationMessage", "_0ShouldBeNullWhen1IsNotSet");
  export const _0ShouldBeNullWhen1IsNotSetTo2 = new MessageKey("ValidationMessage", "_0ShouldBeNullWhen1IsNotSetTo2");
  export const _0ShouldBeNullWhen1IsSet = new MessageKey("ValidationMessage", "_0ShouldBeNullWhen1IsSet");
  export const _0ShouldBeNullWhen1IsSetTo2 = new MessageKey("ValidationMessage", "_0ShouldBeNullWhen1IsSetTo2");
  export const _0ShouldBeNull = new MessageKey("ValidationMessage", "_0ShouldBeNull");
  export const _0ShouldBeADateInThePast = new MessageKey("ValidationMessage", "_0ShouldBeADateInThePast");
  export const BeInThePast = new MessageKey("ValidationMessage", "BeInThePast");
  export const _0ShouldBeGreaterThan1 = new MessageKey("ValidationMessage", "_0ShouldBeGreaterThan1");
  export const _0ShouldBeGreaterThanOrEqual1 = new MessageKey("ValidationMessage", "_0ShouldBeGreaterThanOrEqual1");
  export const _0ShouldBeLessThan1 = new MessageKey("ValidationMessage", "_0ShouldBeLessThan1");
  export const _0ShouldBeLessThanOrEqual1 = new MessageKey("ValidationMessage", "_0ShouldBeLessThanOrEqual1");
  export const _0HasAPrecisionOf1InsteadOf2 = new MessageKey("ValidationMessage", "_0HasAPrecisionOf1InsteadOf2");
  export const _0ShouldBeOfType1 = new MessageKey("ValidationMessage", "_0ShouldBeOfType1");
  export const _0ShouldNotBeOfType1 = new MessageKey("ValidationMessage", "_0ShouldNotBeOfType1");
  export const _0And1CanNotBeSetAtTheSameTime = new MessageKey("ValidationMessage", "_0And1CanNotBeSetAtTheSameTime");
  export const _0Or1ShouldBeSet = new MessageKey("ValidationMessage", "_0Or1ShouldBeSet");
  export const _0And1And2CanNotBeSetAtTheSameTime = new MessageKey("ValidationMessage", "_0And1And2CanNotBeSetAtTheSameTime");
  export const _0Have1ElementsButAllowedOnly2 = new MessageKey("ValidationMessage", "_0Have1ElementsButAllowedOnly2");
  export const _0IsEmpty = new MessageKey("ValidationMessage", "_0IsEmpty");
  export const _0ShouldBeEmpty = new MessageKey("ValidationMessage", "_0ShouldBeEmpty");
  export const _AtLeastOneValueIsNeeded = new MessageKey("ValidationMessage", "_AtLeastOneValueIsNeeded");
  export const PowerOf = new MessageKey("ValidationMessage", "PowerOf");
  export const BeAString = new MessageKey("ValidationMessage", "BeAString");
  export const BeAMultilineString = new MessageKey("ValidationMessage", "BeAMultilineString");
  export const IsATimeOfTheDay = new MessageKey("ValidationMessage", "IsATimeOfTheDay");
  export const ThereAre0InState1 = new MessageKey("ValidationMessage", "ThereAre0InState1");
  export const ThereAre0ThatReferenceThis1 = new MessageKey("ValidationMessage", "ThereAre0ThatReferenceThis1");
  export const _0IsNotCompatibleWith1 = new MessageKey("ValidationMessage", "_0IsNotCompatibleWith1");
  export const _0IsRepeated = new MessageKey("ValidationMessage", "_0IsRepeated");
}


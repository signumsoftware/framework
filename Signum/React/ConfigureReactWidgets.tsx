import * as React from 'react';
import { DateTime, Settings } from 'luxon';
import * as ReactWidgets from 'react-widgets-up';
import { UserProvidedMessages } from 'react-widgets-up/messages';
import { ReactWidgetsMessage } from './Signum.Entities';
import { NumberLocalizer } from 'react-widgets-up/IntlLocalizer';
import { toFormatWithFixes } from './Reflection';

export function getMessages(): UserProvidedMessages{
  return ({
    moveToday: ReactWidgetsMessage.MoveToday.niceToString(),
    moveBack: ReactWidgetsMessage.MoveBack.niceToString(),
    moveForward: ReactWidgetsMessage.MoveForward.niceToString(),
    dateButton: ReactWidgetsMessage.DateButton.niceToString(),
    openCombobox: ReactWidgetsMessage.OpenCombobox.niceToString(),
    emptyList: ReactWidgetsMessage.EmptyList.niceToString(),
    emptyFilter: ReactWidgetsMessage.EmptyFilter.niceToString(),
    createOption: (_value, searchTerm) =>
      !searchTerm ? ReactWidgetsMessage.CreateOption.niceToString() :
        ReactWidgetsMessage.CreateOption0.niceToString().formatHtml(<strong>"{searchTerm}"</strong>),
    tagsLabel: ReactWidgetsMessage.TagsLabel.niceToString(),
    removeLabel: ReactWidgetsMessage.RemoveLabel.niceToString(),
    noneSelected: ReactWidgetsMessage.NoneSelected.niceToString(),
    selectedItems: (labels) => ReactWidgetsMessage.SelectedItems0.niceToString(labels.join(", ")),
    increment: ReactWidgetsMessage.IncrementValue.niceToString(),
    decrement: ReactWidgetsMessage.DecrementValue.niceToString(),
  });
}

export function getNumberLocalizer(): ReactWidgets.NumberLocalizer<any> {
  return new NumberLocalizer();
}

export function getDateLocalizer(maxTwoDigitYear?: number): ReactWidgets.DateLocalizer<string> {

  var maxTwoDigitYearDefault = maxTwoDigitYear ?? DateTime.local().year + 10;

  function endOfDecade(date: Date) {
    return DateTime.fromJSDate(date).plus({ years: 10 }).minus({ millisecond: 1 }).toJSDate();
  }

  function endOfCentury(date: Date) {
    return DateTime.fromJSDate(date).plus({ years: 100 }).minus({ millisecond: 1 }).toJSDate();
  }

  return {
    date: (date, format) => toFormatWithFixes(DateTime.fromJSDate(date), format ?? "D"),
    time: (date, format) => DateTime.fromJSDate(date).toFormat(format ?? "t"),
    datetime: (date, format) => DateTime.fromJSDate(date).toFormat(format ?? "FF"),
    header: (date, format) => DateTime.fromJSDate(date).toFormat(format ?? "MMMM yyyy"),
    weekday: (date, format) => toFormatWithFixes(DateTime.fromJSDate(date), format ?? "EE"),
    dayOfMonth: (date, format) => DateTime.fromJSDate(date).toFormat(format ?? "dd"),
    month: (date, format) => DateTime.fromJSDate(date).toFormat(format ?? "MMM"),
    year: (date, format) => DateTime.fromJSDate(date).toFormat(format ?? "yyyy"),
    decade: (date, format) => DateTime.fromJSDate(date).toFormat(format ?? 'yyyy') + ' - ' + DateTime.fromJSDate(endOfDecade(date)).toFormat(format ?? 'yyyy'),
    century: (date, format) => DateTime.fromJSDate(date).toFormat(format ?? 'yyyy') + ' - ' + DateTime.fromJSDate(endOfCentury(date)).toFormat(format ?? 'yyyy'),

    firstOfWeek: function firstOfWeek(): number {
      var day = firstDay[Settings.defaultLocale?.tryAfter("-") ?? "ES"];

      switch (day) {
        case "sun": return 0;
        case "mon": return 1;
        case "fri": return 5;
        case "sat": return 6;
        default: throw new Error("Unexpected " + day);
      }
    },

    parse: function parse(value: string, format?: string) {

      value = value?.trim();

      if (value == undefined || value == "")
        return null;

      let t = DateTime.fromFormat(value, format ?? "F")
      if (t.isValid)
        return t.toJSDate();

      t = DateTime.fromFormat(value, "D")
      if (t.isValid)
        return t.toJSDate();

      t = DateTime.fromFormat(value, "ddMMyy")
      if (t.isValid) {
        if (value.length == 6) {
          t = t.set({ year: t.year > maxTwoDigitYearDefault ? t.year - 100 : t.year });
        }
        return t.toJSDate();
      }

      t = DateTime.fromFormat(value, "dd.MM.yy")
      if (t.isValid) {
        if (value.length == 8) {
          t = t.set({ year: t.year > maxTwoDigitYearDefault ? t.year - 100 : t.year });
        }
        return t.toJSDate();
      }

      t = DateTime.fromFormat(value, "dd/MM/yy")
      if (t.isValid) {
        if (value.length == 8) {
          t = t.set({ year: t.year > maxTwoDigitYearDefault ? t.year - 100 : t.year });
        }
        return t.toJSDate();
      }

      return null;
    }
  };
}

//https://github.com/unicode-cldr/cldr-core/blob/master/supplemental/weekData.json#L61
export const firstDay: { [isoCode: string]: "mon" | "sat" | "sun" | "fri" } = {
  "001": "mon",
  "AD": "mon",
  "AE": "sat",
  "AF": "sat",
  "AG": "sun",
  "AI": "mon",
  "AL": "mon",
  "AM": "mon",
  "AN": "mon",
  "AR": "mon",
  "AS": "sun",
  "AT": "mon",
  "AU": "sun",
  "AX": "mon",
  "AZ": "mon",
  "BA": "mon",
  "BD": "sun",
  "BE": "mon",
  "BG": "mon",
  "BH": "sat",
  "BM": "mon",
  "BN": "mon",
  "BR": "sun",
  "BS": "sun",
  "BT": "sun",
  "BW": "sun",
  "BY": "mon",
  "BZ": "sun",
  "CA": "sun",
  "CH": "mon",
  "CL": "mon",
  "CM": "mon",
  "CN": "sun",
  "CO": "sun",
  "CR": "mon",
  "CY": "mon",
  "CZ": "mon",
  "DE": "mon",
  "DJ": "sat",
  "DK": "mon",
  "DM": "sun",
  "DO": "sun",
  "DZ": "sat",
  "EC": "mon",
  "EE": "mon",
  "EG": "sat",
  "ES": "mon",
  "ET": "sun",
  "FI": "mon",
  "FJ": "mon",
  "FO": "mon",
  "FR": "mon",
  "GB": "mon",
  "GB-alt-variant": "sun",
  "GE": "mon",
  "GF": "mon",
  "GP": "mon",
  "GR": "mon",
  "GT": "sun",
  "GU": "sun",
  "HK": "sun",
  "HN": "sun",
  "HR": "mon",
  "HU": "mon",
  "ID": "sun",
  "IE": "mon",
  "IL": "sun",
  "IN": "sun",
  "IQ": "sat",
  "IR": "sat",
  "IS": "mon",
  "IT": "mon",
  "JM": "sun",
  "JO": "sat",
  "JP": "sun",
  "KE": "sun",
  "KG": "mon",
  "KH": "sun",
  "KR": "sun",
  "KW": "sat",
  "KZ": "mon",
  "LA": "sun",
  "LB": "mon",
  "LI": "mon",
  "LK": "mon",
  "LT": "mon",
  "LU": "mon",
  "LV": "mon",
  "LY": "sat",
  "MC": "mon",
  "MD": "mon",
  "ME": "mon",
  "MH": "sun",
  "MK": "mon",
  "MM": "sun",
  "MN": "mon",
  "MO": "sun",
  "MQ": "mon",
  "MT": "sun",
  "MV": "fri",
  "MX": "sun",
  "MY": "mon",
  "MZ": "sun",
  "NI": "sun",
  "NL": "mon",
  "NO": "mon",
  "NP": "sun",
  "NZ": "mon",
  "OM": "sat",
  "PA": "sun",
  "PE": "sun",
  "PH": "sun",
  "PK": "sun",
  "PL": "mon",
  "PR": "sun",
  "PT": "sun",
  "PY": "sun",
  "QA": "sat",
  "RE": "mon",
  "RO": "mon",
  "RS": "mon",
  "RU": "mon",
  "SA": "sun",
  "SD": "sat",
  "SE": "mon",
  "SG": "sun",
  "SI": "mon",
  "SK": "mon",
  "SM": "mon",
  "SV": "sun",
  "SY": "sat",
  "TH": "sun",
  "TJ": "mon",
  "TM": "mon",
  "TR": "mon",
  "TT": "sun",
  "TW": "sun",
  "UA": "mon",
  "UM": "sun",
  "US": "sun",
  "UY": "mon",
  "UZ": "mon",
  "VA": "mon",
  "VE": "sun",
  "VI": "sun",
  "VN": "mon",
  "WS": "sun",
  "XK": "mon",
  "YE": "sun",
  "ZA": "sun",
  "ZW": "sun"
};

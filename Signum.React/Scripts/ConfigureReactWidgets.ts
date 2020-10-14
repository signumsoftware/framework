import { DateTime } from 'luxon';

import * as ReactWidgets from 'react-widgets';

export function configure(maxTwoDigitYear?: number) {

  var maxTwoDigitYearDefault = maxTwoDigitYear ?? DateTime.local().year + 10;

  function endOfDecade(date: Date) {
    return DateTime.fromJSDate(date).plus({ years: 10 }).minus({ millisecond: 1 }).toJSDate();
  }

  function endOfCentury(date: Date) {
    return DateTime.fromJSDate(date).plus({ years: 100 }).minus({ millisecond: 1 }).toJSDate();
  }

  const localizer = {
    formats: {
      date: 'D',
      time: 't',
      'default': 'FF',
      header: 'MMMM yyyy',
      footer: 'DDD',
      weekday: 'EE',
      dayOfMonth: 'dd',
      month: 'MMM',
      year: 'yyyy',

      decade: function decade(date: Date, culture: string, localizer: any) {
        return localizer.format(date, 'YYYY', culture) + ' - ' + localizer.format(endOfDecade(date), 'YYYY', culture);
      },

      century: function century(date: Date, culture: string, localizer: any) {
        return localizer.format(date, 'YYYY', culture) + ' - ' + localizer.format(endOfCentury(date), 'YYYY', culture);
      }
    },

    firstOfWeek: function firstOfWeek(culture: string) {
      var day = fistDay[culture?.tryAfter("-") ?? "ES"];

      switch (day) {
        case "sun": return 0;
        case "mon": return 1;
        case "fri": return 5;
        case "sat": return 6;
        default: throw new Error("Unexpected " + day);
      }
    },

    parse: function parse(value: string, format: string, culture: string) {
      if (value == undefined || value == "")
        return undefined;

      let t = DateTime.fromFormat(value, format ?? "F", { locale: culture })
      if (t.isValid)
        return t.toJSDate();

      t = DateTime.fromFormat(value, "D", { locale: culture })
      if (t.isValid)
        return t.toJSDate();

      t = DateTime.fromFormat(value, "ddMMyy", { locale: culture })
      if (t.isValid) {
        if (value.length == 6) {
          var twoDigitYear = parseInt(value.substr(4, 2));
          t = t.set({ year: t.year > maxTwoDigitYearDefault ? 1900 + twoDigitYear : 2000 + twoDigitYear });
        }
        return t.toJSDate();
      }

      return undefined;
    },

    format: function format(value: Date, _format: string, culture: string) {
      if (value == undefined)
        return "";

      return DateTime.fromJSDate(value, { locale: culture }).toFormatFixed(_format);
    }
  };
  (ReactWidgets as any).setDateLocalizer(localizer);

}

//https://github.com/unicode-cldr/cldr-core/blob/master/supplemental/weekData.json#L61
const fistDay: { [isoCode: string]: "mon" | "sat" | "sun" | "fri" } = {
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


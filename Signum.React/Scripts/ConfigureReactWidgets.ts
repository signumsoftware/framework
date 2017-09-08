import * as moment from "moment"
import * as ReactWidgets from "react-widgets"

import conf from 'react-widgets/lib/configure';

export function configure(){

    if (typeof moment !== 'function') throw new TypeError('You must provide a valid moment object');

    const localField = (m: moment.Moment) => m.locale || m.lang,
        hasLocaleData = !!moment.localeData;

    if (!hasLocaleData) throw new TypeError('The Moment localizer depends on the `localeData` api, please provide a moment object v2.2.0 or higher');

    function getMoment(culture: string, value: any, format: string | undefined) {
        return culture ? localField(moment(value, format))(culture) : moment(value, format);
    }

    function endOfDecade(date: Date) {
        return moment(date).add(10, 'year').add(-1, 'millisecond').toDate();
    }

    function endOfCentury(date: Date) {
        return moment(date).add(100, 'year').add(-1, 'millisecond').toDate();
    }

    const localizer = {
        formats: {
            date: 'L',
            time: 'LT',
            'default': 'lll',
            header: 'MMMM YYYY',
            footer: 'LL',
            weekday: 'dd',
            dayOfMonth: 'DD',
            month: 'MMM',
            year: 'YYYY',

            decade: function decade(date: Date, culture: string, localizer: any) {
                return localizer.format(date, 'YYYY', culture) + ' - ' + localizer.format(endOfDecade(date), 'YYYY', culture);
            },

            century: function century(date: Date, culture: string, localizer: any) {
                return localizer.format(date, 'YYYY', culture) + ' - ' + localizer.format(endOfCentury(date), 'YYYY', culture);
            }
        },

        firstOfWeek: function firstOfWeek(culture: string) {
            return (moment.localeData(culture) as any).firstDayOfWeek();
        },

        parse: function parse(value: string, format: string, culture: string) {
            if (value == undefined || value == "")
                return undefined;

            return getMoment(culture, value, format).toDate();
        },

        format: function format(value: Date, _format: string, culture: string) {
            if (value == undefined)
                return "";

            return getMoment(culture, value, undefined).format(_format);
        }
    };
    conf.setDateLocalizer(localizer);

}

declare module "moment" {

    interface Moment {
        fromUserInterface(this: moment.Moment): Moment;
        toUserInterface(this: moment.Moment): Moment;
    }

    function smartNow(this: moment.Moment): Moment;

    interface Duration {
        format(template?: string, precision?: string, settings?: any): string;
    }
}

export function asumeGlobalUtcMode(m: typeof moment, utcMode: boolean) {
    if (utcMode) {
        m.fn.fromUserInterface = function (this: moment.Moment) { return this.utc(); };
        m.fn.toUserInterface = function (this: moment.Moment) { return this.local(); };
        m.smartNow = function () { return moment.utc(); };
    }

    else {
        m.fn.fromUserInterface = function (this: moment.Moment) { return this; };
        m.fn.toUserInterface = function (this: moment.Moment) { return this; };
        m.smartNow = function () { return moment(); };
    }
}

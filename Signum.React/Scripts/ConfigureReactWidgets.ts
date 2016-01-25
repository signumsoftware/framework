
import * as moment from "moment"
import { setDateLocalizer } from "react-widgets"

export function configure(){

    if (typeof moment !== 'function') throw new TypeError('You must provide a valid moment object');

    var localField = typeof moment().locale === 'function' ? 'locale' : 'lang',
        hasLocaleData = !!moment.localeData;

    if (!hasLocaleData) throw new TypeError('The Moment localizer depends on the `localeData` api, please provide a moment object v2.2.0 or higher');

    function getMoment(culture, value, format) {
        return culture ? moment(value, format)[localField](culture) : moment(value, format);
    }

    function endOfDecade(date) {
        return moment(date).add(10, 'year').add(-1, 'millisecond').toDate();
    }

    function endOfCentury(date) {
        return moment(date).add(100, 'year').add(-1, 'millisecond').toDate();
    }

    var localizer = {
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

            decade: function decade(date, culture, localizer) {
                return localizer.format(date, 'YYYY', culture) + ' - ' + localizer.format(endOfDecade(date), 'YYYY', culture);
            },

            century: function century(date, culture, localizer) {
                return localizer.format(date, 'YYYY', culture) + ' - ' + localizer.format(endOfCentury(date), 'YYYY', culture);
            }
        },

        firstOfWeek: function firstOfWeek(culture) {
            return (moment.localeData(culture) as any).firstDayOfWeek();
        },

        parse: function parse(value, format, culture) {
            return getMoment(culture, value, format).toDate();
        },

        format: function format(value, _format, culture) {
            return getMoment(culture, value, null).format(_format);
        }
    };

    setDateLocalizer(localizer);

}

export function asumeGlobalUtcMode(moment: moment.MomentStatic, utcMode: boolean) {
    if (utcMode) {
        moment.fn.fromUserInterface = function () { return this.utc(); };
        moment.fn.toUserInterface = function () { return this.local(); };
        moment.smartNow = function () { return moment.utc(); };
    }

    else {
        moment.fn.fromUserInterface = function () { return this; };
        moment.fn.toUserInterface = function () { return this; };
        moment.smartNow = function () { return moment(); };
    }
}

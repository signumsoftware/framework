// Overrides jquery calendar in (jquery-ui-1.7.2.js) to format dates in .net dateformat that can be found here:
// http://msdn.microsoft.com/en-us/library/8kb3ddd4%28v=VS.71%29.aspx
(function ($) {
    $.datepicker.formatDate = function (format, date, settings) {
        if (!date)
            return '';
        var dayNamesShort = (settings ? settings.dayNamesShort : null) || this._defaults.dayNamesShort;
        var dayNames = (settings ? settings.dayNames : null) || this._defaults.dayNames;
        var monthNamesShort = (settings ? settings.monthNamesShort : null) || this._defaults.monthNamesShort;
        var monthNames = (settings ? settings.monthNames : null) || this._defaults.monthNames;

        // Get patternChar number of repetitions
        var getAdvanceChars = function (pos, patternChar) {
            var repeatPattern = pos + 1;
            while ((repeatPattern < format.length) && (format.charAt(repeatPattern) == patternChar)) {
                repeatPattern++;
            }
            return (repeatPattern - pos);
        };

        // Format a number, with leading zero if necessary
        var formatNumber = function (value, len) {
            var num = '' + value;
            while (num.length < len)
                num = '0' + num;
            return num;
        };

        var output = '';
        var advanceChars;
        if (date) {
            for (var iFormat = 0; iFormat < format.length; iFormat += advanceChars) {
                var patternChar = format.charAt(iFormat);
                advanceChars = getAdvanceChars(iFormat, patternChar);

                switch (patternChar) {
                    case 'y':
                        if (advanceChars > 2)
                            output += date.getFullYear();
else {
                            var year = '' + date.getFullYear();
                            if (advanceChars == 2)
                                output += year.charAt(2);
                            output += year.charAt(3);
                        }
                        break;
                    case 'M':
                        if (advanceChars == 1)
                            output += formatNumber(date.getMonth() + 1, 1);
else if (advanceChars == 2)
                            output += formatNumber(date.getMonth() + 1, 2);
else if (advanceChars == 3)
                            output += monthNamesShort[date.getMonth()];
else if (advanceChars == 4)
                            output += monthNames[date.getMonth()];
                        break;
                    case 'd':
                        if (advanceChars == 1)
                            output += formatNumber(date.getDate(), 1);
else if (advanceChars == 2)
                            output += formatNumber(date.getDate(), 2);
else if (advanceChars == 3)
                            output += dayNamesShort[date.getDay()];
else if (advanceChars == 4)
                            output += dayNames[date.getDay()];
                        break;
                    case 'H':
                    case 'h':
                        output += formatNumber(0, advanceChars);
                        break;
                    case 'm':
                        output += formatNumber(0, advanceChars);
                        break;
                    case 's':
                        output += formatNumber(0, advanceChars);
                        break;
                    case 't':
                        output += 'a';
                        if (advanceChars == 2)
                            output += 'm';
                        break;
                    case 'f':
                        break;
                    default:
                        output += format.charAt(iFormat);
                }
            }
        }
        return output;
    };

    $.datepicker.parseDate = function (format, value, settings) {
        if (format == null || value == null)
            throw 'Invalid arguments';

        value = (typeof value == 'object' ? value.toString() : value + '');
        if (value == '')
            return null;

        var shortYearCutoff = (settings ? settings.shortYearCutoff : null) || this._defaults.shortYearCutoff;
        var dayNamesShort = (settings ? settings.dayNamesShort : null) || this._defaults.dayNamesShort;
        var dayNames = (settings ? settings.dayNames : null) || this._defaults.dayNames;
        var monthNamesShort = (settings ? settings.monthNamesShort : null) || this._defaults.monthNamesShort;
        var monthNames = (settings ? settings.monthNames : null) || this._defaults.monthNames;

        var year = -1;
        var month = -1;
        var day = -1;
        var doy = -1;

        // Get patternChar number of repetitions
        var getAdvanceChars = function (pos, patternChar) {
            var repeatPattern = pos + 1;
            while ((repeatPattern < format.length) && (format.charAt(repeatPattern) == patternChar)) {
                repeatPattern++;
            }
            return (repeatPattern - pos);
        };

        var getValueAdvanceChars = function (iFormat, advanceChars, valueCurrentIndex) {
            if (format.length == iFormat + advanceChars)
                return value.length - valueCurrentIndex;
            var templateNextChar = format.charAt(iFormat + advanceChars);
            return value.indexOf(templateNextChar, valueCurrentIndex) - valueCurrentIndex;
        };

        // Extract a number from the string value
        var getNumber = function (pos, len) {
            var num = 0;
            var index;
            for (index = 0; index < len; index++) {
                var currChar = value.charAt(pos + index);
                if (currChar < '0' || currChar > '9')
                    throw 'Missing number at position ' + pos + index;
                num = num * 10 + parseInt(currChar, 10);
            }
            return num;
        };

        // Extract a name from the string value and convert to an index
        var getNameIndex = function (name, arrayNames) {
            for (var i = 0; i < arrayNames.length; i++) {
                if (name == arrayNames[i])
                    return i + 1;
            }
            throw 'Unknown name ' + name;
        };

        // Confirm that a literal character matches the string value
        var checkLiteral = function (pos, valueCurrentIndex) {
            if (value.charAt(valueCurrentIndex) != format.charAt(pos))
                throw 'Unexpected literal at position ' + iValue;
            iValue++;
        };

        var valueCurrentIndex = 0;
        var valueAdvanceChars = 0;
        var iValue = 0;
        var advanceChars;
        for (var iFormat = 0; iFormat < format.length; iFormat += advanceChars) {
            var patternChar = format.charAt(iFormat);
            advanceChars = getAdvanceChars(iFormat, patternChar);
            valueAdvanceChars = getValueAdvanceChars(iFormat, advanceChars, valueCurrentIndex);
            switch (patternChar) {
                case 'y':
                    year = getNumber(valueCurrentIndex, valueAdvanceChars);
                    if (advanceChars > 2)
                        break;
else {
                        var currYear = '' + new Date().getFullYear();
                        if (advanceChars == 2)
                            year = parseInt(currYear.charAt(0) + currYear.charAt(1) + year);
else
                            year = parseInt(currYear.charAt(0) + currYear.charAt(1) + currYear.charAt(2) + year);
                    }
                    break;
                case 'M':
                    if (advanceChars == 1 || advanceChars == 2)
                        month = getNumber(valueCurrentIndex, valueAdvanceChars);
else {
                        var monthStr = value.substr(valueCurrentIndex, valueAdvanceChars);
                        if (advanceChars == 3)
                            month = getNameIndex(monthStr, monthNamesShort);
else if (advanceChars == 4)
                            month = getNameIndex(monthStr, monthNames);
                    }

                    break;
                case 'd':
                    if (advanceChars == 1 || advanceChars == 2)
                        day = getNumber(valueCurrentIndex, valueAdvanceChars);
else {
                        var dayStr = value.substr(valueCurrentIndex, valueAdvanceChars);
                        if (advanceChars == 3)
                            day = getNameIndex(dayStr, dayNamesShort);
else if (advanceChars == 4)
                            day = getNameIndex(dayStr, dayNames);
                    }
                    break;
                case 'D':
                    throw new Error("not implemented");

                    break;
                case 'H':
                case 'h':
                case 'm':
                case 's':
                case 't':
                case 'f':
                    break;
                default: {
                    checkLiteral(iFormat, valueCurrentIndex);
                    advanceChars = 1;
                    valueAdvanceChars = 1;
                }
            }
            valueCurrentIndex += valueAdvanceChars;
        }

        if (year == -1)
            year = new Date().getFullYear();

        var date = this._daylightSavingAdjust(new Date(year, month - 1, day));
        if (date.getFullYear() != year || date.getMonth() + 1 != month || date.getDate() != day)
            throw 'Invalid date';
        return date;
    };
})(jQuery);

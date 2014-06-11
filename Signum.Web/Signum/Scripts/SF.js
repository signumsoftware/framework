/// <reference path="../Headers/es6-promises/es6-promises.d.ts"/>
/// <reference path="../Headers/jquery/jquery.d.ts"/>

var onceStorage = {};
function once(key, func) {
    if (onceStorage[key] === undefined) {
        func();
        onceStorage[key] = "loaded";
    }
}

var SF;
(function (SF) {
    SF.Urls;
    SF.Locale;

    SF.debug = true;

    function log(s) {
        if (SF.debug) {
            if (typeof console != "undefined" && typeof console.debug != "undefined")
                console.log(s);
        }
    }
    SF.log = log;

    once("setupAjaxRedirectPrefilter", function () {
        return setupAjaxRedirect();
    });

    function setupAjaxRedirect() {
        $.ajaxPrefilter(function (options, originalOptions, jqXHR) {
            var originalSuccess = options.success;

            var getRredirectUrl = function (ajaxResult) {
                if (SF.isEmpty(ajaxResult))
                    return null;

                if (typeof ajaxResult !== "object")
                    return null;

                if (ajaxResult.result == null)
                    return null;

                if (ajaxResult.result == 'url')
                    return ajaxResult.url;

                return null;
            };

            options.success = function (result, text, xhr) {
                //if (!options.avoidRedirect && jqXHR.status == 302)
                //    location.href = jqXHR.getResponseHeader("Location");
                var url = getRredirectUrl(result);
                if (!SF.isEmpty(url))
                    location.href = url;

                if (originalSuccess)
                    originalSuccess(result, text, xhr);
            };
        });
    }

    function isEmpty(value) {
        return (value == undefined || value == null || value === "" || value.toString() == "");
    }
    SF.isEmpty = isEmpty;
    ;

    (function (InputValidator) {
        function isNumber(e) {
            var c = e.keyCode;
            return ((c >= 48 && c <= 57) || (c >= 96 && c <= 105) || (c == 8) || (c == 9) || (c == 12) || (c == 27) || (c == 37) || (c == 39) || (c == 46) || (c == 36) || (c == 35) || (c == 109) || (c == 189) || (e.ctrlKey && c == 86) || (e.ctrlKey && c == 67));
        }
        InputValidator.isNumber = isNumber;

        function isDecimal(e) {
            var c = e.keyCode;
            return (this.isNumber(e) || (c == 110) || (c == 190) || (c == 188));
        }
        InputValidator.isDecimal = isDecimal;
    })(SF.InputValidator || (SF.InputValidator = {}));
    var InputValidator = SF.InputValidator;

    (function (Cookies) {
        function read(name) {
            var nameEQ = name + "=";
            var ca = document.cookie.split(';');
            for (var i = 0; i < ca.length; i++) {
                var c = ca[i];
                while (c.charAt(0) == ' ')
                    c = c.substring(1, c.length);
                if (c.indexOf(nameEQ) == 0)
                    return c.substring(nameEQ.length, c.length);
            }
            return null;
        }
        Cookies.read = read;

        function create(name, value, days, domain) {
            var expires = null, path = "/";

            if (days) {
                var date = new Date();
                date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
                expires = date;
            }

            document.cookie = name + "=" + encodeURI(value) + ((expires) ? ";expires=" + expires.toGMTString() : "") + ((path) ? ";path=" + path : "") + ((domain) ? ";domain=" + domain : "");
        }
        Cookies.create = create;
    })(SF.Cookies || (SF.Cookies = {}));
    var Cookies = SF.Cookies;

    (function (LocalStorage) {
        var isSupported = typeof (localStorage) != 'undefined';

        function getItem(key) {
            if (isSupported) {
                try  {
                    return localStorage.getItem(key);
                } catch (e) {
                }
            }
            return Cookies.read(key);
        }
        LocalStorage.getItem = getItem;
        ;

        function setItem(key, value, days) {
            if (isSupported) {
                try  {
                    localStorage.setItem(key, value);
                    return true;
                } catch (e) {
                }
            } else
                Cookies.create(key, value, days ? days : 30);
        }
        LocalStorage.setItem = setItem;
        ;

        return {
            getItem: getItem,
            setItem: setItem
        };
    })(SF.LocalStorage || (SF.LocalStorage = {}));
    var LocalStorage = SF.LocalStorage;

    function hiddenInput(id, value) {
        return "<input type='hidden' id='" + id + "' name='" + id + "' value='" + value + "' />\n";
    }
    SF.hiddenInput = hiddenInput;

    function hiddenDiv(id, innerHtml) {
        return $("<div id='" + id + "' style='display:none'></div>").html(innerHtml);
    }
    SF.hiddenDiv = hiddenDiv;

    function cloneWithValues(elements) {
        var clone = elements.clone(true);

        var sourceSelect = elements.filter("select").add(elements.find("select"));
        var cloneSelect = clone.filter("select").add(clone.filter("selet"));

        for (var i = 0, l = sourceSelect.length; i < l; i++) {
            cloneSelect.eq(i).val(sourceSelect.eq(i).val());
        }

        return clone;
    }
    SF.cloneWithValues = cloneWithValues;

    function ajaxPost(settings) {
        return new Promise(function (resolve, reject) {
            settings.success = resolve;
            settings.error = function (jqXHR, textStatus, errorThrow) {
                return reject({ jqXHR: jqXHR, textStatus: textStatus, errorThrow: errorThrow });
            };
            settings.type = "POST";
            $.ajax(settings);
        });
    }
    SF.ajaxPost = ajaxPost;

    function ajaxGet(settings) {
        return new Promise(function (resolve, reject) {
            settings.success = resolve;
            settings.error = function (jqXHR, textStatus, errorThrow) {
                return reject({ jqXHR: jqXHR, textStatus: textStatus, errorThrow: errorThrow });
            };
            settings.type = "GET";
            $.ajax(settings);
        });
    }
    SF.ajaxGet = ajaxGet;

    function promiseForeach(array, action) {
        return array.reduce(function (prom, val) {
            return prom.then(function () {
                return action(val);
            });
        }, Promise.resolve(null));
    }
    SF.promiseForeach = promiseForeach;

    function submit(urlController, requestExtraJsonData, $form) {
        $form = $form || $("form");
        if (!SF.isEmpty(requestExtraJsonData)) {
            if ($.isFunction(requestExtraJsonData))
                requestExtraJsonData = requestExtraJsonData();

            for (var key in requestExtraJsonData) {
                if (requestExtraJsonData.hasOwnProperty(key)) {
                    $form.append(SF.hiddenInput(key, requestExtraJsonData[key]));
                }
            }
        }

        $form.attr("action", urlController)[0].submit();
    }
    SF.submit = submit;

    function submitOnly(urlController, requestExtraJsonData, openNewWindow) {
        if (requestExtraJsonData == null)
            throw "SubmitOnly needs requestExtraJsonData. Use Submit instead";

        var $form = $("<form />", {
            method: 'post',
            action: urlController
        });

        if (openNewWindow)
            $form.attr("target", "_blank");

        if (!SF.isEmpty(requestExtraJsonData)) {
            if ($.isFunction(requestExtraJsonData)) {
                requestExtraJsonData = requestExtraJsonData();
            }
            for (var key in requestExtraJsonData) {
                if (requestExtraJsonData.hasOwnProperty(key)) {
                    $form.append(SF.hiddenInput(key, requestExtraJsonData[key]));
                }
            }
        }

        var currentForm = $("form");
        currentForm.after($form);

        $form[0].submit();
        $form.remove();

        return false;
    }
    SF.submitOnly = submitOnly;
})(SF || (SF = {}));

once("serializeObject", function () {
    $.fn.serializeObject = function () {
        var o = {};
        var a = this.serializeArray();
        $.each(a, function () {
            if (o[this.name] !== undefined) {
                o[this.name] += "," + (this.value || '');
            } else {
                o[this.name] = this.value || '';
            }
        });
        return o;
    };
});

once("arrayExtensions", function () {
    Array.prototype.groupByArray = function (keySelector) {
        var result = [];
        var objectGrouped = this.groupByObject(keySelector);
        for (var prop in objectGrouped) {
            if (objectGrouped.hasOwnProperty(prop))
                result.push({ key: prop, elements: objectGrouped[prop] });
        }
        return result;
    };

    Array.prototype.groupByObject = function (keySelector) {
        var result = {};

        for (var i = 0; i < this.length; i++) {
            var element = this[i];
            var key = keySelector(element);
            if (!result[key])
                result[key] = [];
            result[key].push(element);
        }
        return result;
    };

    Array.prototype.orderBy = function (keySelector) {
        var cloned = this.slice(0);
        cloned.sort(function (e1, e2) {
            var v1 = keySelector(e1);
            var v2 = keySelector(e2);
            if (v1 > v2)
                return 1;
            if (v1 < v2)
                return -1;
            return 0;
        });
        return cloned;
    };

    Array.prototype.orderByDescending = function (keySelector) {
        var cloned = this.slice(0);
        cloned.sort(function (e1, e2) {
            var v1 = keySelector(e1);
            var v2 = keySelector(e2);
            if (v1 < v2)
                return 1;
            if (v1 > v2)
                return -1;
            return 0;
        });
        return cloned;
    };
});

once("stringExtensions", function () {
    String.prototype.hasText = function () {
        return (this == null || this == undefined || this == '') ? false : true;
    };

    String.prototype.contains = function (str) {
        return this.indexOf(str) !== -1;
    };

    String.prototype.startsWith = function (str) {
        return this.indexOf(str) === 0;
    };

    String.prototype.endsWith = function (str) {
        return this.lastIndexOf(str) === (this.length - str.length);
    };

    String.prototype.format = function () {
        var regex = /\{([\w-]+)(?:\:([\w\.]*)(?:\((.*?)?\))?)?\}/g;

        var args = arguments;

        var getValue = function (key) {
            if (args == null || typeof args === 'undefined')
                return null;

            var value = args[key];
            var type = typeof value;

            return type === 'string' || type === 'number' ? value : null;
        };

        return this.replace(regex, function (match) {
            //match will look like {sample-match}
            //key will be 'sample-match';
            var key = match.substr(1, match.length - 2);

            var value = getValue(key);

            return value != null ? value : match;
        });
    };

    String.prototype.replaceAll = function (from, to) {
        return this.split(from).join(to);
    };

    String.prototype.before = function (separator) {
        var index = this.indexOf(separator);
        if (index == -1)
            throw Error("{0} not found".format(separator));

        return this.substring(0, index);
    };

    String.prototype.after = function (separator) {
        var index = this.indexOf(separator);
        if (index == -1)
            throw Error("{0} not found".format(separator));

        return this.substring(index + separator.length);
    };

    String.prototype.tryBefore = function (separator) {
        var index = this.indexOf(separator);
        if (index == -1)
            return null;

        return this.substring(0, index);
    };

    String.prototype.tryAfter = function (separator) {
        var index = this.indexOf(separator);
        if (index == -1)
            return null;

        return this.substring(index + separator.length);
    };

    String.prototype.beforeLast = function (separator) {
        var index = this.lastIndexOf(separator);
        if (index == -1)
            throw Error("{0} not found".format(separator));

        return this.substring(0, index);
    };

    String.prototype.afterLast = function (separator) {
        var index = this.lastIndexOf(separator);
        if (index == -1)
            throw Error("{0} not found".format(separator));

        return this.substring(index + separator.length);
    };

    String.prototype.tryBeforeLast = function (separator) {
        var index = this.lastIndexOf(separator);
        if (index == -1)
            return null;

        return this.substring(0, index);
    };

    String.prototype.tryAfterLast = function (separator) {
        var index = this.lastIndexOf(separator);
        if (index == -1)
            return null;

        return this.substring(index + separator.length);
    };

    if (typeof String.prototype.trim !== 'function') {
        String.prototype.trim = function () {
            return this.replace(/^\s+|\s+$/, '');
        };
    }

    String.prototype.child = function (pathPart) {
        if (SF.isEmpty(this))
            return pathPart;

        if (SF.isEmpty(pathPart))
            return this;

        if (this.endsWith("_"))
            throw new Error("path {0} ends with _".format(this.toString()));

        if (pathPart.startsWith("_"))
            throw new Error("pathPart {0} starts with _".format(pathPart));

        return this + "_" + pathPart;
    };

    String.prototype.parent = function (pathPart) {
        if (SF.isEmpty(this))
            throw new Error("impossible to pop the empty string");

        if (SF.isEmpty(pathPart)) {
            var index = this.lastIndexOf("_");

            if (index == -1)
                return "";

            return this.substr(0, index);
        } else {
            if (this == pathPart)
                return "";

            var index = this.lastIndexOf("_" + pathPart);

            if (index != -1)
                return this.substr(0, index);

            if (pathPart.startsWith(pathPart + "_"))
                return "";

            throw Error("pathPart {0} not found on {1}".format(pathPart, this.toString()));
        }
    };

    String.prototype.get = function (context) {
        if (SF.isEmpty(this))
            throw new Error("Impossible to call 'get' on the empty string");

        var selector = "[id='" + this + "']";

        var result = $(selector, context);

        if (result.length == 0 && context)
            result = $(context).filter(selector);

        if (result.length == 0)
            throw new Error("No element with id = '{0}' found".format(this.toString()));

        if (result.length > 1)
            throw new Error("{0} elements with id = '{1}' found".format(result.length, this.toString()));

        return result;
    };

    String.prototype.tryGet = function (context) {
        if (SF.isEmpty(this))
            throw new Error("Impossible to call 'get' on the empty string");

        var selector = "[id='" + this + "']";

        var result = $(selector, context);

        if (result.length == 0 && context)
            result = $(context).filter(selector);

        if (result.length > 1)
            throw new Error("{0} elements with id = '{1}' found".format(result.length, this.toString()));

        return result;
    };
});

once("dateExtensions", function () {
    Date.prototype.addMiliseconds = function (inc) {
        var n = new Date(this.valueOf());
        n.setMilliseconds(this.getMilliseconds() + inc);
        return n;
    };

    Date.prototype.addSecond = function (inc) {
        var n = new Date(this.valueOf());
        n.setSeconds(this.getSeconds() + inc);
        return n;
    };

    Date.prototype.addMinutes = function (inc) {
        var n = new Date(this.valueOf());
        n.setMinutes(this.getMinutes() + inc);
        return n;
    };

    Date.prototype.addHour = function (inc) {
        var n = new Date(this.valueOf());
        n.setHours(this.getHours() + inc);
        return n;
    };

    Date.prototype.addDate = function (inc) {
        var n = new Date(this.valueOf());
        n.setDate(this.getDate() + inc);
        return n;
    };

    Date.prototype.addMonth = function (inc) {
        var n = new Date(this.valueOf());
        n.setMonth(this.getMonth() + inc);
        return n;
    };

    Date.prototype.addYear = function (inc) {
        var n = new Date(this.valueOf());
        n.setFullYear(this.getFullYear() + inc);
        return n;
    };
});
//# sourceMappingURL=SF.js.map

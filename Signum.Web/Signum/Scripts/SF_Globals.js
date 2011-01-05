if (!SF && typeof SF == "undefined") {

    var SF = function () {

        var _added = [];
        var _jsloaded = [],
            _cssloaded = [];

        /*  function _scriptForModule(name) {
        //return "combine/areajs?f=signum/scripts/sf_" + name + ".js";
        return "signum/scripts/sf_" + name + ".js";
        }*/

        function add(name, fn) {
            if (!_added[name]) {
                _added[name] = true;
                fn(this);
            }
        }

        /*  function use(name, fn) {
        if (!_added[name]) {
        _added[name] = true;
        var src = _scriptForModule(name);
        Loader.loadJs(src, fn);
        } else {
        fn();
        }
        }*/

        function use(name, fn) {
            fn();
        }

        function setLoaded(src, position) {
            _jsloaded[src] = true;
            _jsSet[position].count--;

            if (_jsSet[position].count == 0) {
                _jsSet[position].func && _jsSet[position].func();
                delete _jsSet[position];
            }

        }

        function _loadJs(src, position) {
            if (!_jsloaded[src]) {
                Loader.loadJs(src, function () { setLoaded(src, position); });
            }
            else {
                setLoaded(src, position);
            }
        }

        var _jsSet = [];

        function loadJs(src, fn) {
            if (typeof src == "object") {
                var position = _jsSet.length;

                _jsSet[position] = { count: src.length, func: fn };

                for (var i in src) {
                    _loadJs(src[i], position);
                }
            } else {
                _loadJs(src, position);
            }
        }

        function _loadCss(src) {
            if (!_cssloaded[src]) {
                _cssloaded[src] = true;
                Loader.loadCss(src);
            }
        }

        function loadCss(src, fn) {
            if (typeof src == "object") {
                for (var i in src) {
                    _loadCss(src[i]);
                }
            } else {
                _loadCss(src);
            }

            if (fn) fn();
        }

        return ({
            add: add,
            use: use,
            loadJs: loadJs,
            loadCss: loadCss
        });
    } ();

    var Loader = (function () {
        var d = document,
        head = d.getElementsByTagName("head")[0];

        var loadJs = function (url, cb) {
            /*var script = d.createElement('script');
            script.setAttribute('src', url);
            script.setAttribute('type', 'text/javascript');

            var loaded = false;
            var loadFunction = function () {
            if (loaded) return;
            loaded = true;
            cb && cb();
            };
            script.onload = loadFunction;
            script.onreadystatechange = loadFunction;
            head.appendChild(script);*/
            $.getScript(url, cb);
        };

        var cachedBrowser;

        var browser = function () {
            if (!cachedBrowser) {
                var ua = navigator.userAgent.toLowerCase();
                var match = /(webkit)[ \/]([\w.]+)/.exec(ua) ||
               /(opera)(?:.*version)?[ \/]([\w.]+)/.exec(ua) ||
               /(msie) ([\w.]+)/.exec(ua) ||
               !/compatible/.test(ua) && /(mozilla)(?:.*? rv:([\w.]+))?/.exec(ua) ||
               [];
                cachedBrowser = match[1];
            }
            return cachedBrowser;
        };

        var loadCss = function (url, cb) {
            var link = d.createElement("link");
            link.type = "text/css";
            link.rel = "stylesheet";
            link.href = url;

            if (cb) {

                if (browser() == "msie")
                    link.onreadystatechange = function () {
                        /loaded|complete/.test(link.readyState) && cb();
                    }
                else if (browser() == "opera")
                    link.onload = cb;
                else
                //FF, Safari, Chrome
                    (function () {
                        try {
                            link.sheet.cssRule;
                        } catch (e) {
                            setTimeout(arguments.callee, 20);
                            return;
                        };
                        cb();
                    })();
            }
            head.appendChild(link);
        };

        return { loadCss: loadCss, loadJs: loadJs };

    })();

    var sfSeparator = "_",
    sfPrefix = "prefix",
    sfPrefixToIgnore = "prefixToIgnore",
    sfTabId = "sfTabId",
    sfPartialViewName = "sfPartialViewName",
    sfReactive = "sfReactive",
    sfFieldErrorClass = "field-validation-error",
    sfInputErrorClass = "input-validation-error",
    sfSummaryErrorClass = "validation-summary-errors",
    sfInlineErrorVal = "inlineVal",
    sfGlobalErrorsKey = "sfGlobalErrors",
    sfGlobalValidationSummary = "sfGlobalValidationSummary",

    sfRuntimeInfo = "sfRuntimeInfo",
    sfStaticInfo = "sfStaticInfo",
    sfImplementations = "sfImplementations",
    sfEntity = "sfEntity",
    sfToStr = "sfToStr",
    sfLink = "sfLink",

    /*potentially movable */

    sfIndex = "sfIndex",
    sfTicks = "sfTicks",

    sfItemsContainer = "sfItemsContainer",
    sfRepeaterItem = "sfRepeaterItem",

    sfQueryUrlName = "sfQueryUrlName",
    sfTop = "sfTop",
    sfAllowMultiple = "sfAllowMultiple",
    sfView = "sfView",

    sfIdRelated = "sfIdRelated",
    sfRuntimeTypeRelated = "sfRuntimeTypeRelated",

    /* end movable */


    sfBtnCancel = "sfBtnCancel",            //viewNavigator, findNavigator, mixin
    sfBtnOk = "sfBtnOk",                    //viewNavigator, findNavigator

    sfEntityTypeName = "sfEntityTypeName";   //operations, findNavigator

    String.prototype.startsWith = function (str) {
        return (this.indexOf(str) === 0);
    }


    SF.ajax = function (jqueryAjaxOptions) {
        var options = $.extend({
            type: null,
            url: null,
            data: null,
            async: false,
            dataType: null,
            success: null,
            error: null
        }, jqueryAjaxOptions);

        $.ajax({
            type: options.type,
            url: options.url,
            data: options.data,
            async: options.async,
            dataType: options.dataType,
            success: function (ajaxResult) {
                var url = SF.checkRedirection(ajaxResult);
                if (!empty(url))
                    window.location.href = isAbsoluteUrl(url) ? url : $("base").attr("href") + url;
                else {
                    if (options.success != null)
                        options.success(ajaxResult);
                }
            },
            error: options.error
        });

        /*
        
        $.ajax($.extend(jqueryAjaxOptions, {
        success: function(ajaxResult) {
        var url = SF.checkRedirection(ajaxResult);
        if (!empty(url))
        window.location.href = $("base").attr("href") + url;
        else {
        if (options.success != null)
        options.success(ajaxResult);
        }
        }
        }));
        
        */
    };

    SF.checkRedirection = function (ajaxResult) {
        if (empty(ajaxResult))
            return null;
        var json;

        if (typeof ajaxResult !== "object") {
            //suppose that if is already an object it will be a json Object            
            if (!SF.isJSON(ajaxResult))
                return null;
            json = $.parseJSON(ajaxResult);
        } else {
            json = ajaxResult;
        }

        if (json.result == null)
            return null;
        if (json.result == 'url')
            return json.url;
        return null;
    };

    //Based on jquery-1.4.2 parseJSON function
    SF.isJSON = function (data) {

        if (typeof data !== "string" || !data)
            return null;

        // Make sure leading/trailing whitespace is removed (IE can't handle it)
        data = jQuery.trim(data);

        // Make sure the incoming data is actual JSON
        // Logic borrowed from http://json.org/json2.js
        if (/^[\],:{}\s]*$/.test(data.replace(/\\(?:["\\\/bfnrt]|u[0-9a-fA-F]{4})/g, "@")
			.replace(/"[^"\\\n\r]*"|true|false|null|-?\d+(?:\.\d*)?(?:[eE][+\-]?\d+)?/g, "]")
			.replace(/(?:^|:|,)(?:\s*\[)+/g, ""))) {

            return true;
        }
        else
            return false;
    };

    function isAbsoluteUrl(s) {
        return s.toLowerCase().startsWith("http");
    }

    String.prototype.startsWith = function (str) {
        return (this.indexOf(str) === 0);
    }

    var StaticInfo = function (_prefix) {
        var prefix = _prefix,
			_staticType = 0,
			_isEmbedded = 1,
			_isReadOnly = 2,
			$elem;				//cache for the element
        
        var find = function () {
			if (!$elem) $elem = $('#' + prefix.compose(sfStaticInfo));
			return $elem;
        };
		
        var value = function () {
            return find().val();
        };
        
		var toArray = function () {
            return value().split(";")
        };
        var toValue = function (array) {
            return array.join(";");
        };
		
        var getValue = function (key) {
            var array = toArray();
            return array[key];
        };
        
		var staticType = function () {
            return getValue(_staticType);
        };
        
		var isEmbedded = function () {
            return getValue(_isEmbedded);
        };
        
		var isReadOnly = function () {
            return getValue(_isReadOnly);
        };
		
        var createValue = function (staticType, isEmbedded, isReadOnly) {
            var array = [];
            array[_staticType] = staticType;
            array[_isEmbedded] = isEmbedded;
            array[_isReadOnly] = isReadOnly;
            return toValue(array);
        };
		
		return{
			staticType: staticType,
			isEmbedded: isEmbedded,
			isReadOnly: isReadOnly,
			createValue: createValue,
            find: find
		};
    };

    function StaticInfoFor(prefix) {
        return new StaticInfo(prefix);
    }

    var RuntimeInfo = function (_prefix) {
        var prefix = _prefix;
        var _runtimeType = 0;
        var _id = 1;
        var _isNew = 2;
        var _ticks = 3;
        var $elem; 			//cache for the element


        var find = function () {
            if (!$elem) { $elem = $('#' + prefix.compose(sfRuntimeInfo)); }
            return $elem;
        };
        var value = function () {
            return find().val();
        };
        var toArray = function () {
            return value().split(";");
        };
        var toValue = function (array) {
            return array.join(";");
        };
        var getSet = function (key, val) {
            var array = toArray();
            if (val == undefined)
                return array[key];

            array[key] = val;
            find().val(toValue(array));
            return self;
        };
        var runtimeType = function () {
            return getSet(_runtimeType);
        };
        var id = function () {
            return getSet(_id);
        };
        var isNew = function () {
            return getSet(_isNew);
        };
        var ticks = function (val) {
            return getSet(_ticks, val);
        };
        var setEntity = function (runtimeType, id) {
            getSet(_runtimeType, runtimeType);
            if (empty(id))
                getSet(_id, '').getSet(_isNew, 'n');
            else
                getSet(_id, id).getSet(_isNew, 'o');
            return self;
        };
        var removeEntity = function () {
            getSet(_runtimeType, '');
            getSet(_id, '');
            getSet(_isNew, 'o');
            return self;
        };
        var createValue = function (runtimeType, id, isNew, ticks) {
            var array = [];
            array[_runtimeType] = runtimeType;
            array[_id] = id;
            array[_isNew] = isNew;
            array[_ticks] = ticks;
            return toValue(array);
        };

        var self = {
            runtimeType: runtimeType,
            id: id,
            isNew: isNew,
            ticks: ticks,
            setEntity: setEntity,
            removeEntity: removeEntity,
            createValue: createValue,
            find: find,
            getSet: getSet,
            value: value
        };

        return self;
    };

    function RuntimeInfoFor(prefix) {
        return new RuntimeInfo(prefix);
    }

    if (typeof ajaxError === "undefined") {
        ajaxError = true;
        $(function () {
            $(document).ajaxError(function (event, XMLHttpRequest, ajaxOptions, thrownError) {

                //check request status
                //request.abort() has status 0, so we don't show this "error", since we have
                //explicitly aborted the request.
                //this error is documented on http://bugs.jquery.com/ticket/7189
                if (XMLHttpRequest.status === 0) return;
                ShowError(XMLHttpRequest, ajaxOptions, thrownError);
            });
        });
    }

    /* show messages on top (info, error...) */

    function NotifyError(s, t) {
        NotifyInfo(s, t, 'error');
    }

    function NotifyInfo(s, t, cssClass) {
        var $messageArea = $("#message-area"), cssClass = (cssClass != undefined ? cssClass : "info");
        if ($messageArea.length == 0) {
            //create the message container
            $messageArea = $("<div id=\"message-area\"><div class=\"message-area-text-container\"><span></span></div></div>").hide().prependTo($("body"));
        }

        $messageArea.find("span").html(s);  //write message
        $messageArea.children().first().addClass(cssClass != undefined ? cssClass : "info");    //set class
        $messageArea.css({ marginLeft: -parseInt($messageArea.outerWidth() / 2), top: 0 }).show();

        if (t != undefined) {
            var timer = setTimeout(function () {
                $messageArea.animate({ top: -30 }, "slow")
                .hide()
                .children().first().removeClass(cssClass);
                clearTimeout(timer);
                timer = null;
            }, t);
        }
    }

    function qp(name, value) {
        return "&" + name + "=" + value;
    }

    function empty(myString) {
        return (myString == undefined || myString == null || myString === "" || myString.toString() == "");
    }

    String.prototype.hasText = function () { return (this == null || this == undefined || this == '') ? false : true; }

    String.prototype.compose = function (name, separator) {
        if (empty(this))
            return name;
        if (empty(name))
            return this.toString();
        if (empty(separator))
            separator = sfSeparator;
        return this.toString() + separator + name.toString();
    }

    function isFalse(value) {
        return value == false || value == "false" || value == "False";
    }

    function isTrue(value) {
        return value == true || value == "true" || value == "True";
    }

    function GetPathPrefixes(prefix) {
        var path = [],
            pathSplit = prefix.split("_");

        for (var i = 0, l = pathSplit.length; i < l; i++)
            path[i] = concat(pathSplit, i, "_");

        for (var i = 0, l = pathSplit.length; i < l; i++)
            path[l + i] = concat(pathSplit, i, "");

        var pathNoReps = new Array();

        var hasEmpty = false;
        for (var i = 0, l = path.length; i < l; i++) {
            if ($.inArray(path[i], pathNoReps) == -1) {
                pathNoReps[i] = path[i];
                if (path[i] == "")
                    hasEmpty = true;
            }
        }
        if (!hasEmpty)
            pathNoReps[pathNoReps.length] = "";
        return pathNoReps;
    }

    function concat(array, toIndex, firstChar) {
        var path = [];
        var charToWrite = firstChar;
        for (var i = 0; i <= toIndex; i++) {
            if (array[i] != "") {
                path.push(charToWrite + array[i]);
                charToWrite = "_";
            }
        }
        return path.join('');
    }

    function Get(url) {
        document.forms[0].method = "GET";
        document.forms[0].action = url;
        document.forms[0].submit();
        return false;
    }

    function Submit(urlController, requestExtraJsonData) {
        var $form = $("form");
        if (!empty(requestExtraJsonData)) {            
            for (var key in requestExtraJsonData) {
                var str = $.isFunction(requestExtraJsonData[key]) ? requestExtraJsonData[key]() : requestExtraJsonData[key];
                $form.append(hiddenInput(key, str));
            }
        }

        $form.attr("action", urlController).submit();
        return false;
    }

    function SubmitOnly(urlController, requestExtraJsonData) {
        if (requestExtraJsonData == null)
            throw "SubmitOnly needs requestExtraJsonData. Use Submit instead";

        var $form = $("<form method='post' action='" + urlController + "'></form>");

        if (!empty(requestExtraJsonData)) {
            for (var key in requestExtraJsonData) {
                var str = $.isFunction(requestExtraJsonData[key]) ? requestExtraJsonData[key]() : requestExtraJsonData[key];
                $form.append(hiddenInput(key, str));
            }
        }

        var currentForm = $("form");
        currentForm.after($form);

        $form.submit()
            .remove();

        return false;
    }

    function hiddenInput(id, value) {
        return "<input type='hidden' id='" + id + "' name='" + id + "' value='" + value + "' />\n";
    };

    function hiddenDiv(id, innerHTML) {
        return "<div id='" + id + "' name='" + id + "' style='display:none'>" + innerHTML + "</div>\n";
    };

    /*
    s : string to replace
    tr: dictionary of translations
    */
    function replaceAll(s, tr) {
        var v = s;
        for (var t in tr) {
            v = v.split(t).join(tr[t]);
            //v=v.replace(new RegExp(t,'g'),tr[t]); //alternativa
        }
        return v;
    }

    function GetErrorMessage(response) {
        var error;
        if (response != null && response != undefined) {
            var startError = response.indexOf("<title>");
            var endError = response.indexOf("</title>");
            if ((startError != -1) && (endError != -1))
                error = response.substring(startError + 7, endError);
            else
                error = response;
        }
        return error;
    }

    function ShowError(XMLHttpRequest, textStatus, errorThrown) {
        var error = GetErrorMessage(XMLHttpRequest.responseText);
        if (!error) error = textStatus;

        var message = error.length > 50 ? error.substring(0, 49) + "..." : error;
        NotifyError(lang.signum.error + ": " + message, 2000);

        log(error);
        log(XMLHttpRequest);
        log(errorThrown);

        alert("Error: " + error);

        uiBlocked = false;
        $(".uiBlocker").remove();
    }

    var debug = true;
    function log(s) {
        if (debug) {
            if (typeof console != "undefined" && typeof console.debug != "undefined")
                console.log(s);

        }
    }

    function singleQuote(myfunction) {
        if (myfunction != null)
            return myfunction.toString().replace(/"/g, "'");
        else
            return '';
    }

    //Performs input validation
    var validator = new function () {
        this.number = function (e) {
            var c = e.keyCode;
            return (
			(c >= 48 && c <= 57) || //0-9
			(c >= 96 && c <= 105) ||  //NumPad 0-9
			(c == 8) || //BackSpace
			(c == 9) || //Tab
			(c == 12) || //Clear
			(c == 27) || //Escape
			(c == 37) || //Left
			(c == 39) || //Right
			(c == 46) || //Delete
			(c == 36) || //Home
			(c == 35) || //End
			(c == 109) || //NumPad -
			(c == 189)
		);
        };
        this.decimalNumber = function (e) {
            var c = e.keyCode;
            return (
			this.number(e) ||
			(c == 110) ||  //NumPad Decimal
            (c == 190) || //.
			(c == 188) //,
		);
        };
    };

    String.prototype.format = function (values) {
        var regex = /\{([\w-]+)(?:\:([\w\.]*)(?:\((.*?)?\))?)?\}/g;

        var getValue = function (key) {
            if (values == null || typeof values === 'undefined')
                return null;

            var value = values[key];
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

    if (typeof String.prototype.trim !== 'function') {
        String.prototype.trim = function () {
            return this.replace(/^\s+|\s+$/, '');
        }
    }

    var toggler = new 
function () {
    this.divName = function (name) {
        return "div" + name;
    };
    this.option = function (elem) {
        var value = ((elem.type == 'checkbox') ? elem.checked : elem.value === 'true');
        this.optionValue(this.divName(elem.name), value);
        if (!value) $('#' + this.divName(elem.name) + ' :input:text').each(function () {
            this.value = "";
        });
    };
    this.optionInverse = function (elem) {
        var value = ((elem.type == 'checkbox') ? !elem.checked : elem.value === 'false');
        this.optionValue(this.divName(elem.name), value);
        if (!value) $('#' + this.divName(elem.name) + ' :input:text').each(function () {
            this.value = "";
        });
    };
    this.numeric = function (id, max) {
        var name = this.divName(id);
        var value = $("#" + id).val();
        if (value == "") {
            this.optionValue(name, false);
            return;
        } else {
            value = parseInt(value);
        }
        this.optionValue(name, (value > max));
    };
    this.numericCombo = function (id, max) {
        var name = this.divName(id);
        var option = $("#" + combo + " option:selected");
        if (option.val() == "") {
            this.optionValue(name, false);
            return;
        }
        var html = option.html();
        if (html.length == 1 && html <= max.toString()) this.optionValue(name, false);
        else this.optionValue(name, true);
    };
    this.optionValue = function (name, value) {
        value ? $("#" + name).show() : $("#" + name).hide();
    };
}


    $.getScript = function (url, callback, cache, async) { $.ajax({ type: "GET", url: url, cache: true, success: callback, async: async, dataType: "script", cache: cache }); };

    var resourcesLoaded = new Array();
    $.jsLoader = function (cond, url, callback, async) {
        var a = (async != undefined) ? async : true;
        log("Retrieving from " + url + " " + (a ? "a" : "") + "synchronuosly");
        if (!resourcesLoaded[url] && cond) {
            log("Getting js " + url);
            $.getScript(url, function () {
                resourcesLoaded[url] = true;
                if (callback) callback();
            },
            true,
            a);
        }
    };
    $.cssLoader = function (cond, url) {
        if (!resourcesLoaded[url] && cond) {
            //   console.log("Getting css " + url);
            /*   jQuery( document.createElement('link') ).attr({
            href: url,
            media: media || 'screen',
            type: 'text/css',
            rel: 'stylesheet'
            }).appendTo($('head')); */
            var head = document.getElementsByTagName('head')[0];
            $(document.createElement('link'))
            .attr({ type: 'text/css', href: url, rel: 'stylesheet', media: 'screen' })
            .appendTo(head);
            resourcesLoaded[url] = true;
        }
    };

    /* forms */
    if (typeof placeholder === "undefined") {
        placeholder = true;
        $(function () {
            $('input[placeholder], textarea[placeholder]').placeholder();
        });

        (function ($) {
            $.fn.placeholder = function () {
                if ($.fn.placeholder.supported()) {
                    return $(this);
                } else {

                    $(this).parent('form').submit(function (e) {
                        $('input[placeholder].placeholder, textarea[placeholder].placeholder', this).val('');
                    });

                    $(this).each(function () {
                        $.fn.placeholder.on(this);
                    });

                    return $(this)

            .focus(function () {
                if ($(this).hasClass('placeholder')) {
                    $.fn.placeholder.off(this);
                }
            })

            .blur(function () {
                if ($(this).val() == '') {
                    $.fn.placeholder.on(this);
                }
            });
                }
            };

            // Extracted from: http://diveintohtml5.org/detect.html#input-placeholder
            $.fn.placeholder.supported = function () {
                var input = document.createElement('input');
                return !!('placeholder' in input);
            };

            $.fn.placeholder.on = function (el) {
                var $el = $(el);
                if ($el.val() == '') $el.val($el.attr('placeholder')).addClass('placeholder');
            };

            $.fn.placeholder.off = function (el) {
                $(el).val('').removeClass('placeholder');
            };
        })(jQuery);
    }

    /* functions to read / write cookies */
    function readCookie(name) {
        var nameEQ = name + "=";
        var ca = document.cookie.split(';');
        for (var i = 0; i < ca.length; i++) {
            var c = ca[i];
            while (c.charAt(0) == ' ') c = c.substring(1, c.length);
            if (c.indexOf(nameEQ) == 0) return c.substring(nameEQ.length, c.length);
        }
        return null;
    }

    function createCookie(name, value, days, domain) {
        var expires = null,
            path = "/";

        if (days) {
            var date = new Date();
            date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
            expires = date;
        }

        document.cookie = name + "=" +escape( value ) +
        ( ( expires ) ? ";expires=" + expires.toGMTString() : "" ) +
        ( ( path ) ? ";path=" + path : "" ) +
        ( ( domain ) ? ";domain=" + domain : "" );

    }

    SF.userSettings = {
        hasLocalStorage: typeof (localStorage) != 'undefined',

        read: function (key) {
            if (this.hasLocalStorage) {
                try { return localStorage.getItem(key); }
                catch (e) { }
            }
            return readCookie(key);
        },

        store: function (key, value, days) {
            if (this.hasLocalStorage) {
                try { localStorage.setItem(key, value); return true; }
                catch (e) { }
            }
            createCookie(key, value, days ? days : 30);
        }
    };

    SF.dropdowns =
    {
        opened: [],
        register: function (elem) {
            this.opened.push(elem);
        },
        closeOpened: function () {
            //close open dropdowns except elem, if it is being opened
            for (var i = 0; i < this.opened.length; i++)
                this.opened[i].toggleClass("open");
            this.opened = [];
        }
    };

    $(function () {
        $("body").click(function (e) {
            //manages close of dropdown different than current
            $(e.target).closest(".dropdown").length == 0 && SF.dropdowns.closeOpened();
        });
    });

    $(function () { $('#form input[type=text]').keypress(function (e) { return e.which != 13 }) })


}
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
    sfIndex = "sfIndex",
    sfCombo = "sfCombo",
    sfTicks = "sfTicks",

    sfItemsContainer = "sfItemsContainer",
    sfRepeaterItem = "sfRepeaterItem",

    sfBtnCancel = "sfBtnCancel",
    sfBtnOk = "sfBtnOk",

    sfQueryUrlName = "sfQueryUrlName",
    sfTop = "sfTop",
    sfAllowMultiple = "sfAllowMultiple",
    sfView = "sfView",
    sfEntityTypeName = "sfEntityTypeName",

    sfIdRelated = "sfIdRelated",
    sfRuntimeTypeRelated = "sfRuntimeTypeRelated",

    lang = {
        "error": "Error",
        "saving": "Guardando...",
        "saved": "Guardado",
        "searching": "Buscando...",
        "buscar": "Buscar",
        "executingOperation": "Ejecutando operación...",
        "operationExecuted": "Operación ejecutada",
        "noElementsSelected": "Debe seleccionar algún elemento",
        "popupErrors": "Hay errores en la entidad, ¿desea continuar?",
        "popupErrorsStop": "Hay errores en la entidad"
    };

    var StaticInfo = function(_prefix) {
        this.prefix = _prefix;
        this._staticType = 0;
        this._isEmbedded = 1;
        this._isReadOnly = 2;

        this.find = function() {
            return $('#' + this.prefix.compose(sfStaticInfo));
        };
        this.value = function() {
            return this.find().val();
        };
        this.toArray = function() {
            return this.value().split(";")
        };
        this.toValue = function(array) {
            return array.join(";");     //return array[0] + ";" + array[1] + ";" + array[2];
        };
        this.getValue = function(key) {
            var array = this.toArray();
            return array[key];
        };
        this.staticType = function() {
            return this.getValue(this._staticType);
        };
        this.isEmbedded = function() {
            return this.getValue(this._isEmbedded);
        };
        this.isReadOnly = function() {
            return this.getValue(this._isReadOnly);
        };
        this.createValue = function(staticType, isEmbedded, isReadOnly) {
            var array = new Array();
            array[this._staticType] = staticType;
            array[this._isEmbedded] = isEmbedded;
            array[this._isReadOnly] = isReadOnly;
            return this.toValue(array);
        };
    }
function StaticInfoFor(prefix) {
    return new StaticInfo(prefix);
}

var RuntimeInfo = function(_prefix) {
    this.prefix = _prefix;
    this._runtimeType = 0;
    this._id = 1;
    this._isNew = 2;
    this._ticks = 3;

    this.find = function() {
    return $('#' + this.prefix.compose(sfRuntimeInfo));
    };
    this.value = function() {
        return this.find().val();
    };
    this.toArray = function() {
        return this.value().split(";")
    };
    this.toValue = function(array) {
        return array.join(";"); //return array[0] + ";" + array[1] + ";" + array[2] + ";" + array[3];
    };
    this.getSet = function(key, val) {
        var array = this.toArray();
        if (val == undefined)
            return array[key];

        array[key] = val;
        this.find().val(this.toValue(array));
        return this;
    };
    this.runtimeType = function() {
        return this.getSet(this._runtimeType, null);
    };
    this.id = function() {
        return this.getSet(this._id, null);
    };
    this.isNew = function() {
        return this.getSet(this._isNew, null);
    };
    this.ticks = function(val) {
        return this.getSet(this._ticks, val);
    };
    this.setEntity = function(runtimeType, id) {
        this.getSet(this._runtimeType, runtimeType);
        if (empty(id))
            this.getSet(this._id, '').getSet(this._isNew, 'n');
        else
            this.getSet(this._id, id).getSet(this._isNew, 'o');
        return this;
    };
    this.removeEntity = function() {
        this.getSet(this._runtimeType, '');
        this.getSet(this._id, '');
        this.getSet(this._isNew, 'o');
        return this;
    };
    this.createValue = function(runtimeType, id, isNew, ticks) {
        var array = new Array();
        array[this._runtimeType] = runtimeType;
        array[this._id] = id;
        array[this._isNew] = isNew;
        array[this._ticks] = ticks;
        return this.toValue(array);
    };
}
function RuntimeInfoFor(prefix) {
    return new RuntimeInfo(prefix);
}

$(function() {
    $(document).ajaxError(function(event, XMLHttpRequest, ajaxOptions, thrownError) {
        ShowError(XMLHttpRequest, ajaxOptions, thrownError);
    });
});

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
        var timer = setTimeout(function() {
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

String.prototype.hasText = function() { return (this == null || this == undefined || this == '') ? false : true; }

String.prototype.compose = function(name, separator) {
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
    var path = new Array();
    var pathSplit = prefix.split("_");

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
    var path = "";
    var charToWrite = firstChar;
    for (var i = 0; i <= toIndex; i++) {
        if (array[i] != "") {
            path += charToWrite + array[i];
            charToWrite = "_";
        }
    }
    return path;
}

function Submit(urlController, requestExtraJsonData) {
    if (!empty(requestExtraJsonData)) {
        var $form = $("form");
        for (var key in requestExtraJsonData) {
            var str = $.isFunction(requestExtraJsonData[key]) ? requestExtraJsonData[key]() : requestExtraJsonData[key];
            
            $form.append(hiddenInput(key, str));
        }
    }

    document.forms[0].action = urlController;
    document.forms[0].submit();
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
function ShowError(XMLHttpRequest, textStatus, errorThrown) {
    var error;
    if (XMLHttpRequest.responseText != null && XMLHttpRequest.responseText != undefined) {
        var startError = XMLHttpRequest.responseText.indexOf("<title>");
        var endError = XMLHttpRequest.responseText.indexOf("</title>");
        if ((startError != -1) && (endError != -1))
            error = XMLHttpRequest.responseText.substring(startError + 7, endError);
        else
            error = XMLHttpRequest.responseText;
    }
    else {
        error = textStatus;
    }
    
    var message = error.length > 50 ? error.substring(0,49) + "..." : error;
    NotifyError(lang['error'] + ": " + message, 2000);

    alert("Error: " + error);
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
var validator = new function() {
    this.number = function(e) {
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
    this.decimalNumber = function(e) {
        var c = e.keyCode;
        return (
			this.number(e) ||
			(c == 110) ||  //NumPad Decimal
            (c == 190) || //.
			(c == 188) //,
		);
    };
};

String.prototype.format = function(values) {
    var regex = /\{([\w-]+)(?:\:([\w\.]*)(?:\((.*?)?\))?)?\}/g;

    var getValue = function(key) {
        if (values == null || typeof values === 'undefined')
            return null;

        var value = values[key];
        var type = typeof value;

        return type === 'string' || type === 'number' ? value : null;
    };

    return this.replace(regex, function(match) {
        //match will look like {sample-match}
        //key will be 'sample-match';
        var key = match.substr(1, match.length - 2);

        var value = getValue(key);

        return value != null ? value : match;
    });
};

if (typeof String.prototype.trim !== 'function') {
    String.prototype.trim = function() {
        return this.replace(/^\s+|\s+$/, '');
    }
}

var toggler = new 
function() {
    this.divName = function(name) {
        return "div" + name;
    };
    this.option = function(elem) {
        var value = ((elem.type == 'checkbox') ? elem.checked : elem.value === 'true');
        this.optionValue(this.divName(elem.name), value);
        if (!value) $('#' + this.divName(elem.name) + ' :input:text').each(function() {
            this.value = "";
        });
    };
    this.optionInverse = function(elem) {
        var value = ((elem.type == 'checkbox') ? !elem.checked : elem.value === 'false');
        this.optionValue(this.divName(elem.name), value);
        if (!value) $('#' + this.divName(elem.name) + ' :input:text').each(function() {
            this.value = "";
        });
    };
    this.numeric = function(id, max) {
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
    this.numericCombo = function(id, max) {
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
    this.optionValue = function(name, value) {
        value ? $("#" + name).show() : $("#" + name).hide();
    };
}


$.getScript = function(url, callback, cache, async){ $.ajax({ type: "GET", url: url, cache:true, success: callback, async: async, dataType: "script", cache: cache }); }; 

var resourcesLoaded = new Array();
$.jsLoader = function(cond, url, callback, async) {
    var a = (async != undefined) ? async : true;
    log("Retrieving from " + url + " " + (a ? "a" : "") + "synchronuosly");
    if (!resourcesLoaded[url] && cond) {
         log("Getting js " + url);
         $.getScript(url, function() {
            resourcesLoaded[url]=true;
            if (callback) callback();
            },
            true,
            a);
        }
};
$.cssLoader = function(cond, url) {
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
            .attr({type: 'text/css', href: url, rel: 'stylesheet', media: 'screen'})
            .appendTo(head);                 
        resourcesLoaded[url]=true;
    }
};

/* forms */
$(function() {
    $('input[placeholder], textarea[placeholder]').placeholder();
});

(function($) {
    $.fn.placeholder = function() {
        if ($.fn.placeholder.supported()) {
            return $(this);
        } else {

            $(this).parent('form').submit(function(e) {
            $('input[placeholder].placeholder, textarea[placeholder].placeholder', this).val('');
            });

            $(this).each(function() {
                $.fn.placeholder.on(this);
            });

            return $(this)

        .focus(function() {
            if ($(this).hasClass('placeholder')) {
                $.fn.placeholder.off(this);
            }
        })

        .blur(function() {
            if ($(this).val() == '') {
                $.fn.placeholder.on(this);
            }
        });
        }
    };

    // Extracted from: http://diveintohtml5.org/detect.html#input-placeholder
    $.fn.placeholder.supported = function() {
        var input = document.createElement('input');
        return !!('placeholder' in input);
    };

    $.fn.placeholder.on = function(el) {
        var $el = $(el);
        $el.val($el.attr('placeholder')).addClass('placeholder');
    };

    $.fn.placeholder.off = function(el) {
        $(el).val('').removeClass('placeholder');
    };
})(jQuery);

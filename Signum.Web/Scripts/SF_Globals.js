var sfPrefix = "prefix";
var sfPrefixToIgnore = "prefixToIgnore";
var sfReactive = "sfReactive";
var sfFieldErrorClass = "field-validation-error";
var sfInputErrorClass = "input-validation-error";
var sfSummaryErrorClass = "validation-summary-errors";
var sfInlineErrorVal = "inlineVal";
var sfGlobalErrorsKey = "sfGlobalErrors";
var sfGlobalValidationSummary = "sfGlobalValidationSummary";
var sfDivASustituir = "divASustituir";

var sfInfo = "_sfInfo";
var sfStaticType = "_sfStaticType";
var sfRuntimeType = "_sfRuntimeType";
var sfImplementations = "_sfImplementations";
var sfImplementationsDDL = "_sfImplementationsDDL";
var sfId = "_sfId";
var sfEntity = "_sfEntity";
//var sfEntityTemp = "_sfEntityTemp";
var sfToStr = "_sfToStr";
var sfLink = "_sfLink";
var sfIsNew = "_sfIsNew";
var sfIndex = "_sfIndex";
var sfCombo = "_sfCombo";
var sfTicks = "_sfTicks";

var sfItemsContainer = "_sfItemsContainer";
var sfRepeaterItem = "_sfRepeaterItem";

var sfBtnCancel = "sfBtnCancel";
var sfBtnOk = "sfBtnOk";
var sfBtnCancelS = "sfBtnCancelS";
var sfBtnOkS = "sfBtnOkS";

var sfQueryUrlName = "sfQueryUrlName";
var sfTop = "sfTop";
var sfAllowMultiple = "sfAllowMultiple";
var sfSearchOnLoad = "sfSearchOnLoad";
var sfEntityTypeName = "sfEntityTypeName";
var sfSuffix = "sfSuffix";

var sfEmbeddedControl = "sfEmbeddedControl";
var sfTabId = "sfTabId";

var sfIdRelated = "sfIdRelated";
var sfRuntimeTypeRelated = "sfRuntimeTypeRelated";

var lang = {
    "error": "Error",
    "saving": "Guardando...",
    "saved": "Guardado",
    "searching": "Buscando...",
    "buscar": "Buscar",
    "executingOperation": "Ejecutando operación...",
    "operationExecuted": "Operación ejecutada",
    "popupErrors": "Hay errores en la entidad, ¿desea continuar?",
    "popupErrorsStop": "Hay errores en la entidad"
};

var EntityInfo = function(_prefix) {
    this.prefix = _prefix;
    this._staticType = 0;
    this._runtimeType = 1;
    this._id = 2;
    this._isEmbedded = 3;
    this._isNew = 4;
    this._ticks = 5;

    this.find = function() {
        return $('#' + this.prefix + sfInfo);
    };
    this.value = function() {
        return this.find().val();
    };
    this.toArray = function() {
        return this.value().split(";")
    };
    this.toValue = function(array) {
        return array[0] + ";" + array[1] + ";" + array[2] + ";" + array[3] + ";" + array[4] + ";" + array[5];
    };
    this.getSet = function(key, val) {
        var array = this.toArray();
        if (val == undefined)
            return array[key];

        array[key] = val;
        this.find().val(this.toValue(array));
        return this;
    };
    this.staticType = function() {
        return this.getSet(this._staticType, null);
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
    this.isEmbedded = function() {
        return this.getSet(this._isEmbedded, null);
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
    this.createValue = function(staticType, runtimeType, id, isEmbedded, isNew, ticks) {
        var array = new Array();
        array[this._staticType] = staticType;
        array[this._runtimeType] = runtimeType;
        array[this._id] = id;
        array[this._isEmbedded] = isEmbedded;
        array[this._isNew] = isNew;
        array[this._ticks] = ticks;
        return this.toValue(array);
    };
}
function EntityInfoFor(prefix) {
    return new EntityInfo(prefix);
}


$().ajaxError(function(event, XMLHttpRequest, ajaxOptions, thrownError) {
    ShowError(XMLHttpRequest, ajaxOptions, thrownError);
});

function NotifyError(s, t) {
    NotifyInfo(s, t);
}
function NotifyInfo(s, t) {
    $("#loading-area-text").html(s);
    //$("#loading-area-text").css({left: parseInt(document.documentElement.clientWidth - $("#loading-area").outerWidth() / 2) + "px"});
    $("#loading-area").css({ marginLeft: -parseInt($("#loading-area").outerWidth() / 2) + "px" });
    $("#loading-area").show();
    if (t != undefined) {
        var timer = setTimeout(function() {
            $("#loading-area").fadeOut("slow");
            clearTimeout(timer);
            timer = null;
        }, t);
    }
}

function qp(name, value) {
    return "&" + name + "=" + value;
}

function empty(myString) {
    return (myString == undefined || myString == "");
}

String.prototype.hasText = function() { return (this == null || this == undefined || this == '') ? false : true; }

function isFalse(value) {
    return value == false || value == "false" || value == "False";
}

function GetPathPrefixes(prefix) {
    var path = new Array();
    var pathSplit = prefix.split("_");
    for (var i = 0; i < pathSplit.length; i++)
        path[i] = concat(pathSplit, i);
    return path;
}

function concat(array, toIndex) {
    var path = "";
    for (var i = 0; i <= toIndex; i++) {
        if (array[i] != "")
            path += "_" + array[i];
    }
    return path;
}

function PostServer(urlController) {
    document.forms[0].action = urlController;
    document.forms[0].submit();
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
    NotifyError(lang['error'], 2000);
    /* error = replaceAll(error,
    {'&#237;' : 'í',
    '&#243;' : 'ó' });*/
    alert("Error: " + error);

}

// establece clase "focused" al div alrededor del campo con foco
function initAroundDivs() {
    $('.valueLine,.rbValueLine').each(function() {
        if (this.id != undefined && this.id != null && this.id != "") {
            var elementID = $("#" + this.id);
            var around = elementID.parents('div[class=around]');
            if (around.length > 0) {
                elementID.focus(function() { around.addClass('focused'); });
                elementID.blur(function() { around.removeClass('focused'); });
            }
        }
    });
}



$(function() { initAroundDivs(); });

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


$.getScript = function(url, callback, cache){ $.ajax({ type: "GET", url: url, success: callback, dataType: "script", cache: cache }); }; 

var resourcesLoaded = new Array();
$.jsLoader = function(cond, url, callback) {
    if (!resourcesLoaded[url] && cond) {
         //console.log("Getting js " + url);
         $.getScript(url, function() {resourcesLoaded[url]=true; if (callback) callback();}, true);
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
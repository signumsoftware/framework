"use strict";

if (!SF)
    throw "SF.Utils not initialized";

SF.registerModule("Globals", function () {

    SF.Keys = {
        separator: "_",
        tabId: "sfTabId",
        antiForgeryToken: "__RequestVerificationToken",

        entityTypeNames: "sfEntityTypeNames",

        runtimeInfo: "sfRuntimeInfo",
        staticInfo: "sfStaticInfo",
        toStr: "sfToStr",
        link: "sfLink"
    };

    SF.StaticInfo = function (_prefix) {
        var prefix = _prefix,
			_types = 0,
            _isEmbedded = 1,
			_isReadOnly = 2,
			$elem; 			//cache for the element

        var find = function () {
            if (!$elem) {
                $elem = $('#' + SF.compose(prefix, SF.Keys.staticInfo));
            }
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

        var singleType = function () {
            var typeArray = types().split(',');
            if (typeArray.length !== 1) {
                throw "types should have only one element for element {0}".format(prefix);
            }
            return typeArray[0];
        };

        var types = function () {
            return getValue(_types);
        };

        var isEmbedded = function () {
            return getValue(_isEmbedded) == "e";
        };

        var isReadOnly = function () {
            return getValue(_isReadOnly) == "r";
        };

        var createValue = function (types, isEmbedded, isReadOnly) {
            var array = [];
            array[_types] = types;
            array[_isEmbedded] = isEmbedded ? "e" : "i";
            array[_isReadOnly] = isReadOnly ? "r" : "";
            return toValue(array);
        };

        return {
            types: types,
            singleType: singleType,
            isEmbedded: isEmbedded,
            isReadOnly: isReadOnly,
            createValue: createValue,
            find: find
        };
    };

    SF.RuntimeInfo = function (_prefix) {
        var prefix = _prefix;
        var _entityType = 0;
        var _id = 1;
        var _isNew = 2;
        var $elem; 			//cache for the element

        var find = function () {
            if (!$elem) {
                $elem = $('#' + SF.compose(prefix, SF.Keys.runtimeInfo));
            }
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
            if (val === undefined) {
                return array[key];
            }
            array[key] = val;
            find().val(toValue(array));
        };
        var entityType = function () {
            return getSet(_entityType);
        };
        var id = function () {
            return getSet(_id);
        };
        var isNew = function () {
            return getSet(_isNew);
        };
        var setEntity = function (entityType, id) {
            getSet(_entityType, entityType);
            if (SF.isEmpty(id)) {
                getSet(_id, '');
                getSet(_isNew, 'n');
            }
            else {
                getSet(_id, id);
                getSet(_isNew, 'o');
            }
        };
        var removeEntity = function () {
            getSet(_entityType, '');
            getSet(_id, '');
            getSet(_isNew, 'o');
        };
        var createValue = function (entityType, id, isNew) {
            var array = [];
            array[_entityType] = entityType;
            array[_id] = id;
            if (SF.isEmpty(isNew)) {
                array[_isNew] = SF.isEmpty(id) ? "n" : "o";
            }
            else {
                array[_isNew] = isNew;
            }
            return toValue(array);
        };

        return {
            entityType: entityType,
            id: id,
            isNew: isNew,
            setEntity: setEntity,
            removeEntity: removeEntity,
            createValue: createValue,
            find: find,
            getSet: getSet,
            value: value
        };
    };

    SF.Serializer = function () {
        var result = "";

        var concat = function (value) {
            if (result === "") {
                result = value;
            } else {
                result += "&" + value;
            }
        };

        this.add = function (param, value) {
            if (typeof param === "string") {
                if (value === undefined) {
                    concat(param);
                } else {
                    concat(param + "=" + value);
                }
            }
            else if ($.isFunction(param)) {
                var data = param();
                //json
                for (var key in data) {
                    if (data.hasOwnProperty(key)) {
                        var value = data[key];
                        concat(key + "=" + value);
                    }
                }
            }
            else {
                //json
                for (var key in param) {
                    if (param.hasOwnProperty(key)) {
                        var value = param[key];
                        concat(key + "=" + value);
                    }
                }
            }
            return this;
        };

        this.serialize = function () {
            return result;
        };
    };

    SF.compose = function (str1, str2, separator) {
        if (typeof (str1) !== "string" && str1 !== null && str1 != undefined) {
            throw "str1 " + str1 + " is not a string";
        }

        if (typeof (str2) !== "string" && str2 !== null && str2 != undefined) {
            throw "str2 " + str2 + " is not a string";
        }

        if (SF.isEmpty(str1)) {
            return str2;
        }

        if (SF.isEmpty(str2)) {
            return str1;
        }

        if (SF.isEmpty(separator)) {
            separator = SF.Keys.separator;
        }

        return str1 + separator + str2;
    };

    SF.cloneContents = function (sourceContainerId) {
        var $source = $('#' + sourceContainerId);
        var $clone = $source.children().clone(true);

        var $sourceSelect = $source.find("select");
        var $cloneSelect = $clone.find("select");

        for (var i = 0, l = $sourceSelect.length; i < l; i++) {
            $cloneSelect.eq(i).val($sourceSelect.eq(i).val());
        }

        return $clone;
    };

    SF.getPathPrefixes = function (prefix) {
        var path = [],
            pathSplit = prefix.split("_");

        for (var i = 0, l = pathSplit.length; i < l; i++)
            path[i] = pathSplit.slice(0, i).join("_");

        return path;
    };

    SF.submit = function (urlController, requestExtraJsonData, $form) {
        $form = $form || $("form");
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

        $form.attr("action", urlController)[0].submit();
        return false;
    };

    SF.submitOnly = function (urlController, requestExtraJsonData) {
        if (requestExtraJsonData == null)
            throw "SubmitOnly needs requestExtraJsonData. Use Submit instead";

        var $form = $("<form />",
        { method: 'post',
            action: urlController
        });

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

    SF.hiddenInput = function (id, value) {
        return "<input type='hidden' id='" + id + "' name='" + id + "' value='" + value + "' />\n";
    };

    SF.hiddenDiv = function (id, innerHtml) {
        return "<div id='" + id + "' name='" + id + "' style='display:none'>" + innerHtml + "</div>";
    };

    SF.Dropdowns =
    {
        toggle: function (event, elem, topFix) {
            var $elem = $(elem),
                clss = "sf-open";

            if (!$elem.hasClass("sf-dropdown")) {
                $elem = $elem.closest(".sf-dropdown");
            }

            var opened = $elem.hasClass(clss);
            if (opened) {      
                $elem.removeClass(clss);
            }
            else {
                //topFix is used to correct top when the toggler element is inside another panel with borders or anything
                if (typeof topFix == "undefined") {
                    topFix = 0;
                }

                $(".sf-dropdown").removeClass(clss);
                var $content = $elem.find(".sf-menu-button");
                var left = $elem.width() - $content.width(); 
                $content.css({
                    top: $elem.outerHeight() + topFix,
                    left: ($elem.position().left - $elem.parents("div").first().position().left) < Math.abs(left) ? 0 : left
                });
                $elem.addClass(clss);
            }

            SF.stopPropagation(event);
        }
    };

    SF.Blocker = (function () {

        var blocked = false,
            $elem;

        function isEnabled() {
            return blocked;
        }

        function enable() {
            blocked = true;
            $elem =
                $("<div/>", {
                    "class": "sf-ui-blocker",
                    "width": "300%",
                    "height": "300%"
                }).appendTo($("body"));
        }

        function disable() {
            blocked = false;
            $elem.remove();
        }

        return {
            isEnabled: isEnabled,
            enable: enable,
            disable: disable
        };

    })();


    $(function () {
        $(document).on("click", function (e) {
            $(".sf-dropdown").removeClass("sf-open");
        });
    });

    $(function () { $('#form input[type=text]').keypress(function (e) { return e.which != 13 }) });

    $(function () {
        $("body").bind("sf-ajax-error", function (event, XMLHttpRequest, textStatus, thrownError) {

            var error = XMLHttpRequest.responseText;
            if (!error) {
                error = textStatus;
            }

            var message = error.length > 50 ? error.substring(0, 49) + "..." : error;
            SF.Notify.error(lang.signum.error + ": " + message, 2000);

            SF.log(error);
            SF.log(XMLHttpRequest);
            SF.log(thrownError);

            alert(lang.signum.error + ": " + error);
            if (SF.Blocker.isEnabled()) {
                SF.Blocker.disable();
            }
        });
    })
});

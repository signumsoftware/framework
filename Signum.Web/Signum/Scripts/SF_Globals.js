/// <reference path="SF_Utils.ts"/>
var SF;
(function (SF) {
    var StaticInfo = (function () {
        function StaticInfo(prefix) {
            this.prefix = prefix;
        }
        StaticInfo.prototype.find = function () {
            if (!this.$elem) {
                this.$elem = $('#' + SF.compose(this.prefix, SF.Keys.staticInfo));
            }
            return this.$elem;
        };

        StaticInfo.prototype.value = function () {
            return this.find().val();
        };

        StaticInfo.prototype.toArray = function () {
            return this.value().split(";");
        };

        StaticInfo.prototype.toValue = function (array) {
            return array.join(";");
        };

        StaticInfo.prototype.getValue = function (key) {
            var array = this.toArray();
            return array[key];
        };

        StaticInfo.prototype.singleType = function () {
            var typeArray = this.types().split(',');
            if (typeArray.length !== 1) {
                throw "types should have only one element for element {0}".format(this.prefix);
            }
            return typeArray[0];
        };

        StaticInfo.prototype.types = function () {
            return this.getValue(StaticInfo._types);
        };

        StaticInfo.prototype.isEmbedded = function () {
            return this.getValue(StaticInfo._isEmbedded) == "e";
        };

        StaticInfo.prototype.isReadOnly = function () {
            return this.getValue(StaticInfo._isReadOnly) == "r";
        };

        StaticInfo.prototype.rootType = function () {
            return this.getValue(StaticInfo._rootType);
        };

        StaticInfo.prototype.propertyRoute = function () {
            return this.getValue(StaticInfo._propertyRoute);
        };

        StaticInfo.prototype.createValue = function (types, isEmbedded, isReadOnly, rootType, propertyRoute) {
            var array = [];
            array[StaticInfo._types] = types;
            array[StaticInfo._isEmbedded] = isEmbedded ? "e" : "i";
            array[StaticInfo._isReadOnly] = isReadOnly ? "r" : "";
            array[StaticInfo._rootType] = rootType;
            array[StaticInfo._propertyRoute] = propertyRoute;
            return this.toValue(array);
        };
        StaticInfo._types = 0;
        StaticInfo._isEmbedded = 1;
        StaticInfo._isReadOnly = 2;
        StaticInfo._rootType = 3;
        StaticInfo._propertyRoute = 4;
        return StaticInfo;
    })();
    SF.StaticInfo = StaticInfo;

    var RuntimeInfo = (function () {
        function RuntimeInfo(prefix) {
            this.prefix = prefix;
        }
        RuntimeInfo.prototype.find = function () {
            if (!this.$elem) {
                this.$elem = $('#' + SF.compose(this.prefix, SF.Keys.runtimeInfo));
            }
            return this.$elem;
        };
        RuntimeInfo.prototype.value = function () {
            return this.find().val();
        };
        RuntimeInfo.prototype.toArray = function () {
            return this.value().split(";");
        };
        RuntimeInfo.prototype.toValue = function (array) {
            return array.join(";");
        };
        RuntimeInfo.prototype.getSet = function (key, val) {
            var array = this.toArray();
            if (val === undefined) {
                return array[key];
            }
            array[key] = val;
            this.find().val(this.toValue(array));
        };
        RuntimeInfo.prototype.entityType = function () {
            return this.getSet(RuntimeInfo._entityType);
        };
        RuntimeInfo.prototype.id = function () {
            return this.getSet(RuntimeInfo._id);
        };
        RuntimeInfo.prototype.isNew = function () {
            return this.getSet(RuntimeInfo._isNew);
        };
        RuntimeInfo.prototype.ticks = function () {
            return this.getSet(RuntimeInfo._ticks);
        };
        RuntimeInfo.prototype.setEntity = function (entityType, id) {
            this.getSet(RuntimeInfo._entityType, entityType);
            if (SF.isEmpty(id)) {
                this.getSet(RuntimeInfo._id, '');
                this.getSet(RuntimeInfo._isNew, 'n');
            } else {
                this.getSet(RuntimeInfo._id, id);
                this.getSet(RuntimeInfo._isNew, 'o');
            }
        };
        RuntimeInfo.prototype.removeEntity = function () {
            this.getSet(RuntimeInfo._entityType, '');
            this.getSet(RuntimeInfo._id, '');
            this.getSet(RuntimeInfo._isNew, 'o');
        };
        RuntimeInfo.prototype.createValue = function (entityType, id, isNew, ticks) {
            var array = [];
            array[RuntimeInfo._entityType] = entityType;
            array[RuntimeInfo._id] = id;
            if (SF.isEmpty(isNew)) {
                array[RuntimeInfo._isNew] = SF.isEmpty(id) ? "n" : "o";
            } else {
                array[RuntimeInfo._isNew] = isNew;
            }
            array[RuntimeInfo._ticks] = ticks;
            return this.toValue(array);
        };
        RuntimeInfo._entityType = 0;
        RuntimeInfo._id = 1;
        RuntimeInfo._isNew = 2;
        RuntimeInfo._ticks = 3;
        return RuntimeInfo;
    })();
    SF.RuntimeInfo = RuntimeInfo;
})(SF || (SF = {}));

var SF;
(function (SF) {
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

    var Serializer = (function () {
        function Serializer() {
        }
        Serializer.prototype.concat = function (value) {
            if (this.result === "") {
                this.result = value;
            } else {
                this.result += "&" + value;
            }
        };

        Serializer.prototype.add = function (param, value) {
            if (typeof param === "string") {
                if (value === undefined) {
                    this.concat(param);
                } else {
                    this.concat(param + "=" + value);
                }
            } else if ($.isFunction(param)) {
                var data = param();

                for (var key in data) {
                    if (data.hasOwnProperty(key)) {
                        var value = data[key];
                        this.concat(key + "=" + value);
                    }
                }
            } else {
                for (var key in param) {
                    if (param.hasOwnProperty(key)) {
                        var value = param[key];
                        this.concat(key + "=" + value);
                    }
                }
            }
            return this;
        };

        Serializer.prototype.serialize = function () {
            return this.result;
        };
        return Serializer;
    })();
    SF.Serializer = Serializer;

    function compose(str1, str2, separator) {
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
    }
    SF.compose = compose;

    function cloneContents(sourceContainerId) {
        var $source = $('#' + sourceContainerId);
        var $clone = $source.children().clone(true);

        var $sourceSelect = $source.find("select");
        var $cloneSelect = $clone.find("select");

        for (var i = 0, l = $sourceSelect.length; i < l; i++) {
            $cloneSelect.eq(i).val($sourceSelect.eq(i).val());
        }

        return $clone;
    }
    SF.cloneContents = cloneContents;

    function getPathPrefixes(prefix) {
        var path = [], pathSplit = prefix.split("_");

        for (var i = 0, l = pathSplit.length; i < l; i++)
            path[i] = pathSplit.slice(0, i).join("_");

        return path;
    }
    SF.getPathPrefixes = getPathPrefixes;

    function submit(urlController, requestExtraJsonData, $form) {
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
    }
    SF.submit = submit;
    ;

    function submitOnly(urlController, requestExtraJsonData) {
        if (requestExtraJsonData == null)
            throw "SubmitOnly needs requestExtraJsonData. Use Submit instead";

        var $form = $("<form />", {
            method: 'post',
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
    SF.submitOnly = submitOnly;

    function hiddenInput(id, value) {
        return "<input type='hidden' id='" + id + "' name='" + id + "' value='" + value + "' />\n";
    }
    SF.hiddenInput = hiddenInput;

    function hiddenDiv(id, innerHtml) {
        return "<div id='" + id + "' name='" + id + "' style='display:none'>" + innerHtml + "</div>";
    }
    SF.hiddenDiv = hiddenDiv;

    var Dropdowns;
    (function (Dropdowns) {
        function toggle(event, elem, topFix) {
            var $elem = $(elem), clss = "sf-open";

            if (!$elem.hasClass("sf-dropdown")) {
                $elem = $elem.closest(".sf-dropdown");
            }

            var opened = $elem.hasClass(clss);
            if (opened) {
                $elem.removeClass(clss);
            } else {
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
        Dropdowns.toggle = toggle;
    })(Dropdowns || (Dropdowns = {}));

    var Blocker;
    (function (Blocker) {
        var blocked = false;
        var $elem;

        function isEnabled() {
            return blocked;
        }
        Blocker.isEnabled = isEnabled;

        function enable() {
            blocked = true;
            $elem = $("<div/>", {
                "class": "sf-ui-blocker",
                "width": "300%",
                "height": "300%"
            }).appendTo($("body"));
        }
        Blocker.enable = enable;

        function disable() {
            blocked = false;
            $elem.remove();
        }
        Blocker.disable = disable;
    })(Blocker || (Blocker = {}));

    $(function () {
        $(document).on("click", function (e) {
            $(".sf-dropdown").removeClass("sf-open");
        });
    });

    $(function () {
        $('#form input[type=text]').keypress(function (e) {
            return e.which != 13;
        });
    });

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
            if (Blocker.isEnabled()) {
                Blocker.disable();
            }
        });
    });
})(SF || (SF = {}));

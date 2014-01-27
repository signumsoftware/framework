/// <reference path="references.ts"/>
var __extends = this.__extends || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};
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
            var typeArray = this.types();
            if (typeArray.length !== 1) {
                throw "types should have only one element for element {0}".format(this.prefix);
            }
            return typeArray[0];
        };

        StaticInfo.prototype.types = function () {
            return this.getValue(StaticInfo._types).split(',');
        };

        StaticInfo.prototype.typeNiceNames = function () {
            return this.getValue(StaticInfo._typeNiceNames).split(',');
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
        StaticInfo._types = 0;
        StaticInfo._typeNiceNames = 1;
        StaticInfo._isEmbedded = 2;
        StaticInfo._isReadOnly = 3;
        StaticInfo._rootType = 4;
        StaticInfo._propertyRoute = 5;
        return StaticInfo;
    })();
    SF.StaticInfo = StaticInfo;

    var EntityHtml = (function (_super) {
        __extends(EntityHtml, _super);
        function EntityHtml(prefix, runtimeInfo, toString, link) {
            _super.call(this, runtimeInfo, toString, link);

            if (this.prefix == null)
                throw new Error("prefix is mandatory for EntityHtml");
        }
        EntityHtml.prototype.assertPrefixAndType = function (prefix, staticInfo) {
            _super.prototype.assertPrefixAndType.call(this, prefix, staticInfo);

            if (this.prefix != null && this.prefix != prefix)
                throw Error("EntityHtml prefix should be {0} instead of  {1}".format(prefix, this.prefix));
        };

        EntityHtml.prototype.isLoaded = function () {
            return this.html != null && this.html.length != 0;
        };

        EntityHtml.fromHtml = function (prefix, html) {
            var result = new EntityHtml(prefix, new RuntimeInfoValue("?", null));
            result.html = $(html);
            return result;
        };
        return EntityHtml;
    })(EntityValue);
    SF.EntityHtml = EntityHtml;

    var EntityValue = (function () {
        function EntityValue(runtimeInfo, toString, link) {
            if (runtimeInfo == null)
                throw new Error("runtimeInfo is mandatory for an EntityValue");

            this.runtimeInfo = runtimeInfo;
            this.toStr = toString;
            this.link = link;
        }
        EntityValue.prototype.assertPrefixAndType = function (prefix, staticInfo) {
            var types = staticInfo.types();

            if (types.length == 0 && types[0] == "[All]")
                return;

            if (types.indexOf(this.runtimeInfo.type) == -1)
                throw new Error("{0} not found in types {1}".format(this.runtimeInfo.type, types.join(", ")));
        };

        EntityValue.prototype.isLoaded = function () {
            return false;
        };
        return EntityValue;
    })();
    SF.EntityValue = EntityValue;

    var RuntimeInfoValue = (function () {
        function RuntimeInfoValue(entityType, id, isNew, ticks) {
            if (SF.isEmpty(entityType))
                throw new Error("entityTyp is mandatory for RuntimeInfoValue");

            this.type = entityType;
            this.id = id;
            this.isNew = isNew;
            this.ticks = ticks;
        }
        RuntimeInfoValue.parse = function (runtimeInfoString) {
            if (SF.isEmpty(runtimeInfoString))
                return null;

            var array = runtimeInfoString.split(',');
            return new RuntimeInfoValue(array[0], SF.isEmpty(array[1]) ? null : parseInt(array[1]), array[2] == "n", SF.isEmpty(array[3]) ? null : parseInt(array[3]));
        };

        RuntimeInfoValue.prototype.toString = function () {
            return [
                this.type,
                this.id,
                this.isNew ? "n" : "o",
                this.ticks].join(";");
        };

        RuntimeInfoValue.fromKey = function (key) {
            if (SF.isEmpty(key))
                return null;

            var array = key.split(',');
            return new RuntimeInfoValue(array[0], parseInt(array[1]), false, null);
        };

        RuntimeInfoValue.prototype.key = function () {
            if (this.id == null)
                throw Error("RuntimeInfoValue has no Id");

            return this.type + ";" + this.id;
        };
        return RuntimeInfoValue;
    })();
    SF.RuntimeInfoValue = RuntimeInfoValue;

    var RuntimeInfoElement = (function () {
        function RuntimeInfoElement(prefix) {
            this.prefix = prefix;
        }
        RuntimeInfoElement.prototype.getElem = function () {
            if (!this.$elem) {
                this.$elem = $('#' + SF.compose(this.prefix, SF.Keys.runtimeInfo));
            }
            return this.$elem;
        };

        RuntimeInfoElement.prototype.value = function () {
            return RuntimeInfoValue.parse(this.getElem().val());
        };

        RuntimeInfoElement.prototype.setValue = function (runtimeInfo) {
            this.getElem().val(runtimeInfo == null ? null : runtimeInfo.toString());
        };
        return RuntimeInfoElement;
    })();
    SF.RuntimeInfoElement = RuntimeInfoElement;

    SF.Keys = {
        separator: "_",
        tabId: "sfTabId",
        antiForgeryToken: "__RequestVerificationToken",
        entityTypeNames: "sfEntityTypeNames",
        runtimeInfo: "sfRuntimeInfo",
        staticInfo: "sfStaticInfo",
        toStr: "sfToStr",
        link: "sfLink",
        loading: "loading",
        entityState: "sfEntityState"
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
        return cloneWithValues($('#' + sourceContainerId).children());
    }
    SF.cloneContents = cloneContents;

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
            if ($.isFunction(requestExtraJsonData))
                requestExtraJsonData = requestExtraJsonData();

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
        return $("<div id='" + id + "' style='display:none'></div>").html(innerHtml);
    }
    SF.hiddenDiv = hiddenDiv;

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
        Dropdowns.toggle = toggle;
    })(SF.Dropdowns || (SF.Dropdowns = {}));
    var Dropdowns = SF.Dropdowns;

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

        function wrap(promise) {
            if (blocked)
                return promise();

            enable();

            return promise().then(function (val) {
                disable();
                return val;
            }).catch(function (err) {
                disable();
                throw err;
                return null;
            });
        }
        Blocker.wrap = wrap;
    })(SF.Blocker || (SF.Blocker = {}));
    var Blocker = SF.Blocker;

    once("closeDropDowns", function () {
        return $(function () {
            $(document).on("click", function (e) {
                $(".sf-dropdown").removeClass("sf-open");
            });
        });
    });

    once("removeKeyPress", function () {
        return $(function () {
            $('#form input[type=text]').keypress(function (e) {
                return e.which != 13;
            });
        });
    });

    once("ajaxError", function () {
        return $(function () {
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
    });
})(SF || (SF = {}));
//# sourceMappingURL=SF_Globals.js.map

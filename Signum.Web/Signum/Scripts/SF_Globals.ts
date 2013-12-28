/// <reference path="references.ts"/>

module SF
{
    export class StaticInfo {
        static _types = 0;
        static _isEmbedded = 1;
        static _isReadOnly = 2;
        static _rootType = 3;
        static _propertyRoute = 4;

        prefix: string;
        $elem: JQuery;

        constructor(prefix: string) {
            this.prefix = prefix;
        }

        public find() {
            if (!this.$elem) {
                this.$elem = $('#' + SF.compose(this.prefix, SF.Keys.staticInfo));
            }
            return this.$elem;
        }

        public value() :string {
            return this.find().val();
        }

        public toArray() {
            return this.value().split(";")
        }

        public toValue(array) {
            return array.join(";");
        }

        public getValue(key) {
            var array = this.toArray();
            return array[key];
        }

        public singleType() {
            var typeArray = this.types().split(',');
            if (typeArray.length !== 1) {
                throw "types should have only one element for element {0}".format(this.prefix);
            }
            return typeArray[0];
        }

        public types() {
            return this.getValue(StaticInfo._types);
        }

        public isEmbedded() {
            return this.getValue(StaticInfo._isEmbedded) == "e";
        }

        public isReadOnly() {
            return this.getValue(StaticInfo._isReadOnly) == "r";
        }

        public rootType() {
            return this.getValue(StaticInfo._rootType);
        }

        public propertyRoute() {
            return this.getValue(StaticInfo._propertyRoute);
        }

        public createValue(types, isEmbedded, isReadOnly, rootType, propertyRoute) {
            var array = [];
            array[StaticInfo._types] = types;
            array[StaticInfo._isEmbedded] = isEmbedded ? "e" : "i";
            array[StaticInfo._isReadOnly] = isReadOnly ? "r" : "";
            array[StaticInfo._rootType] = rootType;
            array[StaticInfo._propertyRoute] = propertyRoute;
            return this.toValue(array);
        }
    }

    export class RuntimeInfo {
        static _entityType = 0;
        static _id = 1;
        static _isNew = 2;
        static _ticks = 3;

        prefix: string;
        $elem: JQuery;

        constructor(prefix?: string) {
            this.prefix = prefix;
        }


        public find() {
            if (!this.$elem) {
                this.$elem = $('#' + SF.compose(this.prefix, SF.Keys.runtimeInfo));
            }
            return this.$elem;
        }
        public value() {
            return this.find().val();
        }
        public toArray() {
            return this.value().split(";");
        }
        public toValue(array) {
            return array.join(";");
        }
        public getSet(key, val?) {
            var array = this.toArray();
            if (val === undefined) {
                return array[key];
            }
            array[key] = val;
            this.find().val(this.toValue(array));
        }
        public entityType() {
            return this.getSet(RuntimeInfo._entityType);
        }
        public id() {
            return this.getSet(RuntimeInfo._id);
        }
        public isNew() {
            return this.getSet(RuntimeInfo._isNew);
        }
        public ticks() {
            return this.getSet(RuntimeInfo._ticks);
        }
        public setEntity(entityType, id) {
            this.getSet(RuntimeInfo._entityType, entityType);
            if (SF.isEmpty(id)) {
                this.getSet(RuntimeInfo._id, '');
                this.getSet(RuntimeInfo._isNew, 'n');
            }
            else {
                this.getSet(RuntimeInfo._id, id);
                this.getSet(RuntimeInfo._isNew, 'o');
            }
        }
        public removeEntity() {
            this.getSet(RuntimeInfo._entityType, '');
            this.getSet(RuntimeInfo._id, '');
            this.getSet(RuntimeInfo._isNew, 'o');
        }
        public createValue(entityType, id, isNew, ticks) {
            var array = [];
            array[RuntimeInfo._entityType] = entityType;
            array[RuntimeInfo._id] = id;
            if (SF.isEmpty(isNew)) {
                array[RuntimeInfo._isNew] = SF.isEmpty(id) ? "n" : "o";
            }
            else {
                array[RuntimeInfo._isNew] = isNew;
            }
            array[RuntimeInfo._ticks] = ticks;
            return this.toValue(array);
        }
    }
}

module SF
{
    export var Keys = {
        separator: "_",
        tabId: "sfTabId",
        antiForgeryToken: "__RequestVerificationToken",

        entityTypeNames: "sfEntityTypeNames",

        runtimeInfo: "sfRuntimeInfo",
        staticInfo: "sfStaticInfo",
        toStr: "sfToStr",
        link: "sfLink"
    };

    export class Serializer {
        result: string;

        concat(value) {
            if (this.result === "") {
                this.result = value;
            } else {
                this.result += "&" + value;
            }
        }

        public add(param, value?) {
            if (typeof param === "string") {
                if (value === undefined) {
                    this.concat(param);
                } else {
                    this.concat(param + "=" + value);
                }
            }
            else if ($.isFunction(param)) {
                var data = param();
                //json
                for (var key in data) {
                    if (data.hasOwnProperty(key)) {
                        var value = data[key];
                        this.concat(key + "=" + value);
                    }
                }
            }
            else {
                //json
                for (var key in param) {
                    if (param.hasOwnProperty(key)) {
                        var value = param[key];
                        this.concat(key + "=" + value);
                    }
                }
            }
            return this;
        }

        public serialize() {
            return this.result;
        }
    }

    export function compose (str1: string, str2: string, separator?: string) {
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

    export function cloneContents(sourceContainerId) : JQuery {
        var $source = $('#' + sourceContainerId);
        var $clone = $source.children().clone(true);

        var $sourceSelect = $source.find("select");
        var $cloneSelect = $clone.find("select");

        for (var i = 0, l = $sourceSelect.length; i < l; i++) {
            $cloneSelect.eq(i).val($sourceSelect.eq(i).val());
        }

        return $clone;
    }

    export function getPathPrefixes(prefix) {
        var path = [],
            pathSplit = prefix.split("_");

        for (var i = 0, l = pathSplit.length; i < l; i++)
            path[i] = pathSplit.slice(0, i).join("_");

        return path;
    }

    export function submit(urlController, requestExtraJsonData?, $form?) {
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

    export function submitOnly(urlController, requestExtraJsonData) {
        if (requestExtraJsonData == null)
            throw "SubmitOnly needs requestExtraJsonData. Use Submit instead";

        var $form = $("<form />",
            {
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

        (<HTMLFormElement>$form[0]).submit();
        $form.remove();

        return false;
    }

    export function hiddenInput(id, value) {
        return "<input type='hidden' id='" + id + "' name='" + id + "' value='" + value + "' />\n";
    }

    export function hiddenDiv(id, innerHtml) {
        return "<div id='" + id + "' name='" + id + "' style='display:none'>" + innerHtml + "</div>";
    }

    export module Dropdowns
    {
        export function toggle(event, elem, topFix) {
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
    }

    export module Blocker
    {
        var blocked = false;
        var $elem: JQuery;

        export function isEnabled() {
            return blocked;
        }

        export function enable() {
            blocked = true;
            $elem =
            $("<div/>", {
                "class": "sf-ui-blocker",
                "width": "300%",
                "height": "300%"
            }).appendTo($("body"));
        }

        export function disable() {
            blocked = false;
            $elem.remove();
        }
    }

    once("closeDropDowns", () =>
        $(function () {
            $(document).on("click", function (e) {
                $(".sf-dropdown").removeClass("sf-open");
            });
        })
    );

    once("removeKeyPress", () =>
        $(function () { $('#form input[type=text]').keypress(function (e) { return e.which != 13 }) }));


    once("ajaxError", () =>
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
        }));
}


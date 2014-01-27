/// <reference path="references.ts"/>

module SF
{
    export class StaticInfo {
        static _types = 0;
        static _typeNiceNames = 1;
        static _isEmbedded = 2;
        static _isReadOnly = 3;
        static _rootType = 4;
        static _propertyRoute = 5;

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
            var typeArray = this.types();
            if (typeArray.length !== 1) {
                throw "types should have only one element for element {0}".format(this.prefix);
            }
            return typeArray[0];
        }

        public types() : string[] {
            return this.getValue(StaticInfo._types).split(',');
        }

        public typeNiceNames(): string[] {
            return this.getValue(StaticInfo._typeNiceNames).split(',');
        }

        public isEmbedded() : boolean {
            return this.getValue(StaticInfo._isEmbedded) == "e";
        }

        public isReadOnly() : boolean {
            return this.getValue(StaticInfo._isReadOnly) == "r";
        }

        public rootType(): string{
            return this.getValue(StaticInfo._rootType);
        }

        public propertyRoute(): string{
            return this.getValue(StaticInfo._propertyRoute);
        }
    }

    export class EntityHtml extends EntityValue
    {
        prefix: string;
        html: JQuery; 
      
        hasErrors: boolean;

        constructor(prefix: string, runtimeInfo: RuntimeInfoValue, toString?: string, link?: string) {
            super(runtimeInfo, toString, link); 

            if (this.prefix == null)
                throw new Error("prefix is mandatory for EntityHtml"); 
        }

        assertPrefixAndType(prefix: string, staticInfo: StaticInfo) {

            super.assertPrefixAndType(prefix, staticInfo);

            if (this.prefix != null && this.prefix != prefix)
                throw Error("EntityHtml prefix should be {0} instead of  {1}".format(prefix, this.prefix));
        }

        isLoaded() {
            return this.html != null && this.html.length != 0;
        }

        static fromHtml(prefix: string, html: string): EntityHtml {
            var result = new EntityHtml(prefix, new RuntimeInfoValue("?", null));
            result.html = $(html);
            return result;
        }
    }

    export class EntityValue
    {
        constructor(runtimeInfo: RuntimeInfoValue, toString?: string, link?: string) {
            if (runtimeInfo == null)
                throw new Error("runtimeInfo is mandatory for an EntityValue");

            this.runtimeInfo = runtimeInfo;
            this.toStr = toString;
            this.link = link;
        }

        runtimeInfo: RuntimeInfoValue;
        toStr: string;
        link: string;

        assertPrefixAndType(prefix: string, staticInfo: StaticInfo)
        {
            var types = staticInfo.types();

            if (types.length == 0 && types[0] == "[All]")
                return;

            if (types.indexOf(this.runtimeInfo.type) == -1)
                throw new Error("{0} not found in types {1}".format(this.runtimeInfo.type, types.join(", ")));
        }

        isLoaded() {
            return false;
        }
    }

    export class RuntimeInfoValue {
        type: string;
        id: number;
        isNew: boolean;
        ticks: number;

        constructor(entityType: string, id: number, isNew?: boolean, ticks?: number) {
            if (SF.isEmpty(entityType))
                throw new Error("entityTyp is mandatory for RuntimeInfoValue");

            this.type = entityType;
            this.id = id;
            this.isNew = isNew;
            this.ticks = ticks;
        }

        public static parse(runtimeInfoString: string): RuntimeInfoValue {
            if (SF.isEmpty(runtimeInfoString))
                return null;

            var array = runtimeInfoString.split(',');
            return new RuntimeInfoValue(
                array[0],
                SF.isEmpty(array[1]) ? null : parseInt(array[1]),
                array[2] == "n",
                SF.isEmpty(array[3]) ? null : parseInt(array[3]));
        }

        toString() {
            return [this.type,
                this.id,
                this.isNew ? "n" : "o",
                this.ticks].join(";");
        }

        public static fromKey(key: string): RuntimeInfoValue {
            if (SF.isEmpty(key))
                return null;

            var array = key.split(',');
            return new RuntimeInfoValue(
                array[0],
                parseInt(array[1]),
                false, null);
        }

        key(): string {
            if (this.id == null)
                throw Error("RuntimeInfoValue has no Id");

            return this.type + ";" + this.id;
        }
    }

    export class RuntimeInfoElement {
      
        prefix: string;
        $elem: JQuery;

        constructor(prefix: string) {
            this.prefix = prefix;
        }

        public getElem() {
            if (!this.$elem) {
                this.$elem = $('#' + SF.compose(this.prefix, SF.Keys.runtimeInfo));
            }
            return this.$elem;
        }

        value(): RuntimeInfoValue {
            return RuntimeInfoValue.parse(this.getElem().val());
        }

        setValue(runtimeInfo: RuntimeInfoValue) {
            this.getElem().val(runtimeInfo == null ? null : runtimeInfo.toString());
        }
    }

    export var Keys = {
        separator: "_",
        tabId: "sfTabId",
        antiForgeryToken: "__RequestVerificationToken",

        entityTypeNames: "sfEntityTypeNames",

        runtimeInfo: "sfRuntimeInfo",
        staticInfo: "sfStaticInfo",
        toStr: "sfToStr",
        link: "sfLink",
        loading: "loading",
        entityState: "sfEntityState",
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

    export function cloneContents(sourceContainerId : string) : JQuery {
        return cloneWithValues($('#' + sourceContainerId).children());
    }

    export function cloneWithValues(elements: JQuery): JQuery {
        var clone = elements.clone(true);

        var sourceSelect = elements.filter("select").add(elements.find("select"));
        var cloneSelect = clone.filter("select").add(clone.filter("selet"));

        for (var i = 0, l = sourceSelect.length; i < l; i++) {
            cloneSelect.eq(i).val(sourceSelect.eq(i).val());
        }

        return clone;
    }



    export function getPathPrefixes(prefix) {
        var path = [],
            pathSplit = prefix.split("_");

        for (var i = 0, l = pathSplit.length; i < l; i++)
            path[i] = pathSplit.slice(0, i).join("_");

        return path;
    }

    export function submit(urlController: string, requestExtraJsonData?: any, $form?: JQuery) {
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

        (<HTMLFormElement>$form.attr("action", urlController)[0]).submit();
        return false;
    }

    export function submitOnly(urlController : string, requestExtraJsonData) {
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

    export function hiddenInput(id : string, value : any) {
        return "<input type='hidden' id='" + id + "' name='" + id + "' value='" + value + "' />\n";
    }

    export function hiddenDiv(id : string, innerHtml : any) {
        return $("<div id='" + id + "' style='display:none'></div>").html(innerHtml);
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

        export function wrap<T>(promise: ()=>Promise<T>): Promise<T> {
            if (blocked)
                return promise();

            enable();

            return promise()
                .then(val=> { disable(); return val; })
                .catch(err=> { disable(); throw err; return <T>null; }); //Typescript bug?
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


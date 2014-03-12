/// <reference path="globals.ts"/>
var __extends = this.__extends || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};
define(["require", "exports"], function(require, exports) {
    exports.Keys = {
        tabId: "sfTabId",
        antiForgeryToken: "__RequestVerificationToken",
        entityTypeNames: "sfEntityTypeNames",
        entityTypeNiceNames: "sfEntityTypeNiceNames",
        runtimeInfo: "sfRuntimeInfo",
        staticInfo: "sfStaticInfo",
        toStr: "sfToStr",
        link: "sfLink",
        loading: "loading",
        entityState: "sfEntityState",
        template: "sfTemplate",
        viewMode: "sfViewMode"
    };

    var RuntimeInfo = (function () {
        function RuntimeInfo(entityType, id, isNew, ticks) {
            if (SF.isEmpty(entityType))
                throw new Error("entityTyp is mandatory for RuntimeInfo");

            this.type = entityType;
            this.id = id;
            this.isNew = isNew;
            this.ticks = ticks;
        }
        RuntimeInfo.parse = function (runtimeInfoString) {
            if (SF.isEmpty(runtimeInfoString))
                return null;

            var array = runtimeInfoString.split(';');
            return new RuntimeInfo(array[0], SF.isEmpty(array[1]) ? null : parseInt(array[1]), array[2] == "n", array[3]);
        };

        RuntimeInfo.prototype.toString = function () {
            return [
                this.type,
                this.id,
                this.isNew ? "n" : "o",
                this.ticks].join(";");
        };

        RuntimeInfo.fromKey = function (key) {
            if (SF.isEmpty(key))
                return null;

            return new RuntimeInfo(key.before(";"), parseInt(key.after(";")), false);
        };

        RuntimeInfo.prototype.key = function () {
            if (this.id == null)
                throw Error("RuntimeInfo has no Id");

            return this.type + ";" + this.id;
        };

        RuntimeInfo.getHiddenInput = function (prefix, context) {
            var result = $('#' + SF.compose(prefix, exports.Keys.runtimeInfo), context);

            if (result.length != 1)
                throw new Error("{0} elements with id {1} found".format(result.length, SF.compose(prefix, exports.Keys.runtimeInfo)));

            return result;
        };

        RuntimeInfo.getFromPrefix = function (prefix, context) {
            return RuntimeInfo.parse(RuntimeInfo.getHiddenInput(prefix, context).val());
        };

        RuntimeInfo.setFromPrefix = function (prefix, runtimeInfo, context) {
            RuntimeInfo.getHiddenInput(prefix, context).val(runtimeInfo == null ? "" : runtimeInfo.toString());
        };
        return RuntimeInfo;
    })();
    exports.RuntimeInfo = RuntimeInfo;

    var EntityValue = (function () {
        function EntityValue(runtimeInfo, toString, link) {
            if (runtimeInfo == null)
                throw new Error("runtimeInfo is mandatory for an EntityValue");

            this.runtimeInfo = runtimeInfo;
            this.toStr = toString;
            this.link = link;
        }
        EntityValue.prototype.assertPrefixAndType = function (prefix, types) {
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
    exports.EntityValue = EntityValue;

    var EntityHtml = (function (_super) {
        __extends(EntityHtml, _super);
        function EntityHtml(prefix, runtimeInfo, toString, link) {
            _super.call(this, runtimeInfo, toString, link);

            if (prefix == null)
                throw new Error("prefix is mandatory for EntityHtml");

            this.prefix = prefix;
        }
        EntityHtml.prototype.assertPrefixAndType = function (prefix, types) {
            _super.prototype.assertPrefixAndType.call(this, prefix, types);

            if (this.prefix != null && this.prefix != prefix)
                throw Error("EntityHtml prefix should be {0} instead of  {1}".format(prefix, this.prefix));
        };

        EntityHtml.prototype.isLoaded = function () {
            return this.html != null && this.html.length != 0;
        };

        EntityHtml.prototype.loadHtml = function (htmlText) {
            this.html = $('<div/>').html(htmlText).contents();
        };

        EntityHtml.fromHtml = function (prefix, htmlText) {
            var result = new EntityHtml(prefix, new RuntimeInfo("?", null, false));
            result.loadHtml(htmlText);
            return result;
        };

        EntityHtml.fromDiv = function (prefix, div) {
            var result = new EntityHtml(prefix, new RuntimeInfo("?", null, false));
            result.html = div.clone();
            return result;
        };

        EntityHtml.withoutType = function (prefix) {
            var result = new EntityHtml(prefix, new RuntimeInfo("?", null, false));
            return result;
        };
        return EntityHtml;
    })(EntityValue);
    exports.EntityHtml = EntityHtml;
});
//# sourceMappingURL=Entities.js.map

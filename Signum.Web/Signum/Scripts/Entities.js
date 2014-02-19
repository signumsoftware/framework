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
        viewMode: "sfViewMode"
    };

    var StaticInfo = (function () {
        function StaticInfo(prefix) {
            this.prefix = prefix;
        }
        StaticInfo.prototype.find = function () {
            if (!this.$elem) {
                this.$elem = $('#' + SF.compose(this.prefix, exports.Keys.staticInfo));
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

        StaticInfo.getFor = function (prefix) {
            if (!prefix)
                throw new Error("prefix not provided");

            var staticInfo = new StaticInfo(prefix);
            if (staticInfo.find().length > 0)
                return staticInfo;

            return new StaticInfo(prefix.tryBeforeLast("_") || prefix);
        };
        StaticInfo._types = 0;
        StaticInfo._typeNiceNames = 1;
        StaticInfo._isEmbedded = 2;
        StaticInfo._isReadOnly = 3;
        StaticInfo._rootType = 4;
        StaticInfo._propertyRoute = 5;
        return StaticInfo;
    })();
    exports.StaticInfo = StaticInfo;

    var RuntimeInfoElement = (function () {
        function RuntimeInfoElement(prefix) {
            this.prefix = prefix;
        }
        RuntimeInfoElement.prototype.getElem = function () {
            if (!this.$elem) {
                this.$elem = $('#' + SF.compose(this.prefix, exports.Keys.runtimeInfo));
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
    exports.RuntimeInfoElement = RuntimeInfoElement;

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

            var array = runtimeInfoString.split(';');
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

            return new RuntimeInfoValue(key.before(";"), parseInt(key.after(";")), false);
        };

        RuntimeInfoValue.prototype.key = function () {
            if (this.id == null)
                throw Error("RuntimeInfoValue has no Id");

            return this.type + ";" + this.id;
        };
        return RuntimeInfoValue;
    })();
    exports.RuntimeInfoValue = RuntimeInfoValue;

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
    exports.EntityValue = EntityValue;

    var EntityHtml = (function (_super) {
        __extends(EntityHtml, _super);
        function EntityHtml(prefix, runtimeInfo, toString, link) {
            _super.call(this, runtimeInfo, toString, link);

            if (prefix == null)
                throw new Error("prefix is mandatory for EntityHtml");

            this.prefix = prefix;
        }
        EntityHtml.prototype.assertPrefixAndType = function (prefix, staticInfo) {
            _super.prototype.assertPrefixAndType.call(this, prefix, staticInfo);

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
            var result = new EntityHtml(prefix, new RuntimeInfoValue("?", null, false));
            result.loadHtml(htmlText);
            return result;
        };

        EntityHtml.fromDiv = function (prefix, div) {
            var result = new EntityHtml(prefix, new RuntimeInfoValue("?", null, false));
            result.html = div.clone();
            return result;
        };

        EntityHtml.withoutType = function (prefix) {
            var result = new EntityHtml(prefix, new RuntimeInfoValue("?", null, false));
            return result;
        };
        return EntityHtml;
    })(EntityValue);
    exports.EntityHtml = EntityHtml;
});
//# sourceMappingURL=Entities.js.map

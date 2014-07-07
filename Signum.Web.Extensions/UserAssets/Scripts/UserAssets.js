/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports"], function(require, exports) {
    function attachShowCurrentEntity(el) {
        var showOnEntity = function () {
            var visible = !!el.getRuntimeInfo();
            el.element.closest(".form-group").next("p.messageEntity").toggle(visible);
        };

        showOnEntity();

        el.entityChanged = showOnEntity;
    }
    exports.attachShowCurrentEntity = attachShowCurrentEntity;

    function attachShowEmbeddedInEntity(el) {
        var showOnEntity = function () {
            var visible = !!el.getRuntimeInfo();

            var vl = el.prefix.parent("EntityType").child("EmbeddedInEntity").get();
            if (!visible)
                vl.val(null);

            vl.closest(".form-group").toggle(visible);
        };

        showOnEntity();

        el.entityChanged = showOnEntity;
    }
    exports.attachShowEmbeddedInEntity = attachShowEmbeddedInEntity;
});
//# sourceMappingURL=UserAssets.js.map

/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports"], function(require, exports) {
    function attachShowCurrentEntity(el) {
        var showOnEntity = function () {
            var visible = !!el.getRuntimeInfo();
            el.element.closest(".form-group").next("p.messageEntity").toggle(visible);

            var vl = el.prefix.parent("EntityType").child("Disposition").get();
            if (!visible)
                vl.val(null);

            vl.closest(".form-group").toggle(visible);
        };

        showOnEntity();

        el.entityChanged = showOnEntity;
    }
    exports.attachShowCurrentEntity = attachShowCurrentEntity;
});
//# sourceMappingURL=UserAssets.js.map

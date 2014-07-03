/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Lines = require("Framework/Signum.Web/Signum/Scripts/Lines")
import Finder = require("Framework/Signum.Web/Signum/Scripts/Finder")
import Validator = require("Framework/Signum.Web/Signum/Scripts/Validator")
import Operations = require("Framework/Signum.Web/Signum/Scripts/Operations")

export function attachShowCurrentEntity(el: Lines.EntityLine) {
    var showOnEntity = function () {
        var visible = !!el.getRuntimeInfo();
        el.element.closest(".form-group").next("p.messageEntity").toggle(visible);
    };

    showOnEntity();

    el.entityChanged = showOnEntity;
} 

export function attachShowEmbeddedInEntity(el: Lines.EntityLine) {
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
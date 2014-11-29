/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports"], function(require, exports) {
    function edit() {
        $(".editable").show();
        $(".shortcut").show();
        $(".wiki").hide();

        "edit-action".get().hide();
        "syntax-action".get().show();
        "save-action".get().show();
        "translate-action".get().show();
    }
    exports.edit = edit;

    function save() {
        "save-action".get().html("save-action".get().html() + "...");

        $.ajax({
            url: document.getElementById("form-save").action,
            async: false,
            data: $("form#form-save :input").serializeObject(),
            success: function (result) {
                if (!result) {
                    location.reload(true);
                }
            }
        });
    }

    function translate(v) {
        v.preventDefault();
        $.ajax({
            url: $(v.currentTarget).attr("href"),
            async: false,
            data: $("form#form-save :input").serializeObject(),
            success: function (result) {
                if (!result) {
                    location.reload(true);
                }
            }
        });
    }

    function hashSelected() {
        $(".hash-selected").removeClass("hash-selected");

        if (window.location.hash) {
            $(window.location.hash).addClass("hash-selected");
        }
    }

    function init() {
        $(function () {
            //$(".shortcut").click(function () { $.copy($(this).html()); });
            hashSelected();

            $(window).on('hashchange', function () {
                hashSelected();
            });

            "save-action".get().click(save);
            "edit-action".get().click(exports.edit);

            "syntax-action".get().click(function (e) {
                $("#syntax-list").slideToggle("slow");
                $(e.currentTarget).toggleClass("active");
                return null;
            });

            "translate-action".get().find("a").click(function (args) {
                return translate(args);
            });
        });
    }
    exports.init = init;
});
//# sourceMappingURL=help.js.map

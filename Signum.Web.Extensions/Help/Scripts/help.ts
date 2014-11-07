/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

export function edit() {
    $(".editable").show();
    $(".shortcut").show();
    $(".wiki").hide();

    "edit-action".get().hide();
    "syntax-action".get().show();
    "save-action".get().show();
}

function save() {
    "save-action".get().html("save-action".get().html() + "...");

    $.ajax({
        url: (<HTMLFormElement>document.getElementById("form-save")).action,
        async: false,
        data: $("form#form-save :input").serializeObject(),
        success: function (result) {
            if (!result) {
                location.reload(true);
                $("#saving-error").hide();
            }
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            var msg;
            if (XMLHttpRequest.responseText != null && XMLHttpRequest.responseText != undefined) {
                var startError = XMLHttpRequest.responseText.indexOf("<title>");
                var endError = XMLHttpRequest.responseText.indexOf("</title>");
                if ((startError != -1) && (endError != -1))
                    msg = XMLHttpRequest.responseText.substring(startError + 7, endError);
                else
                    msg = XMLHttpRequest.responseText;
            }
            $("#saving-error .text").html(msg);
            $("#saving-error").show();
        }
    });
}

function hashSelected() {
    $(".hash-selected").removeClass("hash-selected");

    if (window.location.hash) {
        $(window.location.hash).addClass("hash-selected"); 
    }
}

export function init() {
    $(function () {
        //$(".shortcut").click(function () { $.copy($(this).html()); });

        hashSelected();

        $(window).on('hashchange', function () {
            hashSelected();
        });

        "save-action".get().click(save);
        "edit-action".get().click(edit);

        "syntax-action".get().click(function () {
            $("#syntax-list").slideToggle("slow");
            $(this).toggleClass("active");
            return null;
        });
    });
}
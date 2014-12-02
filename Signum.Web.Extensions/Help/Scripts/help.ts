/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

export function edit() {
    $(".editable").show();
    $(".shortcut").show();
    $(".wiki").hide();

    "edit-action".get().hide();
    "syntax-action".get().show();
    "save-action".get().show();
    "translate-action".get().show();
}

function save() {
    "save-action".get().html("save-action".get().html() + "...");

    $.ajax({
        url: (<HTMLFormElement>document.getElementById("form-save")).action,
        async: false,
        data: $("form#form-save :input").serializeObject(),
        success: result => {
            if (!result) {
                location.reload(true);
            }
        }
    });
}


function translate(v: JQueryEventObject) {
    v.preventDefault();
    $.ajax({
        url: $(v.currentTarget).attr("href"),
        async: false,
        data: $("form#form-save :input").serializeObject(),
        success: (result) => {
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

export function init() {
    $(() => {
        //$(".shortcut").click(function () { $.copy($(this).html()); });

        hashSelected();

        $(window).on('hashchange', function () {
            hashSelected();
        });

        "save-action".get().click(save);
        "edit-action".get().click(edit);

        "syntax-action".get().click(e => {
            $("#syntax-list").slideToggle("slow");
            $(e.currentTarget).toggleClass("active");
            return null;
        });

        "translate-action".get().find("a").click(args => translate(args)); 
    });
}
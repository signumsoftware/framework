/// <reference path="../../../../Framework/Signum.Web/Signum/Headers/jquery/jquery.d.ts"/>
/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/references.ts"/>

module SF.Help 
{

    export function edit() {
        $(".editable").each(function () {
            var self = $(this);
            self.bind('focus', function (event) {
                $(this).addClass("modified");
            });

            $("dd").addClass("editing");
            $("#entityName").addClass("editing");
        });

        $(".shortcut").css("display", "block");
        $("#edit-action").hide();
        $("#syntax-action").show();
        $("#save-action").show();
    }

    export function save() {
        $("#save-action").html("Guardando...");
        $.ajax({
            url: (<HTMLFormElement>document.getElementById("form-save")).action,
            async: false,
            data: $("form#form-save .modified").serialize(),
            success: function (msg) {
                location.reload(true);
                $("#saving-error").hide();
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

    once("SF.Help", () =>
        $(function () {
            $(".shortcut").click(function () { $.copy($(this).html()); });

            if (typeof window.location.hash != 'undefined' && window.location.hash != '') {
                window.location.hash += "-editor";

                var $dd = $(window.location.hash).parents("dd").first();

                $dd.css('background-color', '#ccffaa');
                $dd.prev().css('background-color', '#ccffaa');

            }

            $("#syntax-action").click(function () {
                $("#syntax-list").slideToggle("slow");
                $(this).toggleClass("active");
                return null;
            });
        }));
}
/// <reference path="../../../../Framework/Signum.Web/Signum/Headers/jquery/jquery.d.ts"/>
/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/references.ts"/>

module SF.UserQuery {
    once("SF-UserQuery", () => {
        $(document).on("click", ".sf-userquery", function (e) {
            e.preventDefault();
            var findOptionsQueryString = SF.FindNavigator.getFor("").requestDataForSearchInUrl();
            var url = $(this).attr("href") + findOptionsQueryString;

            if (e.ctrlKey || e.which == 2) {
                window.open(url);
            }
            else if (e.which == 1) {
                window.location.href = url;
            }
        });
    });

    export function attachShowCurrentEntity(el: SF.EntityLine) {
        var showOnEntity = function () {
            el.element.next("p").toggle(el.runtimeInfo().entityType() != "");
        }; 

        showOnEntity(); 

        el.onEntityChanged = showOnEntity; 
    }
}
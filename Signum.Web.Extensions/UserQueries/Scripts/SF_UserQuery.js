/// <reference path="../../../../Framework/Signum.Web/Signum/Headers/jquery/jquery.d.ts"/>
/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/references.ts"/>
var SF;
(function (SF) {
    (function (UserQuery) {
        once("SF-UserQuery", function () {
            $(document).on("click", ".sf-userquery", function (e) {
                e.preventDefault();
                var findOptionsQueryString = SF.FindNavigator.getFor("").requestDataForSearchInUrl();
                var url = $(this).attr("href") + findOptionsQueryString;

                if (e.ctrlKey || e.which == 2) {
                    window.open(url);
                } else if (e.which == 1) {
                    window.location.href = url;
                }
            });
        });
    })(SF.UserQuery || (SF.UserQuery = {}));
    var UserQuery = SF.UserQuery;
})(SF || (SF = {}));
//# sourceMappingURL=SF_UserQuery.js.map

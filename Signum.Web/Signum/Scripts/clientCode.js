/// <reference path="references.ts"/>
var Code;
(function (Code) {
    function attachEntityLine(el) {
        el.creating = function () {
            var fn = new FindNavigator({
                prefix: "fnPerson",
                webQueryName: "myQuery",
                onOk: function (e) {
                    SF.Navgator.View("MyController/NewEntity", e, function (e) {
                        return el.SetEntity(e);
                    });
                }
            }).openFinder();

            SF.Find("myQuery", function (e) {
                return SF.Navgator.View("MyController/NewEntity", e, function (e) {
                    return el.SetEntity(e);
                });
            });
        };
    }
    Code.attachEntityLine = attachEntityLine;
})(Code || (Code = {}));
//# sourceMappingURL=clientCode.js.map

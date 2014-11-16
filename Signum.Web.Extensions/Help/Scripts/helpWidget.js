/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports"], function(require, exports) {
    

    var drawLayer;

    function entityClick(button, prefix, entityHelp, propertyRoutesUrl) {
        click(button, function (helpLayer) {
            addAdorner($("#divMainPage > h3 > .sf-entity-title"), helpLayer, entityHelp.Info);
            addAdorner($("#divMainPage > h3 > .sf-type-nice-name"), helpLayer, entityHelp.Info);

            var array = [];

            $("[data-route]").each(function (i, e) {
                var element = $(e);
                var route = element.attr("data-route");
                var info = entityHelp.Properties[route];

                if (info)
                    addAdorner(element, helpLayer, info);
                else
                    array.push({ element: element, route: route });
            });

            if (array.length) {
                SF.ajaxPost({ url: propertyRoutesUrl, data: { routes: JSON.stringify(array.map(function (p) {
                            return p.route;
                        })) } }).then(function (res) {
                    array.forEach(function (p) {
                        return addAdorner(p.element, helpLayer, res[p.route]);
                    });
                });
            }

            $("[data-operation]").each(function (i, e) {
                var element = $(e);
                var operation = element.attr("data-operation");
                var info = entityHelp.Operations[operation];

                if (info)
                    addAdorner(element, helpLayer, info);
            });
        });
    }
    exports.entityClick = entityClick;

    function click(button, addAdorners) {
        var btn = $(button);

        if (btn.hasClass("active")) {
            $(window).off("resize", drawLayer);

            $("#helpLayer").remove();
            btn.removeClass("active");
        } else {
            drawLayer = function () {
                if (btn.hasClass("active")) {
                    $("#helpLayer").remove();
                    var helpLayer = $("<div/>").attr("id", "helpLayer").appendTo($("body"));

                    addAdorners(helpLayer);
                }
            };

            btn.addClass("active");

            drawLayer();

            $(window).resize(drawLayer);
        }
    }

    function addAdorner(element, helpLayer, info) {
        if (!element.is(":visible"))
            return;

        var part = $("<a/>").addClass("help-tooltip").addClass(SF.isEmpty(info.Description) ? null : "description").css({
            position: "absolute",
            top: element.offset().top + "px",
            left: element.offset().left + "px",
            width: element.outerWidth() + "px",
            height: element.outerHeight() + "px"
        }).attr("href", info.Link).appendTo(helpLayer);

        part.popover({
            title: info.Title,
            content: "<span class='gray'>" + info.Info + "</span>" + (SF.isEmpty(info.Description) ? "" : "<br>" + info.Description),
            trigger: "hover",
            html: true,
            placement: "bottom"
        });
    }
});
//# sourceMappingURL=helpWidget.js.map

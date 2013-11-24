/// <reference path="../Headers/jquery/jquery.d.ts"/>
/// <reference path="SF_Utils.ts"/>
var SF;
(function (SF) {
    function slider($container) {
        var w = $container.width(), $target = $container.children("table").first(), mw = $target.width(), containerLeft = $container.offset().left;

        $container.css({ "overflow-x": "hidden" });
        var $track = $("<div class='sf-search-track'></div>").css({ width: w });

        var sliderWidth = Math.max(100, (2 * w - mw));
        var $slider = $("<div class='sf-search-slider' title='Arrastrar para hacer scroll'></div>").css({ width: sliderWidth }).appendTo($track);

        var proportion = (mw - w) / (w - sliderWidth);
        if (mw <= w)
            $track.hide();

        $target.before($track);
        $target.after($track.clone());

        var mouseDown = false, left = 0, prevLeft;

        $container.find(".sf-search-slider").bind("mousedown", function (e) {
            mouseDown = true;
            left = getMousePosition(e).x - containerLeft;
            prevLeft = $(this).position().left;
        });

        $container.find(".sf-search-track").bind("click", function (e) {
            if ($(e.target).hasClass("sf-search-slider"))
                return;

            var $track = $(this), clicked = getMousePosition(e).x - containerLeft, $slider = $track.find(".sf-search-slider"), sliderPosLeft = $slider.position().left, sliderWidth = $slider.width();

            var isLeft = sliderPosLeft > clicked;

            var left = 0;
            if (isLeft) {
                //move sliders left
                left = Math.max(sliderPosLeft - sliderWidth, 0);
            } else {
                left = Math.min(sliderPosLeft + sliderWidth, w - sliderWidth);
            }

            $track.parent().find(".sf-search-slider").css({ left: left });

            $container.children("table").first().css({ marginLeft: -left * proportion });
        });

        $(document).bind("mousemove", function (e) {
            if (mouseDown) {
                var currentLeft = prevLeft + (getMousePosition(e).x - containerLeft - left);
                currentLeft = Math.min(currentLeft, w - (Math.max(100, 2 * w - mw)));
                currentLeft = Math.max(0, currentLeft);

                $container.find(".sf-search-slider").css({ left: currentLeft });
                $target.css({ marginLeft: -currentLeft * proportion });
            }
        }).bind("mouseup", function () {
            mouseDown = false;
        });

        var resize = function ($c, $t) {
            if (!mouseDown) {
                var _w = $c.width(), _mw = $c.children("table").first().width();
                if ((w != _w || mw != _mw)) {
                    if (_mw > _w) {
                        w = _w;
                        mw = _mw;
                        $t.css({ width: w, left: 0 }).show();

                        var sliderWidth = Math.max(100, (2 * w - mw));

                        proportion = (mw - w) / (w - sliderWidth);

                        $t.find(".sf-search-slider").css({ width: sliderWidth });
                        $container.children("table").first().css({ marginLeft: 0 });
                    } else {
                        $t.hide();
                    }
                }
            }
            setTimeout(function () {
                resize($c, $t);
            }, 1000);
        };
        resize($container, $container.find(".sf-search-track"));

        $container.find(".sf-search-slider").disableTextSelect();
    }
    SF.slider = slider;

    var getMousePosition = function (e) {
        var posx = 0, posy = 0;

        if (window.event) {
            posx = window.event.clientX + document.documentElement.scrollLeft + document.body.scrollLeft;
            posy = window.event.clientY + document.documentElement.scrollTop + document.body.scrollTop;
        } else {
            //posx = e.clientX + window.scrollX;
            //posy = e.clientY + window.scrollY;
        }

        return {
            'x': posx,
            'y': posy
        };
    };
})(SF || (SF = {}));

$.extend($.fn.disableTextSelect = function () {
    return this.each(function () {
        var $this = $(this);
        $this.bind('selectstart', function () {
            return false;
        });
    });
});

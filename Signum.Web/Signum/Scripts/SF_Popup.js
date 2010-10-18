SF.add('popup', function (S) {

    var popup = function (_visualOptions) {
        log("popup");
        this.visualOptions = $.extend({
            fade: false
        }, _visualOptions);

        this.show = function (idExternalPopupDiv) {
            log("popup show (" + idExternalPopupDiv + ")");

            var $external = $("#" + idExternalPopupDiv);
                $external.show();

                var modalBackground = $external.find(".popupBackground");

            var otherPopups = $(".popupWindow");
            var maxZindex = 100;
            otherPopups.each(function () {
                var zindex = $(this).css('z-index');
                if (zindex > maxZindex)
                    maxZindex = zindex;
            });

            modalBackground.width('500%').height('500%').css('z-index', +maxZindex + 1);
            //Read offsetWidth and offsetHeight after display=block or otherwise it's 0
            var panelPopup = $external.find(".popupWindow");
            //panelPopup.attr('z-index', parseInt(maxZindex) + 2);
            var popup2 = panelPopup[0];
            var parentDiv = $external.parent();
            var popupWidth = popup2.offsetWidth;
            var bodyWidth = document.body.clientWidth;
            var left = Math.max((bodyWidth - popupWidth) / 2, 10) + "px";
            var popupHeight = popup2.offsetHeight;
            var bodyHeight = document.documentElement.clientHeight;
            var top = Math.max((bodyHeight - popupHeight) / 2, 10) + "px";

            $external.hide();

            popup2.style.left = left;
            popup2.style.top = top;
            popup2.style.minWidth = popupWidth + "px";
            popup2.style.maxWidth = "95%";
            popup2.style.zIndex = +maxZindex + 2;

            var maxPercentageWidth = 0.95;
            popup2.style.maxWidth = (maxPercentageWidth * 100) + "%";
            popup2.style.maxHeight = $(window).height() + "px";

            popup2.style.minWidth = ((popupWidth > (maxPercentageWidth * 100)) ? (maxPercentageWidth * 100) : popupWidth) + "px";

            if ($("#" + idExternalPopupDiv + " :file").length > 0)
                popup2.style.minWidth = "500px";

            $external.show('fast');
            modalBackground[0].style.left = 0;
            modalBackground.css('filter', 'alpha(opacity=40)')
            if (this.visualOptions.fade)
                modalBackground.fadeIn('slow');
            else
                modalBackground.show();

            var searchControl = $(popup2).find(".searchControl");   //we have to limit its height
            if (searchControl.length > 0) {
                var marginTop = searchControl.position().top;
                searchControl.css({ "maxHeight": $(window).height() - marginTop, "overflow": "auto" });
            }
        };
    };

    window.popup = popup;

});
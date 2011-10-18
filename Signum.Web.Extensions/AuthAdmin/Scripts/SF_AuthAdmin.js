var SF = SF || {};

SF.Auth = (function () {
    var coloredRadios = function ($ctx) {
        var updateBackground = function () {
            var $this = $(this);
            $this.toggleClass("sf-auth-chooser-disabled", !$this.find(":radio").attr("checked"));
        };

        var $links = $ctx.find(".sf-auth-rules .sf-auth-chooser");

        $links.find(":radio").hide();
        $links.each(updateBackground);

        $links.click(function () {
            var $this = $(this);
            var $tr = $this.closest("tr");
            $tr.find(".sf-auth-chooser :radio").attr("checked", false);
            var $radio = $this.find(":radio");
            $radio.attr("checked", true);
            $tr.find(".sf-auth-overriden").attr("checked", $radio.val() != $tr.find("input[name$=Base]").val());
            $tr.find(".sf-auth-chooser").each(updateBackground);
        });
    };

    var multiSelRadios = function ($ctx) {
        var updateBackground = function () {
            var $this = $(this);
            $this.toggleClass("sf-auth-chooser-disabled", !$this.find(":checkbox").attr("checked"));
        };

        var $links = $ctx.find(".sf-auth-rules .sf-auth-chooser");

        $links.find(":checkbox").hide();
        $links.each(updateBackground);

        $links.bind("mousedown", function () {
            this.onselectstart = function () { return false };
        });
        $links.click(function (e) {
            var $this = $(this);
            var $tr = $this.closest("tr");
            var $cb = $this.find(":checkbox");

            if (!e.shiftKey) {
                $tr.find(".sf-auth-chooser :checkbox").attr("checked", false);
                $cb.attr("checked", true);
            } else {
                var num = $tr.find(".sf-auth-chooser :checkbox:checked").length;
                if (!$cb.attr("checked") && num == 1) {
                    $cb.attr("checked", true);
                }
                else if ($cb.attr("checked") && num >= 2) {
                    $cb.attr("checked", false);
                }
            }

            var total = $.map($tr.find(".sf-auth-chooser :checkbox:checked"), function (a) { return $(a).attr("tag"); }).join(",");

            $tr.find(".sf-auth-overriden").attr("checked", total != $tr.find("input[name$=Base]").val());
            $tr.find(".sf-auth-chooser").each(updateBackground);
        });
    };

    var treeView = function () {
        $(".sf-auth-namespace").live("click", function (e) {
            e.preventDefault();
            var $this = $(this);
            $this.find(".sf-auth-expanded-last,.sf-auth-closed-last").toggleClass("sf-auth-expanded-last").toggleClass("sf-auth-closed-last");
            $this.find(".sf-auth-expanded,.sf-auth-closed").toggleClass("sf-auth-expanded").toggleClass("sf-auth-closed");
            var ns = $this.find(".sf-auth-namespace-name").html();
            $(".sf-auth-rules tr").filter(function () {
                return $(this).attr("data-namespace") == ns;
            }).toggle();
        });
    };

    var openDialog = function (e) {
        e.preventDefault();
        var $this = $(this);
        var navigator = new SF.ViewNavigator({
            controllerUrl: $this.attr("href"),
            requestExtraJsonData: null,
            type: 'PropertyRulePack',
            onCancelled: function () {
                $this.closest("div").css("opacity", 0.5);
                $this.find(".sf-auth-thumb").css("opacity", 0.5);
            },
            onLoaded: function (divId) {
                SF.Auth.coloredRadios($("#" + divId));
            }
        });
        navigator.createSave();
    };

    var postDialog = function (controllerUrl, prefix) {
        new SF.PartialValidator({ controllerUrl: controllerUrl, prefix: prefix }).trySave();
    };

    return {
        coloredRadios: coloredRadios,
        multiSelRadios: multiSelRadios,
        treeView: treeView,
        openDialog: openDialog,
        postDialog: postDialog
    };
})();
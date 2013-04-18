var SF = SF || {};

SF.UserQuery = (function () {
    
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

})();
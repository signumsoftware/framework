var FindNavigator = function(_findOptions) {
    this.findOptions = $.extend({
        prefix: "",
        suffix: "S", //To allow multiple Search controls in one page
        queryUrlName: null,
        searchOnLoad: false,
        allowMultiple: null,
        create: true,
        view: true,
        top: null,
        filters: null,
        filterMode: null,
        navigatorControllerUrl: "Signum/PartialFind",
        searchControllerUrl: "Signum/Search",
        onOk: null,
        onCancelled: null,
        onOkClosed: null,
        async: false
    }, _findOptions);
};

FindNavigator.prototype = {

    pf: function(s) {
        return "#" + this.findOptions.prefix + s;
    },

    tempDivId: function() {
        return this.findOptions.prefix + "Temp" + this.findOptions.suffix;
    },

    openFinder: function() {
        log("FindNavigator openFinder");
        var self = this;
        $.ajax({
            type: "POST",
            url: this.findOptions.navigatorControllerUrl,
            data: this.requestData(),
            async: false,
            dataType: "html",
            success: function(popupHtml) {
                $('#divASustituir').after(hiddenDiv(self.tempDivId(), popupHtml));
                new popup().show(self.tempDivId());
                $('#' + self.findOptions.prefix + sfBtnOk + self.findOptions.suffix).unbind('click').click(function() { self.onSearchOk(); });
                $('#' + self.findOptions.prefix + sfBtnCancel + self.findOptions.suffix).unbind('click').click(function() { self.onSearchCancel(); });
            }
        });
    },

    selectedItems: function() {
        log("FindNavigator selectedItems");
        var items = new Array();
        var selected = $("input:radio[name=" + this.findOptions.prefix + "rowSelection" + this.findOptions.suffix + "]:checked, input:checkbox[name^=" + this.findOptions.prefix + "rowSelection]:checked").filter("[name$=" + this.findOptions.suffix + "]");
        if (selected.length == 0)
            return items;

        var self = this;
        selected.each(function(i) {
            var currentItem = this.value;
            var __index = currentItem.indexOf("__");
            var __index2 = currentItem.indexOf("__", __index + 2);
            var item = new Object();
            item.id = currentItem.substring(0, __index);
            item.type = currentItem.substring(__index + 2, __index2);
            item.toStr = currentItem.substring(__index2 + 2, currentItem.length);
            item.link = $('#' + this.id).parent().next('#' + self.findOptions.prefix + 'tdResults' + self.findOptions.suffix).children('a').attr('href');
            items[i] = item;
        });

        return items;
    },

    splitSelectedIds: function() {
        log("FindNavigator splitSelectedIds");
        var selected = this.selectedItems();
        var result = "";
        $(selected).each(function(i, value) {
            result += value.id + ",";
        });
        if (!empty(result))
            result = result.substring(0, result.length - 1);
        return result;
    },

    search: function() {
        //	var async = concurrentSearch[prefix + "btnSearch"];
        //	if (async) concurrentSearch[prefix + "btnSearch"]=false;
    var btnSearch = $(this.pf("btnSearch") + this.findOptions.suffix);
        btnSearch.toggleClass('loading').val(lang['searching']);
        var self = this;
        $.ajax({
            type: "POST",
            url: this.findOptions.searchControllerUrl,
            data: this.requestData(),
            async: this.findOptions.async,
            dataType: "html",
            success: function(resultsHtml) {
                $('#' + self.findOptions.prefix + "divResults" + self.findOptions.suffix).html(resultsHtml);
            }
        });
        btnSearch.val(lang['buscar']).toggleClass('loading');
    },

    requestData: function() {
        var requestData = new Object();
        requestData[sfQueryUrlName] = ((empty(this.findOptions.queryUrlName)) ? $(this.pf("sfQueryUrlName") + this.findOptions.suffix).val() : this.findOptions.queryUrlName);
        requestData[sfTop] = empty(this.findOptions.top) ? $(this.pf(sfTop) + this.findOptions.suffix).val() : this.findOptions.top;
        requestData[sfAllowMultiple] = (this.findOptions.allowMultiple == undefined) ? $(this.pf(sfAllowMultiple) + this.findOptions.suffix).val() : this.findOptions.allowMultiple;

        var canView = $(this.pf(sfView) + this.findOptions.suffix).val();
        requestData[sfView] = (!this.findOptions.view) ? false : (empty(canView) ? this.findOptions.view : canView);
        requestData[sfSearchOnLoad] = this.findOptions.searchOnLoad;

        if (this.findOptions.async)
            requestData["sfAsync"] = this.findOptions.async;

        if (this.findOptions.filterMode != null)
            requestData["sfFilterMode"] = this.findOptions.filterMode;

        if (!this.findOptions.create)
            requestData["sfCreate"] = this.findOptions.create;

        //var currentfilters = (empty(this.findOptions.filters)) ? this.serializeFiltersJson() : this.findOptions.filters;
        var currentfilters = this.serializeFilters();
        if (!empty(currentfilters))
            $.extend(requestData, currentfilters); //requestData += currentfilters

        requestData[sfPrefix] = this.findOptions.prefix;
        requestData[sfSuffix] = this.findOptions.suffix;

        return requestData;
    },

    serializeFilters: function() {
        var result = "";
        var self = this;
        $(this.pf("tblFilters") + this.findOptions.suffix + " > tbody > tr").each(function() {
        result = $.extend(result, self.serializeFilter(this.id.substring(this.id.lastIndexOf("_") + 1, this.id.length-self.findOptions.suffix.length)));
        });
        return result;
    },

    serializeFilter: function(index) {
        var tds = $(this.pf("trFilter_") + index + this.findOptions.suffix + " td");
        var columnName = tds[0].id.substring(tds[0].id.indexOf("__") + 2, tds[0].id.length - this.findOptions.suffix.length);
        var selector = $(this.pf("ddlSelector_") + index + this.findOptions.suffix + " option:selected");
        var value = $(this.pf("value_") + index + this.findOptions.suffix).val();

        var valBool = $("input:checkbox[id=" + this.findOptions.prefix + "value_" + index + this.findOptions.suffix + "]"); //it's a checkbox
        if (valBool.length > 0) value = valBool[0].checked;

        var info = RuntimeInfoFor(this.findOptions.prefix + "value_" + index + this.findOptions.suffix);
        if (info.find().length > 0) //If it's a Lite, the value is the Id
            value = info.id() + ";" + info.runtimeType();

        var filter = new Object();
        filter["cn" + index] = columnName;
        filter["sel" + index] = selector.val();
        filter["val" + index] = value;
        return filter;
    },

    onSearchOk: function() {
        log("FindNavigator onSearchOk");
        var selected = this.selectedItems();
        if (selected.length == 0)
            return;
        var doDefault = (this.findOptions.onOk != null) ? this.findOptions.onOk(selected) : true;
        if (doDefault != false) {
            $('#' + this.tempDivId()).remove();
            if (this.findOptions.onOkClosed != null)
                this.findOptions.onOkClosed();
        }
    },

    onSearchCancel: function() {
        log("FindNavigator onSearchCancel");
        $('#' + this.tempDivId()).remove();
        if (this.findOptions.onCancelled != null)
            this.findOptions.onCancelled();
    },

    newFilterRowIndex: function() {
        log("FindNavigator newFilterRowIndex");
        var lastRow = $(this.pf("tblFilters") + this.findOptions.suffix + " tbody tr:last");
        var lastRowIndex = -1;
        if (lastRow.length == 1)
            lastRowIndex = lastRow[0].id.substr(lastRow[0].id.lastIndexOf("_") + 1, lastRow[0].id.length);
        return parseInt(lastRowIndex) + 1;
    },

    addFilter: function() {
        log("FindNavigator addFilter");
        var selectedColumn = $(this.pf("ddlNewFilters") + this.findOptions.suffix + " option:selected");
        if (selectedColumn.length == 0) return;

        var tableFilters = $(this.pf("tblFilters") + this.findOptions.suffix + " tbody");
        if (tableFilters.length == 0)
            throw "Adding filters is not allowed";

        var filterType = selectedColumn.val();
        var optionId = selectedColumn[0].id;
        var filterName = optionId.substring(optionId.indexOf("__") + 2, optionId.length);

        var self = this;
        $.ajax({
            type: "POST",
            url: "Signum/AddFilter",
            data: { "filterType": filterType, "columnName": filterName, "displayName": selectedColumn.html(), "index": this.newFilterRowIndex(), "prefix": this.findOptions.prefix, "suffix": this.findOptions.suffix },
            async: false,
            dataType: "html",
            success: function(filterHtml) {
                $(self.pf("filters-list" + self.findOptions.suffix + " .explanation")).hide();
                $(self.pf("filters-list" + self.findOptions.suffix + " table")).show('fast');
                tableFilters.append(filterHtml);
            }
        });
    },

    quickFilter: function(idTD) {
        log("FindNavigator quickFilter");
        var tableFilters = $(this.pf("tblFilters") + this.findOptions.suffix + " tbody");
        if (tableFilters.length == 0)
            return;
        var params;
        var ahref = $('#' + idTD + ' a');
        if (ahref.length == 0)
            params = { "isLite": "false", "sfValue": $('#' + idTD).html() };
        else {
            var route = ahref.attr("href");
            var separator = route.indexOf("/");
            var lastSeparator = route.lastIndexOf("/");
            params = { "isLite": "true", "typeUrlName": route.substring(separator + 1, lastSeparator), "sfId": route.substring(lastSeparator + 1, route.length) };
        }
        var idTDnoPrefix = idTD.substring(this.findOptions.prefix.length, idTD.length);
        var colIndex = parseInt(idTDnoPrefix.substring(idTDnoPrefix.indexOf("_") + 1, idTDnoPrefix.length)) - 1
        var self = this;
        $.ajax({
            type: "POST",
            url: "Signum/QuickFilter",
            data: $.extend(params, { "sfQueryUrlName": $(this.pf("sfQueryUrlName") + this.findOptions.suffix).val(), "sfColIndex": colIndex, "prefix": this.findOptions.prefix, "suffix": this.findOptions.suffix, "index": this.newFilterRowIndex() }),
            async: false,
            dataType: "html",
            success: function(filterHtml) {
                $(self.pf("filters-list") + self.findOptions.suffix + " .explanation").hide();
                $(self.pf("filters-list") + self.findOptions.suffix + " table").show('fast');
                tableFilters.append(filterHtml);
            }
        });
    },

    deleteFilter: function(index) {
        log("FindNavigator deleteFilter");
        var tr = $(this.pf("trFilter_" + index) + this.findOptions.suffix)
        if (tr.length == 0) return;

        if ($(this.pf("trFilter_" + index) + this.findOptions.suffix + " select[disabled]").length == 0) tr.remove();
        if ($(this.pf("tblFilters") + this.findOptions.suffix + " tbody tr").length == 0) {
            $(this.pf("filters-list") + this.findOptions.suffix + " .explanation").show();
            $(this.pf("filters-list") + this.findOptions.suffix + " table").hide('fast');
        }
    },

    clearAllFilters: function() {
        log("FindNavigator clearAllFilters");
        var self = this;
        $(this.pf("tblFilters") + this.findOptions.suffix + " > tbody > tr").each(function(index) {
            self.deleteFilter(this.id.substring(this.id.lastIndexOf("_") + 1, this.id.length - self.findOptions.suffix.length));
        });
    },

    requestDataForSearchPopupCreate: function() {
        var requestData = this.serializeFilters();
        var requestData = $.extend(requestData, { sfQueryUrlName: ((empty(this.findOptions.queryUrlName)) ? $(this.pf("sfQueryUrlName") + this.findOptions.suffix).val() : this.findOptions.queryUrlName) });
        return requestData;
    },

    viewOptionsForSearchCreate: function(_viewOptions) {
        log("FindNavigator viewOptionsForSearchCreate");
        if (this.findOptions.prefix != _viewOptions.prefix)
            throw "FindOptions prefix and ViewOptions prefix don't match";
        _viewOptions.prefix = _viewOptions.prefix + "New";
        var self = this;
        return $.extend({
            type: $(this.pf(sfEntityTypeName) + this.findOptions.suffix).val(),
            containerDiv: null,
            onCancelled: null,
            controllerUrl: empty(this.findOptions.prefix) ? "Signum/Create" : "Signum/PopupCreate"
        }, _viewOptions);
    },

    viewOptionsForSearchPopupCreate: function(_viewOptions) {
        log("FindNavigator viewOptionsForSearchPopupCreate");
        if (this.findOptions.prefix != _viewOptions.prefix)
            throw "FindOptions prefix and ViewOptions prefix don't match";
        _viewOptions.prefix = _viewOptions.prefix + "New";
        var self = this;
        return $.extend({
            type: $(this.pf(sfEntityTypeName) + this.findOptions.suffix).val(),
            containerDiv: null,
            requestExtraJsonData: this.requestDataForSearchPopupCreate(),
            onCancelled: null,
            controllerUrl: empty(this.findOptions.prefix) ? "Signum/Create" : "Signum/PopupCreate"
        }, _viewOptions);
    }
};

function OpenFinder(_findOptions) {
    new FindNavigator(_findOptions).openFinder();
}

function Search(_findOptions) {
    new FindNavigator(_findOptions).search();
}

function SelectedItems(_findOptions) {
    return new FindNavigator(_findOptions).selectedItems();
}

function SplitSelectedIds(_findOptions) {
    return new FindNavigator(_findOptions).splitSelectedIds();
}

function AddFilter(prefix, suffix) {
    new FindNavigator({ prefix: prefix, suffix: suffix }).addFilter();
}

function QuickFilter(prefix, suffix, idTd) {
    new FindNavigator({ prefix: prefix, suffix: suffix }).quickFilter(idTd);
}

function DeleteFilter(prefix, suffix, index) {
    new FindNavigator({ prefix: prefix, suffix: suffix }).deleteFilter(index);
}

function ClearAllFilters(prefix, suffix) {
    new FindNavigator({ prefix: prefix, suffix: suffix }).clearAllFilters();
}

function SearchCreate(viewOptions, suffix){
    var findNavigator = new FindNavigator({prefix: viewOptions.prefix, suffix: suffix});
    if (empty(viewOptions.prefix)) {
        var viewOptions = findNavigator.viewOptionsForSearchCreate(viewOptions);
        new ViewNavigator(viewOptions).navigate();
    }
    else{
        var viewOptions = findNavigator.viewOptionsForSearchPopupCreate(viewOptions);
        new ViewNavigator(viewOptions).createSave();
    }
}

function toggleVisibility(elementId) {
	$('#' + elementId).toggle();
}

function toggleFilters(id) {
    var elem = $('#' + id + " .filters-header");
    var R = elem.attr('rev');
    var D = $('#' + R);
    D.toggle('fast');
    $('#' + id + ' .filters').toggle('fast');
    elem.toggleClass('close');
    if (elem.hasClass('close')) elem.html('Mostrar filtros'); else elem.html('Ocultar filtros');
    return false;
}

var concurrentSearch = new Array();
function SearchOnLoad(btnSearchId) {
    concurrentSearch[btnSearchId] = true;
	$("#" + btnSearchId).click();
}

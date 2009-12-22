var FindNavigator = function(_findOptions) {
    this.findOptions = $.extend({
        prefix: "",
        suffix: "S", //To allow multiple Search controls in one page
        queryUrlName: null,
        searchOnLoad: false,
        allowMultiple: null,
        create: true,
        top: null,
        filters: null,
        filterMode: null,
        navigatorControllerUrl: "Signum/PartialFind",
        searchControllerUrl: "Signum/Search",
        onOk: null,
        onCancelled: null,
        async: false
    }, _findOptions);
};

FindNavigator.prototype = {

    pf: function(s) {
        return "#" + this.findOptions.prefix + s;
    },

    tempDivId: function() {
        return this.findOptions.prefix + "Temp";
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
                $('#' + self.findOptions.prefix + sfBtnOkS).unbind('click').click(function() { self.onSearchOk(); });
                $('#' + self.findOptions.prefix + sfBtnCancelS).unbind('click').click(function() { self.onSearchCancel(); });
            }
        });
    },

    selectedItems: function() {
        log("FindNavigator selectedItems");
        var items = new Array();
        var selected = $("input:radio[name=" + this.findOptions.prefix + "rowSelection]:checked, #" + this.findOptions.prefix + "tdRowSelection input:checked");
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
            item.link = $('#' + this.id).parent().next('#' + self.findOptions.prefix + 'tdResults').children('a').attr('href');
            items[i] = item;
        });

        return items;
    },

    search: function() {
        //	var async = concurrentSearch[prefix + "btnSearch"];
        //	if (async) concurrentSearch[prefix + "btnSearch"]=false;
        var btnSearch = $(this.pf("btnSearch"));
        btnSearch.toggleClass('loading').val(lang['searching']);
        var self = this;
        $.ajax({
            type: "POST",
            url: this.findOptions.searchControllerUrl,
            data: this.requestData(),
            async: this.findOptions.async,
            dataType: "html",
            success: function(resultsHtml) {
                $('#' + self.findOptions.prefix + "divResults").html(resultsHtml);
            }
        });
        btnSearch.val(lang['buscar']).toggleClass('loading');
    },

    requestData: function() {
        var requestData = sfQueryUrlName + "=" + ((empty(this.findOptions.queryUrlName)) ? $(this.pf("sfQueryUrlName")).val() : this.findOptions.queryUrlName);
        requestData += qp(sfTop, empty(this.findOptions.top) ? $(this.pf(sfTop)).val() : this.findOptions.top);
        requestData += qp(sfAllowMultiple, (this.findOptions.allowMultiple == undefined) ? $(this.pf(sfAllowMultiple)).val() : this.findOptions.allowMultiple);
        requestData += qp(sfSearchOnLoad, this.findOptions.searchOnLoad);

        if (this.findOptions.async)
            requestData += qp("sfAsync", this.findOptions.async);

        if (this.findOptions.filterMode != null)
            requestData += qp("sfFilterMode", this.findOptions.filterMode);

        if (this.findOptions.create)
            requestData += qp("sfCreate", this.findOptions.create);
            
        var currentfilters = (empty(this.findOptions.filters)) ? this.serializeFilters() : this.findOptions.filters;
        if (!empty(currentfilters))
            requestData += currentfilters

        requestData += qp(sfPrefix, this.findOptions.prefix);
        requestData += qp(sfSuffix, this.findOptions.suffix);

        return requestData;
    },

    serializeFilters: function() {
        var result = "";
        var self = this;
        $(this.pf("tblFilters > tbody > tr")).each(function() {
            result += self.serializeFilter(this.id.substr(this.id.lastIndexOf("_") + 1, this.id.length));
        });
        return result;
    },

    serializeFilter: function(index) {
        var tds = $(this.pf("trFilter_") + index + " td");
        var columnName = tds[0].id.substr(tds[0].id.indexOf("__") + 2, tds[0].id.length);
        var selector = $(this.pf("ddlSelector_") + index + " option:selected");
        var value = $(this.pf("value_") + index).val();

        var valBool = $("input:checkbox[id=" + this.findOptions.prefix + "value_" + index + "]"); //it's a checkbox
        if (valBool.length > 0) value = valBool[0].checked;

        var info = EntityInfoFor(this.findOptions.prefix + "value_" + index);
        if (info.find().length > 0) //If it's a Lite, the value is the Id
            value = info.id() + ";" + info.runtimeType();

        return qp("cn" + index, columnName) + qp("sel" + index, selector.val()) + qp("val" + index, value);
    },

    onSearchOk: function() {
        log("FindNavigator onSearchOk");
        var selected = this.selectedItems();
        if (selected.length == 0)
            return;
        var doDefault = (this.findOptions.onOk != null) ? this.findOptions.onOk(selected) : true;
        if (doDefault != false)
            $('#' + this.tempDivId()).remove();
    },

    onSearchCancel: function() {
        log("FindNavigator onSearchCancel");
        $('#' + this.tempDivId()).remove();
        if (this.findOptions.onCancelled != null)
            this.findOptions.onCancelled();
    },

    newFilterRowIndex: function() {
        log("FindNavigator newFilterRowIndex");
        var lastRow = $(this.pf("tblFilters") + " tbody tr:last");
        var lastRowIndex = -1;
        if (lastRow.length == 1)
            lastRowIndex = lastRow[0].id.substr(lastRow[0].id.lastIndexOf("_") + 1, lastRow[0].id.length);
        return parseInt(lastRowIndex) + 1;
    },

    addFilter: function() {
        log("FindNavigator addFilter");
        var selectedColumn = $(this.pf("ddlNewFilters") + " option:selected");
        if (selectedColumn.length == 0) return;

        var tableFilters = $(this.pf("tblFilters") + " tbody");
        if (tableFilters.length == 0)
            throw "Adding filters is not allowed";

        var filterType = selectedColumn.val();
        var optionId = selectedColumn[0].id;
        var filterName = optionId.substring(optionId.indexOf("__") + 2, optionId.length);

        var self = this;
        $.ajax({
            type: "POST",
            url: "Signum/AddFilter",
            data: "filterType=" + filterType + qp("columnName", filterName) + qp("displayName", selectedColumn.html()) + qp("index", this.newFilterRowIndex()) + qp(sfPrefix, this.findOptions.prefix),
            async: false,
            dataType: "html",
            success: function(filterHtml) {
                $(self.pf("filters-list .explanation")).hide();
                $(self.pf("filters-list table")).show('fast');
                tableFilters.append(filterHtml);
            }
        });
    },

    quickFilter: function(idTD) {
        log("FindNavigator quickFilter");
        var tableFilters = $(this.pf("tblFilters") + " tbody");
        if (tableFilters.length == 0)
            return;
        var params = "";
        var ahref = $('#' + idTD + ' a');
        if (ahref.length == 0)
            params = qp("isLite", "false") + qp("sfValue", $('#' + idTD).html());
        else {
            var route = ahref.attr("href");
            var separator = route.indexOf("/");
            var lastSeparator = route.lastIndexOf("/");
            params = qp("isLite", "true") + qp("typeUrlName", route.substring(separator + 1, lastSeparator)) + qp("sfId", route.substring(lastSeparator + 1, route.length));
        }
        var idTDnoPrefix = idTD.substring(this.findOptions.prefix.length, idTD.length);
        var colIndex = parseInt(idTDnoPrefix.substring(idTDnoPrefix.indexOf("_") + 1, idTDnoPrefix.length)) - 1
        var self = this;
        $.ajax({
            type: "POST",
            url: "Signum/QuickFilter",
            data: "sfQueryUrlName=" + $(this.pf("sfQueryUrlName")).val() + qp("sfColIndex", colIndex) + params + qp(sfPrefix, this.findOptions.prefix) + qp("index", this.newFilterRowIndex()),
            async: false,
            dataType: "html",
            success: function(filterHtml) {
                $(self.pf("filters-list .explanation")).hide();
                $(self.pf("filters-list table")).show('fast');
                tableFilters.append(filterHtml);
            }
        });
    },

    deleteFilter: function(index) {
        log("FindNavigator deleteFilter");
        var tr = $(this.pf("trFilter_" + index))
        if (tr.length == 0) return;

        if ($(this.pf("trFilter_" + index) + " select[disabled]").length == 0) tr.remove();
        if ($(this.pf("tblFilters tbody tr")).length == 0) {
            $(this.pf("filters-list .explanation")).show();
            $(this.pf("filters-list table")).hide('fast');
        }
    },

    clearAllFilters: function() {
        log("FindNavigator clearAllFilters");
        var self = this;
        $(this.pf("tblFilters > tbody > tr")).each(function(index) {
            self.deleteFilter(this.id.substr(this.id.lastIndexOf("_") + 1, this.id.length));
        });
    },

    viewOptionsForSearchCreate: function(_viewOptions) {
        log("FindNavigator viewOptionsForSearchCreate");
        if (this.findOptions.prefix != _viewOptions.prefix)
            throw "FindOptions prefix and ViewOptions prefix don't match";
        _viewOptions.prefix = _viewOptions.prefix + "New";
        var self = this;
        return $.extend({
            type: $(this.pf(sfEntityTypeName)).val(),
            containerDiv: null,
            //onOk: function(clonedElements) { return self.onCreatingOK(clonedElements, defaultValidateUrl, _viewOptions.type); },
            onCancelled: null,
            controllerUrl: empty(this.findOptions.prefix) ? "Signum/Create" : "Signum/PopupView"
        }, _viewOptions);
    },

    viewOptionsForSearchPopupCreate: function(_viewOptions) {
        log("FindNavigator viewOptionsForSearchPopupCreate");
        if (this.findOptions.prefix != _viewOptions.prefix)
            throw "FindOptions prefix and ViewOptions prefix don't match";
        _viewOptions.prefix = _viewOptions.prefix + "New";
        var self = this;
        return $.extend({
            type: $(this.pf(sfEntityTypeName)).val(),
            containerDiv: null,
            //onOk: function(clonedElements) { return self.onCreatingOK(clonedElements, defaultValidateUrl, _viewOptions.type); },
            onCancelled: null,
            controllerUrl: empty(this.findOptions.prefix) ? "Signum/Create" : "Signum/PopupView"
        }, _viewOptions);
    }
};

function OpenFinder(_findOptions) {
    new FindNavigator(_findOptions).openFinder();
}

function Search(_findOptions) {
    new FindNavigator(_findOptions).search();
}

function AddFilter(prefix) {
    new FindNavigator({ prefix: prefix }).addFilter();
}

function QuickFilter(prefix, idTd) {
    new FindNavigator({ prefix: prefix }).quickFilter(idTd);
}

function DeleteFilter(prefix, index) {
    new FindNavigator({ prefix: prefix }).deleteFilter(index);
}

function ClearAllFilters(prefix) {
    new FindNavigator({ prefix: prefix }).clearAllFilters();
}

function SearchCreate(viewOptions){
    var findNavigator = new FindNavigator({prefix: viewOptions.prefix});
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

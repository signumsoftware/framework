/// <reference path="globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Navigator = require("Framework/Signum.Web/Signum/Scripts/Navigator")

export interface FindOptions {
    allowChangeColumns?: boolean;
    allowOrder?: boolean;
    allowSelection?: boolean;
    multipleSelection?: boolean;
    columnMode?: ColumnOptionsMode;
    columns?: ColumnOption[]; 
    create?: boolean;
    elems?: number;
    selectedItemsContextMenu?: boolean;
    filterMode?: FilterMode;
    filters?: FilterOption[]; 
    navigate?: boolean;
    openFinderUrl?: boolean;
    orders?: OrderOption[]; 
    prefix: string;
    webQueryName: string;
    searchOnLoad?: boolean;
}

export interface FilterOption {
    columnName: string;
    operation: FilterOperation;
    value: string
}

export enum FilterOperation {
    EqualTo,
    DistinctTo,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Contains,
    StartsWith,
    EndsWith,
    Like,
    NotContains,
    NotStartsWith,
    NotEndsWith,
    NotLike,
    IsIn,
}

export enum FilterMode {
    Visible,
    Hidden,
    AlwaysHidden,
    OnlyResults,
}


export interface OrderOption {
    columnName: string;
    orderType: OrderType;
}

export enum OrderType {
    Ascending,
    Descending
}


export interface ColumnOption {
    columnName: string;
    displayName: string;
}

export enum ColumnOptionsMode {
    Add,
    Remove,
    Replace,
}

export function getFor(prefix: string): Promise<SearchControl> {
    return $("#" + SF.compose(prefix, "sfSearchControl")).SFControl<SearchControl>();
}

export function findMany(findOptions: FindOptions): Promise<Array<Entities.EntityValue>> {
    findOptions.allowSelection = true;
    findOptions.multipleSelection = true;
    return findInternal(findOptions);
}

export function find(findOptions: FindOptions): Promise<Entities.EntityValue> {
    findOptions.allowSelection = true;
    findOptions.multipleSelection = false;
    return findInternal(findOptions).then(array=> array == null ? null : array[0]);
}

function findInternal(findOptions: FindOptions): Promise<Array<Entities.EntityValue>> {
    return new Promise<Array<Entities.EntityValue>>(function (resolve) {
        $.ajax({
            url: findOptions.openFinderUrl || SF.Urls.partialFind,
            data: requestDataForOpenFinder(findOptions, false),
            async: false,
            success: function (popupHtml) {
                var divId = SF.compose(findOptions.prefix, "Temp");
                $("body").append(SF.hiddenDiv(divId, popupHtml));
                var div = $("#" + divId);
                SF.triggerNewContent(div);
                   
                div.popup({
                    onOk: function () {

                        getFor(findOptions.prefix).then(sc=>{

                            var items = sc.selectedItems();
                            if (items.length == 0) {
                                SF.Notify.info(lang.signum.noElementsSelected);
                                return null;
                            }

                            if (items.length > 1 && !findOptions.multipleSelection) {
                                SF.Notify.info(lang.signum.onlyOneElement);
                                return;
                            }

                            div.remove();

                            resolve(items);
                        });
                    },
                    onCancel: function () { div.remove(); resolve(null); }
                });
            }
        });
    });
}

export function explore(findOptions: FindOptions): Promise<void> {
    return new Promise<void>(function (resolve) {
        $.ajax({
            url: findOptions.openFinderUrl || SF.Urls.partialFind,
            data: requestDataForOpenFinder(findOptions, true),
            async: false,
            success: function (popupHtml) {
                var divId = SF.compose(findOptions.prefix, "Temp");
                $("body").append(SF.hiddenDiv(divId, popupHtml));
                var div = $("#" + divId);
                SF.triggerNewContent(div);

                var sc = getFor(findOptions.prefix)

                    //$.extend(sc.options, findOptions); //Copy all properties (i.e. onOk was not transmitted)
                div.popup({
                    onCancel: function () { div.remove(); resolve(null); }
                });
            }
        });
    });
}

export function requestDataForOpenFinder(findOptions: FindOptions, isExplore : boolean) {
    var requestData = {
        webQueryName: findOptions.webQueryName,
        elems: findOptions.elems,
        allowSelection: findOptions.allowSelection,
        prefix: findOptions.prefix
    };

    if (findOptions.navigate == false) {
        requestData["navigate"] = findOptions.navigate;
    }
    if (findOptions.searchOnLoad == true) {
        requestData["searchOnLoad"] = findOptions.searchOnLoad;
    }
    if (findOptions.filterMode != null) {
        requestData["filterMode"] = findOptions.filterMode;
    }
    if (!findOptions.create) {
        requestData["create"] = findOptions.create;
    }
    if (!findOptions.allowChangeColumns) {
        requestData["allowChangeColumns"] = findOptions.allowChangeColumns;
    }
    if (findOptions.filters != null) {
        requestData["filters"] = findOptions.filters.map(f=> f.columnName + "," + FilterOperation[f.operation] + "," + SearchControl.encodeCSV(f.value)).join(";");//List of filter names "token1,operation1,value1;token2,operation2,value2"
    }
    if (findOptions.orders != null) {
        requestData["orders"] = serializeOrders(findOptions.orders);
    }
    if (findOptions.columns != null) {
        requestData["columns"] = findOptions.columns.map(c=> c.columnName + "," + c.displayName).join(";");//List of column names "token1,displayName1;token2,displayName2"
    }
    if (findOptions.columnMode != null) {
        requestData["columnMode"] = findOptions.columnMode;
    }

    requestData["isExplore"] = isExplore;

    return requestData;
}

function serializeOrders(orders: OrderOption[]) {
    return orders.map(f=> (f.orderType == OrderType.Ascending ? "" : "-") + f.columnName).join(";");//A Json array like ["Id","-Name"] => Id asc, then Name desc
}

export function deleteFilter(trId) {
    var $tr = $("tr#" + trId);
    if ($tr.find("select[disabled]").length > 0) {
        return;
    }

    if ($tr.siblings().length == 0) {
        var $filterList = $tr.closest(".sf-filters-list");
        $filterList.find(".sf-explanation").show();
        $filterList.find("table").hide();
    }

    $tr.remove();
}

export function removeOverlay() {
    $('.sf-search-ctxmenu-overlay').remove();
}





export class SearchControl {

    element: JQuery;

    keys = {
        elems: "sfElems",
        page: "sfPage",
        pagination: "sfPaginationMode"
    };

    options: FindOptions;

    creating: () => void;

    constructor(element: JQuery, _options: FindOptions) {
        element.data("SF-control", this);

        this.element = element;

        this.options = $.extend({
            allowChangeColumns: true,
            allowOrder: true,
            allowSelection: true,
            allowMultiple: true,
            columnMode: "Add",
            columns: null,
            create: true,
            elems: null,
            selectedItemsContextMenu: true,
            filterMode: "Visible",
            filters: null,
            navigate: true,
            openFinderUrl: null,
            orders: [], //A Json array like ["Id","-Name"] => Id asc, then Name desc
            prefix: "",
            searchOnLoad: false,
            webQueryName: null
        }, _options);

        this._create();
    }

    public ready() {
        this.element.SFControlFullfill(this); 
    }

    public pf(s) {
        return "#" + SF.compose(this.options.prefix, s);
    }

    public closeMyOpenedCtxMenu() {
        if (this.element.find(".sf-search-ctxmenu-overlay").length > 0) {
            $('.sf-search-ctxmenu-overlay').remove();
            return false;
        }

        return true;
    }

    _create() {
        var self = this;


        var $tblResults = self.element.find(".sf-search-results-container");

        if (this.options.allowOrder) {
            $tblResults.on("click", "th:not(.sf-th-entity):not(.sf-th-selection),th:not(.sf-th-entity):not(.sf-th-selection) span,th:not(.sf-th-entity):not(.sf-th-selection) .sf-header-droppable", function (e) {
                if (e.target != this || $(this).closest(".sf-search-ctxmenu").length > 0) {
                    return;
                }
                self.newSortOrder($(e.target).closest("th"), e.shiftKey);
                self.search();
                return false;
            });
        }

        if (this.options.allowChangeColumns || (this.options.filterMode != FilterMode.AlwaysHidden && this.options.filterMode != FilterMode.OnlyResults)) {
            $tblResults.on("contextmenu", "th:not(.sf-th-entity):not(.sf-th-selection)", function (e) {
                if (!self.closeMyOpenedCtxMenu()) {
                    return false;
                }
                self.headerContextMenu(e);
                return false;
            });
        }

        if (this.options.allowChangeColumns) {
            $tblResults.on("click", ".sf-search-ctxitem.sf-remove-column > span", function () {
                var $elem = $(this).closest("th");
                $('.sf-search-ctxmenu-overlay').remove();

                self.removeColumn($elem);
                return false;
            });

            $tblResults.on("click", ".sf-search-ctxitem.sf-edit-column > span", function () {
                var $elem = $(this).closest("th");
                $('.sf-search-ctxmenu-overlay').remove();

                self.editColumn($elem);
                return false;
            });

            this.createMoveColumnDragDrop();
        }

        if (this.options.filterMode != FilterMode.AlwaysHidden && this.options.filterMode != FilterMode.OnlyResults) {
            $tblResults.on("contextmenu", "td:not(.sf-td-no-results):not(.sf-td-multiply,.sf-search-footer-pagination)", function (e) {
                if (!self.closeMyOpenedCtxMenu()) {
                    return false;
                }

                var $td = $(this).closest("td");

                var $tr = $td.closest("tr");
                var $currentRowSelector = $tr.find(".sf-td-selection");
                if ($currentRowSelector.filter(":checked").length == 0) {
                    self.changeRowSelection($(self.pf("sfSearchControl .sf-td-selection:checked")), false);
                    self.changeRowSelection($currentRowSelector, true);
                }

                var index = $td.index();
                var $th = $td.closest("table").find("th").eq(index);
                if ($th.hasClass('sf-th-selection') || $th.hasClass('sf-th-entity')) {
                    if (self.options.selectedItemsContextMenu == true) {
                        self.entityContextMenu(e);
                    }
                }
                else {
                    self.cellContextMenu(e);
                }
                return false;
            });

            $tblResults.on("click", ".sf-search-ctxitem.sf-quickfilter > span", function () {
                var $elem = $(this).closest("td");
                $('.sf-search-ctxmenu-overlay').remove();
                self.quickFilterCell($elem);
            });

            $tblResults.on("click", ".sf-search-ctxitem.sf-quickfilter-header > span", function () {
                var $elem = $(this).closest("th");
                $('.sf-search-ctxmenu-overlay').remove();
                self.quickFilterHeader($elem);
                return false;
            });
        }

        if (this.options.filterMode != FilterMode.OnlyResults) {
            $tblResults.on("click", ".sf-pagination-button", function () {
                $(self.pf(self.keys.page)).val($(this).attr("data-page"));
                self.search();
            });

            $tblResults.on("change", ".sf-pagination-size", function () {
                if ($(this).find("option:selected").val() == "All") {
                    self.clearResults();
                }
                else {
                    self.search();
                }
            });

            $tblResults.on("change", ".sf-td-selection", function () {
                self.changeRowSelection($(this), $(this).filter(":checked").length > 0);
            });

            $(this.pf("sfFullScreen")).on("mousedown", function (e) {
                e.preventDefault();
                self.fullScreen(e);
            });

            this.element.on("sf-new-subtokens-combo", function (event, ...args) {
                self.newSubTokensComboAdded($("#" + args[0]));
            });

            this.element.find(".sf-tm-selected").click(function () {
                if (!self.closeMyOpenedCtxMenu()) {
                    return false;
                }

                self.ctxMenuInDropdown($(this).closest(".sf-dropdown"));
            });
        }

        $tblResults.on("selectstart", "th:not(.sf-th-entity):not(.sf-th-selection)", function (e) {
            return false;
        });

        if (this.options.searchOnLoad) {
            this.searchOnLoad();
        }
    }



    changeRowSelection($rowSelectors, select : boolean) {
        $rowSelectors.prop("checked", select);
        $rowSelectors.closest("tr").toggleClass("ui-state-active", select);

        var $control = $(this.pf("sfSearchControl"));

        var selected = $control.find(".sf-td-selection:checked").length;
        $control.find(".sf-tm-selected > .ui-button-text").html(lang.signum.searchControlMenuSelected + " (" + selected + ")");
    }

    createCtxMenu($rightClickTarget) {
        var left = $rightClickTarget.position().left + ($rightClickTarget.outerWidth() / 2);
        var top = $rightClickTarget.position().top + ($rightClickTarget.outerHeight() / 2);

        var $cmenu = $("<div class='ui-state-default sf-search-ctxmenu'></div>");
        $cmenu.css({
            left: left,
            top: top,
            zIndex: '101'
        });

        var $ctxMenuOverlay = $('<div class="sf-search-ctxmenu-overlay"></div>').click(function (e) {
            var $clickTarget = $(e.target);
            if ($clickTarget.hasClass("sf-search-ctxitem") || $clickTarget.parent().hasClass("sf-search-ctxitem"))
                $cmenu.hide();
            else
                $('.sf-search-ctxmenu-overlay').remove();
        }).append($cmenu);

        return $ctxMenuOverlay;
    }

    headerContextMenu(e) {
        var $th = $(e.target).closest("th");
        var $menu = this.createCtxMenu($th);

        var $itemContainer = $menu.find(".sf-search-ctxmenu");
        if (this.options.filterMode != FilterMode.AlwaysHidden && this.options.filterMode != FilterMode.OnlyResults){
            $itemContainer.append("<div class='sf-search-ctxitem sf-quickfilter-header'><span>" + lang.signum.addFilter + "</span></div>");
        }

        if (this.options.allowChangeColumns) {
            $itemContainer.append("<div class='sf-search-ctxitem sf-edit-column'><span>" + lang.signum.editColumnName + "</span></div>")
                .append("<div class='sf-search-ctxitem sf-remove-column'><span>" + lang.signum.removeColumn + "</span></div>");
        }

        $th.append($menu);
        return false;
    }

    cellContextMenu(e) {
        var $td = $(e.target);
        var $menu = this.createCtxMenu($td);

        $menu.find(".sf-search-ctxmenu")
            .html("<div class='sf-search-ctxitem sf-quickfilter'><span>" + lang.signum.addFilter + "</span></div>");

        $td.append($menu);
        return false;
    }

    requestDataForContextMenu() {
        return {
            liteKeys: this.element.find(".sf-td-selection:checked").closest("tr").map(function () { return $(this).data("entity"); }).toArray().join(","),
            webQueryName: this.options.webQueryName,
            prefix: this.options.prefix,
            implementationsKey: $(this.pf(Entities.Keys.entityTypeNames)).val()
        };
    }

    entityContextMenu(e) {
        var $td = $(e.target).closest("td");

        var $menu = this.createCtxMenu($td);
        var $itemContainer = $menu.find(".sf-search-ctxmenu");

        $.ajax({
            url: SF.Urls.selectedItemsContextMenu,
            data: this.requestDataForContextMenu(),
            success: function (items) {
                $itemContainer.html(items);
                $td.append($menu);
                SF.triggerNewContent($menu);
            }
        });

        return false;
    }

    ctxMenuInDropdown($dropdown) {
        if ($dropdown.hasClass("sf-open")) {
            var requestData = this.requestDataForContextMenu();
            if (SF.isEmpty(requestData.implementationsKey)) {
                return;
            }

            var loadingClass = "sf-tm-selected-loading";

            var $ul = $dropdown.children(".sf-menu-button");
            $ul.html($("<li></li>").addClass(loadingClass).html($("<span></span>").addClass("sf-query-button").html(lang.signum.loading)));

            $.ajax({
                url: SF.Urls.selectedItemsContextMenu,
                data: requestData,
                success: function (items) {
                    $ul.find("li").removeClass(loadingClass).html(items);
                    SF.triggerNewContent($ul);
                }
            });
        }
    }

    fullScreen(evt) {

        var urlParams = this.requestDataForSearchInUrl();

        var url = this.element.attr("data-find-url") + urlParams;
        if (evt.ctrlKey || evt.which == 2) {
            window.open(url);
        }
        else if (evt.which == 1) {
            window.location.href = url;
        }
    }

    search() {
        var $searchButton = $(this.pf("qbSearch"));
        $searchButton.addClass("sf-searching");
        var count = parseInt($searchButton.attr("data-searchCount")) || 0;
        var self = this;
        $.ajax({
            url: SF.Urls.search,
            data: this.requestDataForSearch(),
            success: function (r) {
                var $tbody = self.element.find(".sf-search-results-container tbody");
                if (!SF.isEmpty(r)) {
                    $tbody.html(r);
                    SF.triggerNewContent(self.element.find(".sf-search-results-container tbody"));
                }
                else {
                    $tbody.html("");
                }
                $searchButton.removeClass("sf-searching");
                $searchButton.attr("data-searchCount", count + 1);
            }
        });
    }

    requestDataForSearchInUrl(): string {
        var form = this.requestDataForSearch();
        form["webQueryName"] = undefined;

        return $.param(form);
    }

    requestDataForSearch() : FormObject {
        var requestData: FormObject = {};
        requestData["webQueryName"] = this.options.webQueryName;
        requestData["pagination"] = $(this.pf(this.keys.pagination)).val();
        requestData["elems"] = $(this.pf(this.keys.elems)).val();
        requestData["page"] = ($(this.pf(this.keys.page)).val() || "1");
        requestData["allowSelection"] = this.options.allowSelection;
        requestData["navigate"] = this.options.navigate;
        requestData["filters"] = this.serializeFilters();
        requestData["filterMode"] = this.options.filterMode;
        requestData["orders"] = this.serializeOrders();
        requestData["columns"] = this.serializeColumns();
        requestData["columnMode"] = 'Replace';

        requestData["prefix"] = this.options.prefix;
        return requestData;
    }


    serializeFilters() {

        return $(this.pf("tblFilters > tbody > tr")).toArray().map(f=> {
            var $filter = $(f);

            var id = $filter[0].id;
            var index = id.afterLast("_");

            var selector = $(SF.compose(this.pf("ddlSelector"), index) + " option:selected", $filter);

            var value = this.encodeValue($filter, index);

            return $filter.find("td:nth-child(2) > :hidden").val() + "," + selector.val() + "," + value;
        }).join(";");
    }

    encodeValue($filter: JQuery, index: string) {
        var valBool = $("input:checkbox[id=" +  SF.compose(this.options.prefix, "value", index) + "]", $filter); //it's a checkbox
        if (valBool.length > 0)
            return (<HTMLInputElement> valBool[0]).checked;

        var infoElem = $("#" + SF.compose(this.options.prefix, "value", index, Entities.Keys.runtimeInfo));
        if (infoElem.length > 0) { //If it's a Lite, the value is the Id
            var val = Entities.RuntimeInfo.parse(infoElem.val()); 
            return SearchControl.encodeCSV(val == null ? null : val.key());
        }

        return SearchControl.encodeCSV($(SF.compose(this.pf("value"), index), $filter).val());

    }

    static encodeCSV(value: string) {
        if (!value)
            return "";

        var hasQuote = value.indexOf("\"") != -1;
        if (hasQuote || value.indexOf(",") != -1 || value.indexOf(";") != -1) {
            if (hasQuote)
                value = value.replace(/"/g, "\"\"");
            return "\"" + value + "\"";
        }

        return value;
    }

    serializeOrders() {
        return serializeOrders(this.options.orders);
    }

    serializeColumns() {
        var self = this;
        return $(this.pf("tblResults thead tr th:not(.sf-th-entity):not(.sf-th-selection)")).toArray().map(th=> {
            var $this = $(th);
            var token = $this.find("input:hidden").val();
            var displayName = $this.text().trim();
            if (token == displayName)
                return token;
            else
                return token + "," + displayName;
        }).join(";");
    }

    static getSelectedItems(prefix: string) : Array<Entities.EntityValue> {
        return $("input:checkbox[name^=" + SF.compose(prefix, "rowSelection") + "]:checked").toArray().map(v=> {
            var parts = (<HTMLInputElement>v).value.split("__");
            return new Entities.EntityValue(new Entities.RuntimeInfo(parts[1], parseInt(parts[0]), false),
                parts[2],
                $(v).parent().next().children('a').attr('href'));
        }); 
    }

    static liteKeys(values: Array<Entities.EntityValue>): string {
        return values.map(v=> v.runtimeInfo.key()).join(",");
    }

    selectedItems(): Array<Entities.EntityValue> {
        return SearchControl.getSelectedItems(this.options.prefix); 
    }

    hasSelectedItems(onSuccess: (item: Array<Entities.EntityValue>) => void) {
        var items = this.selectedItems();
        if (items.length == 0) {
            SF.Notify.info(lang.signum.noElementsSelected);
            return;
        }
        onSuccess(items);
    }

    hasSelectedItem(onSuccess: (item: Entities.EntityValue) => void) {
        var items = this.selectedItems();
        if (items.length == 0) {
            SF.Notify.info(lang.signum.noElementsSelected);
            return;
        }
        else if (items.length > 1) {
            SF.Notify.info(lang.signum.onlyOneElement);
            return;
        }
        onSuccess(items[0]);
    }

    selectedKeys() {
        return this.selectedItems().map(function (item) { return item.runtimeInfo.key(); }).join(',');
    }

    newSortOrder($th : JQuery, multiCol : boolean) {
        var columnName = $th.find("input:hidden").val();
        
        var cols = this.options.orders.filter(o=> o.columnName == columnName);
        var col = cols.length == 0 ? null : cols[0];


        var oposite = col == null ? OrderType.Ascending :
            col.orderType == OrderType.Ascending ? OrderType.Descending : OrderType.Ascending;
     

        if (!multiCol) {
            this.element.find(".sf-search-results-container th").removeClass("sf-header-sort-up sf-header-sort-down");
            this.options.orders = [{ columnName: columnName, orderType: oposite }];
        }
        else {
            if (col !== null) {
                col.orderType = oposite;
            }
            else {
                this.options.orders.push({ columnName: columnName, orderType: oposite });
            }
        }

        if (oposite == OrderType.Descending)
            $th.removeClass("sf-header-sort-down").addClass("sf-header-sort-up");
        else
            $th.removeClass("sf-header-sort-up").addClass("sf-header-sort-down");
    }

    addColumn() {
        if (!this.options.allowChangeColumns || $(this.pf("tblFilters tbody")).length == 0) {
            throw "Adding columns is not allowed";
        }

        var tokenName = QueryTokenBuilder.constructTokenName(this.options.prefix);
        if (SF.isEmpty(tokenName)) {
            return;
        }

        var prefixedTokenName = SF.compose(this.options.prefix, tokenName);
        if ($(this.pf("tblResults thead tr th[id=\"" + prefixedTokenName + "\"]")).length > 0) {
            return;
        }

        var $tblHeaders = $(this.pf("tblResults thead tr"));

        var self = this;
        $.ajax({
            url: $(this.pf("btnAddColumn")).attr("data-url"),
            data: { "webQueryName": this.options.webQueryName, "tokenName": tokenName },
            async: false,
            success: function (columnNiceName) {
                $tblHeaders.append("<th class='ui-state-default'>" +
                    "<div class='sf-header-droppable sf-header-droppable-right'></div>" +
                    "<div class='sf-header-droppable sf-header-droppable-left'></div>" +
                    "<input type=\"hidden\" value=\"" + tokenName + "\" />" +
                    "<span>" + columnNiceName + "</span></th>");
                var $newTh = $tblHeaders.find("th:last");
                self.createMoveColumnDragDrop($newTh, $newTh.find(".sf-header-droppable"));
            }
        });
    }

    editColumn($th : JQuery) {
        var colName = $th.text().trim();

        var popupPrefix = SF.compose(this.options.prefix, "newName");

        var divId = SF.compose(popupPrefix, "Temp");
        var $div = $("<div id='" + divId + "'></div>");
        $div.html("<p>" + lang.signum.enterTheNewColumnName + "</p>")
            .append("<br />")
            .append("<input type='text' value='" + colName + "' />")
            .append("<br />").append("<br />")
            .append("<input type='button' id='" + SF.compose(popupPrefix, "btnOk") + "' class='sf-button sf-ok-button' value='OK' />");

        var $tempContainer = $("<div></div>").append($div);
        SF.triggerNewContent($tempContainer);
        $tempContainer.popup({
            onOk: function () { $th.find("span").text($div.find("input:text").val()); $tempContainer.remove();  },
            onCancel: function () { $tempContainer.remove(); }
        });
    }


    moveColumn($source : JQuery, $target : JQuery, before : boolean) {
        if (before) {
            $target.before($source);
        }
        else {
            $target.after($source);
        }

        $source.removeAttr("style"); //remove absolute positioning
        this.clearResults();
        this.createMoveColumnDragDrop();
    }

    createMoveColumnDragDrop($draggables? : JQuery, $droppables? : JQuery) {
        $draggables = $draggables || $(this.pf("tblResults") + " th:not(.sf-th-entity):not(.sf-th-selection)");
        $droppables = $droppables || $(this.pf("tblResults") + " .sf-header-droppable");

        $draggables.draggable({
            revert: "invalid",
            axis: "x",
            opacity: 0.5,
            distance: 8,
            cursor: "move"
        });
        $draggables.removeAttr("style"); //remove relative positioning

        var self = this;
        $droppables.droppable({
            hoverClass: "sf-header-droppable-active",
            tolerance: "pointer",
            drop: function (event, ui) {
                var $dragged = ui.draggable;

                var $targetPlaceholder = $(this); //droppable
                var $targetCol = $targetPlaceholder.closest("th");

                self.moveColumn($dragged, $targetCol, $targetPlaceholder.hasClass("sf-header-droppable-left"));
            }
        });
    }

    removeColumn($th) {
        $th.remove();
        this.clearResults();
    }

    clearResults() {
        var $tbody = $(this.pf("tblResults tbody"));
        $tbody.find("tr:not('.sf-search-footer')").remove();
        $tbody.prepend($("<tr></tr>").append($("<td></td>").attr("colspan", $tbody.find(".sf-search-footer td").attr("colspan"))));
    }

    toggleFilters() {
        var $toggler = this.element.find(".sf-filters-header");
        this.element.find(".sf-filters").toggle();
        $toggler.toggleClass('close');
        if ($toggler.hasClass('close')) {
            $toggler.find(".ui-button-icon-primary").removeClass("ui-icon-triangle-1-n").addClass("ui-icon-triangle-1-e");
            $toggler.find(".ui-button-text").html(lang.signum.showFilters);
        }
        else {
            $toggler.find(".ui-button-icon-primary").removeClass("ui-icon-triangle-1-e").addClass("ui-icon-triangle-1-n");
            $toggler.find(".ui-button-text").html(lang.signum.hideFilters);
        }
        return false;
    }

    addFilter(url?, requestExtraJsonData?) {
        var tableFilters = $(this.pf("tblFilters tbody"));
        if (tableFilters.length == 0) {
            throw "Adding filters is not allowed";
        }

        var tokenName = QueryTokenBuilder.constructTokenName(this.options.prefix);
        if (SF.isEmpty(tokenName)) {
            return;
        }

        var data = $.extend({
            webQueryName: this.options.webQueryName,
            tokenName: tokenName,
            index: this.newFilterRowIndex(),
            prefix: this.options.prefix
        }, requestExtraJsonData);

        var self = this;
        $.ajax({
            url: url || SF.Urls.addFilter,
            data: data,
            async: false,
            success: function (filterHtml) {
                var $filterList = self.element.closest(".sf-search-control").find(".sf-filters-list");
                $filterList.find(".sf-explanation").hide();
                $filterList.find("table").show();

                tableFilters.append(filterHtml);
                SF.triggerNewContent($(self.pf("tblFilters tbody tr:last")));
            }
        });
    }

    newFilterRowIndex(): number {
        var lastRow = $(this.pf("tblFilters tbody tr:last"));
        if (lastRow.length == 1) {
            return parseInt(lastRow[0].id.substr(lastRow[0].id.lastIndexOf("_") + 1, lastRow[0].id.length)) + 1;
        }
        return 0;
    }

    newSubTokensComboAdded($selectedCombo: JQuery) {
        var $btnAddFilter = $(this.pf("btnAddFilter"));
        var $btnAddColumn = $(this.pf("btnAddColumn"));

        var self = this;
        var $selectedOption = $selectedCombo.children("option:selected");
        $selectedCombo.attr("title", $selectedOption.attr("title"));
        $selectedCombo.attr("style", $selectedOption.attr("style"));
        if ($selectedOption.val() == "") {
            var $prevSelect = $selectedCombo.prev("select");
            if ($prevSelect.length == 0) {
                this.changeButtonState($btnAddFilter, lang.signum.selectToken);
                this.changeButtonState($btnAddColumn, lang.signum.selectToken);
            }
            else {
                var $prevSelectedOption = $prevSelect.find("option:selected");
                this.changeButtonState($btnAddFilter, $prevSelectedOption.attr("data-filter"), function () { self.addFilter(); });
                this.changeButtonState($btnAddColumn, $prevSelectedOption.attr("data-column"), function () { self.addColumn(); });
            }
            return;
        }

        this.changeButtonState($btnAddFilter, $selectedOption.attr("data-filter"), function () { self.addFilter(); });
        this.changeButtonState($btnAddColumn, $selectedOption.attr("data-column"), function () { self.addColumn(); });
    }

    changeButtonState($button: JQuery, disablingMessage: string, enableCallback?: (eventObject: JQueryEventObject) => any) {
        var hiddenId = $button.attr("id") + "temp";
        if (typeof disablingMessage != "undefined") {
            $button.addClass("ui-button-disabled").addClass("ui-state-disabled").addClass("sf-disabled").attr("disabled", "disabled").attr("title", disablingMessage);
            $button.unbind('click').bind('click', function (e) { e.preventDefault(); return false; });
        }
        else {
            var self = this;
            $button.removeClass("ui-button-disabled").removeClass("ui-state-disabled").removeClass("sf-disabled").prop("disabled", null).attr("title", "");
            $button.unbind('click').bind('click', enableCallback);
        }
    }




    quickFilter(value, tokenName) {
        var tableFilters = $(this.pf("tblFilters tbody"));
        if (tableFilters.length === 0) {
            return;
        }

        var params = {
            "value": value,
            "webQueryName": this.options.webQueryName,
            "tokenName": tokenName,
            "prefix": this.options.prefix,
            "index": this.newFilterRowIndex()
        };

        var self = this;
        $.ajax({
            url: SF.Urls.quickFilter,
            data: params,
            async: false,
            success: function (filterHtml) {
                var $filterList = self.element.find(".sf-filters-list");
                $filterList.find(".sf-explanation").hide();
                $filterList.find("table").show();

                tableFilters.append(filterHtml);
                SF.triggerNewContent($(self.pf("tblFilters tbody tr:last")));
            }
        });
    }

    quickFilterCell($elem) {
        var value = $elem.data("value");
        if (typeof value == "undefined") {
            value = $elem.html().trim()
                }

        var cellIndex = $elem[0].cellIndex;
        var tokenName = $($($elem.closest(".sf-search-results")).find("th")[cellIndex]).children("input:hidden").val();

        this.quickFilter(value, tokenName);
    }

    quickFilterHeader($elem) {
        this.quickFilter("", $elem.find("input:hidden").val());
    }

    create_click() {
        this.onCreate();
    }

    onCreate() {
        if (this.creating != null)
            this.creating();
        else
            this.getEntityType().then(type => {
                if (type == null)
                    return;

                var requestData = this.requestDataForSearchPopupCreate()

                var runtimeInfo = new Entities.RuntimeInfo(type, null, true);
                if (SF.isEmpty(this.options.prefix))
                    Navigator.navigate(runtimeInfo, false);
                else
                    Navigator.navigatePopup(new Entities.EntityHtml(SF.compose(this.options.prefix, "Temp"), runtimeInfo), { requestExtraJsonData: requestData });
            });
    }

    getEntityType(): Promise<string> {
        var names = (<string>$(this.pf(Entities.Keys.entityTypeNames)).val()).split(","); 
        var niceNames = (<string>$(this.pf(Entities.Keys.entityTypeNiceNames)).val()).split(","); 

        var options = names.map((p, i)=> ({
            type: p,
            toStr: niceNames[i]
        }));
        if (options.length == 1) {
            return Promise.resolve(options[0].type);
        }
        return Navigator.chooser(this.options.prefix, lang.signum.chooseAType, options).then(o=> o == null ? null : o.type);
    }


    viewOptionsForSearchCreate(viewOptions) {
        return $.extend({
            controllerUrl: SF.Urls.create
        }, viewOptions);
    }

    viewOptionsForSearchPopupCreate(viewOptions) {
        return $.extend({
            controllerUrl: SF.Urls.popupNavigate,
            requestExtraJsonData: this.requestDataForSearchPopupCreate()
        }, viewOptions);
    }

    requestDataForSearchPopupCreate() {
        return {
            filters: this.serializeFilters(),
            webQueryName: this.options.webQueryName
        };
    }

    toggleSelectAll() {
        var select = $(this.pf("cbSelectAll:checked"));
        this.changeRowSelection($(this.pf("sfSearchControl .sf-td-selection")), (select.length > 0) ? true : false);
    }

    searchOnLoadFinished = false;

    searchOnLoad() {
        var btnSearchId = SF.compose(this.options.prefix, "qbSearch");
        var $button = $("#" + btnSearchId);
        var self = this;
        var makeSearch = function () {
            if (!self.searchOnLoadFinished) {
                $button.click();
                self.searchOnLoadFinished = true;
            }
        };

        var $tabContainer = $button.closest(".sf-tabs");
        if ($tabContainer.length == 0) {
            makeSearch();
        }
        else {
            var self = this;
            $tabContainer.bind("tabsactivate", function (evt, ui) {
                if ($(ui.newPanel).find(self.element).length > 0) {
                    makeSearch();
                }
            });
        }
    }
}

export module QueryTokenBuilder {

    export function init(containerId : string, webQueryName: string, controllerUrl: string, requestExtraJsonData: any) {
        $("#" + containerId).on("change", "select", function () {
            tokenChanged($(this), webQueryName, controllerUrl, requestExtraJsonData);
        });
    }

    export function tokenChanged($selectedCombo: JQuery, webQueryName : string,  controllerUrl : string, requestExtraJsonData : any) {

        var prefix = $selectedCombo.attr("id").before("ddlTokens_");
        if (prefix.endsWith("_"))
            prefix = prefix.substr(0, prefix.length - 1);

        var index = parseInt($selectedCombo.attr("id").after("ddlTokens_"));

        clearChildSubtokenCombos($selectedCombo, prefix, index);
        $selectedCombo.trigger("sf-new-subtokens-combo", $selectedCombo.attr("id"));
        
        var $selectedOption = $selectedCombo.children("option:selected");
        if ($selectedOption.val() == "") {
            return;
        }

        var tokenName = constructTokenName(prefix);

        var data = $.extend({
            webQueryName: webQueryName,
            tokenName: tokenName,
            index: index,
            prefix: prefix
        }, requestExtraJsonData);

        $.ajax({
            url: controllerUrl,
            data: data,
            dataType: "html",
            success: function (newHtml) {
                $selectedCombo.parent().html(newHtml);
            }
        });
    };

    export function clearChildSubtokenCombos($selectedCombo: JQuery, prefix: string, index: number) {
        $selectedCombo.next("select,input[type=hidden]").remove();
    }

    export function constructTokenName(prefix) {
        var tokenName = "";
        var stop = false;
        for (var i = 0; ; i++) {
            var currSubtoken = $("#" + SF.compose(prefix, "ddlTokens_" + i));
            if (currSubtoken.length == 0)
                break;

            var part = currSubtoken.val();
            tokenName = !tokenName ? part :
            !part ? tokenName :
            (tokenName + "." + part);
        }
        return tokenName;
    }
}


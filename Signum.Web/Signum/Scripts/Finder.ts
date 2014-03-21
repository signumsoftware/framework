/// <reference path="globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Navigator = require("Framework/Signum.Web/Signum/Scripts/Navigator")

export var doubleScroll = true;

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

export enum RequestType {
    QueryRequest,
    FindOptions,
    FullScreen
}

function findInternal(findOptions: FindOptions): Promise<Array<Entities.EntityValue>> {
    return SF.ajaxPost({
        url: findOptions.openFinderUrl || SF.Urls.partialFind,
        data: requestDataForOpenFinder(findOptions, false)
    }).then(modalDivHtml => {

            var modalDiv = $(modalDivHtml);

            var okButtonId = SF.compose(findOptions.prefix, "btnOk");

            var items: Entities.EntityValue[];
            return Navigator.openModal(modalDiv, button => {

                if (button.id != okButtonId)
                    return Promise.resolve(true);

                return getFor(findOptions.prefix).then(sc=> {

                    items = sc.selectedItems();
                    if (items.length == 0) {
                        SF.Notify.info(lang.signum.noElementsSelected);
                        return false;
                    }

                    if (items.length > 1 && !findOptions.multipleSelection) {
                        SF.Notify.info(lang.signum.onlyOneElement);
                        return false;
                    }

                    return true;
                });
            }).then(pair => pair.button.id == okButtonId ? items : null);
        });
}

export function explore(findOptions: FindOptions): Promise<void> {
    return SF.ajaxPost({
        url: findOptions.openFinderUrl || SF.Urls.partialFind,
        data: requestDataForOpenFinder(findOptions, true)
    }).then(modalDivHtml => Navigator.openModal($(modalDivHtml)))
        .then(() => null);
}

export function requestDataForOpenFinder(findOptions: FindOptions, isExplore: boolean) {
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



export class SearchControl {

    filterBuilder: FilterBuilder;
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

    public pf(s: string) {
        return "#" + SF.compose(this.options.prefix, s);
    }

    _create() {
        var self = this;

        this.filterBuilder = new FilterBuilder(
            $(this.pf("tblFilterBuilder")),
            this.options.prefix,
            this.options.webQueryName,
            SF.Urls.addFilter);

        this.filterBuilder.addColumnClicked = () => this.addColumn();


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
                self.headerContextMenu(e);
                return false;
            });
        }

        if (this.options.allowChangeColumns) {

            this.createMoveColumnDragDrop();
        }

        if (this.options.filterMode != FilterMode.AlwaysHidden && this.options.filterMode != FilterMode.OnlyResults) {
            $tblResults.on("contextmenu", "td:not(.sf-td-no-results):not(.sf-td-multiply,.sf-search-footer-pagination)", function (e) {

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

        }

        if (this.options.filterMode != FilterMode.OnlyResults) {
            this.element.on("click", ".sf-search-footer ul.pagination a", function () {
                self.search(parseInt($(this).attr("data-page")));
            });

            this.element.on("change", ".sf-search-footer .sf-pagination-size", function () {
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

            this.element.find(this.pf("btnSelected")).click(function () {
                self.ctxMenuInDropdown();
            });
        }

        $tblResults.on("selectstart", "th:not(.sf-th-entity):not(.sf-th-selection)", function (e) {
            return false;
        });

        if (doubleScroll) {
            var div = $(this.pf("divResults"));

            div.removeClass("table-responsive");
            div.css("overflow-x", "auto");

            var divUp = $("<div>")
                .attr("id", SF.compose(this.options.prefix, "divResults_Up"))
                .css("overflow-x", "auto")
                .css("overflow-y", "hidden")
                .css("height", "15")
                .insertBefore(div);

            var resultUp = $("<div>").attr("id", SF.compose(this.options.prefix, "tblResults_Up")).css("height", "1").appendTo(divUp)

            div.scroll(() => { this.syncSize(); divUp.scrollLeft(div.scrollLeft()); });
            divUp.scroll(() => { this.syncSize(); div.scrollLeft(divUp.scrollLeft()); });

            this.syncSize();

            window.onresize = () => this.syncSize();
        }

        if (this.options.searchOnLoad) {
            this.searchOnLoad();
        }
    }

    syncSize() {
        if (!doubleScroll)
            return;

        $(this.pf("tblResults_Up")).width($(this.pf("tblResults")).width());

        $(this.pf("divResults_Up")).css("height",
            $(this.pf("tblResults_Up")).width() > $(this.pf("divResults_Up")).width() ? "15" : "1");
    }

    changeRowSelection($rowSelectors, select: boolean) {
        $rowSelectors.prop("checked", select);
        $rowSelectors.closest("tr").toggleClass("active", select);

        var selected = this.element.find(".sf-td-selection:checked").length;

        this.element.find(this.pf("btnSelectedSpan")).text(selected);
        var btn = this.element.find(this.pf("btnSelected"));
        if (selected == 0)
            btn.attr("disabled", "disabled");
        else
            btn.removeAttr("disabled");
    }

    ctxMenuInDropdown() {

        var $dropdown = $(this.pf("btnSelectedDropDown"));

        if (!$dropdown.closest(".btn-group").hasClass("open")) {

            $dropdown.html(this.loadingMessage());

            SF.ajaxPost({
                url: SF.Urls.selectedItemsContextMenu,
                data: this.requestDataForContextMenu(),
            }).then(items => $dropdown.html(items || this.noActionsFoundMessage()));
        }
    }



    headerContextMenu(e: JQueryEventObject) {
        var $th = $(e.target).closest("th");
        var menu = SF.ContextMenu.createContextMenu(e);

        if (this.options.filterMode != FilterMode.AlwaysHidden && this.options.filterMode != FilterMode.OnlyResults) {
            menu.append($("<li>").append($("<a>").text(lang.signum.addFilter).addClass("sf-quickfilter-header").click(() => this.quickFilterHeader($th))));
        }


        if (this.options.allowChangeColumns) {
            menu
                .append($("<li>").append($("<a>").text(lang.signum.renameColumn).addClass("sf-edit-header").click(() => this.editColumn($th))))
                .append($("<li>").append($("<a>").text(lang.signum.removeColumn).addClass("sf-remove-header").click(() => this.removeColumn($th))));
        }
    }

    cellContextMenu(e: JQueryEventObject) {
        var $td = $(e.target).closest("td");
        var $menu = SF.ContextMenu.createContextMenu(e);

        if (this.options.filterMode != FilterMode.AlwaysHidden && this.options.filterMode != FilterMode.OnlyResults) {
            $menu.append($("<li>").append($("<a>").text(lang.signum.addFilter).addClass("sf-quickfilter").click(() => this.quickFilterCell($td))));
            $menu.append($("<li class='divider'></li>"));
        }

        var message = this.loadingMessage();

        $menu.append(message);

        SF.ajaxPost({
            url: SF.Urls.selectedItemsContextMenu,
            data: this.requestDataForContextMenu()
        }).then((items) => message.replaceWith(items || this.noActionsFoundMessage()));
    }

    requestDataForContextMenu() {
        return {
            liteKeys: this.element.find(".sf-td-selection:checked").closest("tr").map(function () { return $(this).data("entity"); }).toArray().join(","),
            webQueryName: this.options.webQueryName,
            prefix: this.options.prefix,
            implementationsKey: $(this.pf(Entities.Keys.entityTypeNames)).val()
        };
    }

    entityContextMenu(e: JQueryEventObject) {
        var $td = $(e.target).closest("td");

        var $menu = SF.ContextMenu.createContextMenu(e);

        $menu.html(this.loadingMessage());

        SF.ajaxPost({
            url: SF.Urls.selectedItemsContextMenu,
            data: this.requestDataForContextMenu()
        })
            .then((items) => {
                $menu.html(items || this.noActionsFoundMessage());
            });

        return false;
    }

    private loadingMessage() {
        return $("<li></li>").addClass("sf-tm-selected-loading").html($("<span></span>").html(lang.signum.loading));
    }

    private noActionsFoundMessage() {
        return $("<li></li>").addClass("sf-search-ctxitem-no-results").html($("<span></span>").html(lang.signum.noActionsFound));
    }

    fullScreen(evt) {
        var urlParams = this.requestDataForSearchInUrl();

        var url = this.element.attr("data-find-url") + "?" + urlParams;
        if (evt.ctrlKey || evt.which == 2) {
            window.open(url);
        }
        else if (evt.which == 1) {
            window.location.href = url;
        }
    }

    search(page?: number) {
        var $searchButton = $(this.pf("qbSearch"));
        $searchButton.addClass("sf-searching");
        var count = parseInt($searchButton.attr("data-searchCount")) || 0;
        var self = this;
        SF.ajaxPost({
            url: SF.Urls.search,
            data: this.requestDataForSearch(RequestType.QueryRequest, page)
        }).then(r => {
                var $tbody = self.element.find(".sf-search-results-container tbody");
                if (!SF.isEmpty(r)) {
                    var rows = $(r);

                    var divs = rows.filter("tr.extract").children().children();

                    this.element.find("div.sf-search-footer").replaceWith(divs.filter("div.sf-search-footer"));

                    var mult = divs.filter("div.sf-td-multiply");
                    var multCurrent = this.element.find("div.sf-td-multiply");

                    if (multCurrent.length)
                        multCurrent.replaceWith(mult);
                    else
                        this.element.find("div.sf-query-button-bar").after(mult);

                    $tbody.html(rows.not("tr.extract"));
                }
                else {
                    $tbody.html("");
                }
                $searchButton.removeClass("sf-searching");
                $searchButton.attr("data-searchCount", count + 1);
                this.syncSize();
            });
    }

    requestDataForSearchInUrl(): string {
        var page = $(this.pf(this.keys.page)).val() || 1
        var form = this.requestDataForSearch(RequestType.FullScreen, page);

        return $.param(form);
    }



    requestDataForSearch(type: RequestType, page?: number): FormObject {
        var requestData: FormObject = {};
        if (type != RequestType.FullScreen)
            requestData["webQueryName"] = this.options.webQueryName;

        requestData["pagination"] = $(this.pf(this.keys.pagination)).val();
        requestData["elems"] = $(this.pf(this.keys.elems)).val();
        requestData["page"] = page;
        requestData["allowSelection"] = this.options.allowSelection;
        requestData["navigate"] = this.options.navigate;
        requestData["filters"] = this.filterBuilder.serializeFilters();

        if (type != RequestType.FullScreen)
            requestData["filterMode"] = this.options.filterMode;

        requestData["orders"] = this.serializeOrders();
        requestData["columns"] = this.serializeColumns();
        requestData["columnMode"] = 'Replace';

        requestData["prefix"] = this.options.prefix;
        return requestData;
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
            var $th = $(th);
            var token = $th.data("column-name");
            var niceName = $th.data("nice-name");
            var displayName = $th.text().trim();
            if (niceName == displayName)
                return token;
            else
                return token + "," + displayName;
        }).join(";");
    }

    static getSelectedItems(prefix: string): Array<Entities.EntityValue> {
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

    selectedItemsLiteKeys(): string {
        return SearchControl.liteKeys(this.selectedItems());
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

    newSortOrder($th: JQuery, multiCol: boolean) {

        SF.ContextMenu.hideContextMenu();

        var columnName = $th.data("column-name");

        var cols = this.options.orders.filter(o=> o.columnName == columnName);
        var col = cols.length == 0 ? null : cols[0];

        var oposite = col == null ? OrderType.Ascending :
            col.orderType == OrderType.Ascending ? OrderType.Descending : OrderType.Ascending;
        var $sort = $th.find("span.sf-header-sort")
        if (!multiCol) {
            this.element.find("span.sf-header-sort").removeClass("asc desc l0 l1 l2 l3");
            $sort.addClass(oposite == OrderType.Ascending ? "asc" : "desc");
            this.options.orders = [{ columnName: columnName, orderType: oposite }];
        }
        else {
            if (col !== null) {
                col.orderType = oposite;
                $sort.removeClass("asc desc").addClass(oposite == OrderType.Ascending ? "asc" : "desc");
            }
            else {
                this.options.orders.push({ columnName: columnName, orderType: oposite });
                $sort.addClass(oposite == OrderType.Ascending ? "asc" : "desc").addClass("l" + (this.options.orders.length - 1 % 4));
            }
        }
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

        SF.ajaxPost({
            url: SF.Urls.addColumn,
            data: { "webQueryName": this.options.webQueryName, "tokenName": tokenName },
            async: false,
        }).then(html => { $tblHeaders.append(html); this.syncSize(); });
    }

    editColumn($th: JQuery) {
        var colName = $th.find("span").text().trim();

        Navigator.valueLineBox({
            prefix: SF.compose(this.options.prefix, "newName"),
            title: lang.signum.renameColumn,
            message: lang.signum.enterTheNewColumnName,
            value: colName,
            type: Navigator.ValueLineType.TextBox,
        }).then(result => {
                if (result)
                    $th.find("span:not(.sf-header-sort)").text(result);
                this.syncSize();
            });
    }

    moveColumn($source: JQuery, $target: JQuery, before: boolean) {
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

    createMoveColumnDragDrop() {

        var rowsSelector = ".sf-search-results th:not(.sf-th-entity):not(.sf-th-selection)";
        var current : HTMLTableHeaderCellElement = null;
        this.element.on("dragstart", rowsSelector, function (e: JQueryEventObject) {
            var de = <DragEvent><Event>e.originalEvent;
            de.dataTransfer.effectAllowed = "move";
            de.dataTransfer.setData("Text", $(this).attr("data-column-name")); 
            current = this;
        });  


        function dragClass(offsetX: number, width: number) {
            if (!offsetX)
                return null;

            if (width < 100 ? (offsetX < (width / 2)) : (offsetX < 50))
                return "drag-left";

            if (width < 100 ? (offsetX > (width / 2)) : (offsetX > (width - 50)))
                return "drag-right";

            return null;
        }
    
        var onDragOver = function (e: JQueryEventObject) {
            if (e.preventDefault) e.preventDefault();

            var de = <DragEvent><Event>e.originalEvent;
            if (this == current) {
                de.dataTransfer.dropEffect = "none";
                return;
            }

            $(this).removeClass("drag-left drag-right");
            $(this).addClass(dragClass(de.offsetX, $(this).width()));

            de.dataTransfer.dropEffect = "move";
        }
        this.element.on("dragover", rowsSelector, onDragOver);
        this.element.on("dragenter", rowsSelector, onDragOver);


        this.element.on("dragleave", rowsSelector, function () {
            $(this).removeClass("drag-left drag-right");
        });

        var me = this;
        this.element.on("drop", rowsSelector, function (e) {
            $(this).removeClass("drag-left drag-right");

            var de = <DragEvent><Event>e.originalEvent;

            var result = dragClass(de.offsetX, $(this).width());

            if (result)
                me.moveColumn($(current), $(this), result == "drag-left");
        });

        //$draggables = $draggables || $(this.pf("tblResults") + " ");
        //$droppables = $droppables || $(this.pf("tblResults") + " .sf-header-droppable");

        //$draggables.draggable({
        //    revert: "invalid",
        //    axis: "x",
        //    opacity: 0.5,
        //    distance: 8,
        //    cursor: "move"
        //});
        //$draggables.removeAttr("style"); //remove relative positioning

        //var self = this;
        //$droppables.droppable({
        //    hoverClass: "sf-header-droppable-active",
        //    tolerance: "pointer",
        //    drop: function (event, ui) {
        //        var $dragged = ui.draggable;

        //        var $targetPlaceholder = $(this); //droppable
        //        var $targetCol = $targetPlaceholder.closest("th");

        //        self.moveColumn($dragged, $targetCol, $targetPlaceholder.hasClass("sf-header-droppable-left"));
        //    }
        //});
    }

    removeColumn($th) {
        $th.remove();
        this.clearResults();
        this.syncSize();
    }

    clearResults() {
        var $tbody = $(this.pf("tblResults tbody"));
        $tbody.find("tr:not('.sf-search-footer')").remove();
        $tbody.prepend($("<tr></tr>").append($("<td></td>").attr("colspan", $tbody.find(".sf-search-footer td").attr("colspan"))));
    }

    toggleFilters() {
        var $toggler = this.element.find(".sf-filters-header");
        this.element.find(".sf-filters").toggle();
        $toggler.toggleClass('active');
        return false;
    }

    quickFilterCell($elem) {
        var value = $elem.data("value");
        if (typeof value == "undefined")
            value = $elem.html().trim()


        var cellIndex = $elem[0].cellIndex;
        var tokenName = $($($elem.closest(".sf-search-results")).find("th")[cellIndex]).data("column-name");

        this.filterBuilder.addFilter(tokenName, value);
    }

    quickFilterHeader($th) {
        this.filterBuilder.addFilter($th.data("column-name"), "");
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

                var runtimeInfo = new Entities.RuntimeInfo(type, null, true);
                if (SF.isEmpty(this.options.prefix))
                    Navigator.navigate(runtimeInfo, false);

                var requestData = this.requestDataForSearchPopupCreate();

                Navigator.navigatePopup(new Entities.EntityHtml(SF.compose(this.options.prefix, "Temp"), runtimeInfo), { requestExtraJsonData: requestData });
            });
    }

    getEntityType(): Promise<string> {
        var names = (<string>$(this.pf(Entities.Keys.entityTypeNames)).val()).split(",");
        var niceNames = (<string>$(this.pf(Entities.Keys.entityTypeNiceNames)).val()).split(",");

        var options = names.map((p, i) => ({
            type: p,
            toStr: niceNames[i]
        }));
        if (options.length == 1) {
            return Promise.resolve(options[0].type);
        }
        return Navigator.chooser(this.options.prefix, lang.signum.chooseAType, options).then(o=> o == null ? null : o.type);
    }

    requestDataForSearchPopupCreate() {
        return {
            filters: this.filterBuilder.serializeFilters(),
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
        if ($tabContainer.length == 0 || this.element.is(":visible")) {
            makeSearch();
        }
        else {
            var self = this;
            $tabContainer.bind("tabsactivate", function (evt, ui) { //OnVisible doesn't exist yet. 
                if ($(ui.newPanel).find(self.element).length > 0) {
                    makeSearch();
                }
            });
        }
    }
}

export class FilterBuilder {

    addColumnClicked: () => void;

    constructor(
        public element: JQuery,
        public prefix: string,
        public webQueryName: string,
        public url: string) {

        this.element.on("sf-new-subtokens-combo", (event, ...args) => {
            this.newSubTokensComboAdded($("#" + args[0]));
        });
    }

    public pf(s) {
        return "#" + SF.compose(this.prefix, s);
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
                this.changeButtonState($btnAddFilter, $prevSelectedOption.attr("data-filter"), function () { self.addFilterClicked(); });
                this.changeButtonState($btnAddColumn, $prevSelectedOption.attr("data-column"), function () { self.addColumnClicked(); });
            }
            return;
        }

        this.changeButtonState($btnAddFilter, $selectedOption.attr("data-filter"), function () { self.addFilterClicked(); });
        this.changeButtonState($btnAddColumn, $selectedOption.attr("data-column"), function () { self.addColumnClicked(); });
    }

    changeButtonState($button: JQuery, disablingMessage: string, enableCallback?: (eventObject: JQueryEventObject) => any) {

        if (!$button)
            return;

        var hiddenId = $button.attr("id") + "temp";
        if (typeof disablingMessage != "undefined") {
            $button.attr("disabled", "disabled").attr("title", disablingMessage);
            $button.unbind('click').bind('click', function (e) { e.preventDefault(); return false; });
        }
        else {
            var self = this;
            $button.removeAttr("disabled").attr("title", "");
            $button.unbind('click').bind('click', enableCallback);
        }
    }


    addFilterClicked() {
        var tokenName = QueryTokenBuilder.constructTokenName(this.prefix);
        if (SF.isEmpty(tokenName)) {
            return;
        }

        this.addFilter(tokenName, null);
    }

    addFilter(tokenName: string, value: string) {
        var tableFilters = $(this.pf("tblFilters tbody"));
        if (tableFilters.length == 0) {
            throw "Adding filters is not allowed";
        }

        var data = {
            webQueryName: this.webQueryName,
            tokenName: tokenName,
            value: value,
            index: this.newFilterRowIndex(),
            prefix: this.prefix
        };

        var self = this;
        SF.ajaxPost({
            url: this.url,
            data: data,
            async: false,
        }).then((filterHtml) => {
                var $filterList = self.element.find(".sf-filters-list");
                $filterList.find(".sf-explanation").hide();
                $filterList.find("table").show();

                tableFilters.append(filterHtml);
            });
    }

    newFilterRowIndex(): number {
        var lastRow = $(this.pf("tblFilters tbody tr:last"));
        if (lastRow.length == 1) {
            return parseInt(lastRow[0].id.substr(lastRow[0].id.lastIndexOf("_") + 1, lastRow[0].id.length)) + 1;
        }
        return 0;
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
        var id = SF.compose(this.prefix, "value", index);

        var eleme = $filter.find("#" + id);

        if (!eleme.length)
            throw Error("value for filter " + index + " no found");

        var date = $filter.find("#" + SF.compose(id, "Date"));
        var time = $filter.find("#" + SF.compose(id, "Time"));

        if (date.length && time.length)
            return SearchControl.encodeCSV(date.val() + " " + time.val());

        if (eleme.is("input:checkbox"))
            return (<HTMLInputElement> eleme[0]).checked;

        var infoElem = eleme.find("#" + SF.compose(id, Entities.Keys.runtimeInfo));
        if (infoElem.length > 0) { //If it's a Lite, the value is the Id
            var val = Entities.RuntimeInfo.parse(infoElem.val());
            return SearchControl.encodeCSV(val == null ? null : val.key());
        }

        return SearchControl.encodeCSV(eleme.val());
    }

}

export module QueryTokenBuilder {

    export function init(containerId: string, webQueryName: string, controllerUrl: string, requestExtraJsonData: any) {
        $("#" + containerId).on("change", "select", function () {
            tokenChanged($(this), webQueryName, controllerUrl, requestExtraJsonData);
        });
    }

    export function tokenChanged($selectedCombo: JQuery, webQueryName: string, controllerUrl: string, requestExtraJsonData: any) {

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

        SF.ajaxPost({
            url: controllerUrl,
            data: data,
            dataType: "html",
        }).then(newHtml => {
                $selectedCombo.parent().html(newHtml);
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


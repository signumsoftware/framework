/// <reference path="globals.ts"/>
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Entities", "Framework/Signum.Web/Signum/Scripts/Navigator"], function(require, exports, Entities, Navigator) {
    exports.doubleScroll = true;

    (function (FilterOperation) {
        FilterOperation[FilterOperation["EqualTo"] = 0] = "EqualTo";
        FilterOperation[FilterOperation["DistinctTo"] = 1] = "DistinctTo";
        FilterOperation[FilterOperation["GreaterThan"] = 2] = "GreaterThan";
        FilterOperation[FilterOperation["GreaterThanOrEqual"] = 3] = "GreaterThanOrEqual";
        FilterOperation[FilterOperation["LessThan"] = 4] = "LessThan";
        FilterOperation[FilterOperation["LessThanOrEqual"] = 5] = "LessThanOrEqual";
        FilterOperation[FilterOperation["Contains"] = 6] = "Contains";
        FilterOperation[FilterOperation["StartsWith"] = 7] = "StartsWith";
        FilterOperation[FilterOperation["EndsWith"] = 8] = "EndsWith";
        FilterOperation[FilterOperation["Like"] = 9] = "Like";
        FilterOperation[FilterOperation["NotContains"] = 10] = "NotContains";
        FilterOperation[FilterOperation["NotStartsWith"] = 11] = "NotStartsWith";
        FilterOperation[FilterOperation["NotEndsWith"] = 12] = "NotEndsWith";
        FilterOperation[FilterOperation["NotLike"] = 13] = "NotLike";
        FilterOperation[FilterOperation["IsIn"] = 14] = "IsIn";
    })(exports.FilterOperation || (exports.FilterOperation = {}));
    var FilterOperation = exports.FilterOperation;

    (function (OrderType) {
        OrderType[OrderType["Ascending"] = 0] = "Ascending";
        OrderType[OrderType["Descending"] = 1] = "Descending";
    })(exports.OrderType || (exports.OrderType = {}));
    var OrderType = exports.OrderType;

    (function (ColumnOptionsMode) {
        ColumnOptionsMode[ColumnOptionsMode["Add"] = 0] = "Add";
        ColumnOptionsMode[ColumnOptionsMode["Remove"] = 1] = "Remove";
        ColumnOptionsMode[ColumnOptionsMode["Replace"] = 2] = "Replace";
    })(exports.ColumnOptionsMode || (exports.ColumnOptionsMode = {}));
    var ColumnOptionsMode = exports.ColumnOptionsMode;

    function getFor(prefix) {
        return prefix.child("sfSearchControl").get().SFControl();
    }
    exports.getFor = getFor;

    function findMany(findOptions) {
        findOptions.allowSelection = true;
        return findInternal(findOptions, true);
    }
    exports.findMany = findMany;

    function find(findOptions) {
        findOptions.allowSelection = true;
        return findInternal(findOptions, false).then(function (array) {
            return array == null ? null : array[0];
        });
    }
    exports.find = find;

    (function (RequestType) {
        RequestType[RequestType["QueryRequest"] = 0] = "QueryRequest";
        RequestType[RequestType["FindOptions"] = 1] = "FindOptions";
        RequestType[RequestType["FullScreen"] = 2] = "FullScreen";
    })(exports.RequestType || (exports.RequestType = {}));
    var RequestType = exports.RequestType;

    function findInternal(findOptions, multipleSelection) {
        return SF.ajaxPost({
            url: findOptions.openFinderUrl || SF.Urls.partialFind,
            data: exports.requestDataForOpenFinder(findOptions, false)
        }).then(function (modalDivHtml) {
            var modalDiv = $(modalDivHtml);

            var okButtonId = findOptions.prefix.child("btnOk");

            var items;
            return Navigator.openModal(modalDiv, function (button) {
                if (button.id != okButtonId)
                    return Promise.resolve(true);

                return exports.getFor(findOptions.prefix).then(function (sc) {
                    items = sc.selectedItems();
                    if (items.length == 0 || items.length > 1 && !multipleSelection)
                        return false;

                    return true;
                });
            }, function (div) {
                exports.getFor(findOptions.prefix).then(function (sc) {
                    updateOkButton(okButtonId, sc.selectedItems().length, multipleSelection);
                    sc.selectionChanged = function (selected) {
                        return updateOkButton(okButtonId, selected.length, multipleSelection);
                    };
                });
            }).then(function (pair) {
                return pair.button.id == okButtonId ? items : null;
            });
        });
    }

    function updateOkButton(okButtonId, sel, multipleSelection) {
        var okButon = $("#" + okButtonId);
        if (sel == 0 || sel > 1 && !multipleSelection) {
            okButon.attr("disabled", "disabled");
            okButon.parent().tooltip({
                title: sel == 0 ? lang.signum.noElementsSelected : lang.signum.selectOnlyOneElement,
                placement: "top"
            });
        } else {
            okButon.removeAttr("disabled");
            okButon.parent().tooltip("destroy");
        }
    }

    function explore(findOptions) {
        return SF.ajaxPost({
            url: findOptions.openFinderUrl || SF.Urls.partialFind,
            data: exports.requestDataForOpenFinder(findOptions, true)
        }).then(function (modalDivHtml) {
            return Navigator.openModal($(modalDivHtml));
        }).then(function () {
            return null;
        });
    }
    exports.explore = explore;

    function requestDataForOpenFinder(findOptions, isExplore) {
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
        if (findOptions.showHeader == false) {
            requestData["showHeader"] = findOptions.showHeader;
        }
        if (findOptions.showFilters == false) {
            requestData["showFilters"] = findOptions.showFilters;
        }
        if (findOptions.showFilterButton == false) {
            requestData["showFilterButton"] = findOptions.showFilterButton;
        }
        if (findOptions.showFooter == false) {
            requestData["showFooter"] = findOptions.showFooter;
        }
        if (!findOptions.create) {
            requestData["create"] = findOptions.create;
        }
        if (!findOptions.allowChangeColumns) {
            requestData["allowChangeColumns"] = findOptions.allowChangeColumns;
        }
        if (findOptions.filters != null) {
            requestData["filters"] = findOptions.filters.map(function (f) {
                return f.columnName + "," + FilterOperation[f.operation] + "," + SearchControl.encodeCSV(f.value);
            }).join(";"); //List of filter names "token1,operation1,value1;token2,operation2,value2"
        }
        if (findOptions.orders != null) {
            requestData["orders"] = exports.serializeOrders(findOptions.orders);
        }
        if (findOptions.columns != null) {
            requestData["columns"] = findOptions.columns.map(function (c) {
                return c.columnName + "," + c.displayName;
            }).join(";"); //List of column names "token1,displayName1;token2,displayName2"
        }
        if (findOptions.columnMode != null) {
            requestData["columnMode"] = findOptions.columnMode;
        }

        requestData["isExplore"] = isExplore;

        return requestData;
    }
    exports.requestDataForOpenFinder = requestDataForOpenFinder;

    function serializeOrders(orders) {
        return orders.map(function (f) {
            return (f.orderType == 0 /* Ascending */ ? "" : "-") + f.columnName;
        }).join(";");
    }
    exports.serializeOrders = serializeOrders;

    function deleteFilter(trId) {
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
    exports.deleteFilter = deleteFilter;

    var SearchControl = (function () {
        function SearchControl(element, _options, types) {
            this.keys = {
                elems: "sfElems",
                page: "sfPage",
                pagination: "sfPaginationMode"
            };
            this.searchOnLoadFinished = false;
            element.data("SF-control", this);

            this.element = element;
            this.types = types;

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
                showHeader: true,
                showFilters: true,
                showFilterButton: true,
                showFooter: true,
                showContextMenu: true,
                filters: null,
                navigate: true,
                openFinderUrl: null,
                orders: [],
                prefix: "",
                searchOnLoad: false,
                webQueryName: null
            }, _options);

            this.prefix = this.options.prefix;

            this._create();
        }
        SearchControl.prototype.ready = function () {
            this.element.SFControlFullfill(this);
        };

        SearchControl.prototype._create = function () {
            var _this = this;
            this.filterBuilder = new FilterBuilder(this.prefix.child("tblFilterBuilder").get(), this.options.prefix, this.options.webQueryName, SF.Urls.addFilter);

            this.filterBuilder.addColumnClicked = function () {
                return _this.addColumn();
            };

            var $tblResults = this.element.find(".sf-search-results-container");

            if (this.options.allowOrder) {
                $tblResults.on("click", "th:not(.sf-th-entity):not(.sf-th-selection)", function (e) {
                    e.preventDefault();
                    SearchControl.newSortOrder(_this.options.orders, $(e.currentTarget), e.shiftKey);
                    _this.search();
                    return false;
                });
            }

            if (this.options.allowChangeColumns || this.options.showContextMenu) {
                $tblResults.on("contextmenu", "th:not(.sf-th-entity):not(.sf-th-selection)", function (e) {
                    _this.headerContextMenu(e);
                    return false;
                });
            }

            if (this.options.allowChangeColumns) {
                this.createMoveColumnDragDrop();
            }

            if (this.options.showContextMenu) {
                $tblResults.on("contextmenu", "td:not(.sf-td-no-results):not(.sf-td-multiply,.sf-search-footer-pagination)", function (e) {
                    var $td = $(e.currentTarget).closest("td");

                    var $tr = $td.closest("tr");
                    var $currentRowSelector = $tr.find(".sf-td-selection");
                    if ($currentRowSelector.filter(":checked").length == 0) {
                        _this.changeRowSelection(_this.prefix.child("sfSearchControl").get().find(".sf-td-selection:checked"), false);
                        _this.changeRowSelection($currentRowSelector, true);
                    }

                    var index = $td.index();
                    var $th = $td.closest("table").find("th").eq(index);
                    if ($th.hasClass('sf-th-selection') || $th.hasClass('sf-th-entity')) {
                        if (_this.options.selectedItemsContextMenu == true) {
                            _this.entityContextMenu(e);
                        }
                    } else {
                        _this.cellContextMenu(e);
                    }
                    return false;
                });
            }

            if (this.options.showFooter) {
                this.element.on("click", ".sf-search-footer ul.pagination a", function (e) {
                    e.preventDefault();
                    _this.search(parseInt($(e.currentTarget).attr("data-page")));
                });

                this.element.on("change", ".sf-search-footer .sf-pagination-size", function (e) {
                    if ($(e.currentTarget).find("option:selected").val() == "All") {
                        _this.clearResults();
                    } else {
                        _this.search();
                    }
                });
            }

            if (this.options.showContextMenu) {
                $tblResults.on("change", ".sf-td-selection", function (e) {
                    _this.changeRowSelection($(e.currentTarget), $(e.currentTarget).filter(":checked").length > 0);
                });

                this.prefix.child("sfFullScreen").get().on("mousedown", function (e) {
                    e.preventDefault();
                    _this.fullScreen(e);
                });

                this.prefix.child("btnSelected").get().click(function (e) {
                    _this.ctxMenuInDropdown();
                });
            }

            $tblResults.on("selectstart", "th:not(.sf-th-entity):not(.sf-th-selection)", function (e) {
                return false;
            });

            if (exports.doubleScroll) {
                var div = this.prefix.child("divResults").get();

                div.removeClass("table-responsive");
                div.css("overflow-x", "auto");

                var divUp = this.options.prefix.child("divResults_Up").tryGet();

                if (!divUp.length) {
                    divUp = $("<div>").attr("id", this.options.prefix.child("divResults_Up")).css("overflow-x", "auto").css("overflow-y", "hidden").css("height", "15").insertBefore(div);

                    var resultUp = $("<div>").attr("id", this.options.prefix.child("tblResults_Up")).css("height", "1").appendTo(divUp);
                }

                div.scroll(function () {
                    _this.syncSize();
                    divUp.scrollLeft(div.scrollLeft());
                });
                divUp.scroll(function () {
                    _this.syncSize();
                    div.scrollLeft(divUp.scrollLeft());
                });

                this.syncSize();

                window.onresize = function () {
                    return _this.syncSize();
                };
            }

            if (this.options.searchOnLoad) {
                this.searchOnLoad();
            }
        };

        SearchControl.prototype.syncSize = function () {
            if (!exports.doubleScroll)
                return;

            this.prefix.child("tblResults_Up").get().width(this.prefix.child("tblResults").get().width());

            this.prefix.child("divResults_Up").get().css("height", this.prefix.child("tblResults_Up").get().width() > this.prefix.child("divResults_Up").get().width() ? "15" : "1");
        };

        SearchControl.prototype.changeRowSelection = function ($rowSelectors, select) {
            $rowSelectors.prop("checked", select);
            $rowSelectors.closest("tr").toggleClass("active", select);

            this.updateSelectedButton();

            if (this.selectionChanged)
                this.selectionChanged(this.selectedItems());
        };

        SearchControl.prototype.updateSelectedButton = function () {
            var selected = this.element.find(".sf-td-selection:checked").length;

            this.prefix.child("btnSelectedSpan").get().text(selected);
            var btn = this.prefix.child("btnSelected").get();
            if (selected == 0)
                btn.attr("disabled", "disabled");
            else
                btn.removeAttr("disabled");
        };

        SearchControl.prototype.ctxMenuInDropdown = function () {
            var _this = this;
            var $dropdown = this.prefix.child("btnSelectedDropDown").get();

            if (!$dropdown.closest(".btn-group").hasClass("open")) {
                $dropdown.html(this.loadingMessage());

                SF.ajaxPost({
                    url: SF.Urls.selectedItemsContextMenu,
                    data: this.requestDataForContextMenu()
                }).then(function (items) {
                    return $dropdown.html(items || _this.noActionsFoundMessage());
                });
            }
        };

        SearchControl.prototype.headerContextMenu = function (e) {
            var _this = this;
            var $th = $(e.target).closest("th");
            var menu = SF.ContextMenu.createContextMenu(e);

            if (this.options.showHeader && (this.options.showFilterButton || this.options.showFilters)) {
                menu.append($("<li>").append($("<a>").text(lang.signum.addFilter).addClass("sf-quickfilter-header").click(function () {
                    return _this.quickFilterHeader($th);
                })));
            }

            if (this.options.allowChangeColumns) {
                menu.append($("<li>").append($("<a>").text(lang.signum.renameColumn).addClass("sf-edit-header").click(function () {
                    return _this.editColumn($th);
                }))).append($("<li>").append($("<a>").text(lang.signum.removeColumn).addClass("sf-remove-header").click(function () {
                    return _this.removeColumn($th);
                })));
            }
        };

        SearchControl.prototype.cellContextMenu = function (e) {
            var _this = this;
            var $td = $(e.target).closest("td");
            var $menu = SF.ContextMenu.createContextMenu(e);

            if (this.options.showHeader && (this.options.showFilterButton || this.options.showFilters)) {
                $menu.append($("<li>").append($("<a>").text(lang.signum.addFilter).addClass("sf-quickfilter").click(function () {
                    return _this.quickFilterCell($td);
                })));
                $menu.append($("<li class='divider'></li>"));
            }

            var message = this.loadingMessage();

            $menu.append(message);

            SF.ajaxPost({
                url: SF.Urls.selectedItemsContextMenu,
                data: this.requestDataForContextMenu()
            }).then(function (items) {
                return message.replaceWith(items || _this.noActionsFoundMessage());
            });
        };

        SearchControl.prototype.requestDataForContextMenu = function () {
            return {
                liteKeys: this.element.find(".sf-td-selection:checked").closest("tr").map(function () {
                    return $(this).data("entity");
                }).toArray().join(","),
                webQueryName: this.options.webQueryName,
                prefix: this.options.prefix,
                implementationsKey: this.types.map(function (a) {
                    return a.name;
                }).join(",")
            };
        };

        SearchControl.prototype.entityContextMenu = function (e) {
            var _this = this;
            var $td = $(e.target).closest("td");

            var $menu = SF.ContextMenu.createContextMenu(e);

            $menu.html(this.loadingMessage());

            SF.ajaxPost({
                url: SF.Urls.selectedItemsContextMenu,
                data: this.requestDataForContextMenu()
            }).then(function (items) {
                $menu.html(items || _this.noActionsFoundMessage());
            });

            return false;
        };

        SearchControl.prototype.loadingMessage = function () {
            return $("<li></li>").addClass("sf-tm-selected-loading").html($("<span></span>").html(lang.signum.loading));
        };

        SearchControl.prototype.noActionsFoundMessage = function () {
            return $("<li></li>").addClass("sf-search-ctxitem-no-results").html($("<span></span>").html(lang.signum.noActionsFound));
        };

        SearchControl.prototype.fullScreen = function (evt) {
            var urlParams = this.requestDataForSearchInUrl();

            var url = this.element.attr("data-find-url") + "?" + urlParams;
            if (evt.ctrlKey || evt.which == 2) {
                window.open(url);
            } else if (evt.which == 1) {
                window.location.href = url;
            }
        };

        SearchControl.prototype.search = function (page) {
            var _this = this;
            var $searchButton = this.prefix.child("qbSearch").get();
            $searchButton.addClass("sf-searching");
            var count = parseInt($searchButton.attr("data-searchCount")) || 0;
            var self = this;
            SF.ajaxPost({
                url: SF.Urls.search,
                data: this.requestDataForSearch(0 /* QueryRequest */, page)
            }).then(function (r) {
                var $tbody = self.element.find(".sf-search-results-container tbody");
                if (!SF.isEmpty(r)) {
                    var rows = $(r);

                    var divs = rows.filter("tr.extract").children().children();

                    _this.element.find("div.sf-search-footer").replaceWith(divs.filter("div.sf-search-footer"));

                    var mult = divs.filter("div.sf-td-multiply");
                    var multCurrent = _this.element.find("div.sf-td-multiply");

                    if (multCurrent.length)
                        multCurrent.replaceWith(mult);
                    else
                        _this.element.find("div.sf-query-button-bar").after(mult);

                    $tbody.html(rows.not("tr.extract"));
                } else {
                    $tbody.html("");
                }
                $searchButton.removeClass("sf-searching");
                $searchButton.attr("data-searchCount", count + 1);
                _this.syncSize();
                _this.updateSelectedButton();
            });
        };

        SearchControl.prototype.requestDataForSearchInUrl = function () {
            var page = this.prefix.child(this.keys.page).tryGet().val() || 1;
            var form = this.requestDataForSearch(2 /* FullScreen */, page);

            return $.param(form);
        };

        SearchControl.prototype.requestDataForSearch = function (type, page) {
            var requestData = {};
            if (type != 2 /* FullScreen */)
                requestData["webQueryName"] = this.options.webQueryName;

            requestData["pagination"] = this.prefix.child(this.keys.pagination).tryGet().val();
            requestData["elems"] = this.prefix.child(this.keys.elems).tryGet().val();
            requestData["page"] = page || 1;
            requestData["allowSelection"] = this.options.allowSelection;
            requestData["navigate"] = this.options.navigate;
            requestData["filters"] = this.filterBuilder.serializeFilters();

            if (type != 2 /* FullScreen */)
                requestData["showFooter"] = this.options.showFooter;

            requestData["orders"] = this.serializeOrders();
            requestData["columns"] = this.serializeColumns();
            requestData["columnMode"] = 'Replace';

            requestData["prefix"] = this.options.prefix;
            return requestData;
        };

        SearchControl.encodeCSV = function (value) {
            if (!value)
                return "";

            var hasQuote = value.indexOf("\"") != -1;
            if (hasQuote || value.indexOf(",") != -1 || value.indexOf(";") != -1) {
                if (hasQuote)
                    value = value.replace(/"/g, "\"\"");
                return "\"" + value + "\"";
            }

            return value;
        };

        SearchControl.prototype.serializeOrders = function () {
            return exports.serializeOrders(this.options.orders);
        };

        SearchControl.prototype.serializeColumns = function () {
            var self = this;
            return this.prefix.child("tblResults").get().find("thead tr th:not(.sf-th-entity):not(.sf-th-selection)").toArray().map(function (th) {
                var $th = $(th);
                var token = $th.data("column-name");
                var niceName = $th.data("nice-name");
                var displayName = $th.text().trim();
                if (niceName == displayName)
                    return token;
                else
                    return token + "," + displayName;
            }).join(";");
        };

        SearchControl.getSelectedItems = function (prefix) {
            return $("input:checkbox[name^=" + prefix.child("rowSelection") + "]:checked").toArray().map(function (v) {
                var parts = v.value.split("__");
                return new Entities.EntityValue(new Entities.RuntimeInfo(parts[1], parseInt(parts[0]), false), parts[2], $(v).parent().next().children('a').attr('href'));
            });
        };

        SearchControl.liteKeys = function (values) {
            return values.map(function (v) {
                return v.runtimeInfo.key();
            }).join(",");
        };

        SearchControl.prototype.selectedItems = function () {
            return SearchControl.getSelectedItems(this.options.prefix);
        };

        SearchControl.prototype.selectedItemsLiteKeys = function () {
            return SearchControl.liteKeys(this.selectedItems());
        };

        SearchControl.newSortOrder = function (orders, $th, multiCol) {
            SF.ContextMenu.hideContextMenu();

            var columnName = $th.data("column-name");

            var cols = orders.filter(function (o) {
                return o.columnName == columnName;
            });
            var col = cols.length == 0 ? null : cols[0];

            var oposite = col == null ? 0 /* Ascending */ : col.orderType == 0 /* Ascending */ ? 1 /* Descending */ : 0 /* Ascending */;
            var $sort = $th.find("span.sf-header-sort");
            if (!multiCol) {
                $th.closest("thead").find("span.sf-header-sort").removeClass("asc desc l0 l1 l2 l3");
                $sort.addClass(oposite == 0 /* Ascending */ ? "asc" : "desc");

                while (orders.length > 0)
                    orders.pop();

                orders.push({ columnName: columnName, orderType: oposite });
            } else {
                if (col !== null) {
                    col.orderType = oposite;
                    $sort.removeClass("asc desc").addClass(oposite == 0 /* Ascending */ ? "asc" : "desc");
                } else {
                    orders.push({ columnName: columnName, orderType: oposite });
                    $sort.addClass(oposite == 0 /* Ascending */ ? "asc" : "desc").addClass("l" + (orders.length - 1 % 4));
                }
            }
        };

        SearchControl.prototype.addColumn = function () {
            var _this = this;
            if (!this.options.allowChangeColumns || this.prefix.child("tblFilters").get().find("tbody").length == 0) {
                throw "Adding columns is not allowed";
            }

            var tokenName = QueryTokenBuilder.constructTokenName(this.options.prefix.child("tokenBuilder"));
            if (SF.isEmpty(tokenName)) {
                return;
            }

            var tblResults = this.prefix.child("tblResults").get();

            var prefixedTokenName = this.options.prefix.child(tokenName);
            if (tblResults.find("thead tr th[id=\"" + prefixedTokenName + "\"]").length > 0) {
                return;
            }

            var $tblHeaders = tblResults.find("thead tr");

            SF.ajaxPost({
                url: SF.Urls.addColumn,
                data: { "webQueryName": this.options.webQueryName, "tokenName": tokenName },
                async: false
            }).then(function (html) {
                $tblHeaders.append(html);
                _this.syncSize();
            });
        };

        SearchControl.prototype.editColumn = function ($th) {
            var _this = this;
            var colName = $th.find("span").text().trim();

            Navigator.valueLineBox({
                prefix: this.options.prefix.child("newName"),
                title: lang.signum.renameColumn,
                message: lang.signum.enterTheNewColumnName,
                value: colName,
                type: 4 /* TextBox */
            }).then(function (result) {
                if (result)
                    $th.find("span:not(.sf-header-sort)").text(result);
                _this.syncSize();
            });
        };

        SearchControl.prototype.moveColumn = function ($source, $target, before) {
            if (before) {
                $target.before($source);
            } else {
                $target.after($source);
            }

            $source.removeAttr("style"); //remove absolute positioning
            this.clearResults();
        };

        SearchControl.prototype.createMoveColumnDragDrop = function () {
            var rowsSelector = ".sf-search-results th:not(.sf-th-entity):not(.sf-th-selection)";
            var current = null;
            this.element.on("dragstart", rowsSelector, function (e) {
                var de = e.originalEvent;
                de.dataTransfer.effectAllowed = "move";
                de.dataTransfer.setData("Text", $(e.currentTarget).attr("data-column-name"));
                current = e.currentTarget;
            });

            function dragClass(offsetX, width) {
                if (!offsetX)
                    return null;

                if (width < 100 ? (offsetX < (width / 2)) : (offsetX < 50))
                    return "drag-left";

                if (width < 100 ? (offsetX > (width / 2)) : (offsetX > (width - 50)))
                    return "drag-right";

                return null;
            }

            var onDragOver = function (e) {
                if (e.preventDefault)
                    e.preventDefault();

                var de = e.originalEvent;
                if (e.currentTarget == current) {
                    de.dataTransfer.dropEffect = "none";
                    return;
                }

                $(e.currentTarget).removeClass("drag-left drag-right");
                $(e.currentTarget).addClass(dragClass(de.pageX - $(e.currentTarget).offset().left, $(e.currentTarget).width()));

                de.dataTransfer.dropEffect = "move";
            };
            this.element.on("dragover", rowsSelector, onDragOver);
            this.element.on("dragenter", rowsSelector, onDragOver);

            this.element.on("dragleave", rowsSelector, function (e) {
                $(e.currentTarget).removeClass("drag-left drag-right");
            });

            var me = this;
            this.element.on("drop", rowsSelector, function (e) {
                if (e.preventDefault)
                    e.preventDefault();

                var curr = $(e.currentTarget);

                curr.removeClass("drag-left drag-right");

                var de = e.originalEvent;

                var result = dragClass(de.pageX - $(e.currentTarget).offset().left, curr.width());

                if (result)
                    me.moveColumn($(current), curr, result == "drag-left");
            });
        };

        SearchControl.prototype.removeColumn = function ($th) {
            $th.remove();
            this.clearResults();
            this.syncSize();
        };

        SearchControl.prototype.clearResults = function () {
            var $tbody = this.prefix.child("tblResults").get().find("tbody");
            $tbody.find("tr:not('.sf-search-footer')").remove();
            $tbody.prepend($("<tr></tr>").append($("<td></td>").attr("colspan", $tbody.find(".sf-search-footer td").attr("colspan"))));
        };

        SearchControl.prototype.toggleFilters = function () {
            var $toggler = this.element.find(".sf-filters-header");
            this.element.find(".sf-filters").toggle();
            $toggler.toggleClass('active');
            return false;
        };

        SearchControl.prototype.quickFilterCell = function ($elem) {
            var value = $elem.data("value");
            if (typeof value == "undefined")
                value = $elem.html().trim();

            var cellIndex = $elem[0].cellIndex;
            var tokenName = $($($elem.closest(".sf-search-results")).find("th")[cellIndex]).data("column-name");

            this.filterBuilder.addFilter(tokenName, value);
        };

        SearchControl.prototype.quickFilterHeader = function ($th) {
            this.filterBuilder.addFilter($th.data("column-name"), "");
        };

        SearchControl.prototype.create_click = function () {
            this.onCreate();
        };

        SearchControl.prototype.onCreate = function () {
            var _this = this;
            if (this.creating != null)
                this.creating();
            else {
                this.typeChooseCreate().then(function (type) {
                    if (!type)
                        return;

                    type.preConstruct().then(function (args) {
                        if (!args)
                            return;

                        args = $.extend(args, _this.requestDataForSearchPopupCreate());

                        var runtimeInfo = new Entities.RuntimeInfo(type.name, null, true);
                        if (SF.isEmpty(_this.options.prefix)) {
                            Navigator.navigate(runtimeInfo, args, false);
                        } else
                            return Navigator.navigatePopup(new Entities.EntityHtml(_this.options.prefix.child("Temp"), runtimeInfo), { requestExtraJsonData: args });
                    });
                });
            }
        };

        SearchControl.prototype.typeChooseCreate = function () {
            return Navigator.typeChooser(this.options.prefix, this.types.filter(function (t) {
                return t.creable;
            }));
        };

        SearchControl.prototype.requestDataForSearchPopupCreate = function () {
            return {
                filters: this.filterBuilder.serializeFilters(),
                webQueryName: this.options.webQueryName
            };
        };

        SearchControl.prototype.toggleSelectAll = function () {
            this.changeRowSelection(this.prefix.child("sfSearchControl").get().find(".sf-td-selection"), this.prefix.child("cbSelectAll").get().is(":checked"));
        };

        SearchControl.prototype.searchOnLoad = function () {
            var _this = this;
            var $button = this.options.prefix.child("qbSearch").get();

            SF.onVisible($button).then(function () {
                if (!_this.searchOnLoadFinished) {
                    $button.click();
                    _this.searchOnLoadFinished = true;
                }
            });
        };
        return SearchControl;
    })();
    exports.SearchControl = SearchControl;

    var FilterBuilder = (function () {
        function FilterBuilder(element, prefix, webQueryName, addFilterUrl) {
            var _this = this;
            this.element = element;
            this.prefix = prefix;
            this.webQueryName = webQueryName;
            this.addFilterUrl = addFilterUrl;
            this.newSubTokensComboAdded(this.element.find("#" + prefix.child("tokenBuilder") + " select:first"));

            this.element.on("sf-new-subtokens-combo", function (event) {
                var args = [];
                for (var _i = 0; _i < (arguments.length - 1); _i++) {
                    args[_i] = arguments[_i + 1];
                }
                _this.newSubTokensComboAdded($("#" + args[0]));
            });
        }
        FilterBuilder.prototype.newSubTokensComboAdded = function ($selectedCombo) {
            var _this = this;
            var $btnAddFilter = this.prefix.child("btnAddFilter").tryGet();
            var $btnAddColumn = this.prefix.child("btnAddColumn").tryGet();

            var $selectedOption = $selectedCombo.children("option:selected");
            $selectedCombo.attr("title", $selectedOption.attr("title"));
            $selectedCombo.attr("style", $selectedOption.attr("style"));
            if ($selectedOption.val() == "") {
                var $prevSelect = $selectedCombo.prev("select");
                if ($prevSelect.length == 0) {
                    this.changeButtonState($btnAddFilter, lang.signum.selectToken);
                    this.changeButtonState($btnAddColumn, lang.signum.selectToken);
                } else {
                    var $prevSelectedOption = $prevSelect.find("option:selected");
                    this.changeButtonState($btnAddFilter, $prevSelectedOption.attr("data-filter"), function () {
                        return _this.addFilterClicked();
                    });
                    this.changeButtonState($btnAddColumn, $prevSelectedOption.attr("data-column"), function () {
                        return _this.addColumnClicked();
                    });
                }
            } else {
                this.changeButtonState($btnAddFilter, $selectedOption.attr("data-filter"), function () {
                    return _this.addFilterClicked();
                });
                this.changeButtonState($btnAddColumn, $selectedOption.attr("data-column"), function () {
                    return _this.addColumnClicked();
                });
            }
        };

        FilterBuilder.prototype.changeButtonState = function ($button, disablingMessage, enableCallback) {
            if (!$button)
                return;

            if (disablingMessage) {
                $button.attr("disabled", "disabled");
                $button.parent().tooltip({
                    title: disablingMessage,
                    placement: "bottom"
                });
                $button.unbind('click').bind('click', function (e) {
                    e.preventDefault();
                    return false;
                });
            } else {
                var self = this;
                $button.removeAttr("disabled");
                $button.parent().tooltip("destroy");
                $button.unbind('click').bind('click', enableCallback);
            }
        };

        FilterBuilder.prototype.addFilterClicked = function () {
            var tokenName = QueryTokenBuilder.constructTokenName(this.prefix.child("tokenBuilder"));
            if (SF.isEmpty(tokenName)) {
                return;
            }

            this.addFilter(tokenName, null);
        };

        FilterBuilder.prototype.addFilter = function (tokenName, value) {
            var tableFilters = this.prefix.child("tblFilters").get().find("tbody");
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
                url: this.addFilterUrl,
                data: data,
                async: false
            }).then(function (filterHtml) {
                var $filterList = self.element.find(".sf-filters-list");
                $filterList.find(".sf-explanation").hide();
                $filterList.find("table").show();

                tableFilters.append(filterHtml);
            });
        };

        FilterBuilder.prototype.newFilterRowIndex = function () {
            var lastRow = this.prefix.child("tblFilters").get().find("tbody tr:last");
            if (lastRow.length == 1) {
                return parseInt(lastRow[0].id.substr(lastRow[0].id.lastIndexOf("_") + 1, lastRow[0].id.length)) + 1;
            }
            return 0;
        };

        FilterBuilder.prototype.serializeFilters = function () {
            var _this = this;
            return this.prefix.child("tblFilters").get().find("tbody > tr").toArray().map(function (f) {
                var $filter = $(f);

                var id = $filter[0].id;
                var index = id.afterLast("_");

                var selector = _this.prefix.child("ddlSelector").child(index).get($filter).find("option:selected");

                var value = _this.encodeValue($filter, index);

                return $filter.find("td:nth-child(2) > :hidden").val() + "," + selector.val() + "," + value;
            }).join(";");
        };

        FilterBuilder.prototype.encodeValue = function ($filter, index) {
            var filterValuePrefix = this.prefix.child("value").child(index);

            var eleme = filterValuePrefix.get();

            var date = filterValuePrefix.child("Date").tryGet($filter);
            var time = filterValuePrefix.child("Time").tryGet($filter);

            if (date.length && time.length) {
                var dateVal = date.val();
                var timeVal = time.val();
                return SearchControl.encodeCSV(dateVal && timeVal ? (dateVal + " " + timeVal) : null);
            }

            if (eleme.is("input:checkbox"))
                return eleme[0].checked;

            var infoElem = filterValuePrefix.child(Entities.Keys.runtimeInfo).tryGet(eleme);
            if (infoElem.length) {
                var val = Entities.RuntimeInfo.parse(infoElem.val());
                return SearchControl.encodeCSV(val == null ? null : val.key());
            }

            return SearchControl.encodeCSV(eleme.val());
        };
        return FilterBuilder;
    })();
    exports.FilterBuilder = FilterBuilder;

    (function (QueryTokenBuilder) {
        function init(containerId, webQueryName, controllerUrl, options, requestExtraJsonData) {
            $("#" + containerId).on("change", "select", function () {
                tokenChanged($(this), webQueryName, controllerUrl, options, requestExtraJsonData);
            });
        }
        QueryTokenBuilder.init = init;

        function tokenChanged($selectedCombo, webQueryName, controllerUrl, options, requestExtraJsonData) {
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
                prefix: prefix,
                options: options
            }, requestExtraJsonData);

            SF.ajaxPost({
                url: controllerUrl,
                data: data,
                dataType: "html"
            }).then(function (newHtml) {
                $selectedCombo.parent().html(newHtml);
            });
        }
        QueryTokenBuilder.tokenChanged = tokenChanged;
        ;

        function clearChildSubtokenCombos($selectedCombo, prefix, index) {
            $selectedCombo.next("select,input[type=hidden]").remove();
        }
        QueryTokenBuilder.clearChildSubtokenCombos = clearChildSubtokenCombos;

        function constructTokenName(prefix) {
            var tokenName = "";
            var stop = false;
            for (var i = 0; ; i++) {
                var currSubtoken = prefix.child("ddlTokens_" + i).tryGet();
                if (currSubtoken.length == 0)
                    break;

                var part = currSubtoken.val();
                tokenName = !tokenName ? part : !part ? tokenName : (tokenName + "." + part);
            }
            return tokenName;
        }
        QueryTokenBuilder.constructTokenName = constructTokenName;
    })(exports.QueryTokenBuilder || (exports.QueryTokenBuilder = {}));
    var QueryTokenBuilder = exports.QueryTokenBuilder;
});
//# sourceMappingURL=Finder.js.map

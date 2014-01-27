/// <reference path="references.ts"/>
var __extends = this.__extends || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};

var SF;
(function (SF) {
    once("SF-control", function () {
        jQuery.fn.SFControl = function () {
            return this.data("SF-control");
        };
    });

    var EntityBase = (function () {
        function EntityBase(element, _options) {
            this.element = element;
            this.element.data("SF-control", this);
            this.options = $.extend({
                prefix: "",
                partialViewName: ""
            }, _options);

            this._create();

            this.element.trigger("SF-ready");
        }
        EntityBase.prototype._create = function () {
            var _this = this;
            var $txt = $(this.pf(SF.Keys.toStr) + ".sf-entity-autocomplete");
            if ($txt.length > 0) {
                var data = $txt.data();

                this.autoCompleter = new AjaxEntityAutoCompleter(SF.Urls.autocomplete, function (term) {
                    return ({ types: _this.staticInfo().types(), l: 5, q: term });
                });

                this.entityAutocomplete($txt);
            }
        };

        EntityBase.prototype.runtimeInfo = function (itemPrefix) {
            return new SF.RuntimeInfoElement(this.options.prefix);
        };

        EntityBase.prototype.staticInfo = function () {
            return new SF.StaticInfo(this.options.prefix);
        };

        EntityBase.prototype.pf = function (s) {
            return "#" + SF.compose(this.options.prefix, s);
        };

        EntityBase.prototype.containerDiv = function (itemPrefix) {
            var containerDivId = this.pf(EntityBase.key_entity);
            if ($(containerDivId).length == 0)
                this.runtimeInfo().getElem().after(SF.hiddenDiv(containerDivId, ""));

            return $(containerDivId);
        };

        EntityBase.prototype.extractEntityHtml = function (itemPrefix) {
            var runtimeInfo = this.runtimeInfo().value();

            if (runtimeInfo == null)
                return null;

            var div = this.containerDiv();

            var result = new SF.EntityHtml(this.options.prefix, runtimeInfo, null, null);

            result.html = div.children();

            div.html(null);

            return result;
        };

        EntityBase.prototype.setEntitySpecific = function (entityValue, itemPrefix) {
            //virtual function
        };

        EntityBase.prototype.setEntity = function (entityValue, itemPrefix) {
            this.setEntitySpecific(entityValue);

            if (entityValue) {
                entityValue.assertPrefixAndType(this.options.prefix, this.staticInfo());
            }

            SF.triggerNewContent(this.containerDiv().html(entityValue == null ? null : entityValue.html));
            this.runtimeInfo().setValue(entityValue == null ? null : entityValue.runtimeInfo);

            if (entityValue == null) {
                SF.Validation.cleanError($(this.pf(SF.Keys.toStr)).val(""));
                SF.Validation.cleanError($(this.pf(SF.Keys.link)).val("").html(""));
            }

            this.updateButtonsDisplay();
            if (!SF.isEmpty(this.entityChanged)) {
                this.entityChanged();
            }
        };

        EntityBase.prototype.remove_click = function () {
            var _this = this;
            var entity = this.extractEntityHtml();

            this.onRemove(entity).then(function (result) {
                if (result)
                    _this.setEntity(null);
                else
                    _this.setEntity(entity);
            });
        };

        EntityBase.prototype.onRemove = function (entityHtml) {
            if (this.removing != null)
                return this.removing(entityHtml);

            return Promise.resolve(true);
        };

        EntityBase.prototype.create_click = function () {
            var _this = this;
            this.onCreating(this.options.prefix).then(function (result) {
                if (result)
                    _this.setEntity(result);
            });
        };

        EntityBase.prototype.onCreating = function (prefix) {
            var _this = this;
            if (this.creating != null)
                return this.creating(prefix);

            SF.ViewNavigator.typeChooser(this.staticInfo()).then(function (type) {
                if (type == null)
                    return null;

                var newEntity = new SF.EntityHtml(_this.options.prefix, new SF.RuntimeInfoValue(type, null));

                var template = _this.getEmbeddedTemplate();
                if (!SF.isEmpty(template))
                    newEntity.html = $(template);

                return SF.ViewNavigator.viewPopup(newEntity, _this.getDefaultPopupViewOptions());
            });
        };

        EntityBase.prototype.getEmbeddedTemplate = function (itemPrefix) {
            return window[SF.compose(this.options.prefix, "sfTemplate")];
        };

        EntityBase.prototype.view_click = function () {
            var _this = this;
            var entityHtml = this.extractEntityHtml();

            this.onViewing(entityHtml).then(function (result) {
                if (result)
                    _this.setEntity(result);
                else
                    _this.setEntity(entityHtml); //previous entity passed by reference
            });
        };

        EntityBase.prototype.onViewing = function (entityHtml) {
            if (this.viewing != null)
                return this.viewing(entityHtml);

            return SF.ViewNavigator.viewPopup(entityHtml, this.getDefaultPopupViewOptions());
        };

        EntityBase.prototype.find_click = function () {
            var _this = this;
            this.onFinding(this.options.prefix).then(function (result) {
                _this.setEntity(result);
            });
        };

        EntityBase.prototype.onFinding = function (prefix) {
            if (this.finding != null)
                return this.finding(this.options.prefix);

            return SF.ViewNavigator.typeChooser(this.staticInfo()).then(function (type) {
                if (type == null)
                    return null;

                return SF.FindNavigator.find({
                    webQueryName: type,
                    prefix: prefix
                });
            });
        };

        EntityBase.prototype.getDefaultPopupViewOptions = function () {
            var staticInfo = this.staticInfo();

            if (staticInfo.isReadOnly())
                return { readOnly: true };

            return null;
        };

        EntityBase.prototype.updateButtonsDisplay = function () {
            var hasEntity = !!this.runtimeInfo().value;

            $(this.pf("btnCreate")).toggle(!hasEntity);
            $(this.pf("btnFind")).toggle(!hasEntity);
            $(this.pf("btnRemove")).toggle(hasEntity);
            $(this.pf("btnView")).toggle(hasEntity);
            $(this.pf(SF.Keys.link)).toggle(hasEntity);
            $(this.pf(SF.Keys.toStr)).toggle(!hasEntity);
        };

        EntityBase.prototype.entityAutocomplete = function ($txt) {
            var self = this;
            var auto = $txt.autocomplete({
                delay: 200,
                source: function (request, response) {
                    self.autoCompleter.getResults(request.term).then(function (entities) {
                        response(entities.map(function (e) {
                            return ({ label: e.toStr, value: e });
                        }));
                    });
                },
                focus: function (event, ui) {
                    $txt.val(ui.item.value.text);
                    return false;
                },
                select: function (event, ui) {
                    self.setEntity(ui.item.value);
                    event.preventDefault();
                }
            });

            auto.data("uiAutocomplete")._renderItem = function (ul, item) {
                var val = item.value;

                return $("<li>").attr("data-type", val.runtimeInfo.type).attr("data-id", val.runtimeInfo.id).append($("<a>").text(item.label)).appendTo(ul);
            };
        };

        EntityBase.prototype.onAutocompleteSelected = function (controlId, data) {
            throw new Error("onAutocompleteSelected is abstract");
        };
        EntityBase.key_entity = "sfEntity";
        return EntityBase;
    })();
    SF.EntityBase = EntityBase;

    var AjaxEntityAutoCompleter = (function () {
        function AjaxEntityAutoCompleter(controllerUrl, getData) {
            this.controllerUrl = controllerUrl;
            this.getData = getData;
        }
        AjaxEntityAutoCompleter.prototype.getResults = function (term) {
            var _this = this;
            if (this.lastXhr)
                this.lastXhr.abort();

            return new Promise(function (resolve, failure) {
                _this.lastXhr = $.ajax({
                    url: _this.controllerUrl,
                    data: _this.getData(term),
                    success: function (data) {
                        this.lastXhr = null;
                        resolve(data.map(function (item) {
                            return new SF.EntityValue(new SF.RuntimeInfoValue(item.type, parseInt(item.id)), item.toStr, item.link);
                        }));
                    }
                });
            });
        };
        return AjaxEntityAutoCompleter;
    })();
    SF.AjaxEntityAutoCompleter = AjaxEntityAutoCompleter;

    once("SF-entityLine", function () {
        return $.fn.entityLine = function (opt) {
            return new EntityLine(this, opt);
        };
    });

    var EntityLine = (function (_super) {
        __extends(EntityLine, _super);
        function EntityLine() {
            _super.apply(this, arguments);
        }
        EntityLine.prototype.setEntitySpecific = function (entityValue) {
            var link = $(this.pf(SF.Keys.link));
            link.text(entityValue == null ? null : entityValue.toStr);
            if (link.filter('a').length !== 0)
                link.attr('href', entityValue == null ? null : entityValue.link);
            $(this.pf(SF.Keys.toStr)).val('');
        };
        return EntityLine;
    })(EntityBase);
    SF.EntityLine = EntityLine;

    once("SF-entityCombo", function () {
        return $.fn.entityCombo = function (opt) {
            return new EntityCombo(this, opt);
        };
    });

    var EntityCombo = (function (_super) {
        __extends(EntityCombo, _super);
        function EntityCombo() {
            _super.apply(this, arguments);
        }
        EntityCombo.prototype.combo = function () {
            return $(this.pf(EntityCombo.key_combo));
        };

        EntityCombo.prototype.setEntitySpecific = function (entityValue) {
            var c = this.combo();

            if (entityValue == null)
                c.val(null);
            else {
                var o = c.children("option[value=" + entityValue.runtimeInfo.key() + "]");
                if (o.length > 1)
                    o.html(entityValue.toStr);
                else
                    c.add($("<option value='{0}'/>".format(entityValue.runtimeInfo.key())).text(entityValue.toStr));

                c.val(entityValue.runtimeInfo.key());
            }
        };

        EntityCombo.prototype.combo_selected = function () {
            var val = this.combo().val();

            var ri = SF.RuntimeInfoValue.fromKey(val);

            this.setEntity(ri == null ? null : new SF.EntityValue(ri));
        };
        EntityCombo.key_combo = "sfCombo";
        return EntityCombo;
    })(EntityBase);
    SF.EntityCombo = EntityCombo;

    once("SF-entityLineDetail", function () {
        return $.fn.entityLineDetail = function (opt) {
            return new EntityLineDetail(this, opt);
        };
    });

    var EntityLineDetail = (function (_super) {
        __extends(EntityLineDetail, _super);
        function EntityLineDetail(element, options) {
            _super.call(this, element, options);
        }
        EntityLineDetail.prototype.containerDiv = function (itemPrefix) {
            return $("#" + this.options.detailDiv);
        };

        EntityLineDetail.prototype.setEntitySpecific = function (entityValue) {
            if (entityValue == null)
                return;

            if (!entityValue.isLoaded())
                throw new Error("EntityLineDetail requires a loaded EntityHtml, consider calling ViewNavigator.loadPartialView");
        };

        EntityLineDetail.prototype.onCreating = function (prefix) {
            var _this = this;
            if (this.creating != null)
                return this.creating(prefix);

            SF.ViewNavigator.typeChooser(this.staticInfo()).then(function (type) {
                if (type == null)
                    return null;

                var newEntity = new SF.EntityHtml(_this.options.prefix, new SF.RuntimeInfoValue(type, null));

                var template = _this.getEmbeddedTemplate();
                if (!SF.isEmpty(template)) {
                    newEntity.html = $(template);
                    return Promise.resolve(newEntity);
                }

                return SF.ViewNavigator.loadPartialView(newEntity);
            });
        };

        EntityLineDetail.prototype.onFinding = function (prefix) {
            var _this = this;
            return _super.prototype.onFinding.call(this, prefix).then(function (entity) {
                if (entity == null)
                    return null;

                if (entity.html != null)
                    return Promise.resolve(entity);

                return SF.ViewNavigator.loadPartialView(new SF.EntityHtml(_this.options.prefix, entity.runtimeInfo));
            });
        };
        return EntityLineDetail;
    })(EntityBase);
    SF.EntityLineDetail = EntityLineDetail;

    var EntityListBase = (function (_super) {
        __extends(EntityListBase, _super);
        function EntityListBase(element, options) {
            _super.call(this, element, options);
        }
        EntityListBase.prototype.runtimeInfo = function (itemPrefix) {
            return new SF.RuntimeInfoElement(itemPrefix);
        };

        EntityListBase.prototype.containerDiv = function (itemPrefix) {
            var containerDivId = SF.compose(itemPrefix, EntityList.key_entity);
            if ($(containerDivId).length == 0)
                this.runtimeInfo(itemPrefix).getElem().after(SF.hiddenDiv(containerDivId, ""));

            return $(containerDivId);
        };

        EntityListBase.prototype.getEmbeddedTemplate = function (itemPrefix) {
            var template = _super.prototype.getEmbeddedTemplate.call(this);
            if (SF.isEmpty(template))
                return template;

            template = template.replace(new RegExp(SF.compose(this.options.prefix, "0"), "gi"), itemPrefix);
            return template;
        };

        EntityListBase.prototype.extractEntityHtml = function (itemPrefix) {
            var runtimeInfo = this.runtimeInfo(itemPrefix).value();

            var div = this.containerDiv(itemPrefix);

            var result = new SF.EntityHtml(itemPrefix, runtimeInfo, null, null);

            result.html = div.children();

            div.html(null);

            return result;
        };

        EntityListBase.prototype.setEntity = function (entityValue, itemPrefix) {
            if (entityValue == null)
                throw new Error("entityValue is mandatory on setEntityItem");

            this.setEntitySpecific(entityValue);

            if (entityValue)
                entityValue.assertPrefixAndType(itemPrefix, this.staticInfo());

            if (entityValue.isLoaded())
                SF.triggerNewContent(this.containerDiv(itemPrefix).html(entityValue.html));

            this.runtimeInfo(itemPrefix).setValue(entityValue.runtimeInfo);

            this.updateButtonsDisplay();
            if (!SF.isEmpty(this.entityChanged)) {
                this.entityChanged();
            }
        };

        EntityListBase.prototype.addEntitySpecific = function (entityValue, itemPrefix) {
            //virtual
        };

        EntityListBase.prototype.addEntity = function (entityValue, itemPrefix) {
            if (entityValue == null)
                throw new Error("entityValue is mandatory on setEntityItem");

            this.addEntitySpecific(entityValue, itemPrefix);

            if (entityValue)
                entityValue.assertPrefixAndType(itemPrefix, this.staticInfo());

            if (entityValue.isLoaded())
                SF.triggerNewContent(this.containerDiv(itemPrefix).html(entityValue.html));
            this.runtimeInfo(itemPrefix).setValue(entityValue.runtimeInfo);

            this.updateButtonsDisplay();
            if (!SF.isEmpty(this.entityChanged)) {
                this.entityChanged();
            }
        };

        EntityListBase.prototype.removeEntitySpecific = function (entityValue) {
            //virtual
        };

        EntityListBase.prototype.removeEntity = function (entityValue) {
            this.updateButtonsDisplay();
            if (!SF.isEmpty(this.entityChanged)) {
                this.entityChanged();
            }
        };

        EntityListBase.prototype.itemSuffix = function () {
            throw new Error("itemSuffix is abstract");
        };

        EntityListBase.prototype.getItems = function () {
            throw new Error("getItems is abstract");
        };

        EntityListBase.prototype.getNextPrefix = function () {
            var _this = this;
            var lastIndex = Math.max.apply(null, this.getItems().toArray().map(function (e) {
                return parseInt(e.id.after(_this.options.prefix + "_").before("_" + _this.itemSuffix()));
            }));

            return SF.compose(this.options.prefix, lastIndex + 1);
        };

        EntityListBase.prototype.getLastPosIndex = function () {
            var $last = this.getItems().filter(":last");
            if ($last.length == 0) {
                return -1;
            }

            var lastId = $last[0].id;
            var lastPrefix = lastId.substring(0, lastId.indexOf(this.itemSuffix()) - 1);

            return this.getPosIndex(lastPrefix);
        };

        EntityListBase.prototype.canAddItems = function () {
            return SF.isEmpty(this.options.maxElements) || this.getItems().length < this.options.maxElements;
        };

        EntityListBase.prototype.find_click = function () {
            var _this = this;
            this.onFindingMany(this.options.prefix).then(function (result) {
                result.forEach(function (ev) {
                    return _this.addEntity(ev, _this.getNextPrefix());
                });
            });
        };

        EntityListBase.prototype.onFinding = function (prefix) {
            throw new Error("onFinding is deprecated in EntityListBase");
        };

        EntityListBase.prototype.onFindingMany = function (prefix) {
            if (this.findingMany != null)
                return this.findingMany(this.options.prefix);

            return SF.ViewNavigator.typeChooser(this.staticInfo()).then(function (type) {
                if (type == null)
                    return null;

                return SF.FindNavigator.findMany({
                    webQueryName: type,
                    prefix: prefix
                });
            });
        };

        EntityListBase.prototype.moveUp = function (selectedItemPrefix) {
            var suffix = this.itemSuffix();
            var $item = $("#" + SF.compose(selectedItemPrefix, suffix));
            var $itemPrev = $item.prev();

            if ($itemPrev.length == 0) {
                return;
            }

            var itemPrevPrefix = $itemPrev[0].id.before("_" + suffix);

            var prevNewIndex = this.getPosIndex(itemPrevPrefix);
            this.setPosIndex(selectedItemPrefix, prevNewIndex);
            this.setPosIndex(itemPrevPrefix, prevNewIndex + 1);

            $item.insertBefore($itemPrev);
        };

        EntityListBase.prototype.moveDown = function (selectedItemPrefix) {
            var suffix = this.itemSuffix();
            var $item = $("#" + SF.compose(selectedItemPrefix, suffix));
            var $itemNext = $item.next();

            if ($itemNext.length == 0) {
                return;
            }

            var itemNextPrefix = $itemNext[0].id.before("_" + suffix);

            var nextNewIndex = this.getPosIndex(itemNextPrefix);
            this.setPosIndex(selectedItemPrefix, nextNewIndex);
            this.setPosIndex(itemNextPrefix, nextNewIndex - 1);

            $item.insertAfter($itemNext);
        };

        EntityListBase.prototype.getPosIndex = function (itemPrefix) {
            return parseInt($("#" + SF.compose(itemPrefix, EntityList.key_indexes)).val().after(";"));
        };

        EntityListBase.prototype.setPosIndex = function (itemPrefix, newIndex) {
            var $indexes = $("#" + SF.compose(itemPrefix, EntityList.key_indexes));
            $indexes.val($indexes.val().before(";") + ";" + newIndex.toString());
        };
        return EntityListBase;
    })(EntityBase);
    SF.EntityListBase = EntityListBase;

    once("SF-entityList", function () {
        return $.fn.entityList = function (opt) {
            return new EntityList(this, opt);
        };
    });

    var EntityList = (function (_super) {
        __extends(EntityList, _super);
        function EntityList() {
            _super.apply(this, arguments);
        }
        EntityList.prototype.itemSuffix = function () {
            return SF.Keys.toStr;
        };

        EntityList.prototype.updateLinks = function (newToStr, newLink, itemPrefix) {
            $('#' + SF.compose(itemPrefix, SF.Keys.toStr)).html(newToStr);
        };

        EntityList.prototype.selectedItemPrefix = function () {
            var $items = this.getItems().filter(":selected");
            ;
            if ($items.length == 0) {
                return null;
            }

            var nameSelected = $items[0].id;
            return nameSelected.before("_" + this.itemSuffix());
        };

        EntityList.prototype.getItems = function () {
            return $(this.pf(EntityList.key_list) + " > option");
        };

        EntityList.prototype.view_click = function () {
            var _this = this;
            var selectedItemPrefix = this.selectedItemPrefix();

            var entityHtml = this.extractEntityHtml(selectedItemPrefix);

            this.onViewing(entityHtml).then(function (result) {
                if (result)
                    _this.setEntity(result, selectedItemPrefix);
                else
                    _this.setEntity(entityHtml, selectedItemPrefix); //previous entity passed by reference
            });
        };

        EntityList.prototype.create_click = function () {
            var _this = this;
            var prefix = this.getNextPrefix();
            this.onCreating(prefix).then(function (entity) {
                if (entity)
                    _this.addEntity(entity, prefix);
            });
        };

        EntityList.prototype.updateButtonsDisplay = function () {
            var hasElements = this.getItems().length > 0;
            $(this.pf("btnRemove")).toggle(hasElements);
            $(this.pf("btnView")).toggle(hasElements);
            $(this.pf("btnUp")).toggle(hasElements);
            $(this.pf("btnDown")).toggle(hasElements);

            var canAdd = this.canAddItems();

            $(this.pf("btnCreate")).toggle(canAdd);
            $(this.pf("btnFind")).toggle(canAdd);
        };

        EntityList.prototype.addEntitySpecific = function (entityValue, itemPrefix) {
            var $table = $("#" + this.options.prefix + "> .sf-field-list > .sf-field-list-table");

            $table.before(SF.hiddenInput(SF.compose(itemPrefix, EntityList.key_indexes), ";" + (this.getLastPosIndex() + 1).toString()));

            $table.before(SF.hiddenInput(SF.compose(itemPrefix, SF.Keys.runtimeInfo), entityValue.runtimeInfo.toString()));

            $table.before(SF.hiddenDiv(SF.compose(itemPrefix, EntityList.key_entity), ""));

            var select = $(this.pf(EntityList.key_list));
            select.append("\n<option id='" + SF.compose(itemPrefix, SF.Keys.toStr) + "' name='" + SF.compose(itemPrefix, SF.Keys.toStr) + "' value='' class='sf-value-line'>" + entityValue.toStr + "</option>");
            select.children('option').attr('selected', false); //Fix for Firefox: Set selected after retrieving the html of the select
            select.children('option:last').attr('selected', true);
        };

        EntityList.prototype.remove_click = function () {
            var _this = this;
            var selectedItemPrefix = this.selectedItemPrefix();

            var entity = this.extractEntityHtml(selectedItemPrefix);

            this.onRemove(entity).then(function (result) {
                if (result)
                    _this.removeEntity(entity);
                else
                    _this.setEntity(entity);
            });
        };

        EntityList.prototype.removeEntitySpecific = function (entityValue) {
            $("#" + SF.compose(entityValue.prefix, SF.Keys.runtimeInfo)).remove();
            $("#" + SF.compose(entityValue.prefix, SF.Keys.toStr)).remove();
            $("#" + SF.compose(entityValue.prefix, EntityList.key_entity)).remove();
            $("#" + SF.compose(entityValue.prefix, EntityList.key_indexes)).remove();
        };
        EntityList.key_indexes = "sfIndexes";
        EntityList.key_list = "sfList";
        return EntityList;
    })(EntityListBase);
    SF.EntityList = EntityList;

    once("SF-entityListDetail", function () {
        return $.fn.entityListDetail = function (opt) {
            return new EntityListDetail(this, opt);
        };
    });

    var EntityListDetail = (function (_super) {
        __extends(EntityListDetail, _super);
        function EntityListDetail(element, options) {
            _super.call(this, element, options);
        }
        EntityListDetail.prototype.setEntitySpecific = function (entityValue) {
            if (entityValue == null)
                return;

            if (!entityValue.isLoaded())
                throw new Error("EntityListDetail requires a loaded EntityHtml, consider calling ViewNavigator.loadPartialView");
        };

        EntityListDetail.prototype.onCreating = function (prefix) {
            var _this = this;
            if (this.creating != null)
                return this.creating(prefix);

            SF.ViewNavigator.typeChooser(this.staticInfo()).then(function (type) {
                if (type == null)
                    return null;

                var newEntity = new SF.EntityHtml(_this.options.prefix, new SF.RuntimeInfoValue(type, null));

                var template = _this.getEmbeddedTemplate();
                if (!SF.isEmpty(template)) {
                    newEntity.html = $(template);
                    return Promise.resolve(newEntity);
                }

                return SF.ViewNavigator.loadPartialView(newEntity);
            });
        };

        EntityListDetail.prototype.onFindingMany = function (prefix) {
            var _this = this;
            return _super.prototype.onFindingMany.call(this, prefix).then(function (entites) {
                if (entites == null)
                    return null;

                var promises = entites.map(function (entity) {
                    if (entity.html != null)
                        return Promise.resolve(entity);

                    return SF.ViewNavigator.loadPartialView(new SF.EntityHtml(_this.options.prefix, entity.runtimeInfo));
                });

                var result = Promise.all(promises);

                return result;
            });
        };

        EntityListDetail.prototype.stageCurrentSelected = function () {
            var selPrefix = this.getLastPosIndex();

            var child = $("#" + this.options.detailDiv).children();

            if (child.length != 0) {
                var prefix = child[0].id.before("_" + EntityList.key_entity);

                if (selPrefix == prefix)
                    child[0].show();
            }

            var itemPrefix = this.getVisibleItemPrefix();
            if (!SF.isEmpty(itemPrefix)) {
                $('#' + SF.compose(itemPrefix, EntityBase.key_entity)).html('').append(SF.cloneContents(this.options.detailDiv));
            }
        };

        EntityListDetail.prototype.onItemCreated = function (viewOptions) {
            if (SF.isEmpty(viewOptions.type)) {
                throw "ViewOptions type parameter must not be null in entityListDetail onItemCreated. Call create instead";
            }

            var itemPrefix = viewOptions.prefix;
            this.newListItem(null, itemPrefix, { type: viewOptions.type, toStr: null });
        };

        EntityListDetail.prototype.view = function (_viewOptions) {
            var selectedItemPrefix = this.selectedItemPrefix();
            if (SF.isEmpty(selectedItemPrefix)) {
                return;
            }
            this.viewInIndex(_viewOptions, selectedItemPrefix);
        };

        EntityListDetail.prototype.viewInIndex = function (_viewOptions, selectedItemPrefix) {
            this.restoreCurrent();
            if (this.isLoaded(selectedItemPrefix)) {
                this.cloneAndShow(selectedItemPrefix);
            } else {
                var viewOptions = this.viewOptionsForViewing(_viewOptions, selectedItemPrefix);
                new SF.ViewNavigator(viewOptions).viewEmbedded();
                SF.triggerNewContent($('#' + viewOptions.containerDiv));
            }
        };

        EntityListDetail.prototype.viewOptionsForViewing = function (_viewOptions, itemPrefix) {
            var self = this;
            var info = this.itemRuntimeInfo(itemPrefix);
            return $.extend({
                containerDiv: this.options.detailDiv,
                type: info.entityType(),
                id: info.id(),
                prefix: itemPrefix,
                partialViewName: this.options.partialViewName,
                requestExtraJsonData: this.extraJsonParams(itemPrefix)
            }, _viewOptions);
        };

        EntityListDetail.prototype.isLoaded = function (selectedItemPrefix) {
            return !SF.isEmpty($('#' + SF.compose(selectedItemPrefix, EntityBase.key_entity)).html());
        };

        EntityListDetail.prototype.cloneAndShow = function (selectedItemPrefix) {
            $('#' + this.options.detailDiv).html('').append(SF.cloneContents(SF.compose(selectedItemPrefix, EntityBase.key_entity)));

            $('#' + SF.compose(selectedItemPrefix, EntityBase.key_entity)).html('');
        };

        EntityListDetail.prototype.find = function (_findOptions, _viewOptions) {
            var _self = this;
            var type = this.getEntityType(function (type) {
                _self.typedFind($.extend({ webQueryName: type }, _findOptions), _viewOptions);
            });
        };

        EntityListDetail.prototype.typedFind = function (_findOptions, _viewOptions) {
            if (SF.isEmpty(_findOptions.webQueryName)) {
                throw "FindOptions webQueryName parameter must not be null in entityListDetail typedFind. Call find instead";
            }

            this.restoreCurrent();
            var findOptions = this.createFindOptions(_findOptions, _viewOptions);
            SF.FindNavigator.openFinder(findOptions);
        };

        EntityListDetail.prototype.createFindOptions = function (_findOptions, _viewOptions) {
            var newPrefixIndex = this.getLastPrefixIndex() + 1;
            var itemPrefix = SF.compose(this.options.prefix, newPrefixIndex.toString());
            var self = this;
            return $.extend({
                prefix: itemPrefix,
                onOk: function (selectedItems) {
                    return self.onFindingOk(selectedItems);
                }
            }, _findOptions);
        };

        EntityListDetail.prototype.onFindingOk = function (selectedItems, _viewOptions) {
            if (selectedItems == null || selectedItems.length == 0) {
                throw "No item was returned from Find Window";
            }
            var self = this;
            this.foreachNewItem(selectedItems, function (item, itemPrefix) {
                self.newListItem(null, itemPrefix, item);
            });

            //View result in the detailDiv
            $(this.pf(EntityList.key_list)).dblclick();
            return true;
        };

        EntityListDetail.prototype.remove = function (itemPrefix) {
            var selectedItemPrefix = this.selectedItemPrefix();
            if (SF.isEmpty(selectedItemPrefix)) {
                return;
            }
            this.edlineRemoveInIndex(selectedItemPrefix);
        };

        EntityListDetail.prototype.edlineRemoveInIndex = function (itemPrefix) {
            var currentVisible = this.getVisibleItemPrefix();
            if (!SF.isEmpty(currentVisible) && currentVisible == itemPrefix)
                $('#' + this.options.detailDiv).html('');
            this.removeInIndex(itemPrefix);
        };
        return EntityListDetail;
    })(EntityList);
    SF.EntityListDetail = EntityListDetail;

    once("SF-entityRepeater", function () {
        return $.fn.entityRepeater = function (opt) {
            return new EntityRepeater(this, opt);
        };
    });

    var EntityRepeater = (function (_super) {
        __extends(EntityRepeater, _super);
        function EntityRepeater(element, options) {
            _super.call(this, element, options);
        }
        EntityRepeater.prototype.itemSuffix = function () {
            return EntityRepeater.key_repeaterItem;
        };

        EntityRepeater.prototype.getItems = function () {
            return $(this.pf(EntityRepeater.key_itemsContainer) + " > ." + EntityRepeater.key_repeaterItemClass);
        };

        EntityRepeater.prototype.canAddItems = function () {
            if (!SF.isEmpty(this.options.maxElements)) {
                if (this.getItems().length >= +this.options.maxElements) {
                    return false;
                }
            }
            return true;
        };

        EntityRepeater.prototype.typedCreate = function (_viewOptions) {
            if (SF.isEmpty(_viewOptions.type)) {
                throw "ViewOptions type parameter must not be null in entityRepeater typedCreate. Call create instead";
            }
            if (!this.canAddItems()) {
                return;
            }

            var viewOptions = this.viewOptionsForCreating(_viewOptions);
            var template = this.getEmbeddedTemplate();
            if (!SF.isEmpty(template)) {
                template = template.replace(new RegExp(SF.compose(this.options.prefix, "0"), "gi"), viewOptions.prefix);
                this.onItemCreated(template, viewOptions);
            } else {
                var self = this;
                new SF.ViewNavigator(viewOptions).createEmbedded(function (newHtml) {
                    self.onItemCreated(newHtml, viewOptions);
                });
            }
        };

        EntityRepeater.prototype.viewOptionsForCreating = function (_viewOptions) {
            var newPrefixIndex = this.getLastPrefixIndex() + 1;
            var itemPrefix = SF.compose(this.options.prefix, newPrefixIndex.toString());
            return $.extend({
                containerDiv: "",
                prefix: itemPrefix,
                partialViewName: this.options.partialViewName,
                requestExtraJsonData: this.extraJsonParams(itemPrefix)
            }, _viewOptions);
        };

        EntityRepeater.prototype.onItemCreated = function (newHtml, viewOptions) {
            if (SF.isEmpty(viewOptions.type)) {
                throw "ViewOptions type parameter must not be null in entityRepeater onItemCreated";
            }

            var itemPrefix = viewOptions.prefix;
            this.newRepItem(newHtml, itemPrefix, { type: viewOptions.type });
        };

        EntityRepeater.prototype.newRepItem = function (newHtml, itemPrefix, item) {
            var itemInfoValue = this.itemRuntimeInfo(itemPrefix).createValue(item.type, item.id || '', typeof item.id == "undefined" ? 'n' : 'o', null);
            var $div = $("<fieldset id='" + SF.compose(itemPrefix, EntityRepeater.key_repeaterItem) + "' name='" + SF.compose(itemPrefix, EntityRepeater.key_repeaterItem) + "' class='" + EntityRepeater.key_repeaterItemClass + "'>" + "<legend>" + (this.options.remove ? ("<a id='" + SF.compose(itemPrefix, "btnRemove") + "' title='" + lang.signum.remove + "' onclick=\"" + this._getRemoving(itemPrefix) + "\" class='sf-line-button sf-remove' data-icon='ui-icon-circle-close' data-text='false'>" + lang.signum.remove + "</a>") : "") + (this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnUp") + "' title='" + lang.signum.moveUp + "' onclick=\"" + this._getMovingUp(itemPrefix) + "\" class='sf-line-button sf-move-up' data-icon='ui-icon-triangle-1-n' data-text='false'>" + lang.signum.moveUp + "</span>") : "") + (this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnDown") + "' title='" + lang.signum.moveDown + "' onclick=\"" + this._getMovingDown(itemPrefix) + "\" class='sf-line-button sf-move-down' data-icon='ui-icon-triangle-1-s' data-text='false'>" + lang.signum.moveDown + "</span>") : "") + "</legend>" + SF.hiddenInput(SF.compose(itemPrefix, EntityRepeater.key_indexes), ";" + (this.getLastNewIndex() + 1).toString()) + SF.hiddenInput(SF.compose(itemPrefix, SF.Keys.runtimeInfo), itemInfoValue) + "<div id='" + SF.compose(itemPrefix, EntityRepeater.key_entity) + "' name='" + SF.compose(itemPrefix, EntityRepeater.key_entity) + "' class='sf-line-entity'>" + "</div>" + "</fieldset>");

            $(this.pf(EntityRepeater.key_itemsContainer)).append($div);
            $("#" + SF.compose(itemPrefix, EntityRepeater.key_entity)).html(newHtml);
            SF.triggerNewContent($("#" + SF.compose(itemPrefix, EntityRepeater.key_repeaterItem)));
            this.fireOnEntityChanged(false);
        };

        EntityRepeater.prototype._getRepeaterCall = function () {
            return "$('#" + this.options.prefix + "').data('SF-control')";
        };

        EntityRepeater.prototype._getRemoving = function (itemPrefix) {
            return this._getRepeaterCall() + ".remove('" + itemPrefix + "');";
        };

        EntityRepeater.prototype._getMovingUp = function (itemPrefix) {
            return this._getRepeaterCall() + ".moveUp('" + itemPrefix + "');";
        };

        EntityRepeater.prototype._getMovingDown = function (itemPrefix) {
            return this._getRepeaterCall() + ".moveDown('" + itemPrefix + "');";
        };

        EntityRepeater.prototype.viewOptionsForViewing = function (_viewOptions, itemPrefix) {
            return $.extend({
                containerDiv: SF.compose(itemPrefix, EntityBase.key_entity),
                prefix: itemPrefix,
                partialViewName: this.options.partialViewName,
                requestExtraJsonData: this.extraJsonParams(itemPrefix)
            }, _viewOptions);
        };

        EntityRepeater.prototype.find = function (_findOptions, _viewOptions) {
            var _self = this;
            var type = this.getEntityType(function (type) {
                _self.typedFind($.extend({ webQueryName: type }, _findOptions), _viewOptions);
            });
        };

        EntityRepeater.prototype.typedFind = function (_findOptions, _viewOptions) {
            if (SF.isEmpty(_findOptions.webQueryName)) {
                throw "FindOptions webQueryName parameter must not be null in ERep typedFind. Call find instead";
            }
            if (!this.canAddItems()) {
                return;
            }

            var findOptions = this.createFindOptions(_findOptions, _viewOptions);
            SF.FindNavigator.openFinder(findOptions);
        };

        EntityRepeater.prototype.createFindOptions = function (_findOptions, _viewOptions) {
            var newPrefixIndex = this.getLastPrefixIndex() + 1;
            var itemPrefix = SF.compose(this.options.prefix, newPrefixIndex.toString());
            var self = this;
            return $.extend({
                prefix: itemPrefix,
                onOk: function (selectedItems) {
                    return self.onFindingOk(selectedItems, _viewOptions);
                }
            }, _findOptions);
        };

        EntityRepeater.prototype.onFindingOk = function (selectedItems, _viewOptions) {
            if (selectedItems == null || selectedItems.length == 0) {
                throw "No item was returned from Find Window";
            }
            var self = this;
            this.foreachNewItem(selectedItems, function (item, itemPrefix) {
                if (!self.canAddItems()) {
                    return;
                }

                self.newRepItem('', itemPrefix, item);

                //View results in the repeater
                var viewOptions = self.viewOptionsForViewing($.extend(_viewOptions, { type: item.type, id: item.id }), itemPrefix);
                new SF.ViewNavigator(viewOptions).viewEmbedded();
                SF.triggerNewContent($(SF.compose(itemPrefix, EntityRepeater.key_entity)));
            });
            return true;
        };

        EntityRepeater.prototype.remove = function (itemPrefix) {
            $('#' + SF.compose(itemPrefix, EntityRepeater.key_repeaterItem)).remove();
            this.fireOnEntityChanged(false);
        };

        EntityRepeater.prototype.updateButtonsDisplay = function () {
            var $buttons = $(this.pf("btnFind"), this.pf("btnCreate"));
            if (this.canAddItems()) {
                $buttons.show();
            } else {
                $buttons.hide();
            }
        };
        EntityRepeater.key_itemsContainer = "sfItemsContainer";
        EntityRepeater.key_repeaterItem = "sfRepeaterItem";
        EntityRepeater.key_repeaterItemClass = "sf-repeater-element";
        EntityRepeater.key_link = "sfLink";
        return EntityRepeater;
    })(EntityList);
    SF.EntityRepeater = EntityRepeater;

    once("SF-entityStrip", function () {
        return $.fn.entityStrip = function (opt) {
            return new EntityStrip(this, opt);
        };
    });

    var EntityStrip = (function (_super) {
        __extends(EntityStrip, _super);
        function EntityStrip(element, options) {
            _super.call(this, element, options);
        }
        EntityStrip.prototype.itemSuffix = function () {
            return EntityStrip.key_stripItem;
        };

        EntityStrip.prototype.getItems = function () {
            return $(this.pf(EntityStrip.key_itemsContainer) + " > ." + EntityStrip.key_stripItemClass);
        };

        EntityStrip.prototype.viewOptionsForCreating = function (_viewOptions) {
            var self = this;
            var newPrefixIndex = this.getLastPrefixIndex() + 1;
            var itemPrefix = SF.compose(this.options.prefix, newPrefixIndex.toString());
            return $.extend({
                onOk: function (clonedElements) {
                    return self.onCreatingOk(clonedElements, _viewOptions.validationOptions, _viewOptions.type, itemPrefix);
                },
                onCancelled: null,
                controllerUrl: null,
                prefix: itemPrefix,
                partialViewName: this.options.partialViewName,
                requestExtraJsonData: this.extraJsonParams(itemPrefix)
            }, _viewOptions);
        };

        EntityStrip.prototype.onCreatingOk = function (clonedElements, validatorOptions, entityType, itemPrefix) {
            var valOptions = $.extend(validatorOptions || {}, {
                type: entityType
            });
            var validatorResult = this.checkValidation(valOptions, itemPrefix);
            if (validatorResult.acceptChanges) {
                var runtimeInfo;
                var $mainControl = $(".sf-main-control[data-prefix=" + itemPrefix + "]");
                if ($mainControl.length > 0) {
                    runtimeInfo = $mainControl.data("runtimeinfo");
                }
                this.newStripItem(clonedElements, itemPrefix, { runtimeInfo: runtimeInfo, type: entityType, toStr: validatorResult.newToStr, link: validatorResult.newLink });
            }
            return validatorResult.acceptChanges;
        };

        EntityStrip.prototype.newStripItem = function (newHtml, itemPrefix, item) {
            var itemInfoValue = item.runtimeInfo || this.itemRuntimeInfo(itemPrefix).createValue(item.type, item.id || '', typeof item.id == "undefined" ? 'n' : 'o', null);
            var $li = $("<li id='" + SF.compose(itemPrefix, EntityStrip.key_stripItem) + "' name='" + SF.compose(itemPrefix, EntityStrip.key_stripItem) + "' class='" + EntityStrip.key_stripItemClass + "'>" + SF.hiddenInput(SF.compose(itemPrefix, EntityStrip.key_indexes), ";" + (this.getLastNewIndex() + 1).toString()) + SF.hiddenInput(SF.compose(itemPrefix, SF.Keys.runtimeInfo), itemInfoValue) + (this.options.navigate ? ("<a class='sf-entitStrip-link' id='" + SF.compose(itemPrefix, EntityStrip.key_link) + "' href='" + item.link + "' title='" + lang.signum.navigate + "'>" + item.toStr + "</a>") : ("<span class='sf-entitStrip-link' id='" + SF.compose(itemPrefix, EntityStrip.key_link) + "'>" + item.toStr + "</span>")) + "<span class='sf-button-container'>" + ((this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnUp") + "' title='" + lang.signum.moveUp + "' onclick=\"" + this._getMovingUp(itemPrefix) + "\" class='sf-line-button sf-move-up' data-icon='ui-icon-triangle-1-" + (this.options.vertical ? "w" : "n") + "' data-text='false'>" + lang.signum.moveUp + "</span>") : "") + (this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnDown") + "' title='" + lang.signum.moveDown + "' onclick=\"" + this._getMovingDown(itemPrefix) + "\" class='sf-line-button sf-move-down' data-icon='ui-icon-triangle-1-" + (this.options.vertical ? "e" : "s") + "' data-text='false'>" + lang.signum.moveDown + "</span>") : "") + (this.options.view ? ("<a id='" + SF.compose(itemPrefix, "btnView") + "' title='" + lang.signum.view + "' onclick=\"" + this._getView(itemPrefix) + "\" class='sf-line-button sf-view' data-icon='ui-icon-circle-arrow-e' data-text='false'>" + lang.signum.view + "</a>") : "") + (this.options.remove ? ("<a id='" + SF.compose(itemPrefix, "btnRemove") + "' title='" + lang.signum.remove + "' onclick=\"" + this._getRemoving(itemPrefix) + "\" class='sf-line-button sf-remove' data-icon='ui-icon-circle-close' data-text='false'>" + lang.signum.remove + "</a>") : "")) + "</span>" + (!SF.isEmpty(newHtml) ? "<div id='" + SF.compose(itemPrefix, EntityStrip.key_entity) + "' name='" + SF.compose(itemPrefix, EntityStrip.key_entity) + "' style='display:none'></div>" : "") + "</li>");

            $(this.pf(EntityStrip.key_itemsContainer) + " ." + EntityStrip.key_input).before($li);
            if (!SF.isEmpty(newHtml))
                $("#" + SF.compose(itemPrefix, EntityStrip.key_entity)).html(newHtml);
            SF.triggerNewContent($("#" + SF.compose(itemPrefix, EntityStrip.key_stripItem)));
            this.fireOnEntityChanged(false);
        };

        EntityStrip.prototype._getRepeaterCall = function () {
            return "$('#" + this.options.prefix + "').data('SF-control')";
        };

        EntityStrip.prototype._getRemoving = function (itemPrefix) {
            return this._getRepeaterCall() + ".remove('" + itemPrefix + "');";
        };

        EntityStrip.prototype._getView = function (itemPrefix) {
            return this._getRepeaterCall() + ".view('" + itemPrefix + "');";
        };

        EntityStrip.prototype._getMovingUp = function (itemPrefix) {
            return this._getRepeaterCall() + ".moveUp('" + itemPrefix + "');";
        };

        EntityStrip.prototype._getMovingDown = function (itemPrefix) {
            return this._getRepeaterCall() + ".moveDown('" + itemPrefix + "');";
        };

        EntityStrip.prototype.find = function (_findOptions, _viewOptions) {
            var _self = this;
            var type = this.getEntityType(function (type) {
                _self.typedFind($.extend({ webQueryName: type }, _findOptions), _viewOptions);
            });
        };

        EntityStrip.prototype.typedFind = function (_findOptions, _viewOptions) {
            if (SF.isEmpty(_findOptions.webQueryName)) {
                throw "FindOptions webQueryName parameter must not be null in ERep typedFind. Call find instead";
            }
            if (!this.canAddItems()) {
                return;
            }

            var findOptions = this.createFindOptions(_findOptions, _viewOptions);
            SF.FindNavigator.openFinder(findOptions);
        };

        EntityStrip.prototype.createFindOptions = function (_findOptions, _viewOptions) {
            var newPrefixIndex = this.getLastPrefixIndex() + 1;
            var itemPrefix = SF.compose(this.options.prefix, newPrefixIndex.toString());
            var self = this;
            return $.extend({
                prefix: itemPrefix,
                onOk: function (selectedItems) {
                    return self.onFindingOk(selectedItems, _viewOptions);
                }
            }, _findOptions);
        };

        EntityStrip.prototype.onFindingOk = function (selectedItems, _viewOptions) {
            if (selectedItems == null || selectedItems.length == 0) {
                throw "No item was returned from Find Window";
            }
            var self = this;
            this.foreachNewItem(selectedItems, function (item, itemPrefix) {
                if (!self.canAddItems()) {
                    return;
                }

                self.newStripItem(null, itemPrefix, item);
            });
            return true;
        };

        EntityStrip.prototype.remove = function (itemPrefix) {
            $('#' + SF.compose(itemPrefix, EntityStrip.key_stripItem)).remove();
            this.fireOnEntityChanged(false);
        };

        EntityStrip.prototype.view = function (_viewOptions, itemPrefix) {
            this.viewInIndex(_viewOptions || {}, itemPrefix);
        };

        EntityStrip.prototype.updateButtonsDisplay = function () {
            var $buttons = $(this.pf("btnFind") + ", " + this.pf("btnCreate") + ", " + this.pf("sfToStr"));
            if (this.canAddItems()) {
                $buttons.show();
            } else {
                $buttons.hide();
            }
        };

        EntityStrip.prototype.updateLinks = function (newToStr, newLink, itemPrefix) {
            $('#' + SF.compose(itemPrefix, SF.Keys.link)).html(newToStr);
        };

        EntityStrip.prototype.onAutocompleteSelected = function (controlId, data) {
            var selectedItems = [{
                    id: data.id,
                    type: data.type,
                    toStr: data.text,
                    link: data.link
                }];
            this.onFindingOk(selectedItems);
            $("#" + controlId).val("");
            this.fireOnEntityChanged(true);
        };
        EntityStrip.key_itemsContainer = "sfItemsContainer";
        EntityStrip.key_stripItem = "sfStripItem";
        EntityStrip.key_stripItemClass = "sf-strip-element";
        EntityStrip.key_link = "sfLink";
        EntityStrip.key_input = "sf-strip-input";
        return EntityStrip;
    })(EntityList);
    SF.EntityStrip = EntityStrip;
})(SF || (SF = {}));
//# sourceMappingURL=SF_Lines.js.map

/// <reference path="globals.ts"/>
var __extends = this.__extends || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Entities", "Framework/Signum.Web/Signum/Scripts/Validator", "Framework/Signum.Web/Signum/Scripts/Navigator", "Framework/Signum.Web/Signum/Scripts/Finder"], function(require, exports, Entities, Validator, Navigator, Finder) {
    var EntityBase = (function () {
        function EntityBase(element, options) {
            this.element = element;
            this.element.data("SF-control", this);
            this.options = options;
            var temp = $(this.pf(Entities.Keys.template));

            if (temp.length > 0)
                this.options.template = temp.html().replaceAll("<scriptX", "<script").replaceAll("</scriptX", "</script");

            this._create();
        }
        EntityBase.prototype.ready = function () {
            this.element.SFControlFullfill(this);
        };

        EntityBase.prototype._create = function () {
            var _this = this;
            var $txt = $(this.pf(Entities.Keys.toStr) + ".sf-entity-autocomplete");
            if ($txt.length > 0) {
                this.autoCompleter = new AjaxEntityAutocompleter(this.options.autoCompleteUrl || SF.Urls.autocomplete, function (term) {
                    return ({ types: _this.options.types, l: 5, q: term });
                });

                this.setupAutocomplete($txt);
            }
        };

        EntityBase.prototype.runtimeInfoHiddenElement = function (itemPrefix) {
            return $(this.pf(Entities.Keys.runtimeInfo));
        };

        EntityBase.prototype.pf = function (s) {
            return "#" + SF.compose(this.options.prefix, s);
        };

        EntityBase.prototype.containerDiv = function (itemPrefix) {
            var containerDivId = this.pf(EntityBase.key_entity);
            if ($(containerDivId).length == 0)
                this.runtimeInfoHiddenElement().after(SF.hiddenDiv(containerDivId.after('#'), ""));

            return $(containerDivId);
        };

        EntityBase.prototype.extractEntityHtml = function (itemPrefix) {
            var runtimeInfo = Entities.RuntimeInfo.getFromPrefix(this.options.prefix);

            if (runtimeInfo == null)
                return null;

            var div = this.containerDiv();

            var result = new Entities.EntityHtml(this.options.prefix, runtimeInfo, this.getToString(), this.getLink());

            result.html = div.children();

            div.html(null);

            return result;
        };

        EntityBase.prototype.getLink = function (itemPrefix) {
            return null;
        };

        EntityBase.prototype.getToString = function (itemPrefix) {
            return null;
        };

        EntityBase.prototype.setEntitySpecific = function (entityValue, itemPrefix) {
            //virtual function
        };

        EntityBase.prototype.setEntity = function (entityValue, itemPrefix) {
            this.setEntitySpecific(entityValue);

            if (entityValue)
                entityValue.assertPrefixAndType(this.options.prefix, this.options.types);

            SF.triggerNewContent(this.containerDiv().html(entityValue == null ? null : entityValue.html));
            Entities.RuntimeInfo.setFromPrefix(this.options.prefix, entityValue == null ? null : entityValue.runtimeInfo);
            if (entityValue == null) {
                Validator.cleanError($(this.pf(Entities.Keys.toStr)).val(""));
                Validator.cleanError($(this.pf(Entities.Keys.link)).val("").html(""));
            }

            this.updateButtonsDisplay();
            if (!SF.isEmpty(this.entityChanged)) {
                this.entityChanged();
            }
        };

        EntityBase.prototype.remove_click = function () {
            var _this = this;
            return this.onRemove(this.options.prefix).then(function (result) {
                if (result)
                    _this.setEntity(null);
            });
        };

        EntityBase.prototype.onRemove = function (prefix) {
            if (this.removing != null)
                return this.removing(prefix);

            return Promise.resolve(true);
        };

        EntityBase.prototype.create_click = function () {
            var _this = this;
            return this.onCreating(this.options.prefix).then(function (result) {
                if (result)
                    _this.setEntity(result);
            });
        };

        EntityBase.prototype.typeChooser = function () {
            var _this = this;
            return Navigator.typeChooser(this.options.prefix, this.options.types.map(function (t, i) {
                return ({ type: t, toStr: _this.options.typeNiceNames[i] });
            }));
        };

        EntityBase.prototype.singleType = function () {
            if (this.options.types.length != 1)
                throw new Error("There are {0} types in {1}".format(this.options.types.length, this.options.prefix));

            return this.options.types[0];
        };

        EntityBase.prototype.onCreating = function (prefix) {
            var _this = this;
            if (this.creating != null)
                return this.creating(prefix);

            return this.typeChooser().then(function (type) {
                if (type == null)
                    return null;

                var newEntity = _this.options.template ? _this.getEmbeddedTemplate(prefix) : new Entities.EntityHtml(prefix, new Entities.RuntimeInfo(type, null, true));

                return Navigator.viewPopup(newEntity, _this.defaultViewOptions());
            });
        };

        EntityBase.prototype.getEmbeddedTemplate = function (itemPrefix) {
            if (!this.options.template)
                throw new Error("no template in " + this.options.prefix);

            var result = new Entities.EntityHtml(this.options.prefix, new Entities.RuntimeInfo(this.singleType(), null, true));

            result.loadHtml(this.options.template);

            return result;
        };

        EntityBase.prototype.view_click = function () {
            var _this = this;
            var entityHtml = this.extractEntityHtml();

            return this.onViewing(entityHtml).then(function (result) {
                if (result)
                    _this.setEntity(result);
                else
                    _this.setEntity(entityHtml); //previous entity passed by reference
            });
        };

        EntityBase.prototype.onViewing = function (entityHtml) {
            if (this.viewing != null)
                return this.viewing(entityHtml);

            return Navigator.viewPopup(entityHtml, this.defaultViewOptions());
        };

        EntityBase.prototype.find_click = function () {
            var _this = this;
            return this.onFinding(this.options.prefix).then(function (result) {
                if (result)
                    _this.setEntity(result);
            });
        };

        EntityBase.prototype.onFinding = function (prefix) {
            if (this.finding != null)
                return this.finding(prefix);

            return this.typeChooser().then(function (type) {
                if (type == null)
                    return null;

                return Finder.find({
                    webQueryName: type,
                    prefix: prefix
                });
            });
        };

        EntityBase.prototype.defaultViewOptions = function () {
            return {
                readOnly: this.options.isReadonly,
                partialViewName: this.options.partialViewName,
                validationOptions: {
                    rootType: this.options.rootType,
                    propertyRoute: this.options.propertyRoute
                }
            };
        };

        EntityBase.prototype.updateButtonsDisplay = function () {
            var hasEntity = !!Entities.RuntimeInfo.getFromPrefix(this.options.prefix);

            $(this.pf("btnCreate")).toggle(!hasEntity);
            $(this.pf("btnFind")).toggle(!hasEntity);
            $(this.pf("btnRemove")).toggle(hasEntity);
            $(this.pf("btnView")).toggle(hasEntity);
            $(this.pf(Entities.Keys.link)).toggle(hasEntity);
            $(this.pf(Entities.Keys.toStr)).toggle(!hasEntity);
        };

        EntityBase.prototype.setupAutocomplete = function ($txt) {
            var _this = this;
            var auto = $txt.autocomplete({
                delay: 200,
                source: function (request, response) {
                    _this.autoCompleter.getResults(request.term).then(function (entities) {
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
                    _this.onAutocompleteSelected(ui.item.value);
                    event.preventDefault();
                }
            });

            auto.data("uiAutocomplete")._renderItem = function (ul, item) {
                var val = item.value;

                return $("<li>").attr("data-type", val.runtimeInfo.type).attr("data-id", val.runtimeInfo.id).append($("<a>").text(item.label)).appendTo(ul);
            };
        };

        EntityBase.prototype.onAutocompleteSelected = function (entityValue) {
            throw new Error("onAutocompleteSelected is abstract");
        };
        EntityBase.key_entity = "sfEntity";
        return EntityBase;
    })();
    exports.EntityBase = EntityBase;

    var AjaxEntityAutocompleter = (function () {
        function AjaxEntityAutocompleter(controllerUrl, getData) {
            this.controllerUrl = controllerUrl;
            this.getData = getData;
        }
        AjaxEntityAutocompleter.prototype.getResults = function (term) {
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
                            return new Entities.EntityValue(new Entities.RuntimeInfo(item.type, item.id, false), item.text, item.link);
                        }));
                    }
                });
            });
        };
        return AjaxEntityAutocompleter;
    })();
    exports.AjaxEntityAutocompleter = AjaxEntityAutocompleter;

    var EntityLine = (function (_super) {
        __extends(EntityLine, _super);
        function EntityLine() {
            _super.apply(this, arguments);
        }
        EntityLine.prototype.getLink = function (itemPrefix) {
            return $(this.pf(Entities.Keys.link)).attr("href");
        };

        EntityLine.prototype.getToString = function (itemPrefix) {
            return $(this.pf(Entities.Keys.link)).text();
        };

        EntityLine.prototype.setEntitySpecific = function (entityValue, itemPrefix) {
            var link = $(this.pf(Entities.Keys.link));
            link.text(entityValue == null ? null : entityValue.toStr);
            if (link.filter('a').length !== 0)
                link.attr('href', entityValue == null ? null : entityValue.link);
            $(this.pf(Entities.Keys.toStr)).val('');
        };

        EntityLine.prototype.onAutocompleteSelected = function (entityValue) {
            this.setEntity(entityValue);
        };
        return EntityLine;
    })(EntityBase);
    exports.EntityLine = EntityLine;

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
                var o = c.children("option[value='" + entityValue.runtimeInfo.key() + "']");
                if (o.length == 1)
                    o.html(entityValue.toStr);
                else
                    c.add($("<option value='{0}'/>".format(entityValue.runtimeInfo.key())).text(entityValue.toStr));

                c.val(entityValue.runtimeInfo.key());
            }
        };

        EntityCombo.prototype.getToString = function (itemPrefix) {
            return this.combo().children("option[value='" + this.combo().val() + "']").text();
        };

        EntityCombo.prototype.combo_selected = function () {
            var val = this.combo().val();

            var ri = Entities.RuntimeInfo.fromKey(val);

            this.setEntity(ri == null ? null : new Entities.EntityValue(ri, this.getToString()));
        };
        EntityCombo.key_combo = "sfCombo";
        return EntityCombo;
    })(EntityBase);
    exports.EntityCombo = EntityCombo;

    var EntityLineDetail = (function (_super) {
        __extends(EntityLineDetail, _super);
        function EntityLineDetail(element, options) {
            _super.call(this, element, options);
        }
        EntityLineDetail.prototype.containerDiv = function (itemPrefix) {
            return $("#" + this.options.detailDiv);
        };

        EntityLineDetail.prototype.setEntitySpecific = function (entityValue, itemPrefix) {
            if (entityValue == null)
                return;

            if (!entityValue.isLoaded())
                throw new Error("EntityLineDetail requires a loaded Entities.EntityHtml, consider calling Navigator.loadPartialView");
        };

        EntityLineDetail.prototype.onCreating = function (prefix) {
            var _this = this;
            if (this.creating != null)
                return this.creating(prefix);

            if (this.options.template)
                return Promise.resolve(this.getEmbeddedTemplate(prefix));

            return this.typeChooser().then(function (type) {
                if (type == null)
                    return null;

                var newEntity = new Entities.EntityHtml(prefix, new Entities.RuntimeInfo(type, null, true));

                return Navigator.requestPartialView(newEntity, _this.defaultViewOptions());
            });
        };

        EntityLineDetail.prototype.find_click = function () {
            var _this = this;
            return this.onFinding(this.options.prefix).then(function (result) {
                if (result == null)
                    return null;

                if (result.isLoaded())
                    return Promise.resolve(result);

                return Navigator.requestPartialView(new Entities.EntityHtml(_this.options.prefix, result.runtimeInfo), _this.defaultViewOptions());
            }).then(function (result) {
                if (result)
                    _this.setEntity(result);
            });
        };
        return EntityLineDetail;
    })(EntityBase);
    exports.EntityLineDetail = EntityLineDetail;

    var EntityListBase = (function (_super) {
        __extends(EntityListBase, _super);
        function EntityListBase(element, options) {
            _super.call(this, element, options);
        }
        EntityListBase.prototype.runtimeInfo = function (itemPrefix) {
            return $("#" + SF.compose(itemPrefix, Entities.Keys.runtimeInfo));
        };

        EntityListBase.prototype.containerDiv = function (itemPrefix) {
            var containerDivId = "#" + SF.compose(itemPrefix, EntityList.key_entity);
            if ($(containerDivId).length == 0)
                this.runtimeInfo(itemPrefix).after(SF.hiddenDiv(containerDivId.after("#"), ""));

            return $(containerDivId);
        };

        EntityListBase.prototype.getEmbeddedTemplate = function (itemPrefix) {
            if (!this.options.template)
                throw new Error("no template in " + this.options.prefix);

            var result = new Entities.EntityHtml(itemPrefix, new Entities.RuntimeInfo(this.singleType(), null, true));

            var replaced = this.options.template.replace(new RegExp(SF.compose(this.options.prefix, "0"), "gi"), itemPrefix);

            result.loadHtml(replaced);

            return result;
        };

        EntityListBase.prototype.extractEntityHtml = function (itemPrefix) {
            var runtimeInfo = Entities.RuntimeInfo.getFromPrefix(itemPrefix);

            var div = this.containerDiv(itemPrefix);

            var result = new Entities.EntityHtml(itemPrefix, runtimeInfo, this.getToString(itemPrefix), this.getLink(itemPrefix));

            result.html = div.children();

            div.html(null);

            return result;
        };

        EntityListBase.prototype.setEntity = function (entityValue, itemPrefix) {
            if (entityValue == null)
                throw new Error("entityValue is mandatory on setEntityItem");

            this.setEntitySpecific(entityValue, itemPrefix);

            entityValue.assertPrefixAndType(itemPrefix, this.options.types);

            if (entityValue.isLoaded())
                SF.triggerNewContent(this.containerDiv(itemPrefix).html(entityValue.html));

            Entities.RuntimeInfo.setFromPrefix(itemPrefix, entityValue.runtimeInfo);

            this.updateButtonsDisplay();
            if (!SF.isEmpty(this.entityChanged)) {
                this.entityChanged();
            }
        };

        EntityListBase.prototype.create_click = function () {
            var _this = this;
            var itemPrefix = this.getNextPrefix();
            return this.onCreating(itemPrefix).then(function (entity) {
                if (entity)
                    _this.addEntity(entity, itemPrefix);
            });
        };

        EntityListBase.prototype.addEntitySpecific = function (entityValue, itemPrefix) {
            //virtual
        };

        EntityListBase.prototype.addEntity = function (entityValue, itemPrefix) {
            if (entityValue == null)
                throw new Error("entityValue is mandatory on setEntityItem");

            this.addEntitySpecific(entityValue, itemPrefix);

            if (entityValue)
                entityValue.assertPrefixAndType(itemPrefix, this.options.types);

            if (entityValue.isLoaded())
                SF.triggerNewContent(this.containerDiv(itemPrefix).html(entityValue.html));
            Entities.RuntimeInfo.setFromPrefix(itemPrefix, entityValue.runtimeInfo);

            this.updateButtonsDisplay();
            if (!SF.isEmpty(this.entityChanged)) {
                this.entityChanged();
            }
        };

        EntityListBase.prototype.removeEntitySpecific = function (itemPrefix) {
            //virtual
        };

        EntityListBase.prototype.removeEntity = function (itemPrefix) {
            this.removeEntitySpecific(itemPrefix);

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

        EntityListBase.prototype.getPrefixes = function () {
            var _this = this;
            return this.getItems().toArray().map(function (e) {
                return e.id.before("_" + _this.itemSuffix());
            });
        };

        EntityListBase.prototype.getRuntimeInfos = function () {
            return this.getPrefixes().map(function (p) {
                return Entities.RuntimeInfo.getFromPrefix(p);
            });
        };

        EntityListBase.prototype.getNextPrefix = function (inc) {
            if (typeof inc === "undefined") { inc = 0; }
            var _this = this;
            var indices = this.getItems().toArray().map(function (e) {
                return parseInt(e.id.after(_this.options.prefix + "_").before("_" + _this.itemSuffix()));
            });

            var next = indices.length == 0 ? inc : (Math.max.apply(null, indices) + 1 + inc);

            return SF.compose(this.options.prefix, next.toString());
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

        EntityListBase.prototype.getNextPosIndex = function () {
            return ";" + (this.getLastPosIndex() + 1).toString();
        };

        EntityListBase.prototype.canAddItems = function () {
            return SF.isEmpty(this.options.maxElements) || this.getItems().length < this.options.maxElements;
        };

        EntityListBase.prototype.find_click = function () {
            var _this = this;
            return this.onFindingMany(this.options.prefix).then(function (result) {
                if (result)
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
                return this.findingMany(prefix);

            return this.typeChooser().then(function (type) {
                if (type == null)
                    return null;

                return Finder.findMany({
                    webQueryName: type,
                    prefix: prefix
                });
            });
        };

        EntityListBase.prototype.moveUp = function (itemPrefix) {
            var suffix = this.itemSuffix();
            var $item = $("#" + SF.compose(itemPrefix, suffix));
            var $itemPrev = $item.prev();

            if ($itemPrev.length == 0) {
                return;
            }

            var itemPrevPrefix = $itemPrev[0].id.before("_" + suffix);

            var prevNewIndex = this.getPosIndex(itemPrevPrefix);
            this.setPosIndex(itemPrefix, prevNewIndex);
            this.setPosIndex(itemPrevPrefix, prevNewIndex + 1);

            $item.insertBefore($itemPrev);
        };

        EntityListBase.prototype.moveDown = function (itemPrefix) {
            var suffix = this.itemSuffix();
            var $item = $("#" + SF.compose(itemPrefix, suffix));
            var $itemNext = $item.next();

            if ($itemNext.length == 0) {
                return;
            }

            var itemNextPrefix = $itemNext[0].id.before("_" + suffix);

            var nextNewIndex = this.getPosIndex(itemNextPrefix);
            this.setPosIndex(itemPrefix, nextNewIndex);
            this.setPosIndex(itemNextPrefix, nextNewIndex - 1);

            $item.insertAfter($itemNext);
        };

        EntityListBase.prototype.getPosIndex = function (itemPrefix) {
            return parseInt($("#" + SF.compose(itemPrefix, EntityListBase.key_indexes)).val().after(";"));
        };

        EntityListBase.prototype.setPosIndex = function (itemPrefix, newIndex) {
            var $indexes = $("#" + SF.compose(itemPrefix, EntityListBase.key_indexes));
            $indexes.val($indexes.val().before(";") + ";" + newIndex.toString());
        };
        EntityListBase.key_indexes = "sfIndexes";
        return EntityListBase;
    })(EntityBase);
    exports.EntityListBase = EntityListBase;

    var EntityList = (function (_super) {
        __extends(EntityList, _super);
        function EntityList() {
            _super.apply(this, arguments);
        }
        EntityList.prototype.itemSuffix = function () {
            return Entities.Keys.toStr;
        };

        EntityList.prototype.updateLinks = function (newToStr, newLink, itemPrefix) {
            $('#' + SF.compose(itemPrefix, Entities.Keys.toStr)).html(newToStr);
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

            return this.onViewing(entityHtml).then(function (result) {
                if (result)
                    _this.setEntity(result, selectedItemPrefix);
                else
                    _this.setEntity(entityHtml, selectedItemPrefix); //previous entity passed by reference
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

        EntityList.prototype.getToString = function (itemPrefix) {
            return $("#" + SF.compose(itemPrefix, Entities.Keys.toStr)).text();
        };

        EntityList.prototype.setEntitySpecific = function (entityValue, itemPrefix) {
            $("#" + SF.compose(itemPrefix, Entities.Keys.toStr)).text(entityValue.toStr);
        };

        EntityList.prototype.addEntitySpecific = function (entityValue, itemPrefix) {
            var $table = $("#" + this.options.prefix + "> .sf-field-list > .sf-field-list-table");

            $table.before(SF.hiddenInput(SF.compose(itemPrefix, EntityList.key_indexes), this.getNextPosIndex()));

            $table.before(SF.hiddenInput(SF.compose(itemPrefix, Entities.Keys.runtimeInfo), entityValue.runtimeInfo.toString()));

            $table.before(SF.hiddenDiv(SF.compose(itemPrefix, EntityList.key_entity), ""));

            var select = $(this.pf(EntityList.key_list));
            select.append("\n<option id='" + SF.compose(itemPrefix, Entities.Keys.toStr) + "' name='" + SF.compose(itemPrefix, Entities.Keys.toStr) + "' value='' class='sf-value-line'>" + entityValue.toStr + "</option>");
            select.children('option').attr('selected', false); //Fix for Firefox: Set selected after retrieving the html of the select
            select.children('option:last').attr('selected', true);
        };

        EntityList.prototype.remove_click = function () {
            var _this = this;
            var selectedItemPrefix = this.selectedItemPrefix();
            return this.onRemove(selectedItemPrefix).then(function (result) {
                if (result)
                    _this.removeEntity(selectedItemPrefix);
            });
        };

        EntityList.prototype.removeEntitySpecific = function (itemPrefix) {
            $("#" + SF.compose(itemPrefix, Entities.Keys.runtimeInfo)).remove();
            $("#" + SF.compose(itemPrefix, Entities.Keys.toStr)).remove();
            $("#" + SF.compose(itemPrefix, EntityList.key_entity)).remove();
            $("#" + SF.compose(itemPrefix, EntityList.key_indexes)).remove();
        };

        EntityList.prototype.moveUp_click = function () {
            this.moveUp(this.selectedItemPrefix());
        };

        EntityList.prototype.moveDown_click = function () {
            this.moveDown(this.selectedItemPrefix());
        };
        EntityList.key_list = "sfList";
        return EntityList;
    })(EntityListBase);
    exports.EntityList = EntityList;

    var EntityListDetail = (function (_super) {
        __extends(EntityListDetail, _super);
        function EntityListDetail(element, options) {
            _super.call(this, element, options);
        }
        EntityListDetail.prototype.selection_Changed = function () {
            this.stageCurrentSelected();
        };

        EntityListDetail.prototype.remove_click = function () {
            var _this = this;
            return _super.prototype.remove_click.call(this).then(function () {
                return _this.stageCurrentSelected();
            });
        };

        EntityListDetail.prototype.create_click = function () {
            var _this = this;
            return _super.prototype.create_click.call(this).then(function () {
                return _this.stageCurrentSelected();
            });
        };

        EntityListDetail.prototype.find_click = function () {
            var _this = this;
            return _super.prototype.find_click.call(this).then(function () {
                return _this.stageCurrentSelected();
            });
        };

        EntityListDetail.prototype.stageCurrentSelected = function () {
            var selPrefix = this.selectedItemPrefix();

            var detailDiv = $("#" + this.options.detailDiv);

            var children = detailDiv.children();

            if (children.length != 0) {
                var itemPrefix = children[0].id.before("_" + EntityListDetail.key_entity);
                if (selPrefix == itemPrefix) {
                    children.show();
                    return;
                }
                children.hide();
                this.runtimeInfo(itemPrefix).after(children);
            }

            var selContainer = this.containerDiv(selPrefix);

            if (selContainer.children().length > 0) {
                detailDiv.append(selContainer);
                selContainer.show();
            } else {
                var entity = new Entities.EntityHtml(selPrefix, Entities.RuntimeInfo.getFromPrefix(selPrefix), null, null);

                Navigator.requestPartialView(entity, this.defaultViewOptions()).then(function (e) {
                    selContainer.html(e.html);
                    detailDiv.append(selContainer);
                    selContainer.show();
                });
            }
        };

        EntityListDetail.prototype.onCreating = function (prefix) {
            var _this = this;
            if (this.creating != null)
                return this.creating(prefix);

            if (this.options.template)
                return Promise.resolve(this.getEmbeddedTemplate(prefix));

            return this.typeChooser().then(function (type) {
                if (type == null)
                    return null;

                var newEntity = new Entities.EntityHtml(prefix, new Entities.RuntimeInfo(type, null, true));

                return Navigator.requestPartialView(newEntity, _this.defaultViewOptions());
            });
        };
        return EntityListDetail;
    })(EntityList);
    exports.EntityListDetail = EntityListDetail;

    var EntityRepeater = (function (_super) {
        __extends(EntityRepeater, _super);
        function EntityRepeater() {
            _super.apply(this, arguments);
        }
        EntityRepeater.prototype.itemSuffix = function () {
            return EntityRepeater.key_repeaterItem;
        };

        EntityRepeater.prototype.getItems = function () {
            return $(this.pf(EntityRepeater.key_itemsContainer) + " > ." + EntityRepeater.key_repeaterItemClass);
        };

        EntityRepeater.prototype.removeEntitySpecific = function (itemPrefix) {
            $("#" + SF.compose(itemPrefix, EntityRepeater.key_repeaterItem)).remove();
        };

        EntityRepeater.prototype.addEntitySpecific = function (entityValue, itemPrefix) {
            var fieldSet = $("<fieldset id='" + SF.compose(itemPrefix, EntityRepeater.key_repeaterItem) + "' class='" + EntityRepeater.key_repeaterItemClass + "'>" + "<legend>" + (this.options.remove ? ("<a id='" + SF.compose(itemPrefix, "btnRemove") + "' title='" + lang.signum.remove + "' onclick=\"" + this.getRemoving(itemPrefix) + "\" class='sf-line-button sf-remove' data-icon='ui-icon-circle-close' data-text='false'>" + lang.signum.remove + "</a>") : "") + (this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnUp") + "' title='" + lang.signum.moveUp + "' onclick=\"" + this.getMovingUp(itemPrefix) + "\" class='sf-line-button sf-move-up' data-icon='ui-icon-triangle-1-n' data-text='false'>" + lang.signum.moveUp + "</span>") : "") + (this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnDown") + "' title='" + lang.signum.moveDown + "' onclick=\"" + this.getMovingDown(itemPrefix) + "\" class='sf-line-button sf-move-down' data-icon='ui-icon-triangle-1-s' data-text='false'>" + lang.signum.moveDown + "</span>") : "") + "</legend>" + SF.hiddenInput(SF.compose(itemPrefix, EntityListBase.key_indexes), this.getNextPosIndex()) + SF.hiddenInput(SF.compose(itemPrefix, Entities.Keys.runtimeInfo), null) + "<div id='" + SF.compose(itemPrefix, EntityRepeater.key_entity) + "' name='" + SF.compose(itemPrefix, EntityRepeater.key_entity) + "' class='sf-line-entity'>" + "</div>" + "</fieldset>");

            $(this.pf(EntityRepeater.key_itemsContainer)).append(fieldSet);

            SF.triggerNewContent(fieldSet);
        };

        EntityRepeater.prototype.getRepeaterCall = function () {
            return "$('#" + this.options.prefix + "').data('SF-control')";
        };

        EntityRepeater.prototype.getRemoving = function (itemPrefix) {
            return this.getRepeaterCall() + ".removeItem_click('" + itemPrefix + "');";
        };

        EntityRepeater.prototype.getMovingUp = function (itemPrefix) {
            return this.getRepeaterCall() + ".moveUp('" + itemPrefix + "');";
        };

        EntityRepeater.prototype.getMovingDown = function (itemPrefix) {
            return this.getRepeaterCall() + ".moveDown('" + itemPrefix + "');";
        };

        EntityRepeater.prototype.remove_click = function () {
            throw new Error("remove_click is deprecated in EntityRepeater");
        };

        EntityRepeater.prototype.removeItem_click = function (itemPrefix) {
            var _this = this;
            return this.onRemove(itemPrefix).then(function (result) {
                if (result)
                    _this.removeEntity(itemPrefix);
            });
        };

        EntityRepeater.prototype.onCreating = function (prefix) {
            var _this = this;
            if (this.creating != null)
                return this.creating(prefix);

            if (this.options.template)
                return Promise.resolve(this.getEmbeddedTemplate(prefix));

            return this.typeChooser().then(function (type) {
                if (type == null)
                    return null;

                var newEntity = new Entities.EntityHtml(prefix, new Entities.RuntimeInfo(type, null, true));

                return Navigator.requestPartialView(newEntity, _this.defaultViewOptions());
            });
        };

        EntityRepeater.prototype.find_click = function () {
            var _this = this;
            return this.onFindingMany(this.options.prefix).then(function (result) {
                if (!result)
                    return;

                Promise.all(result.map(function (e, i) {
                    return ({ entity: e, prefix: _this.getNextPrefix(i) });
                }).map(function (t) {
                    var promise = t.entity.isLoaded() ? Promise.resolve(t.entity) : Navigator.requestPartialView(new Entities.EntityHtml(t.prefix, t.entity.runtimeInfo), _this.defaultViewOptions());

                    return promise.then(function (ev) {
                        return _this.addEntity(ev, t.prefix);
                    });
                }));
            });
        };

        EntityRepeater.prototype.updateButtonsDisplay = function () {
            var canAdd = this.canAddItems();

            $(this.pf("btnCreate")).toggle(canAdd);
            $(this.pf("btnFind")).toggle(canAdd);
        };
        EntityRepeater.key_itemsContainer = "sfItemsContainer";
        EntityRepeater.key_repeaterItem = "sfRepeaterItem";
        EntityRepeater.key_repeaterItemClass = "sf-repeater-element";
        return EntityRepeater;
    })(EntityListBase);
    exports.EntityRepeater = EntityRepeater;

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

        EntityStrip.prototype.setEntitySpecific = function (entityValue, itemPrefix) {
            var link = $('#' + SF.compose(itemPrefix, Entities.Keys.link));
            link.text(entityValue.toStr);
            if (this.options.navigate)
                link.attr("href", entityValue.link);
        };

        EntityStrip.prototype.getLink = function (itemPrefix) {
            return $('#' + SF.compose(itemPrefix, Entities.Keys.link)).attr("hef");
        };

        EntityStrip.prototype.getToString = function (itemPrefix) {
            return $('#' + SF.compose(itemPrefix, Entities.Keys.link)).text();
        };

        EntityStrip.prototype.removeEntitySpecific = function (itemPrefix) {
            $("#" + SF.compose(itemPrefix, EntityStrip.key_stripItem)).remove();
        };

        EntityStrip.prototype.addEntitySpecific = function (entityValue, itemPrefix) {
            var li = $("<li id='" + SF.compose(itemPrefix, EntityStrip.key_stripItem) + "' class='" + EntityStrip.key_stripItemClass + "'>" + SF.hiddenInput(SF.compose(itemPrefix, EntityStrip.key_indexes), this.getNextPosIndex()) + SF.hiddenInput(SF.compose(itemPrefix, Entities.Keys.runtimeInfo), null) + (this.options.navigate ? ("<a class='sf-entitStrip-link' id='" + SF.compose(itemPrefix, Entities.Keys.link) + "' href='" + entityValue.link + "' title='" + lang.signum.navigate + "'>" + entityValue.toStr + "</a>") : ("<span class='sf-entitStrip-link' id='" + SF.compose(itemPrefix, Entities.Keys.link) + "'>" + entityValue.toStr + "</span>")) + "<span class='sf-button-container'>" + ((this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnUp") + "' title='" + lang.signum.moveUp + "' onclick=\"" + this.getMovingUp(itemPrefix) + "\" class='sf-line-button sf-move-up' data-icon='ui-icon-triangle-1-" + (this.options.vertical ? "w" : "n") + "' data-text='false'>" + lang.signum.moveUp + "</span>") : "") + (this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnDown") + "' title='" + lang.signum.moveDown + "' onclick=\"" + this.getMovingDown(itemPrefix) + "\" class='sf-line-button sf-move-down' data-icon='ui-icon-triangle-1-" + (this.options.vertical ? "e" : "s") + "' data-text='false'>" + lang.signum.moveDown + "</span>") : "") + (this.options.view ? ("<a id='" + SF.compose(itemPrefix, "btnView") + "' title='" + lang.signum.view + "' onclick=\"" + this.getView(itemPrefix) + "\" class='sf-line-button sf-view' data-icon='ui-icon-circle-arrow-e' data-text='false'>" + lang.signum.view + "</a>") : "") + (this.options.remove ? ("<a id='" + SF.compose(itemPrefix, "btnRemove") + "' title='" + lang.signum.remove + "' onclick=\"" + this.getRemoving(itemPrefix) + "\" class='sf-line-button sf-remove' data-icon='ui-icon-circle-close' data-text='false'>" + lang.signum.remove + "</a>") : "")) + "</span>" + "<div id='" + SF.compose(itemPrefix, EntityStrip.key_entity) + "' name='" + SF.compose(itemPrefix, EntityStrip.key_entity) + "' style='display:none'></div>" + "</li>");

            $(this.pf(EntityStrip.key_itemsContainer) + " ." + EntityStrip.key_input).before(li);

            SF.triggerNewContent(li);
        };

        EntityStrip.prototype.getRepeaterCall = function () {
            return "$('#" + this.options.prefix + "').data('SF-control')";
        };

        EntityStrip.prototype.getRemoving = function (itemPrefix) {
            return this.getRepeaterCall() + ".removeItem_click('" + itemPrefix + "');";
        };

        EntityStrip.prototype.getView = function (itemPrefix) {
            return this.getRepeaterCall() + ".view_click('" + itemPrefix + "');";
        };

        EntityStrip.prototype.getMovingUp = function (itemPrefix) {
            return this.getRepeaterCall() + ".moveUp('" + itemPrefix + "');";
        };

        EntityStrip.prototype.getMovingDown = function (itemPrefix) {
            return this.getRepeaterCall() + ".moveDown('" + itemPrefix + "');";
        };

        EntityStrip.prototype.remove_click = function () {
            throw new Error("remove_click is deprecated in EntityRepeater");
        };

        EntityStrip.prototype.removeItem_click = function (itemPrefix) {
            var _this = this;
            return this.onRemove(itemPrefix).then(function (result) {
                if (result)
                    _this.removeEntity(itemPrefix);
            });
        };

        EntityStrip.prototype.view_click = function () {
            throw new Error("remove_click is deprecated in EntityRepeater");
        };

        EntityStrip.prototype.viewItem_click = function (itemPrefix) {
            var _this = this;
            var entityHtml = this.extractEntityHtml(itemPrefix);

            return this.onViewing(entityHtml).then(function (result) {
                if (result)
                    _this.setEntity(result, itemPrefix);
                else
                    _this.setEntity(entityHtml, itemPrefix); //previous entity passed by reference
            });
        };

        EntityStrip.prototype.updateButtonsDisplay = function () {
            var canAdd = this.canAddItems();

            $(this.pf("btnCreate")).toggle(canAdd);
            $(this.pf("btnFind")).toggle(canAdd);
            $(this.pf("sfToStr")).toggle(canAdd);
        };

        EntityStrip.prototype.onAutocompleteSelected = function (entityValue) {
            this.addEntity(entityValue, this.getNextPrefix());
        };
        EntityStrip.key_itemsContainer = "sfItemsContainer";
        EntityStrip.key_stripItem = "sfStripItem";
        EntityStrip.key_stripItemClass = "sf-strip-element";
        EntityStrip.key_input = "sf-strip-input";
        return EntityStrip;
    })(EntityList);
    exports.EntityStrip = EntityStrip;
});
//# sourceMappingURL=Lines.js.map

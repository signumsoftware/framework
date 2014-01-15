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
                partialViewName: "",
                onEntityChanged: null
            }, _options);

            this._create();

            this.element.trigger("SF-ready");
        }
        EntityBase.prototype._create = function () {
            var $txt = $(this.pf(SF.Keys.toStr) + ".sf-entity-autocomplete");
            if ($txt.length > 0) {
                var data = $txt.data();
                this.entityAutocomplete($txt, { delay: 200, types: data.types, url: data.url || SF.Urls.autocomplete, count: 5 });
            }
        };

        EntityBase.prototype.runtimeInfo = function () {
            return new SF.RuntimeInfo(this.options.prefix);
        };

        EntityBase.prototype.staticInfo = function () {
            return new SF.StaticInfo(this.options.prefix);
        };

        EntityBase.prototype.pf = function (s) {
            return "#" + SF.compose(this.options.prefix, s);
        };

        EntityBase.prototype.checkValidation = function (validatorOptions, itemPrefix) {
            if (typeof validatorOptions == "undefined" || typeof validatorOptions.type == "undefined") {
                throw "validatorOptions.type must be supplied to checkValidation";
            }

            var info = this.runtimeInfo();
            $.extend(validatorOptions, {
                prefix: this.options.prefix,
                id: (info.find().length !== 0) ? info.id() : ''
            });
            var validator = new SF.PartialValidator(validatorOptions);
            var validatorResult = validator.validate();
            if (!validatorResult.isValid) {
                if (!confirm(lang.signum.popupErrors)) {
                    $.extend(validatorResult, { acceptChanges: false });
                    return validatorResult;
                } else {
                    validator.showErrors(validatorResult.modelState, true);
                }
            }
            this.updateLinks(validatorResult.newToStr, validatorResult.newLink);
            $.extend(validatorResult, { acceptChanges: true });
            return validatorResult;
        };

        EntityBase.prototype.updateLinks = function (newToStr, newLink, itemPrefix) {
            //Abstract function
        };

        EntityBase.prototype.fireOnEntityChanged = function (hasEntity) {
            this.updateButtonsDisplay(hasEntity);
            if (!SF.isEmpty(this.options.onEntityChanged)) {
                this.options.onEntityChanged();
            }
        };

        EntityBase.prototype.remove = function (itemPrefix) {
            $(this.pf(SF.Keys.toStr)).val("").removeClass(SF.Validator.inputErrorClass);
            $(this.pf(SF.Keys.link)).val("").html("").removeClass(SF.Validator.inputErrorClass);
            this.runtimeInfo().removeEntity();

            this.removeSpecific();
            this.fireOnEntityChanged(false);
        };

        EntityBase.prototype.removeSpecific = function () {
            throw new Error("removeSpecific is abstract");
        };

        EntityBase.prototype.getEntityType = function (_onTypeFound) {
            var types = this.staticInfo().types().split(",");
            if (types.length == 1) {
                return _onTypeFound(types[0]);
            }

            SF.openTypeChooser(this.options.prefix, _onTypeFound);
        };

        EntityBase.prototype.create = function (_viewOptions) {
            var _self = this;
            var type = this.getEntityType(function (type) {
                _self.typedCreate($.extend({ type: type }, _viewOptions));
            });
        };

        EntityBase.prototype.typedCreate = function (_viewOptions) {
            if (SF.isEmpty(_viewOptions.type)) {
                throw "ViewOptions type parameter must not be null in entityBase typedCreate. Call create instead";
            }
            if (_viewOptions.navigate) {
                window.open(_viewOptions.controllerUrl.substring(0, _viewOptions.controllerUrl.lastIndexOf("/") + 1) + _viewOptions.type, "_blank");
                return;
            }
            var viewOptions = this.viewOptionsForCreating(_viewOptions);
            var template = this.getEmbeddedTemplate();
            if (!SF.isEmpty(template)) {
                new SF.ViewNavigator(viewOptions).showCreateOk(template);
            } else {
                new SF.ViewNavigator(viewOptions).createOk();
            }
        };

        EntityBase.prototype.viewOptionsForCreating = function (viewOptions) {
            throw new Error("viewOptionsForCreating is abstract");
        };

        EntityBase.prototype.viewOptionsForViewing = function (_viewOptions, itemPrefix) {
            throw new Error("viewOptionsForViewing is abstract");
        };

        EntityBase.prototype.onFindingOk = function (selectedItems, _viewOptions) {
            throw new Error("onFindingOk is abstract");
        };

        EntityBase.prototype.getEmbeddedTemplate = function (viewOptions) {
            return window[SF.compose(this.options.prefix, "sfTemplate")];
        };

        EntityBase.prototype.find = function (_findOptions, _viewOptions) {
            var _self = this;
            var type = this.getEntityType(function (type) {
                _self.typedFind($.extend({ webQueryName: type }, _findOptions));
            });
        };

        EntityBase.prototype.typedFind = function (_findOptions, _viewOptions) {
            if (SF.isEmpty(_findOptions.webQueryName)) {
                throw "FindOptions webQueryName parameter must not be null in EBaseline typedFind. Call find instead";
            }
            var findOptions = this.createFindOptions(_findOptions);
            SF.FindNavigator.openFinder(findOptions);
        };

        EntityBase.prototype.createFindOptions = function (findOptions, _viewOptions) {
            throw new Error("removeSpecific is abstract");
        };

        EntityBase.prototype.extraJsonParams = function (itemPrefix) {
            var extraParams = {};

            var staticInfo = this.staticInfo();

            //If Embedded Entity => send propertyInfo
            if (staticInfo.isEmbedded()) {
                extraParams.rootType = staticInfo.rootType();
                extraParams.propertyRoute = staticInfo.propertyRoute();
            }

            if (staticInfo.isReadOnly()) {
                extraParams.readOnly = true;
            }

            return extraParams;
        };

        EntityBase.prototype.updateButtonsDisplay = function (hasEntity) {
            var btnCreate = $(this.pf("btnCreate"));
            var btnRemove = $(this.pf("btnRemove"));
            var btnFind = $(this.pf("btnFind"));
            var btnView = $(this.pf("btnView"));
            var link = $(this.pf(SF.Keys.link));
            var txt = $(this.pf(SF.Keys.toStr));

            if (hasEntity == true) {
                if (link.html() == "")
                    link.html("&nbsp;");
                if (link.length > 0) {
                    txt.hide();
                    link.show();
                } else
                    txt.show();
                btnCreate.hide();
                btnFind.hide();
                btnRemove.show();
                btnView.show();
            } else {
                if (link.length > 0) {
                    link.hide();
                    txt.show();
                } else
                    txt.hide();
                btnRemove.hide();
                btnView.hide();
                btnCreate.show();
                btnFind.show();
            }
        };

        EntityBase.prototype.entityAutocomplete = function ($elem, options) {
            var lastXhr;
            var self = this;
            var auto = $elem.autocomplete({
                delay: options.delay || 200,
                source: function (request, response) {
                    if (lastXhr)
                        lastXhr.abort();
                    lastXhr = $.ajax({
                        url: options.url,
                        data: { types: options.types, l: options.count || 5, q: request.term },
                        success: function (data) {
                            lastXhr = null;
                            response($.map(data, function (item) {
                                return {
                                    label: item.text,
                                    value: item
                                };
                            }));
                        }
                    });
                },
                focus: function (event, ui) {
                    $elem.val(ui.item.value.text);
                    return false;
                },
                select: function (event, ui) {
                    var controlId = $elem.attr("id");
                    var prefix = controlId.substr(0, controlId.indexOf(SF.Keys.toStr) - 1);
                    self.onAutocompleteSelected(controlId, ui.item.value);
                    event.preventDefault();
                }
            });

            auto.data("uiAutocomplete")._renderItem = function (ul, item) {
                return $("<li>").attr("data-type", item.value.type).attr("data-id", item.value.id).append($("<a>").text(item.label)).appendTo(ul);
            };
        };

        EntityBase.prototype.onAutocompleteSelected = function (controlId, data) {
            throw new Error("onAutocompleteSelected is abstract");
        };
        EntityBase.key_entity = "sfEntity";
        return EntityBase;
    })();
    SF.EntityBase = EntityBase;

    once("SF-entityLine", function () {
        return $.fn.entityLine = function (opt) {
            new EntityLine(this, opt);
        };
    });

    var EntityLine = (function (_super) {
        __extends(EntityLine, _super);
        function EntityLine() {
            _super.apply(this, arguments);
        }
        EntityLine.prototype.updateLinks = function (newToStr, newLink, itemPrefix) {
            var link = $(this.pf(SF.Keys.link));
            link.text(newToStr);
            if (link.filter('a').length !== 0)
                link.attr('href', newLink);
            $(this.pf(SF.Keys.toStr)).val('');
        };

        EntityLine.prototype.view = function (_viewOptions) {
            var viewOptions = this.viewOptionsForViewing(_viewOptions);
            new SF.ViewNavigator(viewOptions).viewOk();
        };

        EntityLine.prototype.viewOptionsForViewing = function (_viewOptions, itemPrefix) {
            var self = this;
            var info = this.runtimeInfo();
            return $.extend({
                containerDiv: SF.compose(this.options.prefix, EntityBase.key_entity),
                onOk: function () {
                    return self.onViewingOk(_viewOptions.validationOptions);
                },
                onOkClosed: function () {
                    self.fireOnEntityChanged(true);
                },
                type: info.entityType(),
                id: info.id(),
                prefix: this.options.prefix,
                partialViewName: this.options.partialViewName,
                requestExtraJsonData: this.extraJsonParams()
            }, _viewOptions);
        };

        EntityLine.prototype.onViewingOk = function (validatorOptions) {
            var valOptions = $.extend(validatorOptions || {}, {
                type: this.runtimeInfo().entityType()
            });
            return this.checkValidation(valOptions).acceptChanges;
        };

        EntityLine.prototype.viewOptionsForCreating = function (_viewOptions) {
            var self = this;
            return $.extend({
                onOk: function (clonedElements) {
                    return self.onCreatingOk(clonedElements, _viewOptions.validationOptions, _viewOptions.type);
                },
                onOkClosed: function () {
                    self.fireOnEntityChanged(true);
                },
                prefix: this.options.prefix,
                partialViewName: this.options.partialViewName,
                requestExtraJsonData: this.extraJsonParams()
            }, _viewOptions);
        };

        EntityLine.prototype.newEntity = function (clonedElements, item) {
            var info = this.runtimeInfo();
            if (typeof item.runtimeInfo != "undefined") {
                info.find().val(item.runtimeInfo);
            } else {
                info.setEntity(item.type, item.id || '');
            }

            if ($(this.pf(EntityBase.key_entity)).length == 0) {
                info.find().after(SF.hiddenDiv(SF.compose(this.options.prefix, EntityBase.key_entity), ""));
            }
            $(this.pf(EntityBase.key_entity)).append(clonedElements);

            this.updateLinks(item.toStr, item.link);
        };

        EntityLine.prototype.onCreatingOk = function (clonedElements, validatorOptions, entityType, itemPrefix) {
            var valOptions = $.extend(validatorOptions || {}, {
                type: entityType
            });
            var validatorResult = this.checkValidation(valOptions);
            if (validatorResult.acceptChanges) {
                var runtimeInfo;
                var $mainControl = $(".sf-main-control[data-prefix=" + this.options.prefix + "]");
                if ($mainControl.length > 0) {
                    runtimeInfo = $mainControl.data("runtimeinfo");
                }
                this.newEntity(clonedElements, {
                    runtimeInfo: runtimeInfo,
                    type: entityType,
                    toStr: validatorResult.newToStr,
                    link: validatorResult.newLink
                });
            }
            return validatorResult.acceptChanges;
        };

        EntityLine.prototype.createFindOptions = function (_findOptions, _viewOptions) {
            var self = this;
            return $.extend({
                prefix: this.options.prefix,
                onOk: function (selectedItems) {
                    return self.onFindingOk(selectedItems);
                },
                onOkClosed: function () {
                    self.fireOnEntityChanged(true);
                }
            }, _findOptions);
        };

        EntityLine.prototype.onFindingOk = function (selectedItems, _viewOptions) {
            if (selectedItems == null || selectedItems.length != 1) {
                window.alert(lang.signum.onlyOneElement);
                return false;
            }
            this.newEntity(null, selectedItems[0]);
            return true;
        };

        EntityLine.prototype.onAutocompleteSelected = function (controlId, data) {
            var selectedItems = [{
                    id: data.id,
                    type: data.type,
                    toStr: data.text,
                    link: data.link
                }];
            this.onFindingOk(selectedItems);
            this.fireOnEntityChanged(true);
        };

        EntityLine.prototype.removeSpecific = function () {
            $(this.pf(EntityLine.key_entity)).remove();
        };
        return EntityLine;
    })(EntityBase);
    SF.EntityLine = EntityLine;

    once("SF-entityCombo", function () {
        return $.fn.entityCombo = function (opt) {
            var sc = new EntityCombo(this, opt);
        };
    });

    var EntityCombo = (function (_super) {
        __extends(EntityCombo, _super);
        function EntityCombo() {
            _super.apply(this, arguments);
        }
        EntityCombo.prototype.updateLinks = function (newToStr, newLink, itemPrefix) {
            $("#" + this.options.prefix + " option:selected").html(newToStr);
        };

        EntityCombo.prototype.selectedValue = function () {
            var selected = $(this.pf(EntityCombo.key_combo) + " > option:selected");
            if (selected.length === 0) {
                return null;
            }
            var fullValue = selected.val();
            var separator = fullValue.indexOf(";");
            var value = {};
            if (separator === -1) {
                value.entityType = SF.isEmpty(fullValue) ? "" : this.staticInfo().singleType();
                value.id = fullValue;
            } else {
                value.entityType = fullValue.substring(0, separator);
                value.id = fullValue.substring(separator + 1, fullValue.length);
            }
            return value;
        };

        EntityCombo.prototype.setSelected = function () {
            var newValue = this.selectedValue(), newEntityType = "", newId = "", newEntity = newValue !== null && !SF.isEmpty(newValue.id);

            if (newEntity) {
                newEntityType = newValue.entityType;
                newId = newValue.id;
            }
            var runtimeInfo = this.runtimeInfo();
            runtimeInfo.setEntity(newEntityType, newId);
            $(this.pf(EntityBase.key_entity)).html(''); //Clean
            this.fireOnEntityChanged(newEntity);
        };

        EntityCombo.prototype.view = function (_viewOptions) {
            var viewOptions = this.viewOptionsForViewing(_viewOptions);
            if (viewOptions.navigate) {
                var runtimeInfo = this.runtimeInfo();
                if (!SF.isEmpty(runtimeInfo.id())) {
                    window.open(viewOptions.controllerUrl.substring(0, viewOptions.controllerUrl.lastIndexOf("/") + 1) + runtimeInfo.entityType() + "/" + runtimeInfo.id(), "_blank");
                }
            } else {
                new SF.ViewNavigator(viewOptions).viewOk();
            }
        };
        EntityCombo.key_entity = "sfEntity";
        EntityCombo.key_combo = "sfCombo";
        return EntityCombo;
    })(EntityBase);
    SF.EntityCombo = EntityCombo;

    once("SF-entityLineDetail", function () {
        return $.fn.entityLineDetail = function (opt) {
            new EntityLineDetail(this, opt);
        };
    });

    var EntityLineDetail = (function (_super) {
        __extends(EntityLineDetail, _super);
        function EntityLineDetail(element, options) {
            _super.call(this, element, options);
        }
        EntityLineDetail.prototype.typedCreate = function (_viewOptions) {
            if (SF.isEmpty(_viewOptions.type)) {
                throw "ViewOptions type parameter must not be null in entityLineDetail typedCreate. Call create instead";
            }
            var viewOptions = this.viewOptionsForCreating(_viewOptions);
            var template = this.getEmbeddedTemplate();
            if (!SF.isEmpty(template)) {
                $('#' + viewOptions.containerDiv).html(template);
                SF.triggerNewContent($('#' + viewOptions.containerDiv));
            } else {
                new SF.ViewNavigator(viewOptions).viewEmbedded();
                SF.triggerNewContent($("#" + this.options.detailDiv));
            }
            this.onCreated(viewOptions.type);
        };

        EntityLineDetail.prototype.viewOptionsForCreating = function (_viewOptions) {
            return $.extend({
                containerDiv: this.options.detailDiv,
                prefix: this.options.prefix,
                partialViewName: this.options.partialViewName,
                requestExtraJsonData: this.extraJsonParams()
            }, _viewOptions);
        };

        EntityLineDetail.prototype.newEntity = function (entityType) {
            this.runtimeInfo().setEntity(entityType, '');
        };

        EntityLineDetail.prototype.onCreated = function (entityType) {
            this.newEntity(entityType);
            this.fireOnEntityChanged(true);
        };

        EntityLineDetail.prototype.find = function (_findOptions, _viewOptions) {
            var _self = this;
            var type = this.getEntityType(function (type) {
                _self.typedFind($.extend({ webQueryName: type }, _findOptions), _viewOptions);
            });
        };

        EntityLineDetail.prototype.typedFind = function (_findOptions, _viewOptions) {
            if (SF.isEmpty(_findOptions.webQueryName)) {
                throw "FindOptions webQueryName parameter must not be null in entityLineDetail typedFind. Call find instead";
            }
            var findOptions = this.createFindOptions(_findOptions, _viewOptions);
            SF.FindNavigator.openFinder(findOptions);
        };

        EntityLineDetail.prototype.createFindOptions = function (_findOptions, _viewOptions) {
            var self = this;
            return $.extend({
                prefix: this.options.prefix,
                onOk: function (selectedItems) {
                    return self.onFindingOk(selectedItems, _viewOptions);
                },
                onOkClosed: function () {
                    self.fireOnEntityChanged(true);
                }
            }, _findOptions);
        };

        EntityLineDetail.prototype.onFindingOk = function (selectedItems, _viewOptions) {
            if (selectedItems == null || selectedItems.length != 1) {
                window.alert(lang.signum.onlyOneElement);
                return false;
            }

            this.runtimeInfo().setEntity(selectedItems[0].type, selectedItems[0].id);

            //View result in the detailDiv
            var viewOptions = this.viewOptionsForCreating($.extend(_viewOptions, { type: selectedItems[0].type, id: selectedItems[0].id }));
            new SF.ViewNavigator(viewOptions).viewEmbedded();
            SF.triggerNewContent($("#" + this.options.detailDiv));

            return true;
        };

        EntityLineDetail.prototype.removeSpecific = function () {
            $("#" + this.options.detailDiv).html("");
        };
        return EntityLineDetail;
    })(EntityBase);
    SF.EntityLineDetail = EntityLineDetail;

    once("SF-entityList", function () {
        return $.fn.entityList = function (opt) {
            new EntityList(this, opt);
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

        EntityList.prototype.extraJsonParams = function (itemPrefix) {
            var extraParams = {};

            var staticInfo = this.staticInfo();

            //If Embedded Entity => send propertyRoute
            if (staticInfo.isEmbedded()) {
                extraParams.rootType = staticInfo.rootType();
                extraParams.propertyRoute = staticInfo.propertyRoute();
            }

            if (staticInfo.isReadOnly()) {
                extraParams.readOnly = true;
            }

            return extraParams;
        };

        EntityList.prototype.itemRuntimeInfo = function (itemPrefix) {
            return new SF.RuntimeInfo(itemPrefix);
        };

        EntityList.prototype.selectedItemPrefix = function () {
            var $items = this.getItems();
            if ($items.length == 0) {
                return null;
            }
            var selected = $items.filter(":selected");
            if (selected.length == 0) {
                return null;
            }
            var nameSelected = selected[0].id;
            return nameSelected.substr(0, nameSelected.indexOf(SF.Keys.toStr) - 1);
        };

        EntityList.prototype.getItems = function () {
            return $(this.pf(EntityList.key_list) + " > option");
        };

        EntityList.prototype.getLastPrefixIndex = function () {
            var lastPrefixIndex = -1;
            var $items = this.getItems();
            var self = this;
            $items.each(function () {
                var currId = this.id;
                var currPrefixIndex = parseInt(currId.substring(self.options.prefix.length + 1, currId.indexOf(self.itemSuffix()) - 1), 10);
                if (currPrefixIndex > lastPrefixIndex) {
                    lastPrefixIndex = currPrefixIndex;
                }
            });
            return lastPrefixIndex;
        };

        EntityList.prototype.getNewIndex = function (itemPrefix) {
            return parseInt($("#" + SF.compose(itemPrefix, EntityList.key_indexes)).val().split(";")[1]);
        };

        EntityList.prototype.setNewIndex = function (itemPrefix, newIndex) {
            var $indexes = $("#" + SF.compose(itemPrefix, EntityList.key_indexes));
            var indexes = $indexes.val().split(";");
            $indexes.val(indexes[0].toString() + ";" + newIndex.toString());
        };

        EntityList.prototype.getLastNewIndex = function () {
            var $last = this.getItems().filter(":last");
            if ($last.length == 0) {
                return -1;
            }

            var lastId = $last[0].id;
            var lastPrefix = lastId.substring(0, lastId.indexOf(this.itemSuffix()) - 1);

            return this.getNewIndex(lastPrefix);
        };

        EntityList.prototype.checkValidation = function (validatorOptions, itemPrefix) {
            if (typeof validatorOptions == "undefined" || typeof validatorOptions.type == "undefined") {
                throw "validatorOptions.type must be supplied to checkValidation";
            }

            var info = this.itemRuntimeInfo(itemPrefix);
            $.extend(validatorOptions, {
                prefix: itemPrefix,
                id: (info.find().length > 0) ? info.id() : ''
            });

            var validator = new SF.PartialValidator(validatorOptions);
            var validatorResult = validator.validate();
            if (!validatorResult.isValid) {
                if (!confirm(lang.signum.popupErrors)) {
                    $.extend(validatorResult, { acceptChanges: false });
                    return validatorResult;
                } else
                    validator.showErrors(validatorResult.modelState, true);
            }
            this.updateLinks(validatorResult.newToStr, validatorResult.newLink, itemPrefix);
            $.extend(validatorResult, { acceptChanges: true });
            return validatorResult;
        };

        EntityList.prototype.getEmbeddedTemplate = function () {
            var template = window[SF.compose(this.options.prefix, "sfTemplate")];
            if (!SF.isEmpty(template)) {
                var newPrefixIndex = this.getLastPrefixIndex() + 1;
                var itemPrefix = SF.compose(this.options.prefix, newPrefixIndex.toString());
                template = template.replace(new RegExp(SF.compose(this.options.prefix, "0"), "gi"), itemPrefix);
            }
            return template;
        };

        EntityList.prototype.viewOptionsForCreating = function (_viewOptions) {
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

        EntityList.prototype.onCreatingOk = function (clonedElements, validatorOptions, entityType, itemPrefix) {
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
                this.newListItem(clonedElements, itemPrefix, { runtimeInfo: runtimeInfo, type: entityType, toStr: validatorResult.newToStr });
            }
            return validatorResult.acceptChanges;
        };

        EntityList.prototype.newListItem = function (clonedElements, itemPrefix, item) {
            var $table = $("#" + this.options.prefix + "> .sf-field-list > .sf-field-list-table");

            $table.before(SF.hiddenInput(SF.compose(itemPrefix, EntityList.key_indexes), ";" + (this.getLastNewIndex() + 1).toString()));

            var itemInfoValue = item.runtimeInfo || this.itemRuntimeInfo(itemPrefix).createValue(item.type, item.id || '', typeof item.id == "undefined" ? 'n' : 'o', null);
            $table.before(SF.hiddenInput(SF.compose(itemPrefix, SF.Keys.runtimeInfo), itemInfoValue));

            $table.before(SF.hiddenDiv(SF.compose(itemPrefix, EntityList.key_entity), ""));

            $('#' + SF.compose(itemPrefix, EntityList.key_entity)).append(clonedElements);

            var select = $(this.pf(EntityList.key_list));
            if (SF.isEmpty(item.toStr)) {
                item.toStr = "&nbsp;";
            }
            select.append("\n<option id='" + SF.compose(itemPrefix, SF.Keys.toStr) + "' name='" + SF.compose(itemPrefix, SF.Keys.toStr) + "' value='' class='sf-value-line'>" + item.toStr + "</option>");
            select.children('option').attr('selected', false); //Fix for Firefox: Set selected after retrieving the html of the select
            select.children('option:last').attr('selected', true);
            this.fireOnEntityChanged(false);
        };

        EntityList.prototype.view = function (_viewOptions) {
            var selectedItemPrefix = this.selectedItemPrefix();
            if (SF.isEmpty(selectedItemPrefix)) {
                return;
            }
            this.viewInIndex(_viewOptions, selectedItemPrefix);
        };

        EntityList.prototype.viewInIndex = function (_viewOptions, selectedItemPrefix) {
            var viewOptions = this.viewOptionsForViewing(_viewOptions, selectedItemPrefix);
            if (viewOptions.navigate) {
                var itemInfo = this.itemRuntimeInfo(selectedItemPrefix);
                if (!SF.isEmpty(itemInfo.id())) {
                    window.open(_viewOptions.controllerUrl.substring(0, _viewOptions.controllerUrl.lastIndexOf("/") + 1) + itemInfo.entityType() + "/" + itemInfo.id(), "_blank");
                }
                return;
            }
            new SF.ViewNavigator(viewOptions).viewOk();
        };

        EntityList.prototype.viewOptionsForViewing = function (_viewOptions, itemPrefix) {
            var self = this;
            var info = this.itemRuntimeInfo(itemPrefix);
            return $.extend({
                containerDiv: SF.compose(itemPrefix, EntityList.key_entity),
                onOk: function () {
                    return self.onViewingOk(_viewOptions.validationOptions, itemPrefix);
                },
                onOkClosed: function () {
                    self.fireOnEntityChanged(false);
                },
                onCancelled: null,
                controllerUrl: null,
                type: info.entityType(),
                id: info.id(),
                prefix: itemPrefix,
                partialViewName: this.options.partialViewName,
                requestExtraJsonData: this.extraJsonParams(itemPrefix)
            }, _viewOptions);
        };

        EntityList.prototype.onViewingOk = function (validatorOptions, itemPrefix) {
            var valOptions = $.extend(validatorOptions || {}, {
                type: this.itemRuntimeInfo(itemPrefix).entityType()
            });
            var validatorResult = this.checkValidation(valOptions, itemPrefix);
            return validatorResult.acceptChanges;
        };

        EntityList.prototype.createFindOptions = function (_findOptions, _viewOptions) {
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

        EntityList.prototype.onFindingOk = function (selectedItems, _viewOptions) {
            if (selectedItems == null || selectedItems.length == 0) {
                throw "No item was returned from Find Window";
            }
            var self = this;
            this.foreachNewItem(selectedItems, function (item, itemPrefix) {
                self.newListItem(null, itemPrefix, item);
            });
            return true;
        };

        EntityList.prototype.foreachNewItem = function (selectedItems, itemAction) {
            var lastPrefixIndex = this.getLastPrefixIndex();
            for (var i = 0, l = selectedItems.length; i < l; i++) {
                var item = selectedItems[i];
                lastPrefixIndex++;
                var itemPrefix = SF.compose(this.options.prefix, lastPrefixIndex.toString());
                itemAction(item, itemPrefix);
            }
        };

        EntityList.prototype.remove = function (itemPrefix) {
            var selectedItemPrefix = this.selectedItemPrefix();
            if (SF.isEmpty(selectedItemPrefix)) {
                return;
            }
            this.removeInIndex(selectedItemPrefix);
        };

        EntityList.prototype.removeInIndex = function (selectedItemPrefix) {
            $.each([SF.Keys.runtimeInfo, SF.Keys.toStr, EntityList.key_entity, EntityList.key_indexes], function (i, key) {
                $("#" + SF.compose(selectedItemPrefix, key)).remove();
            });
            this.fireOnEntityChanged(false);
        };

        EntityList.prototype.updateButtonsDisplay = function () {
            var hasElements = this.getItems().length > 0;
            $(this.pf("btnRemove")).toggle(hasElements);
            $(this.pf("btnView")).toggle(hasElements);
            $(this.pf("btnUp")).toggle(hasElements);
            $(this.pf("btnDown")).toggle(hasElements);
        };

        EntityList.prototype.moveUp = function (selectedItemPrefix) {
            if (typeof selectedItemPrefix == "undefined") {
                selectedItemPrefix = this.selectedItemPrefix();
            }

            var suffix = this.itemSuffix();
            var $item = $("#" + SF.compose(selectedItemPrefix, suffix));
            var $itemPrev = $item.prev();

            if ($itemPrev.length == 0) {
                return;
            }

            var itemPrevId = $itemPrev[0].id;
            var itemPrevPrefix = itemPrevId.substring(0, itemPrevId.indexOf(suffix) - 1);

            var prevNewIndex = this.getNewIndex(itemPrevPrefix);
            this.setNewIndex(selectedItemPrefix, prevNewIndex);
            this.setNewIndex(itemPrevPrefix, prevNewIndex + 1);

            $item.insertBefore($itemPrev);
        };

        EntityList.prototype.moveDown = function (selectedItemPrefix) {
            if (typeof selectedItemPrefix == "undefined") {
                selectedItemPrefix = this.selectedItemPrefix();
            }

            var suffix = this.itemSuffix();
            var $item = $("#" + SF.compose(selectedItemPrefix, suffix));
            var $itemNext = $item.next();

            if ($itemNext.length == 0) {
                return;
            }

            var itemNextId = $itemNext[0].id;
            var itemNextPrefix = itemNextId.substring(0, itemNextId.indexOf(suffix) - 1);

            var nextNewIndex = this.getNewIndex(itemNextPrefix);
            this.setNewIndex(selectedItemPrefix, nextNewIndex);
            this.setNewIndex(itemNextPrefix, nextNewIndex - 1);

            $item.insertAfter($itemNext);
        };
        EntityList.key_indexes = "sfIndexes";
        EntityList.key_list = "sfList";
        return EntityList;
    })(EntityBase);
    SF.EntityList = EntityList;

    once("SF-entityListDetail", function () {
        return $.fn.entityListDetail = function (opt) {
            new EntityListDetail(this, opt);
        };
    });

    var EntityListDetail = (function (_super) {
        __extends(EntityListDetail, _super);
        function EntityListDetail(element, options) {
            _super.call(this, element, options);
        }
        EntityListDetail.prototype.typedCreate = function (_viewOptions) {
            if (SF.isEmpty(_viewOptions.type)) {
                throw "ViewOptions type parameter must not be null in entityListDetail typedCreate. Call create instead";
            }
            this.restoreCurrent();
            var viewOptions = this.viewOptionsForCreating(_viewOptions);
            var template = this.getEmbeddedTemplate();
            if (!SF.isEmpty(template)) {
                $('#' + viewOptions.containerDiv).html(template);
                SF.triggerNewContent($('#' + viewOptions.containerDiv));
            } else {
                new SF.ViewNavigator(viewOptions).viewEmbedded();
                SF.triggerNewContent($('#' + viewOptions.containerDiv));
            }
            this.onItemCreated(viewOptions);
        };

        EntityListDetail.prototype.viewOptionsForCreating = function (_viewOptions) {
            var newPrefixIndex = this.getLastPrefixIndex() + 1;
            var itemPrefix = SF.compose(this.options.prefix, newPrefixIndex.toString());
            return $.extend({
                containerDiv: this.options.detailDiv,
                prefix: itemPrefix,
                partialViewName: this.options.partialViewName,
                requestExtraJsonData: this.extraJsonParams(itemPrefix)
            }, _viewOptions);
        };

        EntityListDetail.prototype.getVisibleItemPrefix = function () {
            var detail = $('#' + this.options.detailDiv);
            var firstId = detail.find(":input[id^=" + this.options.prefix + "]:first");
            if (firstId.length === 0) {
                return null;
            }
            var id = firstId[0].id;
            var nextSeparator = id.indexOf("_", this.options.prefix.length + 1);
            return id.substring(0, nextSeparator);
        };

        EntityListDetail.prototype.restoreCurrent = function () {
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
            new EntityRepeater(this, opt);
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
            new EntityStrip(this, opt);
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

        EntityStrip.prototype.canAddItems = function () {
            if (!SF.isEmpty(this.options.maxElements)) {
                if (this.getItems().length >= +this.options.maxElements) {
                    return false;
                }
            }
            return true;
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
            var $li = $("<li id='" + SF.compose(itemPrefix, EntityStrip.key_stripItem) + "' name='" + SF.compose(itemPrefix, EntityStrip.key_stripItem) + "' class='" + EntityStrip.key_stripItemClass + "'>" + SF.hiddenInput(SF.compose(itemPrefix, EntityStrip.key_indexes), ";" + (this.getLastNewIndex() + 1).toString()) + SF.hiddenInput(SF.compose(itemPrefix, SF.Keys.runtimeInfo), itemInfoValue) + (this.options.navigate ? ("<a class='sf-value-line' id='" + SF.compose(itemPrefix, EntityStrip.key_link) + "' href='" + item.link + "' title='" + lang.signum.navigate + "'>" + item.toStr + "</a>") : ("<span class='sf-value-line' id='" + SF.compose(itemPrefix, EntityStrip.key_link) + "'>" + item.toStr + "</span>")) + "<span class='sf-button-container'>" + ((this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnUp") + "' title='" + lang.signum.moveUp + "' onclick=\"" + this._getMovingUp(itemPrefix) + "\" class='sf-line-button sf-move-up' data-icon='ui-icon-triangle-1-" + (this.options.vertical ? "w" : "n") + "' data-text='false'>" + lang.signum.moveUp + "</span>") : "") + (this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnDown") + "' title='" + lang.signum.moveDown + "' onclick=\"" + this._getMovingDown(itemPrefix) + "\" class='sf-line-button sf-move-down' data-icon='ui-icon-triangle-1-" + (this.options.vertical ? "e" : "s") + "' data-text='false'>" + lang.signum.moveDown + "</span>") : "") + (this.options.view ? ("<a id='" + SF.compose(itemPrefix, "btnView") + "' title='" + lang.signum.view + "' onclick=\"" + this._getView(itemPrefix) + "\" class='sf-line-button sf-view' data-icon='ui-icon-circle-arrow-e' data-text='false'>" + lang.signum.view + "</a>") : "") + (this.options.remove ? ("<a id='" + SF.compose(itemPrefix, "btnRemove") + "' title='" + lang.signum.remove + "' onclick=\"" + this._getRemoving(itemPrefix) + "\" class='sf-line-button sf-remove' data-icon='ui-icon-circle-close' data-text='false'>" + lang.signum.remove + "</a>") : "")) + "</span>" + (!SF.isEmpty(newHtml) ? "<div id='" + SF.compose(itemPrefix, EntityStrip.key_entity) + "' name='" + SF.compose(itemPrefix, EntityStrip.key_entity) + "' style='display:none'></div>" : "") + "</li>");

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

    function getInfoParams(prefix) {
        return $("#" + SF.compose(prefix, SF.Keys.runtimeInfo) + ", #" + SF.compose(prefix, EntityList.key_indexes));
    }
    SF.getInfoParams = getInfoParams;
    ;
})(SF || (SF = {}));
//# sourceMappingURL=SF_Lines.js.map

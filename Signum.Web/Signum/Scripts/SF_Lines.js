"use strict";

SF.registerModule("Lines", function () {

    (function ($) {
        $.widget("SF.baseLine", {

            options: {
                prefix: "",
                onEntityChanged: null
            },

            keys: {
                entity: "sfEntity"
            },

            runtimeInfo: function () {
                return new SF.RuntimeInfo(this.options.prefix);
            },

            staticInfo: function () {
                return SF.StaticInfo(this.options.prefix);
            },

            pf: function (s) {
                return "#" + SF.compose(this.options.prefix, s);
            },

            checkValidation: function (validatorOptions) {
                SF.log("EBaseLine checkValidation");
                if (typeof validatorOptions == "undefined" || typeof validatorOptions.type == "undefined") {
                    throw "validatorOptions.type must be supplied to checkValidation";
                }

                var info = this.runtimeInfo();
                $.extend(validatorOptions, {
                    prefix: this.options.prefix,
                    id: (info.find().length !== 0) ? info.id() : ''
                });
                var validator = new SF.PartialValidator(validatorOptions);
                var result = validator.validate();
                if (!result.isValid) {
                    if (!confirm(lang.signum.popupErrors)) return false;
                    validator.showErrors(result.modelState, true);
                }
                this.updateLinks(result.newToStr, result.newLink);
                return true;
            },

            updateLinks: function (newToStr, newLink) {
                SF.log("EBaseLine updateLinks");
                //Abstract function
            },

            fireOnEntityChanged: function (hasEntity) {
                SF.log("EBaseLine fireOnEntityChanged");
                this.updateButtonsDisplay(hasEntity);
                if (!SF.isEmpty(this.options.onEntityChanged))
                    this.options.onEntityChanged();
            },

            remove: function () {
                SF.log("EBaseLine remove");
                $(this.pf(SF.Keys.toStr)).val("").removeClass(SF.Validator.inputErrorClass);
                $(this.pf(SF.Keys.link)).val("").html("").removeClass(SF.Validator.inputErrorClass);
                this.runtimeInfo().removeEntity();

                this.removeSpecific();
                this.fireOnEntityChanged(false);
            },

            getRuntimeType: function (_onTypeFound) {
                SF.log("EBaseLine getRuntimeType");
                var types = this.staticInfo().types().split(",");
                if (types.length == 1)
                    return _onTypeFound(types[0]);

                SF.openTypeChooser(this.options.prefix, _onTypeFound);
            },

            create: function (_viewOptions) {
                SF.log("EBaseLine create");
                var _self = this;
                var type = this.getRuntimeType(function (type) {
                    _self.typedCreate($.extend({ type: type }, _viewOptions));
                });
            },

            typedCreate: function (_viewOptions) {
                SF.log("EBaseline typedCreate");
                if (SF.isEmpty(_viewOptions.type)) {
                    throw "ViewOptions type parameter must not be null in EBaseline typedCreate. Call create instead";
                }
                if (_viewOptions.navigate) {
                    window.open(_viewOptions.controllerUrl.substring(0, _viewOptions.controllerUrl.lastIndexOf("/") + 1) + _viewOptions.type, "_blank");
                    return;
                }
                var viewOptions = this.viewOptionsForCreating(_viewOptions);
                var template = window[SF.compose(this.options.prefix, "sfTemplate")];
                if (!SF.isEmpty(template)) { //Template pre-loaded: In case of a list, it will be created with "_0" itemprefix => replace it with the current one
                    template = template.replace(new RegExp(SF.compose(this.options.prefix, "0"), "gi"), viewOptions.prefix);
                    new SF.ViewNavigator(viewOptions).showCreateOk(template);
                }
                else {
                    new SF.ViewNavigator(viewOptions).createOk();
                }
            },

            find: function (_findOptions) {
                SF.log("EBaseLine find");
                var _self = this;
                var type = this.getRuntimeType(function (type) {
                    _self.typedFind($.extend({ webQueryName: type }, _findOptions));
                });
            },

            typedFind: function (_findOptions) {
                SF.log("EBaseline typedFind");
                if (SF.isEmpty(_findOptions.webQueryName)) {
                    throw "FindOptions webQueryName parameter must not be null in EBaseline typedFind. Call find instead";
                }
                var findOptions = this.createFindOptions(_findOptions);
                new SF.FindNavigator(findOptions).openFinder();
            },

            extraJsonParams: function (_prefix) {
                SF.log("EBaseLine extraJsonParams");
                var extraParams = {};

                var staticInfo = this.staticInfo();

                //If Embedded Entity => send path of runtimes and ids to be able to construct a typecontext
                if (staticInfo.isEmbedded()) {
                    var pathInfo = SF.fullPathNodesSelector(this.options.prefix);
                    for (var i = 0, l = pathInfo.length; i < l; i++) {
                        var node = pathInfo[i];
                        extraParams[node.id] = node.value;
                    }
                }

                if (staticInfo.isReadOnly()) {
                    extraParams.readOnly = true;
                }

                return extraParams;
            },

            updateButtonsDisplay: function (hasEntity) {
                SF.log("EBaseLine updateButtonsDisplay");
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
                    }
                    else
                        txt.show();
                    btnCreate.hide(); btnFind.hide();
                    btnRemove.show(); btnView.show();
                }
                else {
                    if (link.length > 0) {
                        link.hide();
                        txt.show();
                    }
                    else
                        txt.hide();
                    btnRemove.hide(); btnView.hide();
                    btnCreate.show(); btnFind.show();
                }
            }
        });

        $.widget("SF.entityLine", $.SF.baseLine, {

            options: {},

            updateLinks: function (newToStr, newLink) {
                SF.log("ELine updateLinks");
                var link = $(this.pf(SF.Keys.link));
                link.html(newToStr);
                if (link.filter('a').length !== 0)
                    link.attr('href', newLink);
                $(this.pf(SF.Keys.toStr)).val('');
            },

            view: function (_viewOptions) {
                SF.log("ELine view");
                var viewOptions = this.viewOptionsForViewing(_viewOptions);
                new SF.ViewNavigator(viewOptions).viewOk();
            },

            viewOptionsForViewing: function (_viewOptions) {
                SF.log("ELine viewOptionsForViewing");
                var self = this;
                var info = this.runtimeInfo();
                return $.extend({
                    containerDiv: SF.compose(this.options.prefix, self.keys.entity),
                    onOk: function () { return self.onViewingOk(_viewOptions.validationOptions); },
                    onOkClosed: function () { self.fireOnEntityChanged(true); },
                    type: info.runtimeType(),
                    id: info.id(),
                    prefix: this.options.prefix,
                    requestExtraJsonData: this.extraJsonParams()
                }, _viewOptions);
            },

            onViewingOk: function (validatorOptions) {
                SF.log("ELine onViewingOk");
                var valOptions = $.extend(validatorOptions || {}, {
                    type: this.runtimeInfo().runtimeType()
                });
                var acceptChanges = this.checkValidation(valOptions);
                return acceptChanges;
            },

            viewOptionsForCreating: function (_viewOptions) {
                SF.log("ELine viewOptionsForCreating");
                var self = this;
                return $.extend({
                    onOk: function (clonedElements) { return self.onCreatingOk(clonedElements, _viewOptions.validationOptions, _viewOptions.type); },
                    onOkClosed: function () { self.fireOnEntityChanged(true); },
                    prefix: this.options.prefix,
                    requestExtraJsonData: this.extraJsonParams()
                }, _viewOptions);
            },

            newEntity: function (clonedElements, runtimeType) {
                var info = this.runtimeInfo();
                info.setEntity(runtimeType, '');
                info.find().after(SF.hiddenDiv(SF.compose(this.options.prefix, this.keys.entity), ""));
                $(this.pf(this.keys.entity)).append(clonedElements);
            },

            onCreatingOk: function (clonedElements, validatorOptions, runtimeType) {
                SF.log("ELine onCreatingOk");
                var valOptions = $.extend(validatorOptions || {}, {
                    type: runtimeType
                });
                var acceptChanges = this.checkValidation(valOptions);
                if (acceptChanges) {
                    this.newEntity(clonedElements, runtimeType);
                }
                return acceptChanges;
            },

            createFindOptions: function (_findOptions) {
                SF.log("ELine createFindOptions");
                var self = this;
                return $.extend({
                    prefix: this.options.prefix,
                    onOk: function (selectedItems) { return self.onFindingOk(selectedItems); },
                    onOkClosed: function () { self.fireOnEntityChanged(true); },
                    allowMultiple: false
                }, _findOptions);
            },

            onFindingOk: function (selectedItems) {
                SF.log("ELine onFindingOk");
                if (selectedItems == null || selectedItems.length != 1)
                    throw "No item or more than one item was returned from Find Window";
                var info = this.runtimeInfo();
                info.setEntity(selectedItems[0].type, selectedItems[0].id);
                if ($(this.pf(this.keys.entity)).length == 0)
                    info.find().after(SF.hiddenDiv(SF.compose(this.options.prefix, this.keys.entity), ""));
                $(this.pf(SF.Keys.toStr)).val(''); //Clean
                $(this.pf(SF.Keys.link)).html(selectedItems[0].toStr).attr('href', selectedItems[0].link);
                return true;
            },

            onAutocompleteSelected: function (controlId, data) {
                SF.log("ELine onAutocompleteSelected");
                var selectedItems = [{
                    id: data.id,
                    type: data.type,
                    toStr: $('#' + controlId).val(),
                    link: ""
                }];
                this.onFindingOk(selectedItems);
                this.fireOnEntityChanged(true);
            },

            entityAutocomplete: function ($elem, options) {
                var lastXhr; //To avoid previous requests results to be shown
                $elem.autocomplete({
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
                                    }
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
                        $("#" + prefix).data("entityLine").onAutocompleteSelected(controlId, ui.item.value);
                    }
                });
            },

            removeSpecific: function () {
                SF.log("ELine removeSpecific");
                $(this.pf(this.keys.entity)).remove();
            }
        });

        $.widget("SF.entityCombo", $.SF.entityLine, {

            options: {},

            updateLinks: function (newToStr, newLink) {
                SF.log("ECombo updateLinks");
                $("#" + this.options.prefix + " option:selected").html(newToStr);
            },

            selectedValue: function () {
                SF.log("ECombo selectedValue");
                var selected = $(this.pf("combo") + " > option:selected");
                if (selected.length === 0) {
                    return null;
                }
                var fullValue = selected.val();
                var separator = fullValue.indexOf(";");
                var value = [];
                if (separator === -1) {
                    value.runtimeType = this.staticInfo().singleType();
                    value.id = fullValue;
                }
                else {
                    value.runtimeType = fullValue.substring(0, separator);
                    value.id = fullValue.substring(separator + 1, fullValue.length);
                }
                return value;
            },

            setSelected: function () {
                SF.log("ECombo setSelected");

                var newValue = this.selectedValue(),
                    newRuntimeType = "",
                    newId = "",
                    newEntity = newValue !== null && !SF.isEmpty(newValue.id);

                if (newEntity) {
                    newRuntimeType = newValue.runtimeType;
                    newId = newValue.id;
                }
                var runtimeInfo = this.runtimeInfo();
                runtimeInfo.setEntity(newRuntimeType, newId);
                $(this.pf(this.keys.entity)).html(''); //Clean
                this.fireOnEntityChanged(newEntity);
            },

            view: function (_viewOptions) {
                SF.log("ELine view");
                var viewOptions = this.viewOptionsForViewing(_viewOptions);
                if (viewOptions.navigate) {
                    var runtimeInfo = this.runtimeInfo();
                    if (!SF.isEmpty(runtimeInfo.id())) {
                        window.open(viewOptions.controllerUrl.substring(0, viewOptions.controllerUrl.lastIndexOf("/") + 1) + runtimeInfo.runtimeType() + "/" + runtimeInfo.id(), "_blank");
                    }
                }
                else {
                    new SF.ViewNavigator(viewOptions).viewOk();
                }
            }
        });

    })(jQuery);

    SF.fullPathNodesSelector = function (prefix) {
        var pathPrefixes = SF.getPathPrefixes(prefix);
        var nodes = $("#" + SF.Keys.runtimeInfo);
        for (var i = 0, l = pathPrefixes.length; i < l; i++) {
            var current = pathPrefixes[i];
            if (!SF.isEmpty(current)) {
                nodes = nodes.add(SF.getInfoParams(current));
            }
        }
        return nodes;
    };

    SF.getInfoParams = function (prefix) {
        return $("#" + SF.compose(prefix, SF.Keys.runtimeInfo) + ", #" + SF.compose(prefix, SF.EList.index));
    };
});

    
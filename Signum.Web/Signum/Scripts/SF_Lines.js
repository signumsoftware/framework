"use strict";

SF.registerModule("Lines", function () {

    /**
    * @constructor
    */
    SF.EBaseLine = function (_eBaseOptions) {
        this.options = $.extend({
            prefix: "",
            onEntityChanged: null
        }, _eBaseOptions);
    };

    SF.EBaseLine.prototype = {
        entity: "sfEntity",

        runtimeInfo: function () {
            return new SF.RuntimeInfo(this.options.prefix);
        },

        staticInfo: function () {
            return SF.StaticInfo(this.options.prefix);
        },

        pf: function (s) {
            return "#" + SF.compose(this.options.prefix, s);
        },

        checkValidation: function (validateUrl, runtimeType) {
            SF.log("EBaseLine checkValidation"); //Receives url as parameter so it can be overriden when setting viewOptions onOk

            var info = this.runtimeInfo();
            var id = (info.find().length !== 0) ? info.id() : '';
            var validator = new SF.PartialValidator({ controllerUrl: validateUrl, prefix: this.options.prefix, id: id, type: runtimeType });
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

        getRuntimeType: function (typeChooserUrl, _onTypeFound) {
            SF.log("EBaseLine getRuntimeType");
            var types = this.staticInfo().types().split(",");
            if (types.length == 1)
                return _onTypeFound(types[0]);

            SF.openTypeChooser(this.options.prefix, _onTypeFound, { controllerUrl: typeChooserUrl });
        },

        create: function (_viewOptions, typeChooserUrl) {
            SF.log("EBaseLine create");
            var _self = this;
            var type = this.getRuntimeType(typeChooserUrl, function (type) {
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

        find: function (_findOptions, typeChooserUrl) {
            SF.log("EBaseLine find");
            var _self = this;
            var type = this.getRuntimeType(typeChooserUrl, function (type) {
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
    };

    /**
    * @constructor
    */
    SF.ELine = function (_elineOptions) {
        SF.EBaseLine.call(this, _elineOptions);

        this.updateLinks = function (newToStr, newLink) {
            SF.log("ELine updateLinks");
            var link = $(this.pf(SF.Keys.link));
            link.html(newToStr);
            if (link.filter('a').length !== 0)
                link.attr('href', newLink);
            $(this.pf(SF.Keys.toStr)).val(''); //Clean
        };

        this.view = function (_viewOptions) {
            SF.log("ELine view");
            var viewOptions = this.viewOptionsForViewing(_viewOptions);
            new SF.ViewNavigator(viewOptions).viewOk();
        };

        this.viewOptionsForViewing = function (_viewOptions) {
            SF.log("ELine viewOptionsForViewing");
            var self = this;
            var info = this.runtimeInfo();
            return $.extend({
                containerDiv: SF.compose(this.options.prefix, self.entity),
                onOk: function () { return self.onViewingOk(_viewOptions.validationOptions.controllerUrl); },
                onOkClosed: function () { self.fireOnEntityChanged(true); },
                onCancelled: null,
                controllerUrl: null,
                type: info.runtimeType(),
                id: info.id(),
                prefix: this.options.prefix,
                requestExtraJsonData: this.extraJsonParams()
            }, _viewOptions);
        };

        this.onViewingOk = function (validateUrl) {
            SF.log("ELine onViewingOk"); //Receives url as parameter so it can be overriden when setting viewOptions onOk
            var acceptChanges = this.checkValidation(validateUrl, this.runtimeInfo().runtimeType());
            return acceptChanges;
        };

        this.viewOptionsForCreating = function (_viewOptions) {
            SF.log("ELine viewOptionsForCreating");
            var self = this;
            return $.extend({
                containerDiv: "",
                onOk: function (clonedElements) { return self.onCreatingOk(clonedElements, _viewOptions.validationOptions.controllerUrl, _viewOptions.type); },
                onOkClosed: function () { self.fireOnEntityChanged(true); },
                onCancelled: null,
                controllerUrl: null,
                prefix: this.options.prefix,
                requestExtraJsonData: this.extraJsonParams()
            }, _viewOptions);
        };

        this.newEntity = function (clonedElements, runtimeType) {
            var info = this.runtimeInfo();
            info.setEntity(runtimeType, '');
            info.find().after(SF.hiddenDiv(SF.compose(this.options.prefix, this.entity), ""));
            $(this.pf(this.entity)).append(clonedElements);
        };

        this.onCreatingOk = function (clonedElements, validateUrl, runtimeType) {
            SF.log("ELine onCreatingOk"); //Receives url as parameter so it can be overriden when setting viewOptions onOk
            var acceptChanges = this.checkValidation(validateUrl, runtimeType);
            if (acceptChanges) {
                this.newEntity(clonedElements, runtimeType);
            }
            return acceptChanges;
        };

        this.createFindOptions = function (_findOptions) {
            SF.log("ELine createFindOptions");
            var self = this;
            return $.extend({
                prefix: this.options.prefix,
                onOk: function (selectedItems) { return self.onFindingOk(selectedItems); },
                onOkClosed: function () { self.fireOnEntityChanged(true); },
                allowMultiple: false
            }, _findOptions);
        };

        this.onFindingOk = function (selectedItems) {
            SF.log("ELine onFindingOk");
            if (selectedItems == null || selectedItems.length != 1)
                throw "No item or more than one item was returned from Find Window";
            var info = this.runtimeInfo();
            info.setEntity(selectedItems[0].type, selectedItems[0].id);
            if ($(this.pf(this.entity)).length == 0)
                info.find().after(SF.hiddenDiv(SF.compose(this.options.prefix, this.entity), ""));
            $(this.pf(SF.Keys.toStr)).val(''); //Clean
            $(this.pf(SF.Keys.link)).html(selectedItems[0].toStr).attr('href', selectedItems[0].link);
            return true;
        };

        this.onAutocompleteSelected = function (controlId, data) {
            SF.log("ELine onAutocompleteSelected");
            var selectedItems = [{
                id: data.id,
                type: data.type,
                toStr: $('#' + controlId).val(),
                link: ""
            }];
            this.onFindingOk(selectedItems);
            this.fireOnEntityChanged(true);
        };

        this.removeSpecific = function () {
            SF.log("ELine removeSpecific");
            $(this.pf(this.entity)).remove();
        };
    }

    /**
    * @constructor
    */
    SF.ELine.prototype = new SF.EBaseLine();

    //EDLineOptions = EBaseLineOptions + detailDiv
    SF.EDLine = function (_edlineOptions) {
        SF.log("EDLine");
        SF.EBaseLine.call(this, _edlineOptions);

        this.typedCreate = function (_viewOptions) {
            SF.log("EDLine create");
            if (SF.isEmpty(_viewOptions.type)) throw "ViewOptions type parameter must not be null in EDLine typedCreate. Call create instead";
            var viewOptions = this.viewOptionsForCreating(_viewOptions);
            var template = window[SF.compose(this.options.prefix, "sfTemplate")];
            if (!SF.isEmpty(template)) { //Template pre-loaded: EmbeddedEntity
                $('#' + viewOptions.containerDiv).html(template);
                SF.triggerNewContent($('#' + viewOptions.containerDiv));
            }
            else {
                new SF.ViewNavigator(viewOptions).viewEmbedded();
                SF.triggerNewContent($("#" + this.options.detailDiv));
            }
            this.onCreated(viewOptions.type);
        };

        this.viewOptionsForCreating = function (_viewOptions) {
            SF.log("EDLine viewOptionsForCreating");
            return $.extend({
                containerDiv: this.options.detailDiv,
                controllerUrl: null,
                prefix: this.options.prefix,
                requestExtraJsonData: this.extraJsonParams()
            }, _viewOptions);
        };

        this.newEntity = function (runtimeType) {
            this.runtimeInfo().setEntity(runtimeType, '');
        };

        this.onCreated = function (runtimeType) {
            SF.log("EDLine onCreated");
            this.newEntity(runtimeType);
            this.fireOnEntityChanged(true);
        };

        this.find = function (_findOptions, _viewOptions, typeChooserUrl) {
            SF.log("EDLine find");
            var _self = this;
            var type = this.getRuntimeType(typeChooserUrl, function (type) {
                _self.typedFind($.extend({ webQueryName: type }, _findOptions), _viewOptions);
            });
        };

        this.typedFind = function (_findOptions, _viewOptions) {
            SF.log("EDLine typedFind");
            if (SF.isEmpty(_findOptions.webQueryName)) throw "FindOptions webQueryName parameter must not be null in EDLine typedFind. Call find instead";
            var findOptions = this.createFindOptions(_findOptions, _viewOptions);
            new SF.FindNavigator(findOptions).openFinder();
        };

        this.createFindOptions = function (_findOptions, _viewOptions) {
            SF.log("EDLine createFindOptions");
            var self = this;
            return $.extend({
                prefix: this.options.prefix,
                onOk: function (selectedItems) { return self.onFindingOk(selectedItems, _viewOptions); },
                onOkClosed: function () { self.fireOnEntityChanged(true); },
                allowMultiple: false
            }, _findOptions);
        };

        this.onFindingOk = function (selectedItems, _viewOptions) {
            SF.log("EDLine onFindingOk");
            if (selectedItems == null || selectedItems.length != 1)
                throw "No item or more than one item was returned from Find Window";
            this.runtimeInfo().setEntity(selectedItems[0].type, selectedItems[0].id);

            //View result in the detailDiv
            var viewOptions = this.viewOptionsForCreating($.extend(_viewOptions, { type: selectedItems[0].type, id: selectedItems[0].id }));
            new SF.ViewNavigator(viewOptions).viewEmbedded();
            SF.triggerNewContent($("#" + this.options.detailDiv));

            return true;
        };

        this.removeSpecific = function () {
            SF.log("EDLine removeSpecific");
            $("#" + this.options.detailDiv).html("");
        };
    };

    /**
    * @constructor
    */
    SF.EDLine.prototype = new SF.EBaseLine();

    //EListOptions = EBaseLineOptions
    SF.EList = function (_elistOptions) {
        SF.EBaseLine.call(this, _elistOptions);

        this.updateLinks = function (newToStr, newLink, itemPrefix) {
            SF.log("EList updateLinks");
            $('#' + SF.compose(itemPrefix, SF.Keys.toStr)).html(newToStr);
        };

        this.extraJsonParams = function (itemPrefix) {
            SF.log("EList extraJsonParams");
            var extraParams = new Object();

            //If Embedded Entity => send path of runtimes and ids to be able to construct a typecontext
            var staticInfo = this.staticInfo();
            if (staticInfo.isEmbedded()) {
                var pathInfo = SF.fullPathNodesSelector(itemPrefix);
                for (var i = 0, l = pathInfo.length; i < l; i++) {
                    var node = pathInfo[i];
                    extraParams[node.id] = node.value;
                }
            }

            if (staticInfo.isReadOnly()) {
                extraParams.readOnly = true;
            }

            return extraParams;
        };

        this.itemRuntimeInfo = function (itemPrefix) {
            return new SF.RuntimeInfo(itemPrefix);
        };

        this.selectedItemPrefix = function () {
            SF.log("EList getSelected");
            var selected = $('#' + this.options.prefix + " > option:selected");
            if (selected.length == 0)
                return null;

            var nameSelected = selected[0].id;
            return nameSelected.substr(0, nameSelected.indexOf(SF.Keys.toStr) - 1);
        };

        this.getLastIndex = function () {
            SF.log("EList getLastIndex");
            var lastElement = $('#' + this.options.prefix + " > option:last");
            var lastIndex = -1;
            if (lastElement.length > 0) {
                var nameSelected = lastElement[0].id;
                lastIndex = nameSelected.substring(this.options.prefix.length + 1, nameSelected.indexOf(SF.Keys.toStr) - 1);
            }
            return lastIndex;
        };

        this.checkValidation = function (validateUrl, runtimeType, itemPrefix) {
            SF.log("EList checkValidation"); //Receives url as parameter so it can be overriden when setting viewOptions onOk

            var info = this.itemRuntimeInfo(itemPrefix);
            var id = (info.find().length > 0) ? info.id() : '';
            var validator = new SF.PartialValidator({ controllerUrl: validateUrl, prefix: itemPrefix, id: id, type: runtimeType });
            var validatorResult = validator.validate();
            if (!validatorResult.isValid) {
                if (!confirm(lang.signum.popupErrors)) {
                    $.extend(validatorResult, { acceptChanges: false });
                    return validatorResult;
                }
                else
                    validator.showErrors(validatorResult.modelState, true);
            }
            this.updateLinks(validatorResult.newToStr, validatorResult.newLink, itemPrefix);
            $.extend(validatorResult, { acceptChanges: true });
            return validatorResult;
        };

        this.viewOptionsForCreating = function (_viewOptions) {
            SF.log("EList viewOptionsForCreating");
            var self = this;
            var newIndex = +this.getLastIndex() + 1;
            var itemPrefix = SF.compose(this.options.prefix, newIndex.toString());
            return $.extend({
                onOk: function (clonedElements) { return self.onCreatingOk(clonedElements, _viewOptions.validationOptions.controllerUrl, _viewOptions.type, itemPrefix); },
                onOkClosed: function () { self.fireOnEntityChanged(); },
                onCancelled: null,
                controllerUrl: null,
                prefix: itemPrefix,
                requestExtraJsonData: this.extraJsonParams(itemPrefix)
            }, _viewOptions);
        };

        this.onCreatingOk = function (clonedElements, validateUrl, runtimeType, itemPrefix) {
            SF.log("EList onCreatingOK"); //Receives url as parameter so it can be overriden when setting viewOptions onOk
            var validatorResult = this.checkValidation(validateUrl, runtimeType, itemPrefix);
            if (validatorResult.acceptChanges) {
                this.newListItem(clonedElements, runtimeType, itemPrefix, validatorResult.newToStr);
            }
            return validatorResult.acceptChanges;
        };

        this.newListItem = function (clonedElements, runtimeType, itemPrefix, newToStr) {
            SF.log("EList newListItem");
            var listInfo = this.staticInfo();
            var itemInfoValue = new SF.RuntimeInfo(itemPrefix).createValue(runtimeType, '', 'n');
            listInfo.find().after(SF.hiddenInput(SF.compose(itemPrefix, SF.Keys.runtimeInfo), itemInfoValue))
                .after(SF.hiddenDiv(SF.compose(itemPrefix, this.entity), ""));
            $('#' + SF.compose(itemPrefix, this.entity)).append(clonedElements);

            var select = $(this.pf(''));
            if (SF.isEmpty(newToStr))
                newToStr = "&nbsp;";
            select.append("\n<option id='" + SF.compose(itemPrefix, SF.Keys.toStr) + "' name='" + SF.compose(itemPrefix, SF.Keys.toStr) + "' value='' class='sf-value-line'>" + newToStr + "</option>");
            select.children('option').attr('selected', false); //Fix for Firefox: Set selected after retrieving the html of the select
            select.children('option:last').attr('selected', true);
        };

        this.view = function (_viewOptions) {
            SF.log("EList view");
            var selectedItemPrefix = this.selectedItemPrefix();
            if (SF.isEmpty(selectedItemPrefix))
                return;
            this.viewInIndex(_viewOptions, selectedItemPrefix);
        };

        this.viewInIndex = function (_viewOptions, selectedItemPrefix) {
            SF.log("EList viewInIndex");
            var viewOptions = this.viewOptionsForViewing(_viewOptions, selectedItemPrefix);
            if (viewOptions.navigate) {
                var itemInfo = this.itemRuntimeInfo(selectedItemPrefix);
                if (!SF.isEmpty(itemInfo.id())) {
                    window.open(_viewOptions.controllerUrl.substring(0, _viewOptions.controllerUrl.lastIndexOf("/") + 1) + itemInfo.runtimeType() + "/" + itemInfo.id(), "_blank");
                }
                return;
            }
            new SF.ViewNavigator(viewOptions).viewOk();
        };

        this.viewOptionsForViewing = function (_viewOptions, itemPrefix) {
            SF.log("EList viewOptionsForViewing");
            var self = this;
            var info = this.itemRuntimeInfo(itemPrefix);
            return $.extend({
                containerDiv: SF.compose(itemPrefix, self.entity),
                onOk: function () { return self.onViewingOk(_viewOptions.validationOptions.controllerUrl, itemPrefix); },
                onOkClosed: function () { self.fireOnEntityChanged(); },
                onCancelled: null,
                controllerUrl: null,
                type: info.runtimeType(),
                id: info.id(),
                prefix: itemPrefix,
                requestExtraJsonData: this.extraJsonParams(itemPrefix)
            }, _viewOptions);
        };

        this.onViewingOk = function (validateUrl, itemPrefix) {
            SF.log("EList onViewingOk"); //Receives url as parameter so it can be overriden when setting viewOptions onOk
            var validatorResult = this.checkValidation(validateUrl, this.itemRuntimeInfo(itemPrefix).runtimeType(), itemPrefix);
            return validatorResult.acceptChanges;
        };

        this.createFindOptions = function (_findOptions) {
            SF.log("EList createFindOptions");
            var newIndex = +this.getLastIndex() + 1;
            var itemPrefix = SF.compose(this.options.prefix, newIndex.toString());
            var self = this;
            return $.extend({
                prefix: itemPrefix,
                onOk: function (selectedItems) { return self.onFindingOk(selectedItems); },
                onOkClosed: function () { self.fireOnEntityChanged(); },
                allowMultiple: true
            }, _findOptions);
        };

        this.onFindingOk = function (selectedItems) {
            SF.log("EList onFindingOk");
            if (selectedItems == null || selectedItems.length == 0)
                throw "No item was returned from Find Window";
            var lastIndex = +this.getLastIndex();
            for (var i = 0, l = selectedItems.length; i < l; i++) {
                var item = selectedItems[i];
                lastIndex++;
                var itemPrefix = SF.compose(this.options.prefix, lastIndex.toString());

                this.newListItem('', item.type, itemPrefix, item.toStr);
                this.itemRuntimeInfo(itemPrefix).setEntity(item.type, item.id);
                $('#' + SF.compose(itemPrefix, SF.Keys.toStr)).html(item.toStr);
            }
            return true;
        };

        this.remove = function () {
            SF.log("EList remove");
            var selectedItemPrefix = this.selectedItemPrefix();
            if (SF.isEmpty(selectedItemPrefix))
                return;
            this.removeInIndex(selectedItemPrefix);
        };

        this.removeInIndex = function (selectedItemPrefix) {
            SF.log("EList removeInIndex");

            $.each([SF.Keys.runtimeInfo, SF.Keys.toStr, this.entity, SF.EList.index], function (i, key) {
                $("#" + SF.compose(selectedItemPrefix, key)).remove();
            });
            this.fireOnEntityChanged();
        };

        this.updateButtonsDisplay = function () {
            SF.log("EList updateButtonsDisplay");
            var hasElements = $('#' + this.options.prefix + " > option").length > 0;
            $(this.pf("btnRemove")).toggle(hasElements);
            $(this.pf("btnView")).toggle(hasElements);
        };
    };

    SF.EList.index = "sfIndex";

    /**
    * @constructor
    */
    SF.EList.prototype = new SF.EBaseLine();

    //ERepOptions = EBaseLineOptions + maxElements + removeItemLinkText
    /**
    * @constructor
    */
    SF.ERep = function (_erepOptions) {
        SF.log("ERep");
        SF.EList.call(this, _erepOptions);

        this.itemsContainer = "sfItemsContainer";
        this.repeaterItem = "sfRepeaterItem";
        this.repeaterItemClass = "sf-repeater-element";

        this.canAddItems = function () {
            SF.log("ERep canAddItems");
            if (!SF.isEmpty(this.options.maxElements)) {
                if ($(this.pf(this.itemsContainer) + " > ." + this.repeaterItemClass).length >= +this.options.maxElements) {
                    return false;
                }
            }
            return true;
        };

        this.getLastIndex = function () {
            SF.log("ERep getLastIndex");
            var lastElement = $(this.pf(this.itemsContainer) + " > ." + this.repeaterItemClass + ":last");
            var lastIndex = -1;
            if (lastElement.length !== 0) {
                var nameSelected = lastElement[0].id;
                lastIndex = nameSelected.substring(this.options.prefix.length + 1, nameSelected.indexOf(this.repeaterItem) - 1);
            }
            return lastIndex;
        };

        this.typedCreate = function (_viewOptions) {
            SF.log("ERep create");
            if (SF.isEmpty(_viewOptions.type)) {
                throw "ViewOptions type parameter must not be null in ERep typedCreate. Call create instead";
            }

            if (!this.canAddItems()) return;

            var viewOptions = this.viewOptionsForCreating(_viewOptions);
            var template = window[SF.compose(this.options.prefix, "sfTemplate")];
            if (!SF.isEmpty(template)) { //Template pre-loaded (Embedded Entity): It will be created with "_0" itemprefix => replace it with the current one
                template = template.replace(new RegExp(SF.compose(this.options.prefix, "0"), "gi"), viewOptions.prefix);
                this.onItemCreated(template, viewOptions);
            }
            else {
                var self = this;
                new SF.ViewNavigator(viewOptions).createEmbedded(function (newHtml) {
                    self.onItemCreated(newHtml, viewOptions);
                });
            }
        };

        this.viewOptionsForCreating = function (_viewOptions) {
            SF.log("ERep viewOptionsForCreating");
            var newIndex = +this.getLastIndex() + 1;
            var itemPrefix = SF.compose(this.options.prefix, newIndex.toString());
            return $.extend({
                containerDiv: "",
                controllerUrl: null,
                prefix: itemPrefix,
                requestExtraJsonData: this.extraJsonParams(itemPrefix)
            }, _viewOptions);
        };

        this.onItemCreated = function (newHtml, viewOptions) {
            SF.log("ERep onItemCreated");
            if (SF.isEmpty(viewOptions.type)) {
                throw "ViewOptions type parameter must not be null in ERep onItemCreated";
            }

            var itemPrefix = viewOptions.prefix;
            this.newRepItem(newHtml, viewOptions.type, itemPrefix);
            this.fireOnEntityChanged();
        };

        this.newRepItem = function (newHtml, runtimeType, itemPrefix) {
            SF.log("ERep newRepItem");
            var listInfo = this.staticInfo();
            var itemInfoValue = this.itemRuntimeInfo(itemPrefix).createValue(runtimeType, '', 'n');

            var $div = $("<fieldset id='" + SF.compose(itemPrefix, this.repeaterItem) + "' name='" + SF.compose(itemPrefix, this.repeaterItem) + "' class='" + this.repeaterItemClass + "'>" +
                "<legend>" +
                "<a id='" + SF.compose(itemPrefix, "btnRemove") + "' title='" + this.options.removeItemLinkText + "' href=\"javascript:new SF.ERep({prefix:'" + this.options.prefix + "', onEntityChanged:" + (SF.isEmpty(this.options.onEntityChanged) ? "''" : this.options.onEntityChanged) + "}).remove('" + itemPrefix + "');\" class='sf-line-button sf-remove' data-icon='ui-icon-circle-close' data-text='false'>" + this.options.removeItemLinkText + "</a>" +
                "</legend>" +
                SF.hiddenInput(SF.compose(itemPrefix, SF.Keys.runtimeInfo), itemInfoValue) +
                "<div id='" + SF.compose(itemPrefix, this.entity) + "' name='" + SF.compose(itemPrefix, this.entity) + "'>" +
                "</div>" + //sfEntity
                "</fieldset>"
                );

            $(this.pf(this.itemsContainer)).append($div);
            $("#" + SF.compose(itemPrefix, this.entity)).html(newHtml);
            SF.triggerNewContent($("#" + SF.compose(itemPrefix, this.repeaterItem)));
        };

        this.viewOptionsForViewing = function (_viewOptions, itemPrefix) { //Used in onFindingOk
            SF.log("ERep viewOptionsForViewing");
            return $.extend({
                containerDiv: SF.compose(itemPrefix, this.entity),
                controllerUrl: null,
                prefix: itemPrefix,
                requestExtraJsonData: this.extraJsonParams(itemPrefix)
            }, _viewOptions);
        };

        this.find = function (_findOptions, _viewOptions, typeChooserUrl) {
            SF.log("ERep find");
            var _self = this;
            var type = this.getRuntimeType(typeChooserUrl, function (type) {
                _self.typedFind($.extend({ webQueryName: type }, _findOptions), _viewOptions);
            });
        };

        this.typedFind = function (_findOptions, _viewOptions) {
            SF.log("ERep typedFind");
            if (SF.isEmpty(_findOptions.webQueryName)) {
                throw "FindOptions webQueryName parameter must not be null in ERep typedFind. Call find instead";
            }

            if (!this.canAddItems()) return;

            var findOptions = this.createFindOptions(_findOptions, _viewOptions);
            new SF.FindNavigator(findOptions).openFinder();
        },

    this.createFindOptions = function (_findOptions, _viewOptions) {
        SF.log("ERep createFindOptions");
        var newIndex = +this.getLastIndex() + 1;
        var itemPrefix = SF.compose(this.options.prefix, newIndex.toString());
        var self = this;
        return $.extend({
            prefix: itemPrefix,
            onOk: function (selectedItems) { return self.onFindingOk(selectedItems, _viewOptions); },
            onOkClosed: function () { self.fireOnEntityChanged(); },
            allowMultiple: true
        }, _findOptions);
    };

        this.onFindingOk = function (selectedItems, _viewOptions) {
            SF.log("ERep onFindingOk");
            if (selectedItems == null || selectedItems.length == 0)
                throw "No item was returned from Find Window";
            var lastIndex = +this.getLastIndex();
            for (var i = 0, l = selectedItems.length; i < l; i++) {
                if (!this.canAddItems()) {
                    return;
                }

                var item = selectedItems[i];
                lastIndex++;
                var itemPrefix = SF.compose(this.options.prefix, lastIndex.toString());

                this.newRepItem('', item.type, itemPrefix);
                this.itemRuntimeInfo(itemPrefix).setEntity(item.type, item.id);

                //View results in the repeater
                var viewOptions = this.viewOptionsForViewing($.extend(_viewOptions, { type: item.type, id: item.id }), itemPrefix);
                new SF.ViewNavigator(viewOptions).viewEmbedded();
                SF.triggerNewContent($(SF.compose(itemPrefix, this.entity)));
            }
            return true;
        };

        this.remove = function (itemPrefix) {
            SF.log("ERep remove");
            $('#' + SF.compose(itemPrefix, this.repeaterItem)).remove();
            this.fireOnEntityChanged();
        };

        this.updateButtonsDisplay = function () {
            SF.log("ERep updateButtonsDisplay");
            var $buttons = $(this.pf("btnFind"), this.pf("btnFind"));
            if (this.canAddItems()) {
                $buttons.show();
            }
            else {
                $buttons.hide();
            }
        };
    };

    /**
    * @constructor
    */
    SF.ERep.prototype = new SF.EList();

    //EDListOptions = EBaseLineOptions + detailDiv
    SF.EDList = function (_edlistOptions) {
        SF.log("EDList");
        SF.EList.call(this, _edlistOptions);

        this.typedCreate = function (_viewOptions) {
            SF.log("EDList create");
            if (SF.isEmpty(_viewOptions.type)) throw "ViewOptions type parameter must not be null in EDList typedCreate. Call create instead";
            this.restoreCurrent();
            var viewOptions = this.viewOptionsForCreating(_viewOptions);
            var template = window[SF.compose(this.options.prefix, "sfTemplate")];
            if (!SF.isEmpty(template)) { //Template pre-loaded (Embedded Entity): It will be created with "_0" itemprefix => replace it with the current one
                template = template.replace(new RegExp(SF.compose(this.options.prefix, "0"), "gi"), viewOptions.prefix);
                $('#' + viewOptions.containerDiv).html(template);
                SF.triggerNewContent($('#' + viewOptions.containerDiv));
            }
            else {
                new SF.ViewNavigator(viewOptions).viewEmbedded();
                SF.triggerNewContent($('#' + viewOptions.containerDiv));
            }
            this.onItemCreated(viewOptions);
        };

        this.viewOptionsForCreating = function (_viewOptions) {
            SF.log("EDList viewOptionsForCreating");
            var newIndex = +this.getLastIndex() + 1;
            var itemPrefix = SF.compose(this.options.prefix, newIndex.toString());
            return $.extend({
                containerDiv: this.options.detailDiv,
                controllerUrl: null,
                prefix: itemPrefix,
                requestExtraJsonData: this.extraJsonParams(itemPrefix)
            }, _viewOptions);
        };

        this.getVisibleItemPrefix = function () {
            SF.log("EDList getCurrentVisible");
            var detail = $('#' + this.options.detailDiv);
            var firstId = detail.find(":input[id^=" + this.options.prefix + "]:first");
            if (firstId.length === 0) {
                return null;
            }
            var id = firstId[0].id;
            var nextSeparator = id.indexOf("_", this.options.prefix.length + 1);
            return id.substring(0, nextSeparator);
        };

        this.restoreCurrent = function () {
            SF.log("EDList restoreCurrent");
            var itemPrefix = this.getVisibleItemPrefix();
            if (!SF.isEmpty(itemPrefix)) {
                $('#' + SF.compose(itemPrefix, this.entity))
                    .html('')
                    .append(SF.cloneContents(this.options.detailDiv));
            }
        };

        this.onItemCreated = function (viewOptions) {
            SF.log("EDList onItemCreated");
            if (SF.isEmpty(viewOptions.type)) {
                throw "ViewOptions type parameter must not be null in EDList onItemCreated. Call create instead";
            }

            var itemPrefix = viewOptions.prefix;
            this.newListItem('', viewOptions.type, itemPrefix);
            this.fireOnEntityChanged();
        };

        this.view = function (_viewOptions) {
            SF.log("EDList view");
            var selectedItemPrefix = this.selectedItemPrefix();
            if (SF.isEmpty(selectedItemPrefix)) {
                return;
            }
            this.viewInIndex(_viewOptions, selectedItemPrefix);
        };

        this.viewInIndex = function (_viewOptions, selectedItemPrefix) {
            SF.log("EDList viewInIndex");
            this.restoreCurrent();
            if (this.isLoaded(selectedItemPrefix)) {
                this.cloneAndShow(selectedItemPrefix);
            }
            else {
                var viewOptions = this.viewOptionsForViewing(_viewOptions, selectedItemPrefix);
                new SF.ViewNavigator(viewOptions).viewEmbedded();
                SF.triggerNewContent($('#' + viewOptions.containerDiv));
            }
        };

        this.viewOptionsForViewing = function (_viewOptions, itemPrefix) {
            SF.log("EDList viewOptionsForCreating");
            var self = this;
            var info = this.itemRuntimeInfo(itemPrefix);
            return $.extend({
                containerDiv: this.options.detailDiv,
                controllerUrl: null,
                type: info.runtimeType(),
                id: info.id(),
                prefix: itemPrefix,
                requestExtraJsonData: this.extraJsonParams(itemPrefix)
            }, _viewOptions);
        };

        this.isLoaded = function (selectedItemPrefix) {
            SF.log("EDList isLoaded");
            return !SF.isEmpty($('#' + SF.compose(selectedItemPrefix, this.entity)).html());
        };

        this.cloneAndShow = function (selectedItemPrefix) {
            SF.log("EDList cloneAndShow");
            $('#' + this.options.detailDiv)
                .html('')
                .append(SF.cloneContents(SF.compose(selectedItemPrefix, this.entity)));

            $('#' + SF.compose(selectedItemPrefix, this.entity))
                .html('');
        };

        this.find = function (_findOptions, _viewOptions, typeChooserUrl) {
            SF.log("EDList find");
            var _self = this;
            var type = this.getRuntimeType(typeChooserUrl, function (type) {
                _self.typedFind($.extend({ webQueryName: type }, _findOptions), _viewOptions);
            });
        };

        this.typedFind = function (_findOptions, _viewOptions) {
            SF.log("EDList typedFind");
            if (SF.isEmpty(_findOptions.webQueryName)) {
                throw "FindOptions webQueryName parameter must not be null in EDList typedFind. Call find instead";
            }

            this.restoreCurrent();
            var findOptions = this.createFindOptions(_findOptions, _viewOptions);
            new SF.FindNavigator(findOptions).openFinder();
        },

    this.createFindOptions = function (_findOptions, _viewOptions) {
        SF.log("EDList createFindOptions");
        var newIndex = +this.getLastIndex() + 1;
        var itemPrefix = SF.compose(this.options.prefix, newIndex.toString());
        var self = this;
        return $.extend({
            prefix: itemPrefix,
            onOk: function (selectedItems) { return self.onFindingOk(selectedItems, _viewOptions); },
            onOkClosed: function () { self.fireOnEntityChanged(); },
            allowMultiple: true
        }, _findOptions);
    };

        this.onFindingOk = function (selectedItems, _viewOptions) {
            SF.log("EDList onFindingOk");
            if (selectedItems == null || selectedItems.length == 0)
                throw "No item was returned from Find Window";
            var lastIndex = +this.getLastIndex();
            for (var i = 0, l = selectedItems.length; i < l; i++) {
                var item = selectedItems[i];
                lastIndex++;
                var itemPrefix = SF.compose(this.options.prefix, lastIndex.toString());

                this.newListItem('', item.type, itemPrefix, item.toStr);
                this.itemRuntimeInfo(itemPrefix).setEntity(item.type, item.id);
                $('#' + SF.compose(itemPrefix, SF.Keys.toStr)).html(item.toStr);

                //View result in the detailDiv
                $('#' + this.options.prefix).dblclick();
            }
            return true;
        };

        this.remove = function () {
            SF.log("EDList remove");
            var selectedItemPrefix = this.selectedItemPrefix();
            if (SF.isEmpty(selectedItemPrefix)) {
                return;
            }
            this.edlineRemoveInIndex(selectedItemPrefix);
        };

        this.edlineRemoveInIndex = function (itemPrefix) {
            SF.log("EDList edlineRemoveInIndex");
            var currentVisible = this.getVisibleItemPrefix();
            if (!SF.isEmpty(currentVisible) && currentVisible == itemPrefix)
                $('#' + this.options.detailDiv).html('');
            this.removeInIndex(itemPrefix);
        };
    };

    /**
    * @constructor
    */
    SF.EDList.prototype = new SF.EList();

    //EComboOptions = EBaseOptions
    SF.ECombo = function (_ecomboOptions) {
        SF.log("ECombo");
        SF.ELine.call(this, _ecomboOptions);

        this.updateLinks = function (newToStr, newLink) {
            SF.log("ECombo updateLinks");
            $("#" + this.options.prefix + " option:selected").html(newToStr);
        };

        this.selectedValue = function () {
            SF.log("ECombo selectedValue");
            var selected = $("#" + this.options.prefix + " > option:selected");
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
        };

        this.setSelected = function () {
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
            $(this.pf(this.entity)).html(''); //Clean
            this.fireOnEntityChanged(newEntity);
        };

        this.view = function (_viewOptions) {
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
        };
    };

    /**
    * @constructor
    */
    SF.ECombo.prototype = new SF.ELine();

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

    SF.autocompleteOnSelected = function (controlId, data) {
        var prefix = controlId.substr(0, controlId.indexOf(SF.Keys.toStr) - 1);
        new SF.ELine({ prefix: prefix }).onAutocompleteSelected(controlId, data);
    };

    SF.entityAutocomplete = function ($elem, options) {
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
                SF.autocompleteOnSelected($elem.attr("id"), ui.item.value);
            }
        });
    };
});

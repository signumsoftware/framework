var EBaseLine = function(_eBaseOptions) {
    this.options = $.extend({
        prefix: "",
        onEntityChanged: null
    }, _eBaseOptions);
};

EBaseLine.prototype = {

    runtimeInfo: function() {
        return RuntimeInfoFor(this.options.prefix);
    },

    staticInfo: function() {
        return StaticInfoFor(this.options.prefix);
    },

    setTicks: function() {
        log("EBaseLine setTicks");
        this.runtimeInfo().ticks(new Date().getTime());
    },

    pf: function(s) {
        return "#" + this.options.prefix + s;
    },

    checkValidation: function(validateUrl, runtimeType) {
        log("EBaseLine checkValidation"); //Receives url as parameter so it can be overriden when setting viewOptions onOk
        var info = this.runtimeInfo();
        var id = (info.find().length > 0) ? info.id() : '';
        var validator = new PartialValidator({ controllerUrl: validateUrl, prefix: this.options.prefix, id: id, type: runtimeType });
        var validatorResult = validator.validate();
        if (!validatorResult.isValid) {
            if (!confirm(lang['popupErrors']))
                return false;
            else 
                validator.showErrors(validatorResult.modelState, true);
        }
        this.updateLinks(validatorResult.newToStr, validatorResult.newLink);
        return true;
    },

    updateLinks: function(newToStr, newLink) {
        log("EBaseLine updateLinks");
        //Abstract function
    },

    fireOnEntityChanged: function(hasEntity) {
        log("EBaseLine fireOnEntityChanged");
        this.setTicks();
        this.updateButtonsDisplay(hasEntity);
        if (!empty(this.options.onEntityChanged))
            this.options.onEntityChanged();
    },

    remove: function() {
        log("EBaseLine remove");
        $(this.pf(sfToStr)).val("").removeClass(sfInputErrorClass);
        $(this.pf(sfLink)).val("").html("").removeClass(sfInputErrorClass);
        this.runtimeInfo().removeEntity();

        this.removeSpecific();
        this.fireOnEntityChanged(false);
    },

    getRuntimeType: function(_onTypeFound) {
        log("EBaseLine getRuntimeType");
        var implSelector = this.pf(sfImplementations);
        var impl = $(implSelector);
        if (impl.length == 0)
            return _onTypeFound(this.staticInfo().staticType());

        var implementations = impl.val().split(";");
        if (implementations.length == 1)
            return _onTypeFound(implementations[0]);

        openChooser(this.options.prefix, _onTypeFound);
    },

    create: function(_viewOptions) {
        log("EBaseLine create");
        var _self = this;
        var type = this.getRuntimeType(function(type) {
            _self.typedCreate($.extend({ type: type }, _viewOptions));
        });
    },

    typedCreate: function(_viewOptions) {
        log("EBaseline create");
        if (empty(_viewOptions.type)) throw "ViewOptions type parameter must not be null in EBaseline typedCreate. Call create instead";
        var viewOptions = this.viewOptionsForCreating(_viewOptions);
        var template = window[this.options.prefix + "_sfTemplate"];
        if (!empty(template)) { //Template pre-loaded: In case of a list, it will be created with "_0" itemprefix => replace it with the current one
            template = template.replace(new RegExp("\"" + this.options.prefix + "_0", "gi"), "\"" + viewOptions.prefix).replace(new RegExp("'" + this.options.prefix + "_0", "gi"), "'" + viewOptions.prefix);
            new ViewNavigator(viewOptions).showCreateOk(template);
        }
        else
            new ViewNavigator(viewOptions).createOk();
    },

    find: function(_findOptions) {
        log("EBaseLine find");
        var _self = this;
        var type = this.getRuntimeType(function(type) {
            _self.typedFind($.extend({ queryUrlName: type }, _findOptions));
        });
    },

    typedFind: function(_findOptions) {
        log("EBaseline typedFind");
        if (empty(_findOptions.queryUrlName)) throw "FindOptions queryUrlName parameter must not be null in EBaseline typedFind. Call find instead";
        var findOptions = this.createFindOptions(_findOptions);
        new FindNavigator(findOptions).openFinder();
    },

    extraJsonParams: function(_prefix) {
        log("EBaseLine extraJsonParams");
        var extraParams = new Object();

        var staticInfo = this.staticInfo();

        //If Embedded Entity => send path of runtimes and ids to be able to construct a typecontext
        if (staticInfo.isEmbedded() == "e") {
            var pathInfo = FullPathNodesSelector(this.options.prefix);
            for (var i = 0; i < pathInfo.length; i++) {
                var node = pathInfo[i];
                extraParams[node.id] = node.value;
            }
        }

        if (staticInfo.isReadOnly() == "r") {
            extraParams.sfReadOnly = true;
        }

        //If reactive => send reactive flag, tabId, and Id & Runtime of the main entity
        if ($('#' + sfReactive).length > 0) {
            extraParams.sfReactive = true;
            extraParams.sfTabId = $('#' + sfTabId).val();
            extraParams._sfRuntimeInfo = RuntimeInfoFor('').value();
        }

        return extraParams;
    },

    updateButtonsDisplay: function(hasEntity) {
        log("EBaseLine updateButtonsDisplay");
        var btnCreate = $(this.pf("_btnCreate"));
        var btnRemove = $(this.pf("_btnRemove"));
        var btnFind = $(this.pf("_btnFind"));
        var btnView = $(this.pf("_btnView"));
        var link = $(this.pf(sfLink));
        var txt = $(this.pf(sfToStr));

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

var ELine = function(_elineOptions) {
    EBaseLine.call(this, _elineOptions);

    var defaultViewUrl = "Signum/PopupView";
    var defaultValidateUrl = "Signum/ValidatePartial";

    this.updateLinks = function(newToStr, newLink) {
        log("ELine updateLinks");
        $(this.pf(sfLink)).html(newToStr).attr('href', newLink);
        $(this.pf(sfToStr)).val(''); //Clean
    };

    this.view = function(_viewOptions) {
        log("ELine view");
        var viewOptions = this.viewOptionsForViewing(_viewOptions);
        new ViewNavigator(viewOptions).viewOk();
    };

    this.viewOptionsForViewing = function(_viewOptions) {
        log("ELine viewOptionsForViewing");
        var self = this;
        var info = this.runtimeInfo();
        return $.extend({
            containerDiv: this.options.prefix + sfEntity,
            onOk: function() { return self.onViewingOk(defaultValidateUrl); },
            onCancelled: null,
            controllerUrl: defaultViewUrl,
            type: info.runtimeType(),
            id: info.id(),
            prefix: this.options.prefix,
            requestExtraJsonData: this.extraJsonParams()
        }, _viewOptions);
    };

    this.onViewingOk = function(validateUrl) {
        log("ELine onViewingOk"); //Receives url as parameter so it can be overriden when setting viewOptions onOk
        var acceptChanges = this.checkValidation(validateUrl, this.runtimeInfo().runtimeType());
        if (acceptChanges)
            this.setTicks();
        return acceptChanges;
    };

    this.viewOptionsForCreating = function(_viewOptions) {
        log("ELine viewOptionsForCreating");
        var self = this;
        return $.extend({
            containerDiv: "",
            onOk: function(clonedElements) { return self.onCreatingOk(clonedElements, defaultValidateUrl, _viewOptions.type); },
            onOkClosed: function() { self.fireOnEntityChanged(true); },
            onCancelled: null,
            controllerUrl: defaultViewUrl,
            prefix: this.options.prefix,
            requestExtraJsonData: this.extraJsonParams()
        }, _viewOptions);
    };

    this.newEntity = function(clonedElements, runtimeType) {
        var info = this.runtimeInfo();
        info.setEntity(runtimeType, '').find()
            .after(hiddenDiv(this.options.prefix + sfEntity, ''));
        $(this.pf(sfEntity)).append(clonedElements);
    };

    this.onCreatingOk = function(clonedElements, validateUrl, runtimeType) {
        log("ELine onCreatingOk"); //Receives url as parameter so it can be overriden when setting viewOptions onOk
        var acceptChanges = this.checkValidation(validateUrl, runtimeType);
        if (acceptChanges) {
            this.newEntity(clonedElements, runtimeType);
        }
        return acceptChanges;
    };

    this.createFindOptions = function(_findOptions) {
        log("ELine createFindOptions");
        var self = this;
        return $.extend({
            prefix: this.options.prefix,
            onOk: function(selectedItems) { return self.onFindingOk(selectedItems); },
            onOkClosed: function() { self.fireOnEntityChanged(true); },
            allowMultiple: false
        }, _findOptions);
    };

    this.onFindingOk = function(selectedItems) {
        log("ELine onFindingOk");
        if (selectedItems == null || selectedItems.length != 1)
            throw "No item or more than one item was returned from Find Window";
        var info = this.runtimeInfo();
        info.setEntity(selectedItems[0].type, selectedItems[0].id);
        if ($(this.pf(sfEntity)).length == 0)
            info.find().after(hiddenDiv(this.options.prefix + sfEntity, ''));
        $(this.pf(sfToStr)).val(''); //Clean
        $(this.pf(sfLink)).html(selectedItems[0].toStr).attr('href', selectedItems[0].link);
        return true;
    };

    this.removeSpecific = function() {
    log("ELine removeSpecific");
        $(this.pf(sfEntity)).remove();
    };
}

ELine.prototype = new EBaseLine();

function ELineOnCreating(_eline, _viewOptions) {
    _eline.create(_viewOptions);
}

function ELineOnFinding(_eline, _findOptions) {
    _eline.find(_findOptions);
}

function ELineOnViewing(_eline, _viewOptions) {
    _eline.view(_viewOptions);
}

function ELineOnRemoving(_eline) {
    _eline.remove();
}

//EDLineOptions = EBaseLineOptions + detailDiv
var EDLine = function(_edlineOptions) {
    log("EDLine");
    EBaseLine.call(this, _edlineOptions);

    this.typedCreate = function(_viewOptions) {
    log("EDLine create");
        if (empty(_viewOptions.type)) throw "ViewOptions type parameter must not be null in EDLine typedCreate. Call create instead";
        var viewOptions = this.viewOptionsForCreating(_viewOptions);
        var template = window[this.options.prefix + "_sfTemplate"];
        if (!empty(template)) { //Template pre-loaded: EmbeddedEntity
            $('#' + viewOptions.containerDiv).html(template);
        }
        else{
            new ViewNavigator(viewOptions).viewEmbedded();
        }
        this.onCreated(viewOptions.type);
    };
    
    this.viewOptionsForCreating = function(_viewOptions) {
        log("EDLine viewOptionsForCreating");
        return $.extend({
            containerDiv: this.options.detailDiv,
            controllerUrl: "Signum/PartialView",
            prefix: this.options.prefix,
            requestExtraJsonData: this.extraJsonParams()
        }, _viewOptions);
    };

    this.newEntity = function(runtimeType) {
        this.runtimeInfo().setEntity(runtimeType, '');
    };

    this.onCreated = function(runtimeType) {
        log("EDLine onCreated");
        this.newEntity(runtimeType);
        this.fireOnEntityChanged(true);
    };

    this.find = function(_findOptions, _viewOptions) {
    log("EDLine find");
        var _self = this;
        var type = this.getRuntimeType(function(type) {
            _self.typedFind($.extend({ queryUrlName: type }, _findOptions), _viewOptions);
        });
    };

    this.typedFind = function(_findOptions, _viewOptions) {
    log("EDLine typedFind");
        if (empty(_findOptions.queryUrlName)) throw "FindOptions queryUrlName parameter must not be null in EDLine typedFind. Call find instead";
        var findOptions = this.createFindOptions(_findOptions, _viewOptions);
        new FindNavigator(findOptions).openFinder();
    };

    this.createFindOptions = function(_findOptions, _viewOptions) {
    log("EDLine createFindOptions");
        var self = this;
        return $.extend({
            prefix: this.options.prefix,
            onOk: function(selectedItems) { return self.onFindingOk(selectedItems, _viewOptions); },
            onOkClosed: function() { self.fireOnEntityChanged(true); },
            allowMultiple: false
        }, _findOptions);
    };

    this.onFindingOk = function(selectedItems, _viewOptions) {
        log("EDLine onFindingOk");
        if (selectedItems == null || selectedItems.length != 1)
            throw "No item or more than one item was returned from Find Window";
        this.runtimeInfo().setEntity(selectedItems[0].type, selectedItems[0].id);

        //View result in the detailDiv
        var viewOptions = this.viewOptionsForCreating($.extend(_viewOptions, { type: selectedItems[0].type, id: selectedItems[0].id }));
        new ViewNavigator(viewOptions).viewEmbedded();

        return true;
    };

    this.removeSpecific = function() {
    log("EDLine removeSpecific");
        $("#" + this.options.detailDiv).html("");
    };
}

EDLine.prototype = new EBaseLine();

function EDLineOnCreating(_edline, _viewOptions) {
    _edline.create(_viewOptions);
}

function EDLineOnFinding(_edline, _findOptions, _viewOptions) {
    _edline.find(_findOptions, _viewOptions);
}

function EDLineOnRemoving(_edline) {
    _edline.remove();
}

//EListOptions = EBaseLineOptions
var EList = function(_elistOptions) {
    EBaseLine.call(this, _elistOptions);

    var defaultViewUrl = "Signum/PopupView";
    var defaultValidateUrl = "Signum/ValidatePartial";

    this.updateLinks = function(newToStr, newLink, itemPrefix) {
        log("EList updateLinks");
        $('#' + itemPrefix + sfToStr).html(newToStr);
    };

    this.extraJsonParams = function(itemPrefix) {
        log("EList extraJsonParams");
        var extraParams = new Object();

        //If Embedded Entity => send path of runtimes and ids to be able to construct a typecontext
        var staticInfo = this.staticInfo();
        if (staticInfo.isEmbedded() == "e") {
            var pathInfo = FullPathNodesSelector(itemPrefix);
            for (var i = 0; i < pathInfo.length; i++) {
                var node = pathInfo[i];
                extraParams[node.id] = node.value;
            }
        }

        if (staticInfo.isReadOnly() == "r") {
            extraParams.sfReadOnly = true;
        }

        //If reactive => send reactive flag, tabId, and Id & Runtime of the main entity
        if ($('#' + sfReactive).length > 0) {
            extraParams.sfReactive = true;
            extraParams.sfTabId = $('#' + sfTabId).val();
            extraParams._sfRuntimeInfo = RuntimeInfoFor('').value();
        }

        return extraParams;
    };

    this.setItemTicks = function(itemPrefix) {
        log("EList setItemTicks");
        //this.itemRuntimeInfo(itemPrefix).ticks(new Date().getTime());
        $('#' + itemPrefix + sfTicks).val(new Date().getTime());
    };

    this.itemRuntimeInfo = function(itemPrefix) {
        return RuntimeInfoFor(itemPrefix);
    };

    this.selectedItemPrefix = function() {
        log("EList getSelected");
        var selected = $(this.pf(" > option:selected"));
        if (selected.length == 0)
            return null;

        var nameSelected = selected[0].id;
        return nameSelected.substr(0, nameSelected.indexOf(sfToStr));
    };

    this.getLastIndex = function() {
        log("EList getLastIndex");
        var lastElement = $(this.pf(" > option:last"));
        var lastIndex = -1;
        if (lastElement.length > 0) {
            var nameSelected = lastElement[0].id;
            lastIndex = nameSelected.substring(this.options.prefix.length + 1, nameSelected.indexOf(sfToStr));
        }
        return lastIndex;
    };

    this.checkValidation = function(validateUrl, runtimeType, itemPrefix) {
        log("EList checkValidation"); //Receives url as parameter so it can be overriden when setting viewOptions onOk
        var info = this.itemRuntimeInfo(itemPrefix);
        var id = (info.find().length > 0) ? info.id() : '';
        var validator = new PartialValidator({ controllerUrl: validateUrl, prefix: itemPrefix, id: id, type: runtimeType });
        var validatorResult = validator.validate();
        if (!validatorResult.isValid) {
            if (!confirm(lang['popupErrors']))
                return false;
            else
                validator.showErrors(validatorResult.modelState, true);
        }
        this.updateLinks(validatorResult.newToStr, validatorResult.newLink, itemPrefix);
        return true;
    };

    this.viewOptionsForCreating = function(_viewOptions) {
        log("EList viewOptionsForCreating");
        var self = this;
        var newIndex = parseInt(this.getLastIndex()) + 1;
        var itemPrefix = this.options.prefix + "_" + newIndex;
        return $.extend({
            onOk: function(clonedElements) { return self.onCreatingOk(clonedElements, defaultValidateUrl, _viewOptions.type, itemPrefix); },
            onOkClosed: function() { self.fireOnEntityChanged(); },
            onCancelled: null,
            controllerUrl: defaultViewUrl,
            prefix: itemPrefix,
            requestExtraJsonData: this.extraJsonParams(itemPrefix)
        }, _viewOptions);
    };

    this.onCreatingOk = function(clonedElements, validateUrl, runtimeType, itemPrefix) {
        log("EList onCreatingOK"); //Receives url as parameter so it can be overriden when setting viewOptions onOk
        var acceptChanges = this.checkValidation(validateUrl, runtimeType, itemPrefix);
        if (acceptChanges) {
            this.newListItem(clonedElements, runtimeType, itemPrefix);
            this.setItemTicks(itemPrefix);
        }
        return acceptChanges;
    };

    this.newListItem = function(clonedElements, runtimeType, itemPrefix) {
        log("EList newListItem");
        var listInfo = this.staticInfo();
        var itemInfoValue = new RuntimeInfo(itemPrefix).createValue(runtimeType, '', 'n', '');
        listInfo.find().after(hiddenInput(itemPrefix + sfRuntimeInfo, itemInfoValue))
                .after(hiddenDiv(itemPrefix + sfEntity, ''));
        $('#' + itemPrefix + sfEntity).append(clonedElements);

        var select = $(this.pf(''));
        //TODO Anto: When validation returns also toStr: put it in the option
        select.append("\n<option id='" + itemPrefix + sfToStr + "' name='" + itemPrefix + sfToStr + "' value='' class='valueLine'>&nbsp;</option>");
        select.children('option').attr('selected', false); //Fix for Firefox: Set selected after retrieving the html of the select
        select.children('option:last').attr('selected', true);
    };

    this.view = function(_viewOptions) {
        log("EList view");
        var selectedItemPrefix = this.selectedItemPrefix();
        if (empty(selectedItemPrefix))
            return;
        this.viewInIndex(_viewOptions, selectedItemPrefix);
    };

    this.viewInIndex = function(_viewOptions, selectedItemPrefix) {
        log("EList viewInIndex");
        var viewOptions = this.viewOptionsForViewing(_viewOptions, selectedItemPrefix);
        new ViewNavigator(viewOptions).viewOk();
    };

    this.viewOptionsForViewing = function(_viewOptions, itemPrefix) {
        log("EList viewOptionsForViewing");
        var self = this;
        var info = this.itemRuntimeInfo(itemPrefix);
        return $.extend({
            containerDiv: itemPrefix + sfEntity,
            onOk: function() { return self.onViewingOk(defaultValidateUrl, itemPrefix); },
            onCancelled: null,
            controllerUrl: defaultViewUrl,
            type: info.runtimeType(),
            id: info.id(),
            prefix: itemPrefix,
            requestExtraJsonData: this.extraJsonParams(itemPrefix)
        }, _viewOptions);
    };

    this.onViewingOk = function(validateUrl, itemPrefix) {
        log("EList onViewingOk"); //Receives url as parameter so it can be overriden when setting viewOptions onOk
        var acceptChanges = this.checkValidation(validateUrl, this.itemRuntimeInfo(itemPrefix).runtimeType(), itemPrefix);
        if (acceptChanges)
            this.setItemTicks(itemPrefix);
        return acceptChanges;
    };

    this.createFindOptions = function(_findOptions) {
        log("EList createFindOptions");
        var newIndex = parseInt(this.getLastIndex()) + 1;
        var itemPrefix = this.options.prefix + "_" + newIndex;
        var self = this;
        return $.extend({
            prefix: itemPrefix,
            onOk: function(selectedItems) { return self.onFindingOk(selectedItems); },
            onOkClosed: function() { self.fireOnEntityChanged(); },
            allowMultiple: true
        }, _findOptions);
    };

    this.onFindingOk = function(selectedItems) {
        log("EList onFindingOk");
        if (selectedItems == null || selectedItems.length == 0)
            throw "No item was returned from Find Window";
        var lastIndex = parseInt(this.getLastIndex());
        for (var i = 0; i < selectedItems.length; i++) {
            var item = selectedItems[i];
            lastIndex += 1;
            var itemPrefix = this.options.prefix + "_" + lastIndex;

            this.newListItem('', item.type, itemPrefix);
            this.itemRuntimeInfo(itemPrefix).setEntity(item.type, item.id);
            $('#' + itemPrefix + sfToStr).html(item.toStr);

            this.setItemTicks(itemPrefix);
        }
        return true;
    };

    this.remove = function() {
        log("EList remove");
        var selectedItemPrefix = this.selectedItemPrefix();
        if (empty(selectedItemPrefix))
            return;
        this.removeInIndex(selectedItemPrefix);
    };

    this.removeInIndex = function(selectedItemPrefix) {
        log("EList removeInIndex");
        $('#' + selectedItemPrefix + sfRuntimeInfo).remove();
        $('#' + selectedItemPrefix + sfToStr).remove();
        $('#' + selectedItemPrefix + sfEntity).remove();
        $('#' + selectedItemPrefix + sfIndex).remove();
        this.fireOnEntityChanged();
    };

    this.updateButtonsDisplay = function() {
        log("EList updateButtonsDisplay");
        var btnRemove = $(this.pf("_btnRemove"));
        if ($(this.pf(" > option")).length > 0)
            btnRemove.show();
        else
            btnRemove.hide();
    };
};

EList.prototype = new EBaseLine();

function EListOnCreating(_elist, _viewOptions) {
    _elist.create(_viewOptions);
};

function EListOnFinding(_elist, _findOptions) {
    _elist.find(_findOptions);
}

function EListOnViewing(_elist, _viewOptions) {
    _elist.view(_viewOptions);
};

function EListOnRemoving(_elist) {
    _elist.remove();
};

//ERepOptions = EBaseLineOptions + maxElements + removeItemLinkText
var ERep = function(_erepOptions) {
    log("ERep");
    EList.call(this, _erepOptions);

    var defaultViewUrl = "Signum/PartialView";

    this.canAddItems = function() {
        log("ERep canAddItems");
        if (!empty(this.options.maxElements)) {
            if ($(this.pf(sfItemsContainer) + " > div[name$=" + sfRepeaterItem + "]").length >= parseInt(this.options.maxElements))
                return false;
        }
        return true;
    };

    this.getLastIndex = function() {
        log("ERep getLastIndex");
        var lastElement = $(this.pf(sfItemsContainer) + " > div[name$=" + sfRepeaterItem + "]:last");
        var lastIndex = -1;
        if (lastElement.length > 0) {
            var nameSelected = lastElement[0].id;
            lastIndex = nameSelected.substring(this.options.prefix.length + 1, nameSelected.indexOf(sfRepeaterItem));
        }
        return lastIndex;
    };

    this.fireOnEntityChanged = function() {
        log("ERep fireOnEntityChanged");
        this.setTicks();
        if (!empty(this.options.onEntityChanged))
            this.options.onEntityChanged();
    };

    this.typedCreate = function(_viewOptions) {
        log("ERep create");
        if (empty(_viewOptions.type)) throw "ViewOptions type parameter must not be null in ERep typedCreate. Call create instead";
        if (!this.canAddItems()) return;
        var viewOptions = this.viewOptionsForCreating(_viewOptions);
        var template = window[this.options.prefix + "_sfTemplate"];
        if (!empty(template)) { //Template pre-loaded (Embedded Entity): It will be created with "_0" itemprefix => replace it with the current one
            template = template.replace(new RegExp("\"" + this.options.prefix + "_0", "gi"), "\"" + viewOptions.prefix).replace(new RegExp("'" + this.options.prefix + "_0", "gi"), "'" + viewOptions.prefix);
            this.onItemCreated(template, viewOptions);
        }
        else {

            var self = this;
            new ViewNavigator(viewOptions).createEmbedded(function(newHtml) {
                self.onItemCreated(newHtml, viewOptions);
            });
        }
    };

    this.viewOptionsForCreating = function(_viewOptions) {
        log("ERep viewOptionsForCreating");
        var self = this;
        var newIndex = parseInt(this.getLastIndex()) + 1;
        var itemPrefix = this.options.prefix + "_" + newIndex;
        return $.extend({
            containerDiv: "",
            controllerUrl: defaultViewUrl,
            prefix: itemPrefix,
            requestExtraJsonData: this.extraJsonParams(itemPrefix)
        }, _viewOptions);
    };

    this.onItemCreated = function(newHtml, viewOptions) {
        log("ERep onItemCreated");
        if (empty(viewOptions.type)) throw "ViewOptions type parameter must not be null in ERep onItemCreated";
        var itemPrefix = viewOptions.prefix;
        this.newRepItem(newHtml, viewOptions.type, itemPrefix);
        this.fireOnEntityChanged();
        this.setItemTicks(itemPrefix);
    };

    this.newRepItem = function(newHtml, runtimeType, itemPrefix) {
        log("ERep newRepItem");
        var listInfo = this.staticInfo();
        var itemInfoValue = this.itemRuntimeInfo(itemPrefix).createValue(runtimeType, '', 'n', '');
        $(this.pf(sfItemsContainer)).append("\n" +
        "<div id='" + itemPrefix + sfRepeaterItem + "' name='" + itemPrefix + sfRepeaterItem + "' class='repeaterElement'>\n" +
        "<a id='" + itemPrefix + "_btnRemove' title='" + this.options.removeItemLinkText + "' href=\"javascript:ERepOnRemoving(new ERep({prefix:'" + this.options.prefix + "', onEntityChanged:" + (empty(this.options.onEntityChanged) ? "''" : this.options.onEntityChanged) + "}), '" + itemPrefix + "');\" class='lineButton remove'>" + this.options.removeItemLinkText + "</a>\n" +
        hiddenInput(itemPrefix + sfRuntimeInfo, itemInfoValue) +
        //hiddenInput(itemPrefix + sfIndex, (parseInt(lastIndex)+1) + "\" />\n" +
        "<div id='" + itemPrefix + sfEntity + "' name='" + itemPrefix + sfEntity + "'>\n" +
        newHtml + "\n" +
        "</div>\n" + //sfEntity
        "</div>\n" //sfRepeaterItem                        
        );
    };

    this.viewOptionsForViewing = function(_viewOptions, itemPrefix) { //Used in onFindingOk
        log("ERep viewOptionsForViewing");
        return $.extend({
            containerDiv: itemPrefix + sfEntity,
            controllerUrl: defaultViewUrl,
            prefix: itemPrefix,
            requestExtraJsonData: this.extraJsonParams(itemPrefix)
        }, _viewOptions);
    };

    this.find = function(_findOptions, _viewOptions) {
        log("ERep find");
        var _self = this;
        var type = this.getRuntimeType(function(type) {
            _self.typedFind($.extend({ queryUrlName: type }, _findOptions), _viewOptions);
        });
    };

    this.typedFind = function(_findOptions, _viewOptions) {
        log("ERep typedFind");
        if (empty(_findOptions.queryUrlName)) throw "FindOptions queryUrlName parameter must not be null in ERep typedFind. Call find instead";
        if (!this.canAddItems()) return;
        var findOptions = this.createFindOptions(_findOptions, _viewOptions);
        new FindNavigator(findOptions).openFinder();
    },

    this.createFindOptions = function(_findOptions, _viewOptions) {
        log("ERep createFindOptions");
        var newIndex = parseInt(this.getLastIndex()) + 1;
        var itemPrefix = this.options.prefix + "_" + newIndex;
        var self = this;
        return $.extend({
            prefix: itemPrefix,
            onOk: function(selectedItems) { return self.onFindingOk(selectedItems, _viewOptions); },
            onOkClosed: function() { self.fireOnEntityChanged(); },
            allowMultiple: true
        }, _findOptions);
    };

    this.onFindingOk = function(selectedItems, _viewOptions) {
        log("ERep onFindingOk");
        if (selectedItems == null || selectedItems.length == 0)
            throw "No item was returned from Find Window";
        var lastIndex = parseInt(this.getLastIndex());
        for (var i = 0; i < selectedItems.length; i++) {
            if (!this.canAddItems())
                return;

            var item = selectedItems[i];
            lastIndex += 1;
            var itemPrefix = this.options.prefix + "_" + lastIndex;

            this.newRepItem('', item.type, itemPrefix);
            this.itemRuntimeInfo(itemPrefix).setEntity(item.type, item.id);

            //View results in the repeater
            var viewOptions = this.viewOptionsForViewing($.extend(_viewOptions, { type: selectedItems[0].type, id: selectedItems[0].id }), itemPrefix);
            new ViewNavigator(viewOptions).viewEmbedded();

            this.setItemTicks(itemPrefix);
        }
        return true;
    };

    this.remove = function(itemPrefix) {
        log("ERep remove");
        $('#' + itemPrefix + sfRepeaterItem).remove();
        this.fireOnEntityChanged();
    };
};

ERep.prototype = new EList();

function ERepOnCreating(_erep, _viewOptions) {
    _erep.create(_viewOptions);
};

function ERepOnFinding(_erep, _findOptions, _viewOptions) {
    _erep.find(_findOptions, _viewOptions);
}

function ERepOnRemoving(_erep, itemPrefix) {
    _erep.remove(itemPrefix);
};

//EDListOptions = EBaseLineOptions + detailDiv
var EDList = function(_edlistOptions) {
    log("EDList");
    EList.call(this, _edlistOptions);

    this.defaultViewUrl = "Signum/PartialView";

    this.typedCreate = function(_viewOptions) {
        log("EDList create");
        if (empty(_viewOptions.type)) throw "ViewOptions type parameter must not be null in EDList typedCreate. Call create instead";
        this.restoreCurrent();
        var viewOptions = this.viewOptionsForCreating(_viewOptions);
        var template = window[this.options.prefix + "_sfTemplate"];
        if (!empty(template)) { //Template pre-loaded (Embedded Entity): It will be created with "_0" itemprefix => replace it with the current one
            template = template.replace(new RegExp("\"" + this.options.prefix + "_0", "gi"), "\"" + viewOptions.prefix).replace(new RegExp("'" + this.options.prefix + "_0", "gi"), "'" + viewOptions.prefix);
            $('#' + viewOptions.containerDiv).html(template);
        }
        else {
            new ViewNavigator(viewOptions).viewEmbedded();
        }
        this.onItemCreated(viewOptions);
    };

    this.viewOptionsForCreating = function(_viewOptions) {
        log("EDList viewOptionsForCreating");
        var newIndex = parseInt(this.getLastIndex()) + 1;
        var itemPrefix = this.options.prefix + "_" + newIndex;
        return $.extend({
            containerDiv: this.options.detailDiv,
            controllerUrl: this.defaultViewUrl,
            prefix: itemPrefix,
            requestExtraJsonData: this.extraJsonParams(itemPrefix)
        }, _viewOptions);
    };

    this.getVisibleItemPrefix = function() {
        log("EDList getCurrentVisible");
        var detail = $('#' + this.options.detailDiv);
        var firstId = detail.find(":input[id^=" + this.options.prefix + "]:first");
        if (firstId.length == 0)
            return null;
        var id = firstId[0].id;
        var nextSeparator = id.indexOf("_", this.options.prefix.length + 1);
        return id.substring(0, nextSeparator);
    };

    this.restoreCurrent = function() {
        log("EDList restoreCurrent");
        var itemPrefix = this.getVisibleItemPrefix();
        if (!empty(itemPrefix)) {
            $('#' + itemPrefix + sfEntity).html('').append(
            cloneContents(this.options.detailDiv));
        }
    };

    this.onItemCreated = function(viewOptions) {
        log("EDList onItemCreated");
        if (empty(viewOptions.type)) throw "ViewOptions type parameter must not be null in EDList onItemCreated. Call create instead";
        var itemPrefix = viewOptions.prefix;
        this.newListItem('', viewOptions.type, itemPrefix);
        this.fireOnEntityChanged();
        this.setItemTicks(itemPrefix);
    };

    this.view = function(_viewOptions) {
        log("EDList view");
        var selectedItemPrefix = this.selectedItemPrefix();
        if (empty(selectedItemPrefix))
            return;
        this.viewInIndex(_viewOptions, selectedItemPrefix);
    };

    this.viewInIndex = function(_viewOptions, selectedItemPrefix) {
        log("EDList viewInIndex");
        this.restoreCurrent();
        if (this.isLoaded(selectedItemPrefix))
            this.cloneAndShow(selectedItemPrefix);
        else {
            var viewOptions = this.viewOptionsForViewing(_viewOptions, selectedItemPrefix);
            new ViewNavigator(viewOptions).viewEmbedded();
        }
    };

    this.viewOptionsForViewing = function(_viewOptions, itemPrefix) {
        log("EDList viewOptionsForCreating");
        var self = this;
        var info = this.itemRuntimeInfo(itemPrefix);
        return $.extend({
            containerDiv: this.options.detailDiv,
            controllerUrl: this.defaultViewUrl,
            type: info.runtimeType(),
            id: info.id(),
            prefix: itemPrefix,
            requestExtraJsonData: this.extraJsonParams(itemPrefix)
        }, _viewOptions);
    };

    this.isLoaded = function(selectedItemPrefix) {
        log("EDList isLoaded");
        return !empty($('#' + selectedItemPrefix + sfEntity).html());
    };

    this.cloneAndShow = function(selectedItemPrefix) {
        log("EDList cloneAndShow");
        $('#' + this.options.detailDiv).html('').append(
        cloneContents(selectedItemPrefix + sfEntity));
        $('#' + selectedItemPrefix + sfEntity).html('');
    };

    this.find = function(_findOptions, _viewOptions) {
        log("EDList find");
        var _self = this;
        var type = this.getRuntimeType(function(type) {
            _self.typedFind($.extend({ queryUrlName: type }, _findOptions), _viewOptions);
        });
    };

    this.typedFind = function(_findOptions, _viewOptions) {
        log("EDList typedFind");
        if (empty(_findOptions.queryUrlName)) throw "FindOptions queryUrlName parameter must not be null in EDList typedFind. Call find instead";
        this.restoreCurrent();
        var findOptions = this.createFindOptions(_findOptions, _viewOptions);
        new FindNavigator(findOptions).openFinder();
    },

    this.createFindOptions = function(_findOptions, _viewOptions) {
        log("EDList createFindOptions");
        var newIndex = parseInt(this.getLastIndex()) + 1;
        var itemPrefix = this.options.prefix + "_" + newIndex;
        var self = this;
        return $.extend({
            prefix: itemPrefix,
            onOk: function(selectedItems) { return self.onFindingOk(selectedItems, _viewOptions); },
            onOkClosed: function() { self.fireOnEntityChanged(); },
            allowMultiple: true
        }, _findOptions);
    };

    this.onFindingOk = function(selectedItems, _viewOptions) {
        log("EDList onFindingOk");
        if (selectedItems == null || selectedItems.length == 0)
            throw "No item was returned from Find Window";
        var lastIndex = parseInt(this.getLastIndex());
        for (var i = 0; i < selectedItems.length; i++) {
            var item = selectedItems[i];
            lastIndex += 1;
            var itemPrefix = this.options.prefix + "_" + lastIndex;

            this.newListItem('', item.type, itemPrefix);
            this.itemRuntimeInfo(itemPrefix).setEntity(item.type, item.id);
            $('#' + itemPrefix + sfToStr).html(item.toStr);

            //View result in the detailDiv
            $('#' + this.options.prefix).dblclick();

            this.setItemTicks(itemPrefix);
        }
        return true;
    };

    this.EDListRemove = function() {
        log("EDList EDListRemove");
        var selectedItemPrefix = this.selectedItemPrefix();
        if (empty(selectedItemPrefix))
            return;
        this.EDListRemoveInIndex(selectedItemPrefix);
    };

    this.EDListRemoveInIndex = function(itemPrefix) {
        log("EDList removeInIndex");
        var currentVisible = this.getVisibleItemPrefix();
        if (!empty(currentVisible) && currentVisible == itemPrefix)
            $('#' + this.options.detailDiv).html('');
        this.removeInIndex(itemPrefix);
    };
};

EDList.prototype = new EList();

function EDListOnCreating(_edlist, _viewOptions) {
    _edlist.create(_viewOptions);
};

function EDListOnFinding(_edlist, _findOptions, _viewOptions) {
    _edlist.find(_findOptions, _viewOptions);
}

function EDListOnViewing(_edlist, _viewOptions) {
    _edlist.view(_viewOptions);
};

function EDListOnRemoving(_edlist) {
    _edlist.EDListRemove();
};

//EComboOptions = EBaseOptions
var ECombo = function(_ecomboOptions) {
    log("ECombo");
    ELine.call(this, _ecomboOptions);

    this.updateLinks = function(newToStr, newLink) {
        log("ECombo updateLinks");
        $(this.pf(sfCombo) + " option:selected").html(newToStr);
    };

    this.selectedValue = function() {
        log("ECombo selectedValue");
        var selected = $(this.pf(sfCombo + " > option:selected"));
        if (selected.length == 0)
            return null;
        var fullValue = selected.val();
        var separator = fullValue.indexOf(";");
        var value = new Object();
        if (separator == -1) {
            value.runtimeType = this.staticInfo().staticType();
            value.id = fullValue;
        }
        else {
            value.runtimeType = fullValue.substring(0, separator);
            value.id = fullValue.substring(separator + 1, fullValue.length);
        }
        return value;
    };

    this.setSelected = function() {
        log("ECombo setSelected");
        var newValue = this.selectedValue();
        var newRuntimeType = "";
        var newId = "";
        var newEntity = newValue != null && !empty(newValue.id);
        if (newEntity) {
            newRuntimeType = newValue.runtimeType;
            newId = newValue.id;
        }
        var runtimeInfo = this.runtimeInfo();
        runtimeInfo.setEntity(newRuntimeType, newId);
        $(this.pf(sfEntity)).html(''); //Clean
        this.fireOnEntityChanged(newEntity);
    };
};

ECombo.prototype = new ELine();

function EComboOnCreating(_ecombo, _viewOptions) {
    _ecombo.create(_viewOptions);
};

function EComboOnViewing(_ecombo, _viewOptions) {
    _ecombo.view(_viewOptions);
};

function EComboOnChanged(_ecombo) {
    _ecombo.setSelected();
}

//FLineOptions = EBaseOptions
var FLine = function(_flineOptions) {
    log("FLine");
    EBaseLine.call(this, _flineOptions);

    var downloadControllerUrl = 'File/Download';
    var uploadControllerUrl = 'File/Upload';

    this.download = function() {
        log("FLine download");
        var id = this.runtimeInfo().id();
        if (empty(id))
            return;
        window.open($("base").attr("href") + downloadControllerUrl + "?filePathID=" + id);
    };

    this.removeSpecific = function() {
        log("FLine removeSpecific");
        $(this.pf('DivOld')).hide();
        $(this.pf('DivNew')).show();
    };

    this.upload = function() {
        log("FLine upload");
        $(this.pf(''))[0].setAttribute('value', $(this.pf(''))[0].value);
        $(this.pf('loading')).show();
        var mform = $('form');
        var cEncType = mform.attr('enctype');
        var cEncoding = mform.attr('encoding');
        var cTarget = mform.attr('target');
        var cAction = mform.attr('action');
        mform.attr('enctype', 'multipart/form-data').attr('encoding', 'multipart/form-data').attr('target', 'frame' + this.options.prefix).attr('action', uploadControllerUrl).submit();
        mform.attr('enctype', cEncType).attr('encoding', cEncoding).attr('target', cTarget).attr('action', cAction);
    };
};

FLine.prototype = new EBaseLine();

function FLineOnDownloading(_fline) {
    _fline.download();
};

function FLineOnRemoving(_fline) {
    _fline.remove();
}

function FLineOnChanged(_fline) {
    _fline.upload();
}

function hiddenInput(id, value)
{
    return "<input type='hidden' id='" + id + "' name='" + id + "' value='" + value + "' />\n";
};

function hiddenDiv(id, innerHTML) {
    return "<div id='" + id + "' name='" + id + "' style='display:none'>" + innerHTML + "</div>\n";
};

function FullPathNodesSelector(prefix) {
    var pathPrefixes = GetPathPrefixes(prefix);
    var nodes = $("#" + sfRuntimeInfo);
    for (var entry in pathPrefixes) {
        var current = pathPrefixes[entry];
        if (!empty(current))
            nodes = nodes.add(GetSFInfoParams(current));
    }
    return nodes;
};

function GetSFInfoParams(prefix) {
    return $("#" + prefix + sfRuntimeInfo + ", #" + prefix + sfIndex);
}

/*function AutocompleteOnSelected(extendedControlName, newIdAndType, newValue, hasEntity) {
    var prefix = extendedControlName.substr(0, extendedControlName.indexOf(sfToStr));
    var _index = newIdAndType.indexOf("_");
    var info = RuntimeInfoFor(prefix);
    info.setEntity(newIdAndType.substr(_index + 1, newIdAndType.length), newIdAndType.substr(0, _index))
        .ticks(new Date().getTime());
    info.find().after(hiddenDiv(prefix + sfEntity, ''));
               
    //$('#' + prefix + sfId).val(newIdAndType.substr(0, _index));
    //$('#' + prefix + sfRuntimeType).val(newIdAndType.substr(_index + 1, newIdAndType.length));
    $('#' + prefix + sfLink).html($('#' + extendedControlName).val());
    //$('#' + prefix + sfTicks).val(new Date().getTime());

    new ELine({ prefix: prefix }).fireOnEntityChanged(true);
    
    //toggleButtonsDisplay(prefix, hasEntity);
}*/

function AutocompleteOnSelected(controlId, data) {
    var prefix = controlId.substr(0, controlId.indexOf(sfToStr));
    var info = RuntimeInfoFor(prefix);
	info.setEntity(data.type, data.id)
        .ticks(new Date().getTime());
    info.find().after(hiddenDiv(prefix + sfEntity, ''));
	$('#' + prefix + sfLink).html($('#' + controlId).val());
    new ELine({ prefix: prefix }).fireOnEntityChanged(true);	
}

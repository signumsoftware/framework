var EBaseLine = function(_eBaseOptions) {
    this.options = $.extend({
        prefix: "",
        onEntityChanged: null
    }, _eBaseOptions);
};

EBaseLine.prototype = {

    entityInfo: function() {
        return EntityInfoFor(this.options.prefix);
    },

    setTicks: function() {
        log("EBaseLine setTicks");
        this.entityInfo().ticks(new Date().getTime());
    },

    pf: function(s) {
        return "#" + this.options.prefix + s;
    },

    checkValidation: function(validateUrl, runtimeType) {
        log("EBaseLine checkValidation"); //Receives url as parameter so it can be overriden when setting viewOptions onOk
        var info = this.entityInfo();
        var id = (info.find().length > 0) ? info.id() : '';
        var validator = new PartialValidator({ controllerUrl: validateUrl, prefix: this.options.prefix, id: id, type: runtimeType });
        var validatorResult = validator.validate();
        if (!validatorResult.isValid) {
            if (!confirm(lang['popupErrors']))
                return false;
            else {
                this.updateLinks(validatorResult.newToStr, validatorResult.newLink);
                validator.showErrors(validatorResult.modelState, true);
            }
        }
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
        this.entityInfo().runtimeType('').id('').isNew(0);

        this.removeSpecific();
        this.fireOnEntityChanged(false);
    },

    getRuntimeType: function(_onTypeFound) {
        log("EBaseLine getRuntimeType");
        var implSelector = this.pf(sfImplementations);
        var impl = $(implSelector);
        if (impl.length == 0)
            return _onTypeFound(this.entityInfo().staticType());

        var implementations = $(implSelector + " :button");
        if (implementations.length == 1)
            return _onTypeFound(implementations[0].id);

        typeSelector(this.options.prefix, _onTypeFound);
    },

    create: function(_viewOptions) {
        log("EBaseLine create");
        var _self = this;
        var type = this.getRuntimeType(function(type) {
            _self.typedCreate($.extend(_viewOptions, { type: type }));
        });
    },

    typedCreate: function(_viewOptions) {
        log("EBaseline create");
        if (empty(_viewOptions.type)) throw "ViewOptions type parameter must not be null in EBaseline typedCreate. Call create instead";
        var viewOptions = this.viewOptionsForCreating(_viewOptions);
        new ViewNavigator(viewOptions).createOk();
    },

    find: function(_findOptions) {
        log("EBaseLine find");
        var _self = this;
        var type = this.getRuntimeType(function(type) {
            _self.typedFind($.extend(_findOptions, { queryUrlName: type }));
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

        //If Embedded Entity => send path of runtimes and ids to be able to construct a typecontext
        if (EntityInfoFor(this.options.prefix).isEmbedded()) {
            var pathInfo = FullPathNodesSelector(this.options.prefix);
            for (var i = 0; i < pathInfo.length; i++) {
                var node = pathInfo[i];
                extraParams[node.id] = node.value;
            }
        }

        //If reactive => send reactive flag, tabId, and Id & Runtime of the main entity
        if ($('#' + sfReactive).length > 0) {
            extraParams.sfReactive = true;
            extraParams.sfTabId = $('#' + sfTabId).val();
            var mainEntityInfo = EntityInfoFor('');
            extraParams.sfRuntimeType = mainEntityInfo.runtimeType();
            extraParams.sfId = mainEntityInfo.id();
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
            txt.hide(); btnCreate.hide(); btnFind.hide();
            link.show(); btnRemove.show(); btnView.show();
        }
        else {
            link.hide(); btnRemove.hide(); btnView.hide();
            txt.show(); btnCreate.show(); btnFind.show();
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
        var info = this.entityInfo();
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
        var acceptChanges = this.checkValidation(validateUrl, this.entityInfo().runtimeType());
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
            onCancelled: null,
            controllerUrl: defaultViewUrl,
            prefix: this.options.prefix,
            requestExtraJsonData: this.extraJsonParams()
        }, _viewOptions);
    };

    this.newEntity = function(clonedElements, runtimeType) {
        var info = this.entityInfo();
        info.runtimeType(runtimeType).isNew(1).find()
            .after(hiddenDiv(this.options.prefix + sfEntity, ''));
        $(this.pf(sfEntity)).append(clonedElements);
    };

    this.onCreatingOk = function(clonedElements, validateUrl, runtimeType) {
        log("ELine onCreatingOk"); //Receives url as parameter so it can be overriden when setting viewOptions onOk
        var acceptChanges = this.checkValidation(validateUrl, runtimeType);
        if (acceptChanges) {
            this.newEntity(clonedElements, runtimeType);
            this.fireOnEntityChanged(true);
        }
        return acceptChanges;
    };

    this.createFindOptions = function(_findOptions) {
        log("ELine createFindOptions");
        var self = this;
        return $.extend({
            prefix: this.options.prefix,
            onOk: function(selectedItems) { return self.onFindingOk(selectedItems); },
            allowMultiple: false
        }, _findOptions);
    };

    this.onFindingOk = function(selectedItems) {
        log("ELine onFindingOk");
        if (selectedItems == null || selectedItems.length != 1)
            throw "No item or more than one item was returned from Find Window";
        var info = this.entityInfo();
        info.id(selectedItems[0].id).runtimeType(selectedItems[0].type);
        if ($(this.pf(sfEntity)).length == 0)
            info.find().after(hiddenDiv(this.options.prefix + sfEntity, ''));
        $(this.pf(sfToStr)).val(''); //Clean
        $(this.pf(sfLink)).html(selectedItems[0].toStr).attr('href', selectedItems[0].link);
        this.fireOnEntityChanged(true);
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
        new ViewNavigator(viewOptions).viewEmbedded();
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
        this.entityInfo().runtimeType(runtimeType).isNew(1);
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
            _self.typedFind($.extend(_findOptions, { queryUrlName: type }), _viewOptions);
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
            allowMultiple: false
        }, _findOptions);
    };

    this.onFindingOk = function(selectedItems, _viewOptions) {
        log("EDLine onFindingOk");
        if (selectedItems == null || selectedItems.length != 1)
            throw "No item or more than one item was returned from Find Window";
        this.entityInfo().id(selectedItems[0].id).runtimeType(selectedItems[0].type);

        //View result in the detailDiv
        var viewOptions = this.viewOptionsForCreating($.extend(_viewOptions, { type: selectedItems[0].type, id: selectedItems[0].id }));
        new ViewNavigator(viewOptions).viewEmbedded();

        this.fireOnEntityChanged(true);
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
        log("EBaseLine extraJsonParams");
        var extraParams = new Object();

        //If Embedded Entity => send path of runtimes and ids to be able to construct a typecontext
        var info = EntityInfoFor(itemPrefix);
        if (info.find().length == 0)
            info = EntityInfoFor(this.options.prefix); //If new item, there will be no sfInfo for it. Use list sfInfo instead
        if (info.isEmbedded()) {
            var pathInfo = FullPathNodesSelector(itemPrefix);
            for (var i = 0; i < pathInfo.length; i++) {
                var node = pathInfo[i];
                extraParams[node.id] = node.value;
            }
        }

        //If reactive => send reactive flag, tabId, and Id & Runtime of the main entity
        if ($('#' + sfReactive).length > 0) {
            extraParams.sfReactive = true;
            extraParams.sfTabId = $('#' + sfTabId).val();
            var mainEntityInfo = EntityInfoFor('');
            extraParams.sfRuntimeType = mainEntityInfo.runtimeType();
            extraParams.sfId = mainEntityInfo.id();
        }

        return extraParams;
    };

    this.setItemTicks = function(itemPrefix) {
        log("EList setItemTicks");
        this.itemEntityInfo(itemPrefix).ticks(new Date().getTime());
    };

    this.itemEntityInfo = function(itemPrefix) {
        return EntityInfoFor(itemPrefix);
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
        var info = this.itemEntityInfo(itemPrefix);
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
            this.fireOnEntityChanged();
            this.setItemTicks(itemPrefix);
        }
        return acceptChanges;
    };

    this.newListItem = function(clonedElements, runtimeType, itemPrefix) {
        log("EList newListItem");
        var listInfo = this.entityInfo();
        var itemInfoValue = new EntityInfo(itemPrefix).createValue(listInfo.staticType(), runtimeType, '', listInfo.isEmbedded(), 1, '');
        listInfo.find().after(hiddenInput(itemPrefix + sfInfo, itemInfoValue))
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
        var info = this.itemEntityInfo(itemPrefix);
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
        var acceptChanges = this.checkValidation(validateUrl, this.itemEntityInfo(itemPrefix).runtimeType(), itemPrefix);
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
            this.itemEntityInfo(itemPrefix).isNew(0).id(item.id);
            $('#' + itemPrefix + sfToStr).html(item.toStr);

            this.fireOnEntityChanged();
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
        $('#' + selectedItemPrefix + sfInfo).remove();
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
            if ($(this.pf(sfItemsContainer) + " > div[name$=" + sfRepeaterItem + "]").length >= parseInt(maxElements))
                return false;
        }
        return true;
    };

    this.itemEntityInfo = function(itemPrefix) {
        return EntityInfoFor(itemPrefix);
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
        var self = this;
        new ViewNavigator(viewOptions).createEmbedded(function(newHtml) {
            self.onItemCreated(newHtml, viewOptions);
        });
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
        var listInfo = this.entityInfo();
        var itemInfoValue = this.itemEntityInfo(itemPrefix).createValue(listInfo.staticType(), runtimeType, '', listInfo.isEmbedded(), 1, '');
        $(this.pf(sfItemsContainer)).append("\n" +
        "<div id='" + itemPrefix + sfRepeaterItem + "' name='" + itemPrefix + sfRepeaterItem + "' class='repeaterElement'>\n" +
        "<a id='" + itemPrefix + "_btnRemove' title='" + this.options.removeItemLinkText + "' href=\"javascript:ERepOnRemoving(new ERep({prefix:'" + this.options.prefix + "', onEntityChanged:"+(empty(this.options.onEntityChanged) ? "''" : this.options.onEntityChanged)+"}), '" + itemPrefix + "');\" class='lineButton remove'>" + this.options.removeItemLinkText + "</a>\n" +
        hiddenInput(itemPrefix + sfInfo, itemInfoValue) +
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
            _self.typedFind($.extend(_findOptions, { queryUrlName: type }), _viewOptions);
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
            this.itemEntityInfo(itemPrefix).isNew(0).id(item.id);

            //View results in the repeater
            var viewOptions = this.viewOptionsForViewing($.extend(_viewOptions, { type: selectedItems[0].type, id: selectedItems[0].id }), itemPrefix);
            new ViewNavigator(viewOptions).viewEmbedded();

            this.fireOnEntityChanged();
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
        new ViewNavigator(viewOptions).viewEmbedded();
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
        var info = this.itemEntityInfo(itemPrefix);
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
            _self.typedFind($.extend(_findOptions, { queryUrlName: type }), _viewOptions);
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
            this.itemEntityInfo(itemPrefix).isNew(0).id(item.id);
            $('#' + itemPrefix + sfToStr).html(item.toStr);

            //View result in the detailDiv
            $('#' + this.options.prefix).dblclick();

            this.fireOnEntityChanged();
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

    this.selectedId = function() {
        log("ECombo selectedId");
        var selected = $(this.pf(sfCombo + " > option:selected"));
        if (selected.length == 0)
            return null;
        return selected.val();
    };

    this.setSelected = function() {
        log("ECombo setSelected");
        var newId = this.selectedId();
        var newRuntimeType = "";
        var newEntity = false;
        var info = this.entityInfo();
        if (empty(newId))
            newId = ""; //Avoid setting null value
        else {
            newRuntimeType = info.staticType();
            newEntity = true;
        }
        var oldId = info.id();
        if (empty(oldId))
            oldId = ""; //Avoid setting null value
        if (newId == oldId)
            return;
        info.id(newId).runtimeType(newRuntimeType).isNew(0);
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

function EComboOnChanged(_ecomboOptions) {
    var ec = new ECombo(_ecomboOptions);
    ec.setSelected();
}

function hiddenInput(id, value)
{
    return "<input type='hidden' id='" + id + "' name='" + id + "' value='" + value + "' />\n";
};

function hiddenDiv(id, innerHTML) {
    return "<div id='" + id + "' name='" + id + "' style='display:none'>" + innerHTML + "</div>\n";
};

var debug = true;
function log(s) {
    if (debug) {
        if (typeof console != "undefined" && typeof console.debug != "undefined")
            console.log(s);
        //else
        //    alert(s);
    }
}

function FullPathNodesSelector(prefix) {
    var pathPrefixes = GetPathPrefixes(prefix);
    var nodes = $("#" + sfInfo);
    for (var entry in pathPrefixes) {
        var current = pathPrefixes[entry];
        if (!empty(current))
            nodes = nodes.add(GetSFInfoParams(current));
    }
    return nodes;
};

function GetSFInfoParams(prefix) {
    return $("#" + prefix + sfInfo + ", #" + prefix + sfIndex);
}

function AutocompleteOnSelected(extendedControlName, newIdAndType, newValue, hasEntity) {
    var prefix = extendedControlName.substr(0, extendedControlName.indexOf(sfToStr));
    var _index = newIdAndType.indexOf("_");
    $('#' + prefix + sfId).val(newIdAndType.substr(0, _index));
    $('#' + prefix + sfRuntimeType).val(newIdAndType.substr(_index + 1, newIdAndType.length));
    $('#' + prefix + sfLink).html($('#' + extendedControlName).val());
    $('#' + prefix + sfTicks).val(new Date().getTime());
    toggleButtonsDisplay(prefix, hasEntity);
}
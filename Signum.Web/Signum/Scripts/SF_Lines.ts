/// <reference path="SF_Utils.ts"/>
/// <reference path="SF_Globals.ts"/>


module SF.Lines {
    export class EntityBaseOptions {
        prefix: string;
        partialViewName: string;
        onEntityChanged: () => any
    }

    export class EntityBase {
        options: EntityBaseOptions;

        constructor(_options: EntityBaseOptions) {
            this.options = $.extend({
                prefix: "",
                partialViewName: "",
                onEntityChanged: null
            }, _options);
        }

        keys = {
            entity: "sfEntity"
        };


        _create(ev, ui) {
            var $txt = $(this.pf(SF.Keys.toStr) + ".sf-entity-autocomplete");
            if ($txt.length > 0) {
                var data = $txt.data();
                this.entityAutocomplete($txt, { delay: 200, types: data.types, url: data.url || SF.Urls.autocomplete, count: 5 });
            }


        }


        runtimeInfo() {
            return new SF.RuntimeInfo(this.options.prefix);
        }

        staticInfo() {
            return SF.StaticInfo(this.options.prefix);
        }

        pf(s) {
            return "#" + SF.compose(this.options.prefix, s);
        }


        checkValidation(validatorOptions) {
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
                }
                else {
                    validator.showErrors(validatorResult.modelState, true);
                }
            }
            this.updateLinks(validatorResult.newToStr, validatorResult.newLink);
            $.extend(validatorResult, { acceptChanges: true });
            return validatorResult;
        }

        updateLinks(newToStr, newLink) {
            //Abstract function
        }


        fireOnEntityChanged(hasEntity) {
            this.updateButtonsDisplay(hasEntity);
            if (!SF.isEmpty(this.options.onEntityChanged)) {
                this.options.onEntityChanged();
            }
        }

        remove() {
            $(this.pf(SF.Keys.toStr)).val("").removeClass(SF.Validator.inputErrorClass);
            $(this.pf(SF.Keys.link)).val("").html("").removeClass(SF.Validator.inputErrorClass);
            this.runtimeInfo().removeEntity();

            this.removeSpecific();
            this.fireOnEntityChanged(false);
        }

        getEntityType(_onTypeFound) {
            var types = this.staticInfo().types().split(",");
            if (types.length == 1) {
                return _onTypeFound(types[0]);
            }

            SF.openTypeChooser(this.options.prefix, _onTypeFound);
        }

        create(_viewOptions) {
            var _self = this;
            var type = this.getEntityType(function (type) {
                _self.typedCreate($.extend({ type: type }, _viewOptions));
            });
        }

        typedCreate(_viewOptions) {
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
            }
            else {
                new SF.ViewNavigator(viewOptions).createOk();
            }
        }

        getEmbeddedTemplate(viewOptions) {
            return window[SF.compose(this.options.prefix, "sfTemplate")];
        }

        find(_findOptions) {
            var _self = this;
            var type = this.getEntityType(function (type) {
                _self.typedFind($.extend({ webQueryName: type }, _findOptions));
            });
        }

        typedFind(_findOptions) {
            if (SF.isEmpty(_findOptions.webQueryName)) {
                throw "FindOptions webQueryName parameter must not be null in EBaseline typedFind. Call find instead";
            }
            var findOptions = this.createFindOptions(_findOptions);
            SF.FindNavigator.openFinder(findOptions);
        }

        extraJsonParams(_prefix) {
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
        }

        updateButtonsDisplay(hasEntity) {
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


        entityAutocomplete($elem, options) {
            var lastXhr; //To avoid previous requests results to be shown
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
                    self.onAutocompleteSelected(controlId, ui.item.value);
                    event.preventDefault();
                },
            });

            auto.data("uiAutocomplete")._renderItem = function (ul, item) {
                return $("<li>")
                    .attr("data-type", item.value.type)
                    .attr("data-id", item.value.id)
                    .append($("<a>").text(item.label))
                    .appendTo(ul);
            };
        }

    }


    export class EntityLine extends EntityBase {

        updateLinks(newToStr, newLink) {
            var link = $(this.pf(SF.Keys.link));
            link.html(newToStr);
            if (link.filter('a').length !== 0)
                link.attr('href', newLink);
            $(this.pf(SF.Keys.toStr)).val('');
        }

        view(_viewOptions) {
            var viewOptions = this.viewOptionsForViewing(_viewOptions);
            new SF.ViewNavigator(viewOptions).viewOk();
        }

        viewOptionsForViewing(_viewOptions) {
            var self = this;
            var info = this.runtimeInfo();
            return $.extend({
                containerDiv: SF.compose(this.options.prefix, self.keys.entity),
                onOk: function () { return self.onViewingOk(_viewOptions.validationOptions); },
                onOkClosed: function () { self.fireOnEntityChanged(true); },
                type: info.entityType(),
                id: info.id(),
                prefix: this.options.prefix,
                partialViewName: this.options.partialViewName,
                requestExtraJsonData: this.extraJsonParams()
            }, _viewOptions);
        }


        newEntity(clonedElements, item) {
            var info = this.runtimeInfo();
            if (typeof item.runtimeInfo != "undefined") {
                info.find().val(item.runtimeInfo);
            }
            else {
                info.setEntity(item.type, item.id || '');
            }

            if ($(this.pf(this.keys.entity)).length == 0) {
                info.find().after(SF.hiddenDiv(SF.compose(this.options.prefix, this.keys.entity), ""));
            }
            $(this.pf(this.keys.entity)).append(clonedElements);

            $(this.pf(SF.Keys.toStr)).val(''); //Clean
            if (typeof item.toStr != "undefined" && typeof item.link != "undefined") {
                $(this.pf(SF.Keys.link)).html(item.toStr).attr('href', item.link);
            }
        }

        onCreatingOk(clonedElements, validatorOptions, entityType) {
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
        }

        createFindOptions(_findOptions) {
            var self = this;
            return $.extend({
                prefix: this.options.prefix,
                onOk: function (selectedItems) { return self.onFindingOk(selectedItems); },
                onOkClosed: function () { self.fireOnEntityChanged(true); }
            }, _findOptions);
        }

        onFindingOk(selectedItems) {
            if (selectedItems == null || selectedItems.length != 1) {
                window.alert(lang.signum.onlyOneElement);
                return false;
            }
            this.newEntity('', selectedItems[0]);
            return true;
        }

        onAutocompleteSelected(controlId, data) {
            var selectedItems = [{
                id: data.id,
                type: data.type,
                toStr: data.text,
                link: data.link
            }];
            this.onFindingOk(selectedItems);
            this.fireOnEntityChanged(true);
        }


        removeSpecific() {
            $(this.pf(this.keys.entity)).remove();
        }
    }

    export class  
}



import * as React from 'react'
import { Route } from 'react-router'
import * as ReactBootstrap from 'react-bootstrap'
import * as ReactRouterBootstrap from 'react-router-bootstrap'
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import * as Search from '../../../Framework/Signum.React/Scripts/Search'
import { ValueSearchControlLine } from '../../../Framework/Signum.React/Scripts/Search'
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import * as EntityOperations from '../../../Framework/Signum.React/Scripts/Operations/EntityOperations'
import { TypeContext } from '../../../Framework/Signum.React/Scripts/TypeContext'
import { isTypeEntity, getTypeInfo, } from '../../../Framework/Signum.React/Scripts/Reflection'
import { Entity, ModifiableEntity } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeEntity } from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor'
import SelectorModal from '../../../Framework/Signum.React/Scripts/SelectorModal'
import { ViewReplacer } from '../../../Framework/Signum.React/Scripts/Frames/ReactVisitor';
import * as Lines from '../../../Framework/Signum.React/Scripts/Lines'
import { ValueLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater } from '../../../Framework/Signum.React/Scripts/Lines'
import { DynamicViewEntity, DynamicViewSelectorEntity, DynamicViewOverrideEntity, DynamicViewMessage, DynamicViewOperation } from './Signum.Entities.Dynamic'
import DynamicViewEntityComponent from './View/DynamicViewEntity' //Just Typing
import * as DynamicClient from './DynamicClient'

import * as DynamicViewComponent from './View/DynamicViewComponent'
import { DynamicViewComponentProps, DynamicViewPart } from './View/DynamicViewComponent'
import { AuthInfo } from './View/AuthInfo'
import * as Nodes from './View/Nodes' //Typings-only


export function start(options: { routes: JSX.Element[] }) {
    
    Navigator.addSettings(new EntitySettings(DynamicViewEntity, w => new ViewPromise(resolve => require(['./View/DynamicViewEntity'], resolve))));
    Navigator.addSettings(new EntitySettings(DynamicViewSelectorEntity, w => new ViewPromise(resolve => require(['./View/DynamicViewSelector'], resolve))));
    Navigator.addSettings(new EntitySettings(DynamicViewOverrideEntity, w => new ViewPromise(resolve => require(['./View/DynamicViewOverride'], resolve))));

    DynamicClient.Options.onGetDynamicLineForType.push((ctx, type) => <ValueSearchControlLine ctx={ctx} findOptions={{ queryName: DynamicViewEntity, parentColumn: "EntityType.CleanName", parentValue: type }} />);
    DynamicClient.Options.onGetDynamicLineForType.push((ctx, type) => <ValueSearchControlLine ctx={ctx} findOptions={{ queryName: DynamicViewSelectorEntity, parentColumn: "EntityType.CleanName", parentValue: type }} />);

    Operations.addSettings(new EntityOperationSettings(DynamicViewOperation.Save, {
        onClick: ctx => {
            (ctx.frame.entityComponent as DynamicViewEntityComponent).beforeSave();
            EntityOperations.defaultExecuteEntity(ctx);
        }
    }));

    Navigator.setViewDispatcher(new DynamicViewViewDispatcher());
}

export class DynamicViewViewDispatcher implements Navigator.ViewDispatcher {

    hasView(typeName: string) {
        return true;
    }

    getView(entity: ModifiableEntity) {

        if (!isTypeEntity(entity.Type))
            return this.fallback(entity);

        return ViewPromise.flat(getSelector(entity.Type).then(sel => {

            if (!sel)
                return this.fallback(entity);

            var viewName = sel(entity as Entity, new AuthInfo());

            if (viewName == "STATIC")
                return this.static(entity);

            if (viewName == "NEW")
                return ViewPromise.flat(createDefaultDynamicView(entity.Type).then(dv => this.dynamicComponent(dv)));

            if (viewName == "CHOOSE")
                return ViewPromise.flat(this.chooseDynamicView(entity.Type, true).then(dv => this.dynamicComponent(dv)));

            return ViewPromise.flat(API.getDynamicView(entity.Type, viewName).then(dv => this.dynamicComponent(dv)));
        }));
    }

    dynamicComponent(promiseDv: DynamicViewEntity): ViewPromise<ModifiableEntity> {
        return new ViewPromise(resolve => require(['./View/DynamicViewComponent'], resolve))
            .withProps({ initialDynamicView: promiseDv });
    }

    fallback(entity: ModifiableEntity): ViewPromise<ModifiableEntity> {
        const settings = Navigator.getSettings(entity.Type) as EntitySettings<ModifiableEntity>;

        if (!settings || !settings.getViewPromise)
            return ViewPromise.flat(this.chooseDynamicView(entity.Type, true).then(dv => this.dynamicComponent(dv)));

        var staticViewPromise = settings.getViewPromise(entity).applyViewOverrides(settings);

        return this.applyDynamicViewOverride(entity.Type, staticViewPromise);
    }

    static(entity: ModifiableEntity): ViewPromise<ModifiableEntity> {
        const settings = Navigator.getSettings(entity.Type) as EntitySettings<ModifiableEntity>;
        
        if (!settings)
            throw new Error(`No EntitySettings registered for ${entity.Type}`);

        if (!settings.getViewPromise)
            throw new Error(`The EntitySettings registered for ${entity.Type} has not getViewPromise`);
        var staticViewPromise = settings.getViewPromise(entity).applyViewOverrides(settings);

        return this.applyDynamicViewOverride(entity.Type, staticViewPromise);
    }

    applyDynamicViewOverride(typeName: string, staticViewPromise: ViewPromise<ModifiableEntity>): ViewPromise<ModifiableEntity> {

        if (!isTypeEntity(typeName))
            return staticViewPromise;


        return ViewPromise.flat(getViewOverride(typeName).then(viewOverride => {
            if (viewOverride == undefined)
                return staticViewPromise;

            staticViewPromise.promise = staticViewPromise.promise.then(func => {
                return (ctx: TypeContext<Entity>) => {
                    var result = func(ctx);
                    var component = result.type as React.ComponentClass<{ ctx: TypeContext<Entity> }>;
                    patchComponent(component, viewOverride);
                    return result;
                };
            });

            return staticViewPromise;
        }));
    }

    chooseDynamicView(typeName: string, avoidMessage = false) {
        return getViewNames(typeName)
            .then(names => SelectorModal.chooseElement(names, {
                title: DynamicViewMessage.ChooseAView.niceToString(),
                message: avoidMessage ? undefined : DynamicViewMessage.SinceThereIsNoDynamicViewSelectorYouNeedToChooseAViewManually.niceToString(),
            })).then(viewName => {
                return this.getOrCreateDynamicView(typeName, viewName);
            });
    }

    getOrCreateDynamicView(typeName: string, viewName: string | undefined): Promise<DynamicViewEntity> {

        if (viewName == undefined)
            return createDefaultDynamicView(typeName);

        return API.getDynamicView(typeName, viewName);
    }

}

export function patchComponent(component: React.ComponentClass < { ctx: TypeContext<Entity> }>, viewOverride: (e: ViewReplacer<Entity>, auth: AuthInfo) => void) {

    if (!component.prototype.render)
        throw new Error("render function not defined in " + component);

    if (component.prototype.render.isDynamic)
        return;

    const staticRender = component.prototype.render as () => void;

    component.prototype.render = function (this: React.Component<any, any>) {

        const ctx = this.props.ctx;

        const view = staticRender.call(this);

        const replacer = new ViewReplacer<Entity>(view, ctx);
        try {
            viewOverride(replacer, new AuthInfo());
            return replacer.result;
        } catch (error) {
            return <div className="alert alert-danger">ERROR: {error && error.message}</div>;
        }
    };

    component.prototype.render.isDynamic = true;
    component.prototype.render.staticRender = staticRender;
}

export function unPatchComponent(component: React.ComponentClass<{ ctx: TypeContext<Entity> }>) {

    if (!component.prototype.render)
        throw new Error("render function not defined in " + component);

    if (!component.prototype.render.isDynamic)
        return;

    component.prototype.render = component.prototype.render.staticRender;
}

function getOrCreate<V>(cache: { [key: string]: V }, key: string, onCreate: (key: string) => Promise<V>): Promise<V> {

    if ((cache as Object).hasOwnProperty(key))
        return Promise.resolve(cache[key]);

    return onCreate(key).then(v => cache[key] = v);
}

const viewNamesCache: { [typeName: string]: string[] } = {};
export function getViewNames(typeName: string): Promise<string[]> {

    return getOrCreate(viewNamesCache, typeName, () =>
        API.getDynamicViewNames(typeName)
    );
}

const selectorCache: { [typeName: string]: ((e: Entity, auth: AuthInfo) => string) | undefined } = {};
export function getSelector(typeName: string): Promise<((e: Entity, auth: AuthInfo) => string) | undefined> {

    return getOrCreate(selectorCache, typeName, () =>
        API.getDynamicViewSelector(typeName)
            .then(dvs => dvs && asSelectorFunction(dvs))
    );
}

export function asSelectorFunction(dvs: DynamicViewSelectorEntity): (e: Entity, auth: AuthInfo) => string {
    let code = dvs.script!;

    if (!code.contains(";") && !code.contains("return"))
        code = "return " + code + ";";

    code = "(function(e, auth){ " + code + "})";

    try {
        return eval(code);
    } catch (e) {
        throw new Error("Syntax in DynamicViewSelector for '" + dvs.entityType!.toStr + "':\r\n" + code + "\r\n" + (e as Error).message);
    }
}

const overrideCache: { [typeName: string]: ((e: ViewReplacer<Entity>, auth: AuthInfo) => string) | undefined } = {};
export function getViewOverride(typeName: string): Promise<((rep: ViewReplacer<Entity>, auth: AuthInfo) => void) | undefined> {

    return getOrCreate(overrideCache, typeName, () =>
        API.getDynamicViewOverride(typeName)
            .then(dvr => dvr && asOverrideFunction(dvr))
    );
}


export function asOverrideFunction(dvr: DynamicViewOverrideEntity): (e: ViewReplacer<Entity>, auth: AuthInfo) => string {
    let code = dvr.script!;

    // Lines
    var ValueLine = Lines.ValueLine;
    var EntityLine = Lines.EntityLine;
    var EntityCombo = Lines.EntityCombo;
    var EnumCheckboxList = Lines.EnumCheckboxList;
    var EntityCheckboxList = Lines.EntityCheckboxList;
    var EntityDetail = Lines.EntityDetail;
    var EntityList = Lines.EntityList;
    var EntityRepeater = Lines.EntityRepeater;
    var EntityTabRepeater = Lines.EntityTabRepeater;
    var EntityStrip = Lines.EntityStrip;
    var EntityTable = Lines.EntityTable;

    // Search
    var ValueSearchControlLine = Search.ValueSearchControlLine;

    // ReactBootstrap
    var Accordion = ReactBootstrap.Accordion;
    var Badge = ReactBootstrap.Badge;
    var Button = ReactBootstrap.Button;
    var ButtonGroup = ReactBootstrap.ButtonGroup;
    var ButtonToolbar = ReactBootstrap.ButtonToolbar;
    var Carousel = ReactBootstrap.Carousel;
    var Checkbox = ReactBootstrap.Checkbox;
    var Collapse = ReactBootstrap.Collapse;
    var Dropdown = ReactBootstrap.Dropdown;
    var DropdownButton = ReactBootstrap.DropdownButton;
    var DropdownMenu = ReactBootstrap.DropdownMenu;
    var DropdownToggle = ReactBootstrap.DropdownToggle;
    var FormGroup = ReactBootstrap.FormGroup;
    var Image = ReactBootstrap.Image;
    var Label = ReactBootstrap.Label;
    var ListGroup = ReactBootstrap.ListGroup;
    var MenuItem = ReactBootstrap.MenuItem;
    var Nav = ReactBootstrap.Nav;
    var NavbarBrand = ReactBootstrap.NavbarBrand;
    var NavDropdown = ReactBootstrap.NavDropdown;
    var Overlay = ReactBootstrap.Overlay;
    var Tabs = ReactBootstrap.Tabs;
    var Tab = ReactBootstrap.Tab;
    var Tooltip = ReactBootstrap.Tooltip;
    var ProgressBar = ReactBootstrap.ProgressBar;

    // ReactRouterBootstrap
    var LinkContainer = ReactRouterBootstrap.LinkContainer;
    var IndexLinkContainer = ReactRouterBootstrap.IndexLinkContainer;

    // Custom
    var DynamicViewPart = DynamicViewComponent.DynamicViewPart;

    code = "(function(vr, auth){ " + code + "})";

    try {
        return eval(code);
    } catch (e) {
        throw new Error("Syntax in DynamicViewOverride for '" + dvr.entityType!.toStr + "':\r\n" + code + "\r\n" + (e as Error).message);
    }
}

export function createDefaultDynamicView(typeName: string): Promise<DynamicViewEntity> {
    return loadNodes().then(nodes =>
        Navigator.API.getType(typeName).then(t => DynamicViewEntity.New(dv => {
            dv.entityType = t;
            dv.viewName = "My View";
            const node = nodes.NodeConstructor.createDefaultNode(getTypeInfo(typeName));
            dv.viewContent = JSON.stringify(node);
        })));
}

export function loadNodes(): Promise<typeof Nodes> {
    return new Promise<typeof Nodes>(resolve => require(["./View/Nodes"], resolve));
}


export namespace API {
    
    export function getDynamicView(typeName: string, viewName: string): Promise<DynamicViewEntity> {
        
            var url = Navigator.currentHistory.createHref({
                pathname: `~/api/dynamic/view/${typeName}`,
                query: { viewName }
            });

            return ajaxGet<DynamicViewEntity>({ url });
    }

    export function getDynamicViewSelector(typeName: string): Promise<DynamicViewSelectorEntity | undefined> {
        return ajaxGet<DynamicViewSelectorEntity>({ url: `~/api/dynamic/selector/${typeName}` });
    }

    export function getDynamicViewOverride(typeName: string): Promise<DynamicViewOverrideEntity | undefined> {
        return ajaxGet<DynamicViewOverrideEntity>({ url: `~/api/dynamic/override/${typeName}` });
    }
    
    export function getDynamicViewNames(typeName: string): Promise<string[]> {
        return ajaxGet<string[]>({ url: `~/api/dynamic/viewNames/${typeName}` });
    }

    export function getSuggestedFindOptions(typeName: string): Promise<SuggestedFindOptions[]> {
        return ajaxGet<SuggestedFindOptions[]>({ url: `~/api/dynamic/suggestedFindOptions/${typeName}` });
    }
}

export interface SuggestedFindOptions {
    queryKey: string;
    parentColumn: string;
}


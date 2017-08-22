
import * as React from 'react'
import { Route } from 'react-router'
import * as Reactstrap from 'reactstrap'
import * as ReactRouterBootstrap from 'react-router-bootstrap'
import * as QueryString from 'query-string'
import { globalModules} from './View/GlobalModules'
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import * as Search from '../../../Framework/Signum.React/Scripts/Search'
import { ValueSearchControlLine } from '../../../Framework/Signum.React/Scripts/Search'
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { TypeContext } from '../../../Framework/Signum.React/Scripts/TypeContext'
import { isTypeEntity, getTypeInfo, PropertyRoute } from '../../../Framework/Signum.React/Scripts/Reflection'
import { Entity, ModifiableEntity } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeEntity } from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor'
import SelectorModal from '../../../Framework/Signum.React/Scripts/SelectorModal'
import { ViewReplacer } from '../../../Framework/Signum.React/Scripts/Frames/ReactVisitor';
import * as Lines from '../../../Framework/Signum.React/Scripts/Lines'
import * as FileLineModule from '../Files/FileLine'
import { ValueLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater } from '../../../Framework/Signum.React/Scripts/Lines'
import { DynamicViewEntity, DynamicViewSelectorEntity, DynamicViewOverrideEntity, DynamicViewMessage, DynamicViewOperation, DynamicViewSelectorOperation } from './Signum.Entities.Dynamic'
import DynamicViewEntityComponent from './View/DynamicView' //Just Typing
import * as DynamicClient from './DynamicClient'

import * as DynamicViewComponent from './View/DynamicViewComponent'
import { DynamicViewComponentProps, DynamicViewPart } from './View/DynamicViewComponent'
import * as Nodes from './View/Nodes' //Typings-only
import * as NodeUtils from './View/NodeUtils' //Typings-only
import MessageModal from "../../../Framework/Signum.React/Scripts/Modals/MessageModal";
import { Dic } from "../../../Framework/Signum.React/Scripts/Globals";


export function start(options: { routes: JSX.Element[] }) {
    
    Navigator.addSettings(new EntitySettings(DynamicViewEntity, w => import('./View/DynamicView')));
    Navigator.addSettings(new EntitySettings(DynamicViewSelectorEntity, w => import('./View/DynamicViewSelector')));
    Navigator.addSettings(new EntitySettings(DynamicViewOverrideEntity, w => import('./View/DynamicViewOverride')));

    DynamicClient.Options.onGetDynamicLineForType.push((ctx, type) => <ValueSearchControlLine ctx={ctx} findOptions={{ queryName: DynamicViewEntity, parentColumn: "EntityType.CleanName", parentValue: type }} />);
    DynamicClient.Options.onGetDynamicLineForType.push((ctx, type) => <ValueSearchControlLine ctx={ctx} findOptions={{ queryName: DynamicViewSelectorEntity, parentColumn: "EntityType.CleanName", parentValue: type }} />);

    Operations.addSettings(new EntityOperationSettings(DynamicViewOperation.Save, {
        onClick: ctx => {
            (ctx.frame.entityComponent as DynamicViewEntityComponent).beforeSave();
            cleanCaches();
            ctx.defaultClick();
        }
    }));

    Operations.addSettings(new EntityOperationSettings(DynamicViewOperation.Delete, {
        onClick: ctx => {
            cleanCaches();
            ctx.defaultClick();
        },
        contextual: { onClick: ctx => { cleanCaches(); ctx.defaultContextualClick(); } },
        contextualFromMany: { onClick: ctx => { cleanCaches(); ctx.defaultContextualClick(); } },
    }));

    Operations.addSettings(new EntityOperationSettings(DynamicViewSelectorOperation.Save, {
        onClick: ctx => {
            cleanCaches();
            ctx.defaultClick();
        }
    }));

    Operations.addSettings(new EntityOperationSettings(DynamicViewSelectorOperation.Delete, {
        onClick: ctx => {
            cleanCaches();
            ctx.defaultClick();
        },
        contextual: { onClick: ctx => { cleanCaches(); ctx.defaultContextualClick(); } },
        contextualFromMany: { onClick: ctx => { cleanCaches(); ctx.defaultContextualClick(); } },
    }));

    Navigator.setViewDispatcher(new DynamicViewViewDispatcher());
}

export const registeredCustomContexts: { [name: string]: CustomContextSettings } = {};

interface CustomContextSettings {
    getTypeContext: (ctx: TypeContext<any>) => TypeContext<any> | undefined;
    getCodeContext: (ctx: NodeUtils.CodeContext) => NodeUtils.CodeContext;
    getPropertyRoute: (dn: NodeUtils.DesignerNode<Nodes.CustomContextNode>) => PropertyRoute;
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

            try {
                var viewName = sel(entity as Entity);

                if (viewName == "STATIC")
                    return this.static(entity);

                if (viewName == "NEW")
                    return ViewPromise.flat(createDefaultDynamicView(entity.Type).then(dv => this.dynamicViewComponent(dv)));

                if (viewName == "CHOOSE")
                    return ViewPromise.flat(this.chooseDynamicView(entity.Type, true).then(dv => this.dynamicViewComponent(dv)));

                return ViewPromise.flat(API.getDynamicView(entity.Type, viewName).then(dv => this.dynamicViewComponent(dv)));
            } catch (error) {
                return MessageModal.showError("There was an error executing the DynamicViewSelector. Fallback to default").then(() => this.fallback(entity));
            }
        }));
    }

    dynamicViewComponent(dynamicView: DynamicViewEntity): ViewPromise<ModifiableEntity> {
        return new ViewPromise(import('./View/DynamicViewComponent'))
            .withProps({ initialDynamicView: dynamicView });
    }

    fallback(entity: ModifiableEntity): ViewPromise<ModifiableEntity> {
        const settings = Navigator.getSettings(entity.Type) as EntitySettings<ModifiableEntity>;

        if (!settings || !settings.getViewPromise) {

            if (!isTypeEntity(entity.Type))
                return new ViewPromise(import('../../../Framework/Signum.React/Scripts/Lines/DynamicComponent'));

            return ViewPromise.flat(this.chooseDynamicView(entity.Type, true).then(dv => this.dynamicViewComponent(dv)));
        }

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

export function patchComponent(component: React.ComponentClass < { ctx: TypeContext<Entity> }>, viewOverride: (e: ViewReplacer<Entity>) => void) {

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
            viewOverride(replacer);
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


export function cleanCaches() {
    Dic.clear(viewNamesCache);
    Dic.clear(selectorCache);
}

const viewNamesCache: { [typeName: string]: string[] } = {};
export function getViewNames(typeName: string): Promise<string[]> {

    return getOrCreate(viewNamesCache, typeName, () =>
        API.getDynamicViewNames(typeName)
    );
}

const selectorCache: { [typeName: string]: ((e: Entity) => string) | undefined } = {};
export function getSelector(typeName: string): Promise<((e: Entity) => string) | undefined> {

    return getOrCreate(selectorCache, typeName, () =>
        API.getDynamicViewSelector(typeName)
            .then(dvs => dvs && asSelectorFunction(dvs))
    );
}

export function asSelectorFunction(dvs: DynamicViewSelectorEntity): (e: Entity) => string {

    const code = "e => " + dvs.script!;

    try {
        return evalWithScope(code, globalModules);
    } catch (e) {
        throw new Error("Syntax in DynamicViewSelector for '" + dvs.entityType!.toStr + "':\r\n" + code + "\r\n" + (e as Error).message);
    }
}

function evalWithScope(code: string, modules: any) {
    return eval(code);
}

const overrideCache: { [typeName: string]: ((vr: ViewReplacer<Entity>) => string) | undefined } = {};
export function getViewOverride(typeName: string): Promise<((vr: ViewReplacer<Entity>) => void) | undefined> {

    return getOrCreate(overrideCache, typeName, () =>
        API.getDynamicViewOverride(typeName)
            .then(dvr => dvr && asOverrideFunction(dvr))
    );
}


export function asOverrideFunction(dvr: DynamicViewOverrideEntity): (vr: ViewReplacer<Entity>) => string {
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
    var FormGroup = Lines.FormGroup;
    var FormControlStatic = Lines.FormControlStatic;
    var FileLine = FileLineModule.default;

    // Search
    var SearchControl = Search.SearchControl;
    var ValueSearchControl = Search.ValueSearchControl;
    var ValueSearchControlLine = Search.ValueSearchControlLine;

    // Reactstrap
    var Badge = Reactstrap.Badge;
    var Button = Reactstrap.Button;
    var ButtonGroup = Reactstrap.ButtonGroup;
    var ButtonToolbar = Reactstrap.ButtonToolbar;
    var Collapse = Reactstrap.Collapse;
    var Dropdown = Reactstrap.Dropdown;
    var Label = Reactstrap.Label;
    var ListGroup = Reactstrap.ListGroup;
    var Nav = Reactstrap.Nav;
    var NavbarBrand = Reactstrap.NavbarBrand;
    var NavDropdown = Reactstrap.NavDropdown;
    var Tooltip = Reactstrap.Tooltip;

    // ReactRouterBootstrap
    var LinkContainer = ReactRouterBootstrap.LinkContainer;
    
    // Custom
    var DynamicViewPart = DynamicViewComponent.DynamicViewPart;

    var modules = globalModules;

    code = "(function(vr){ " + code + "})";

    try {
        return eval(code);
    } catch (e) {
        throw new Error("Syntax in DynamicViewOverride for '" + dvr.entityType!.toStr + "':\r\n" + code + "\r\n" + (e as Error).message);
    }
}

export function createDefaultDynamicView(typeName: string): Promise<DynamicViewEntity> {
    return loadNodes().then(nodes =>
        Navigator.API.getType(typeName).then(t => DynamicViewEntity.New({
            entityType : t,
            viewName : "My View",
            viewContent: JSON.stringify(nodes.NodeConstructor.createDefaultNode(getTypeInfo(typeName))),
        })));
}

export function loadNodes(): Promise<typeof Nodes> {
    return import("./View/Nodes");
}

export function getDynamicViewPromise(typeName: string, viewName: string): ViewPromise<ModifiableEntity> {

    return ViewPromise.flat(
        API.getDynamicView(typeName, viewName)
            .then(vn => new ViewPromise(import('./View/DynamicViewComponent')).withProps({ initialDynamicView: vn }))
    );
}


export namespace API {
    
    export function getDynamicView(typeName: string, viewName: string): Promise<DynamicViewEntity> {
        
            return ajaxGet<DynamicViewEntity>({ url: `~/api/dynamic/view/${typeName}?` + QueryString.stringify({ viewName}) });
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


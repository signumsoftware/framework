
import * as React from 'react'
import { Route } from 'react-router'
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
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
import ButtonBar from '../../../Framework/Signum.React/Scripts/Frames/ButtonBar';

import { ValueLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater } from '../../../Framework/Signum.React/Scripts/Lines'
import { DynamicViewEntity, DynamicViewSelectorEntity, DynamicViewMessage, DynamicViewOperation } from './Signum.Entities.Dynamic'
import DynamicViewEntityComponent from './View/DynamicViewEntity' //Just Typing
import * as DynamicClient from './DynamicClient'

import { DynamicViewComponentProps } from './View/DynamicViewComponent'
import { AuthInfo } from './View/AuthInfo'

export function start(options: { routes: JSX.Element[] }) {
    
    Navigator.addSettings(new EntitySettings(DynamicViewEntity, w => new ViewPromise(resolve => require(['./View/DynamicViewEntity'], resolve))));
    Navigator.addSettings(new EntitySettings(DynamicViewSelectorEntity, w => new ViewPromise(resolve => require(['./View/DynamicViewSelector'], resolve))));

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

        return ViewPromise.flat(getSeletor(entity.Type).then(sel => {

            if (!sel)
                return this.fallback(entity);

            var viewName = sel(entity as Entity, new AuthInfo());

            if (viewName == "STATIC")
                return this.fallback(entity);

            if (viewName == "NEW")
                return ViewPromise.flat(createDefaultDynamicView(entity.Type).then(dv => this.dynamicComponent(dv)));

            if (viewName == "CHOOSE")
                return ViewPromise.flat(chooseDynamicView(entity.Type, true).then(dv => this.dynamicComponent(dv)));

            return ViewPromise.flat(API.getDynamicView(entity.Type, viewName).then(dv => this.dynamicComponent(dv)));
        }));
    }

    dynamicComponent(promiseDv: DynamicViewEntity): ViewPromise<ModifiableEntity>  {
        return new ViewPromise(resolve => require(['./View/DynamicViewComponent'], resolve))
            .withProps({ initialDynamicView: promiseDv});
    }

    fallback(entity: ModifiableEntity): ViewPromise<ModifiableEntity> {
        const settings = Navigator.getSettings(entity.Type) as EntitySettings<ModifiableEntity>;

        if (!settings || !settings.getViewPromise)
            return new ViewPromise<ModifiableEntity>(resolve => require(['./Lines/DynamicViewComponent'], resolve));

        return settings.getViewPromise(entity).applyViewOverrides(settings);
    }
}

export function getSeletor(typeName: string): Promise<((e: Entity, auth: AuthInfo) => any) | undefined> {
    return API.getDynamicViewSelector(typeName).then(dvs => {
        if (!dvs)
            return undefined;

        return asFunction(dvs);
    });
}

export function asFunction(dvs: DynamicViewSelectorEntity): (e: Entity, auth: AuthInfo) => any {
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

export function chooseDynamicView(typeName: string, avoidMessage = false) {
    return API.getDynamicViewNames(typeName)
        .then(names => SelectorModal.chooseElement(names, {
            title: DynamicViewMessage.ChooseAView.niceToString(),
            message: avoidMessage ? undefined : DynamicViewMessage.SinceThereIsNoDynamicViewSelectorYouNeedToChooseAViewManually.niceToString(),
        })).then(viewName => {
            return getOrCreateDynamicView(typeName, viewName);
        });
}

export function getOrCreateDynamicView(typeName: string, viewName: string | undefined): Promise<DynamicViewEntity> {

    if (viewName == undefined)
        return createDefaultDynamicView(typeName);

    return API.getDynamicView(typeName, viewName)
        .then(dv => { return dv; });
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

import * as Nodes from './View/Nodes' //Typings-only
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

    export function getDynamicViewSelector(typeName: string): Promise<DynamicViewSelectorEntity> {
        return ajaxGet<DynamicViewSelectorEntity>({ url: `~/api/dynamic/selector/${typeName}`  });
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


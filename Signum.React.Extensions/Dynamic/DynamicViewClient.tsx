
import * as React from 'react'
import { Route } from 'react-router'
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { TypeContext } from '../../../Framework/Signum.React/Scripts/TypeContext'
import { isTypeEntity, getTypeInfo, } from '../../../Framework/Signum.React/Scripts/Reflection'
import { Entity } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeEntity } from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor'

import { ValueLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater } from '../../../Framework/Signum.React/Scripts/Lines'
import { DynamicViewEntity } from './Signum.Entities.Dynamic'
import { BaseNode, NodeConstructor } from './View/Nodes'
import { DynamicViewComponentProps } from './View/DynamicViewComponent'

export function start(options: { routes: JSX.Element[] }) {

    //Navigator.addSettings(new EntitySettings(DynamicViewEntity, w => new ViewPromise(resolve => require(['./View/DynamicViewEntity'], resolve))));

    Navigator.setFallbackViewPromise(mod => {
        if (!isTypeEntity(mod.Type))
            return new ViewPromise(resolve => require(['../../../Framework/Signum.React/Scripts/Lines/DynamicComponent'], resolve));

        return new ViewPromise(resolve => require(['./View/DynamicViewComponent'], resolve))
            .withProps(getOrCreateDynamicView(mod.Type).then(dv => ({ initialDynamicView : dv })));
    });
}

export function getOrCreateDynamicView(typeName: string): Promise<DynamicViewEntity> {
    return API.getDynamicView(typeName).then(dv => {
        if (dv)
            return dv;

        return createDefaultDynamicView(typeName);
    });
}

export function createDefaultDynamicView(typeName: string): Promise<DynamicViewEntity> {
    return Navigator.API.getType(typeName).then(t => DynamicViewEntity.New(dv => {
        dv.entityType = t;
        dv.viewName = "Default";
        const node = NodeConstructor.createDefaultNode(getTypeInfo(typeName));
        dv.viewContent = JSON.stringify(node);
    }));
}

export namespace API {

    export function getDynamicView(typeName: string, viewName: string = "Default"): Promise<DynamicViewEntity | null> {

        var url = Navigator.currentHistory.createHref({
            pathname: `~/api/dynamic/view/${typeName}`,
            query: { viewName }
        });

        return ajaxGet<DynamicViewEntity | null>({ url });
    }

    export function getDynamicViewNames(typeName: string): Promise<string[]> {
        return ajaxGet<string[]>({ url: `~/api/dynamic/viewNames/${typeName}`  });
    }

    export function getSuggestedFindOptions(typeName: string): Promise<SuggestedFindOptions[]> {
        return ajaxGet<SuggestedFindOptions[]>({ url: `~/api/dynamic/suggestedFindOptions/${typeName}` });
    }
}

export interface SuggestedFindOptions {
    queryKey: string;
    parentColumn: string;
}


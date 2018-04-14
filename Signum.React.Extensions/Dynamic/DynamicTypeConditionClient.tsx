
import * as React from 'react'
import { Route } from 'react-router'
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { ValueSearchControlLine } from '../../../Framework/Signum.React/Scripts/Search'
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import { EntityOperationSettings, OperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { Entity } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor'
import * as DynamicClient from './DynamicClient'

import { ValueLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater } from '../../../Framework/Signum.React/Scripts/Lines'
import { DynamicTypeConditionEntity, DynamicTypeConditionEval, DynamicTypeConditionOperation } from './Signum.Entities.Dynamic'

export function start(options: { routes: JSX.Element[] }) {

    Navigator.addSettings(new EntitySettings(DynamicTypeConditionEntity, w => import('./TypeCondition/DynamicTypeCondition')));

    Operations.addSettings(new EntityOperationSettings(DynamicTypeConditionOperation.Clone, {
        contextual: { icon: "fa fa-clone", iconColor: "black" },
    }))

    Constructor.registerConstructor(DynamicTypeConditionEntity, () => DynamicTypeConditionEntity.New({ eval: DynamicTypeConditionEval.New() }));
    DynamicClient.Options.onGetDynamicLineForPanel.push(ctx => <ValueSearchControlLine ctx={ctx} findOptions={{ queryName: DynamicTypeConditionEntity }} />);
    DynamicClient.Options.onGetDynamicLineForType.push((ctx, type) => <ValueSearchControlLine ctx={ctx} findOptions={{ queryName: DynamicTypeConditionEntity, parentColumn: "EntityType.CleanName", parentValue: type }} />);
}

export namespace API {
    export function typeConditionTest(request: DynamicTypeConditionTestRequest): Promise<DynamicTypeConditionTestResponse> {
        return ajaxPost<DynamicTypeConditionTestResponse>({ url: `~/api/dynamic/typeCondition/test` }, request);
    }
}

export interface DynamicTypeConditionTestRequest {
    dynamicTypeCondition: DynamicTypeConditionEntity;
    exampleEntity: Entity;
}

export interface DynamicTypeConditionTestResponse {
    compileError?: string;
    validationException?: string;
    validationResult?: boolean;
}


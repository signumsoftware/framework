
import * as React from 'react'
import { Route } from 'react-router'
import { ajaxPost, ajaxGet } from '@framework/Services';
import { ValueSearchControlLine } from '@framework/Search'
import { EntitySettings, ViewPromise } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import { EntityOperationSettings, OperationSettings } from '@framework/Operations'
import * as Operations from '@framework/Operations'
import { Entity } from '@framework/Signum.Entities'
import * as Constructor from '@framework/Constructor'
import * as DynamicClientOptions from './DynamicClientOptions'

import { ValueLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater } from '@framework/Lines'
import { DynamicTypeConditionEntity, DynamicTypeConditionEval, DynamicTypeConditionOperation } from './Signum.Entities.Dynamic'

export function start(options: { routes: JSX.Element[] }) {

    Navigator.addSettings(new EntitySettings(DynamicTypeConditionEntity, w => import('./TypeCondition/DynamicTypeCondition')));

    Operations.addSettings(new EntityOperationSettings(DynamicTypeConditionOperation.Clone, {
        contextual: { icon: "clone", iconColor: "black" },
    }))

    Constructor.registerConstructor(DynamicTypeConditionEntity, () => DynamicTypeConditionEntity.New({ eval: DynamicTypeConditionEval.New() }));
    DynamicClientOptions.Options.onGetDynamicLineForPanel.push(ctx => <ValueSearchControlLine ctx={ctx} findOptions={{ queryName: DynamicTypeConditionEntity }} />);
    DynamicClientOptions.Options.onGetDynamicLineForType.push((ctx, type) => <ValueSearchControlLine ctx={ctx} findOptions={{ queryName: DynamicTypeConditionEntity, parentToken: "EntityType.CleanName", parentValue: type }} />);
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


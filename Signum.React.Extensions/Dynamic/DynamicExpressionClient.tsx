
import * as React from 'react'
import { Route } from 'react-router'
import { ajaxPost, ajaxGet } from '@framework/Services';
import { ValueSearchControlLine } from '@framework/Search'
import { EntitySettings, ViewPromise } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import { EntityOperationSettings } from '@framework/Operations'
import * as Operations from '@framework/Operations'
import { Entity } from '@framework/Signum.Entities'
import * as Constructor from '@framework/Constructor'
import * as DynamicClientOptions from './DynamicClientOptions'

import { ValueLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater } from '@framework/Lines'
import { DynamicExpressionEntity } from './Signum.Entities.Dynamic'

export function start(options: { routes: JSX.Element[] }) {

    Navigator.addSettings(new EntitySettings(DynamicExpressionEntity, w => import('./Expression/DynamicExpression')));
    DynamicClientOptions.Options.onGetDynamicLineForPanel.push(ctx => <ValueSearchControlLine ctx={ctx} findOptions={{ queryName: DynamicExpressionEntity }} />);
    DynamicClientOptions.Options.onGetDynamicLineForType.push((ctx, type) => <ValueSearchControlLine ctx={ctx} findOptions={{ queryName: DynamicExpressionEntity, parentToken: "FromType", parentValue: type + "Entity" }} />);
}

export namespace API {
    export function expressionTest(request: DynamicExpressionTestRequest): Promise<DynamicExpressionTestResponse> {
        return ajaxPost<DynamicExpressionTestResponse>({ url: `~/api/dynamic/expression/test` }, request);
    }
}

export interface DynamicExpressionTestRequest {
    dynamicExpression: DynamicExpressionEntity;
    exampleEntity: Entity;
}

export interface DynamicExpressionTestResponse {
    compileError?: string;
    validationException?: string;
    validationResult?: string;
}


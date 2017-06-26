
import * as React from 'react'
import { Route } from 'react-router'
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { ValueSearchControlLine } from '../../../Framework/Signum.React/Scripts/Search'
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { Entity } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor'
import * as DynamicClient from './DynamicClient'

import { ValueLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater } from '../../../Framework/Signum.React/Scripts/Lines'
import { DynamicExpressionEntity } from './Signum.Entities.Dynamic'

export function start(options: { routes: JSX.Element[] }) {

    Navigator.addSettings(new EntitySettings(DynamicExpressionEntity, w => import('./Expression/DynamicExpression')));
    DynamicClient.Options.onGetDynamicLineForPanel.push(ctx => <ValueSearchControlLine ctx={ctx} findOptions={{ queryName: DynamicExpressionEntity }} />);
    DynamicClient.Options.onGetDynamicLineForType.push((ctx, type) => <ValueSearchControlLine ctx={ctx} findOptions={{ queryName: DynamicExpressionEntity, parentColumn: "FromType", parentValue: type + "Entity" }} />);
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


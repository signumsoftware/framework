
import * as React from 'react'
import { Route } from 'react-router'
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { CountSearchControl } from '../../../Framework/Signum.React/Scripts/Search'
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

    Navigator.addSettings(new EntitySettings(DynamicExpressionEntity, w => new ViewPromise(resolve => require(['./Expression/DynamicExpressionEntity'], resolve))));
    DynamicClient.Options.onGetDynamicLine.push(ctx => <CountSearchControl ctx={ctx} findOptions={{ queryName: DynamicExpressionEntity }} />);
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


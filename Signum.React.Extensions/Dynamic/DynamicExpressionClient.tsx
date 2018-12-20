
import * as React from 'react'
import { ajaxPost } from '@framework/Services';
import { ValueSearchControlLine } from '@framework/Search'
import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import { Entity } from '@framework/Signum.Entities'
import * as DynamicClientOptions from './DynamicClientOptions'
import { DynamicExpressionEntity } from './Signum.Entities.Dynamic'

export function start(options: { routes: JSX.Element[] }) {

  Navigator.addSettings(new EntitySettings(DynamicExpressionEntity, w => import('./Expression/DynamicExpression')));
  DynamicClientOptions.Options.onGetDynamicLineForPanel.push(ctx => <ValueSearchControlLine ctx={ctx} findOptions={{ queryName: DynamicExpressionEntity }} />);
  DynamicClientOptions.Options.onGetDynamicLineForType.push((ctx, type) => <ValueSearchControlLine ctx={ctx} findOptions={{ queryName: DynamicExpressionEntity, parentToken: DynamicExpressionEntity.token(e => e.fromType), parentValue: type + "Entity" }} />);
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



import * as React from 'react'
import { RouteObject } from 'react-router'
import { ajaxPost } from '@framework/Services';
import { SearchValueLine } from '@framework/Search'
import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import { Entity } from '@framework/Signum.Entities'
import * as DynamicClientOptions from './DynamicClientOptions'
import { DynamicExpressionEntity } from './Signum.Dynamic.Expression';

export function start(options: { routes: RouteObject[] }) {

  Navigator.addSettings(new EntitySettings(DynamicExpressionEntity, w => import('./Expression/DynamicExpression')));
  DynamicClientOptions.Options.onGetDynamicLineForPanel.push(ctx => <SearchValueLine ctx={ctx} findOptions={{ queryName: DynamicExpressionEntity }} />);
  DynamicClientOptions.Options.onGetDynamicLineForType.push((ctx, type) => <SearchValueLine ctx={ctx} findOptions={{ queryName: DynamicExpressionEntity, filterOptions: [{ token: DynamicExpressionEntity.token(e => e.fromType), value: type + "Entity" }]}} />);
  DynamicClientOptions.Options.registerDynamicPanelSearch(DynamicExpressionEntity, t => [
    { token: t.append(p => p.entity.body), type: "Code" },
    { token: t.append(p => p.name), type: "Text" },
    { token: t.append(p => p.returnType), type: "Text" },
    { token: t.append(p => p.fromType), type: "Text" },
    { token: t.append(p => p.entity.unit), type: "Text" },
    { token: t.append(p => p.entity.format), type: "Text" },
  ]);
}

export namespace API {
  export function expressionTest(request: DynamicExpressionTestRequest): Promise<DynamicExpressionTestResponse> {
    return ajaxPost({ url: `/api/dynamic/expression/test` }, request);
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


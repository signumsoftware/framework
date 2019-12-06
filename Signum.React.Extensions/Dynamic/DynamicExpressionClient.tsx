
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
  DynamicClientOptions.Options.registerDynamicPanelSearch(DynamicExpressionEntity, t => [
    { token: t.entity(p => p.body), type: "Code" },
    { token: t.append(p => p.name), type: "Text" },
    { token: t.append(p => p.returnType), type: "Text" },
    { token: t.append(p => p.fromType), type: "Text" },
    { token: t.entity(p => p.unit), type: "Text" },
    { token: t.entity(p => p.format), type: "Text" },
  ]);
}

export namespace API {
  export function expressionTest(request: DynamicExpressionTestRequest): Promise<DynamicExpressionTestResponse> {
    return ajaxPost({ url: `~/api/dynamic/expression/test` }, request);
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


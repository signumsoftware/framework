
import * as React from 'react'
import { ajaxPost } from '@framework/Services';
import { ValueSearchControlLine } from '@framework/Search'
import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import { EntityOperationSettings } from '@framework/Operations'
import * as Operations from '@framework/Operations'
import { Entity } from '@framework/Signum.Entities'
import * as Constructor from '@framework/Constructor'
import * as DynamicClientOptions from './DynamicClientOptions'
import { DynamicTypeConditionEntity, DynamicTypeConditionEval, DynamicTypeConditionOperation } from './Signum.Entities.Dynamic'

export function start(options: { routes: JSX.Element[] }) {

  Navigator.addSettings(new EntitySettings(DynamicTypeConditionEntity, w => import('./TypeCondition/DynamicTypeCondition')));

  Operations.addSettings(new EntityOperationSettings(DynamicTypeConditionOperation.Clone, {
    contextual: { icon: "clone", iconColor: "black" },
  }))

  Constructor.registerConstructor(DynamicTypeConditionEntity, () => DynamicTypeConditionEntity.New({ eval: DynamicTypeConditionEval.New() }));
  DynamicClientOptions.Options.onGetDynamicLineForPanel.push(ctx => <ValueSearchControlLine ctx={ctx} findOptions={{ queryName: DynamicTypeConditionEntity }} />);
  DynamicClientOptions.Options.onGetDynamicLineForType.push((ctx, type) => <ValueSearchControlLine ctx={ctx} findOptions={{
    queryName: DynamicTypeConditionEntity,
    parentToken: DynamicTypeConditionEntity.token(a => a.entityType!.cleanName),
    parentValue: type
  }} />);
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


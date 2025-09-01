
import * as React from 'react'
import { RouteObject } from 'react-router'
import { ajaxPost } from '@framework/Services';
import { SearchValueLine } from '@framework/Search'
import { Navigator, EntitySettings } from '@framework/Navigator'
import { Operations, EntityOperationSettings } from '@framework/Operations'
import { Entity } from '@framework/Signum.Entities'
import { Constructor } from '@framework/Constructor'
import { EvalClient } from '../Signum.Eval/EvalClient'
import { DynamicTypeConditionEntity, DynamicTypeConditionEval, DynamicTypeConditionOperation } from './Signum.Dynamic.Types';

export namespace DynamicTypeConditionClient {
  
  export function start(options: { routes: RouteObject[] }): void {
  
    Navigator.addSettings(new EntitySettings(DynamicTypeConditionEntity, w => import('./TypeCondition/DynamicTypeCondition')));
  
    Operations.addSettings(new EntityOperationSettings(DynamicTypeConditionOperation.Clone, {
      contextual: { icon: "clone", iconColor: "var(--bs-body-color)" },
    }))
  
    Constructor.registerConstructor(DynamicTypeConditionEntity, () => DynamicTypeConditionEntity.New({ eval: DynamicTypeConditionEval.New() }));
    EvalClient.Options.onGetDynamicLineForPanel.push(ctx => <SearchValueLine ctx={ctx} findOptions={{ queryName: DynamicTypeConditionEntity }} />);
    EvalClient.Options.onGetDynamicLineForType.push((ctx, type) => <SearchValueLine ctx={ctx} findOptions={{
      queryName: DynamicTypeConditionEntity,
      filterOptions: [{ token: DynamicTypeConditionEntity.token(a => a.entityType!.cleanName), value: type}]
    }} />);
    EvalClient.Options.registerDynamicPanelSearch(DynamicTypeConditionEntity, t => [
      { token: t.append(p => p.entity.entityType.cleanName), type: "Text" },
      { token: t.append(p => p.entity.symbolName.name), type: "Text" },
      { token: t.append(p => p.entity.eval!.script), type: "Code" },
    ]);
  }
  
  export namespace API {
    export function typeConditionTest(request: DynamicTypeConditionTestRequest): Promise<DynamicTypeConditionTestResponse> {
      return ajaxPost({ url: `/api/dynamic/typeCondition/test` }, request);
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
}


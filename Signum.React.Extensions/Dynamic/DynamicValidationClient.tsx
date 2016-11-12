
import * as React from 'react'
import { Route } from 'react-router'
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { CountSearchControlLine } from '../../../Framework/Signum.React/Scripts/Search'
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { Entity } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor'
import * as DynamicClient from './DynamicClient'

import { ValueLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater } from '../../../Framework/Signum.React/Scripts/Lines'
import { DynamicValidationEntity, DynamicValidationOperation, DynamicValidationEval } from './Signum.Entities.Dynamic'

export function start(options: { routes: JSX.Element[] }) {

    Navigator.addSettings(new EntitySettings(DynamicValidationEntity, w => new ViewPromise(resolve => require(['./Validation/DynamicValidation'], resolve))));
    Constructor.registerConstructor(DynamicValidationEntity, () => DynamicValidationEntity.New(f => f.eval = DynamicValidationEval.New()));

    DynamicClient.Options.onGetDynamicLineForPanel.push(ctx => <CountSearchControlLine ctx={ctx} findOptions={{ queryName: DynamicValidationEntity }} />);
    DynamicClient.Options.onGetDynamicLineForType.push((ctx, type) => <CountSearchControlLine ctx={ctx} findOptions={{ queryName: DynamicValidationEntity, parentColumn: "EntityType.CleanName", parentValue: type }} />);
}

export namespace API {
    export function validationTest(request: DynamicValidationTestRequest): Promise<DynamicValidationTestResponse> {
        return ajaxPost<DynamicValidationTestResponse>({ url: `~/api/dynamic/validation/test` }, request);
    }
}


export interface DynamicValidationTestRequest {
    dynamicValidation: DynamicValidationEntity;
    exampleEntity: Entity;
}

export interface DynamicValidationTestResponse {
    compileError?: string;
    validationException?: string;
    validationResult?: string[];
}



import * as React from 'react'
import { EntityData, EntityKind, PseudoType } from '@framework/Reflection'
import { StyleContext } from '@framework/TypeContext'
import { FindOptions } from '@framework/FindOptions';

export namespace Options {
    //export let onGetDynamicLineForPanel: ({ line: (ctx: StyleContext) => React.ReactNode, needsCompiling: boolean })[] = [];
    export let onGetDynamicLineForPanel: ((ctx: StyleContext) => React.ReactNode)[] = [];
    export let onGetDynamicLineForType: ((ctx: StyleContext, type: string) => React.ReactNode)[] = [];
    export let checkEvalFindOptions: FindOptions[] = [];

    export let getDynaicMigrationsStep: (() => React.ReactElement<any>) | undefined = undefined;
}

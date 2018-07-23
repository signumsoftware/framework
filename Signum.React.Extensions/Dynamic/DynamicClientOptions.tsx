
import * as React from 'react'
import { EntityData, EntityKind, PseudoType } from '../../../Framework/Signum.React/Scripts/Reflection'
import { StyleContext } from '../../../Framework/Signum.React/Scripts/TypeContext'
import { FindOptions } from '../../../Framework/Signum.React/Scripts/FindOptions';

export namespace Options {
    //export let onGetDynamicLineForPanel: ({ line: (ctx: StyleContext) => React.ReactNode, needsCompiling: boolean })[] = [];
    export let onGetDynamicLineForPanel: ((ctx: StyleContext) => React.ReactNode)[] = [];
    export let onGetDynamicLineForType: ((ctx: StyleContext, type: string) => React.ReactNode)[] = [];
    export let checkEvalFindOptions: FindOptions[] = [];

    export let getDynaicMigrationsStep: (() => React.ReactElement<any>) | undefined = undefined;
}

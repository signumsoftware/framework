/// <reference path="../typings/react/react.d.ts" />

import * as React from 'react'
import { TypeContext, StyleOptions } from 'Framework/Signum.React/Scripts/TypeContext'

export interface LineBaseProps {
    ctx: TypeContext<any>;
}

export class LineBase<P extends LineBaseProps, S> extends React.Component<P, S> {

}



export interface ValueLineProps extends LineBaseProps {
    valueLineType?: ValueLineType;
    unitText?: string;
}

export enum ValueLineType {
    Boolean = "Boolean" as any,
    Enum = "Enum" as any,
    DateTime = "DateTime" as any,
    TimeSpan = "TimeSpan" as any,
    TextBox = "TextBox" as any,
    TextArea = "TextArea" as any,
    Number = "Number" as any,
    Color = "Color" as any,
}

export class ValueLine extends LineBase<ValueLineProps, {}> {

}

export interface EntityLineProps extends LineBaseProps{

}

export class EntityLine extends LineBase<EntityLineProps, {}> {

}

export class EntityComponent<T> extends React.Component<{ typeContext: TypeContext<T> }, {}>{

    get value() {
        return this.props.typeContext.value;
    }

    subContext<R>(property: (val: T) => R, styleOptions?: StyleOptions): TypeContext<R> {
        return this.props.typeContext.subCtx(property, styleOptions);
    }
}

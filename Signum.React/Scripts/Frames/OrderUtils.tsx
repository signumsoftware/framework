import * as React from 'react'
import { Dic  } from '../Globals'

export function setOrder(order: number, element: React.ReactElement<any>) {
    return element && React.cloneElement(element, { order });
}

export function getOrder(element: React.ReactElement<any>): number {
    return element.props.order || 0;
}

export function cloneElementWithoutOrder(element: React.ReactElement<any>, extraProps?: any) {

    if (element == null)
        return null;

    var { order, children, ...props } = element.props;

    if (extraProps != undefined)
        Dic.assign(props, extraProps);

    return React.createElement(element.type as any, props, ...children);
}
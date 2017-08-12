import * as React from 'react'
import { Dic  } from '../Globals'

export function setOrder(order: number, element: React.ReactElement<any>) {
    return React.cloneElement(element, { order });
}

export function getOrder(element: React.ReactElement<any>): number | undefined {
    return element.props.order;
}

export function cloneElementWithoutOrder(element: React.ReactElement<any>, extraProps?: any) {
    var { order, children, ...props } = element.props;

    if (extraProps != undefined)
        Dic.assign(props, extraProps);

    return React.createElement(element.type as any, props, ...children);
}
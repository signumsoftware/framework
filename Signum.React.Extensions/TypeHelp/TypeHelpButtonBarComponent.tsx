import * as React from 'react'
import { Route } from 'react-router'
import { Entity } from '../../../Framework/Signum.React/Scripts/Signum.Entities';
import { TypeHelpMode } from './TypeHelpClient';
import { ModifiableEntity } from '../../../Framework/Signum.React/Scripts/Signum.Entities';
import { TypeContext } from '../../../Framework/Signum.React/Scripts/Lines';


interface TypeHelpButtonBarComponentProps {
    typeName: string;
    mode: TypeHelpMode;
    ctx?: TypeContext<any>;
}

export default class TypeHelpButtonBarComponent extends React.Component<TypeHelpButtonBarComponentProps> {

    static getTypeHelpButtons: Array<(props: TypeHelpButtonBarComponentProps) => ({ element: React.ReactElement<any>, order: number })[]> = [];

    render() {
        return (
            <div>
                {
                    TypeHelpButtonBarComponent.getTypeHelpButtons
                    .flatMap(f => f(this.props))
                    .orderBy(p => p.order)
                    .map((p, i) => React.cloneElement(p.element, { key: i }))
                }
            </div>
        );
    }
}

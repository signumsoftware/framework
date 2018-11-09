import * as React from 'react'
import { TypeHelpMode } from './TypeHelpClient';
import { TypeContext } from '@framework/Lines';

interface TypeHelpButtonBarComponentProps {
  typeName: string;
  mode: TypeHelpMode;
  extraButtons?: React.ReactNode;
  ctx?: TypeContext<any>;
}

export default class TypeHelpButtonBarComponent extends React.Component<TypeHelpButtonBarComponentProps> {
  static getTypeHelpButtons: Array<(props: TypeHelpButtonBarComponentProps) => ({ element: React.ReactElement<any>, order: number })[]> = [];

  render() {
    return (
      <div className="btn-toolbar">
        {this.props.extraButtons}
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

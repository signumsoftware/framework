import * as React from 'react'
import { TypeHelpMode } from './TypeHelpClient';
import { TypeContext } from '@framework/Lines';

interface TypeHelpButtonBarComponentProps {
  typeName: string;
  mode: TypeHelpMode;
  extraButtons?: React.ReactNode;
  ctx?: TypeContext<any>;
}

export default function TypeHelpButtonBarComponent(p : TypeHelpButtonBarComponentProps){
  return (
    <div className="btn-toolbar">
      {p.extraButtons}
      {
        TypeHelpButtonBarComponent.getTypeHelpButtons
          .flatMap(f => f(p))
          .orderBy(p => p.order)
          .map((p, i) => React.cloneElement(p.element, { key: i }))
      }
    </div>
  );
}

TypeHelpButtonBarComponent.getTypeHelpButtons = [] as Array<(props: TypeHelpButtonBarComponentProps) => ({ element: React.ReactElement<any>, order: number })[]>;

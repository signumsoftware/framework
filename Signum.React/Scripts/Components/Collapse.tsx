import * as React from 'react';
import Transition, { EnterHandler, ExitHandler } from 'react-transition-group/Transition';
import { classes } from '../Globals';

interface CollapseProps {
  isOpen?: boolean;
  tag?: React.ComponentType<React.HTMLAttributes<any>>;
  attrs?: React.HTMLAttributes<any>;
  navbar?: boolean;

  timeout?: number | { enter?: number, exit?: number };
  onEnter?: EnterHandler;
  onEntering?: EnterHandler;
  onEntered?: EnterHandler;
  onExit?: ExitHandler;
  onExiting?: ExitHandler;
  onExited?: ExitHandler;

  children?: React.ReactNode;

}

const transitionStatusToClassHash: { [key: string]: string } = {
  entering: 'collapsing',
  entered: 'collapse show',
  exiting: 'collapsing',
  exited: 'collapse',
};

function getHeight(node: HTMLElement) {
  return node.scrollHeight;
}

export function Collapse({
  tag,
  isOpen,
  navbar,
  children,
  attrs,
  timeout,
  onEnter,
  ...p
}: CollapseProps) {

  const [height, setHeight] = React.useState<number | null>(null);

  function onEntering(node: HTMLElement, isAppearing: boolean) {
    setHeight(getHeight(node));
    p.onEntering!(node, isAppearing);
  }

  function onEntered(node: HTMLElement, isAppearing: boolean) {
    setHeight(null);
    p.onEntered!(node, isAppearing);
  }

  function onExit(node: HTMLElement) {
    setHeight(getHeight(node));
    p.onExit!(node);
  }

  function onExiting(node: HTMLElement) {
    // getting this variable triggers a reflow
    setHeight(0);
    p.onExiting!(node);
  }

  function onExited(node: HTMLElement) {
    setHeight(null)
    p.onExited!(node);
  }

  var Tag = tag!;

  return (
    <Transition
      timeout={timeout!}
      in={isOpen}
      onEnter={onEnter}
      onEntering={onEntering}
      onEntered={onEntered}
      onExit={onExit}
      onExiting={onExiting}
      onExited={onExited}
    >
      {(status) => {
        let collapseClass = transitionStatusToClassHash[status] || 'collapse';
        const clss = classes(
          attrs && attrs.className,
          collapseClass,
          navbar && 'navbar-collapse'
        );
        const style = height === null ? null : { height };
        return (
          <Tag {...attrs}
            style={{ ...(attrs && attrs.style), ...style }}
            className={clss}>
            {children}
          </Tag>
        );
      }}
    </Transition>
  );
}

Collapse.defaultProps = {
  ...(Transition as any).defaultProps,
  isOpen: false,
  appear: false,
  enter: true,
  exit: true,
  tag: 'div',
  timeout: 600,
};

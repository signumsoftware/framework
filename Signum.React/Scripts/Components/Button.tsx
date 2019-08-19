import * as React from 'react';
import { BsSize, BsColor } from './Basic';
import { classes } from '../Globals';

export interface ButtonProps extends React.AnchorHTMLAttributes<any> {
  active?: boolean;
  block?: boolean;
  color?: BsColor;
  disabled?: boolean;
  outline?: boolean;
  tag?: React.ReactType,
  innerRef?: (e: HTMLElement | null) => void;
  onClick?: (e: React.MouseEvent<any>) => void;
  size?: BsSize;
  className?: string;
};

export function Button(props: ButtonProps) {

  function onClick(e: React.MouseEvent<any>) {
    if (props.disabled) {
      e.preventDefault();
      return;
    }

    if (props.onClick) {
      props.onClick(e);
    }
  }

  let {
    active,
    disabled,
    block,
    className,
    color,
    outline,
    size,
    tag,
    innerRef,
    ...attributes
  } = props;

  const clss = classes(
    className,
    'btn',
    `btn${outline ? '-outline' : ''}-${color || "secondary"}`,
    size && `btn-${size}`,
    block && 'btn-block',
    active && "active",
    disabled && "disabled"
  );

  let Tag = tag || "button";

  if (attributes.href && Tag === 'button') {
    Tag = 'a';
  }

  return (
    <Tag
      {...attributes}
      className={clss}
      ref={innerRef}
      onClick={onClick}
    />
  );
}

import { FontAwesomeIcon, FontAwesomeIconProps } from "@fortawesome/react-fontawesome";
import * as React from "react";

export function LinkButton({ title, ref, onClick, children, ...rest }:
  { title: string | undefined, ref?: React.Ref<HTMLAnchorElement> } & React.AnchorHTMLAttributes<HTMLAnchorElement>): React.ReactElement {

  var newChildren = React.Children.map(children, e => React.isValidElement(e) && e.type === FontAwesomeIcon ?
    React.cloneElement(e as React.ReactElement<FontAwesomeIconProps>, { "aria-hidden": true }) :
    e);

  return (
    <a role="button" href="#" aria-label={title} title={title} ref={ref} {...rest}
      onClick={e => { e.preventDefault(); onClick?.(e) }}>
      {newChildren}
    </a>
  );
}

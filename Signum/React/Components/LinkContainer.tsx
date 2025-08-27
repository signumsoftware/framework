import * as React from 'react'
import { To } from 'react-router'
import * as PropTypes from 'prop-types'
import { useHref, useLocation, useMatch, useNavigate, Location } from 'react-router-dom';
import { Link } from 'react-router-dom';
import { PathMatch } from 'react-router';
import { classes } from '../Globals';

const isModifiedEvent = (event: React.MouseEvent<any>) =>
  !!(event.metaKey || event.altKey || event.ctrlKey || event.shiftKey)

interface LinkContainerProps extends React.AnchorHTMLAttributes<HTMLAnchorElement> {
  to: To;
  replace?: boolean;
  onClick?: (e: React.MouseEvent<any>) => void;
  innerRef?: (e: any) => void;
  activeClassName?: string;
  activeStyle?: React.CSSProperties;
  strict?: boolean;
  exact?: boolean;
  state?: any;
  isActive?: boolean | ((m: PathMatch | null, l: Location) => boolean);
}


export function LinkContainer({
  children,
  onClick,
  replace, // eslint-disable-line no-unused-vars
  to,
  activeClassName,
  className,
  activeStyle,
  style,
  isActive: getIsActive,
  state,
  // eslint-disable-next-line comma-dangle
  ...props
}: LinkContainerProps): React.ReactElement {

  const path = typeof to === 'object' ? to.pathname || '' : to;
  const navigate = useNavigate();
  const href = useHref(typeof to === 'string' ? { pathname: to } : to);
  const match = useMatch(path);
  const location = useLocation();
  const child = React.Children.only(children) as React.ReactElement;

  const isActive = !!(getIsActive
    ? typeof getIsActive === 'function'
      ? getIsActive(match, location)
      : getIsActive
    : match);

  const handleClick = (event: React.MouseEvent) => {
    if ((child?.props as any).onClick) {
      (child.props as any).onClick(event);
    }

    if (onClick) {
      onClick(event);
    }

    if (
      !event.defaultPrevented && // onClick prevented default
      event.button === 0 && // ignore right clicks
      !isModifiedEvent(event) // ignore clicks with modifier keys
    ) {
      event.preventDefault();

      navigate(to, {
        replace: replace,
        state,
      });
    }
  };

  return React.cloneElement(child, {
    ...(props as any),
    className: classes(className, (child.props as any).className,isActive ? (activeClassName ?? "active") : null),
    style: isActive ? { ...style, ...activeStyle } : style,
    href,
    onClick: handleClick,
  });
}

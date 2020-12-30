import * as React from 'react'
import * as PropTypes from 'prop-types'
import * as H from 'history';
import { Route, match, __RouterContext as RouterContext, matchPath, RouteComponentProps } from 'react-router';
import { Link } from 'react-router-dom';

const isModifiedEvent = (event: React.MouseEvent<any>) =>
  !!(event.metaKey || event.altKey || event.ctrlKey || event.shiftKey)

interface LinkContainerProps extends React.AnchorHTMLAttributes<HTMLAnchorElement> {
  to: H.LocationDescriptor<any>;
  replace?: boolean;
  onClick?: (e: React.MouseEvent<any>) => void;
  innerRef?: (e: any) => void;
  strict?: boolean;
  exact?: boolean;
  isActive?: (m: match<any> | null, l: H.Location<any>) => boolean;
}

export function LinkContainer(p: LinkContainerProps) {


  function handleClick(event: React.MouseEvent<any>, context: RouteComponentProps) {
    if (p.onClick)
      p.onClick(event)

    if (
      !event.defaultPrevented && // onClick prevented default
      event.button === 0 && // ignore everything but left clicks
      !p.target && // let browser handle "target=_blank" etc.
      !isModifiedEvent(event) // ignore clicks with modifier keys
    ) {
      event.preventDefault()

      const history = context.history
      const { replace, to } = p

      if (replace) {
        history.replace(to as string)
      } else {
        history.push(to as string)
      }
    }
  }


  const { exact, strict, isActive: getIsActive, children, replace, to, innerRef, ...props } = p;// eslint-disable-line no-unused-vars

  return (
    <RouterContext.Consumer>
      {context => {
        if (!context)
          throw new Error('You should not use <LinkContainer> outside a <Router>');

        const child = React.Children.only(children) as React.ReactElement<any>;

        if (!child)
          throw new Error("LinkContainer should contain a child");

        const href = context.history.createHref(
          typeof to === 'string' ? { pathname: to } : to
        )

        var path = typeof to === 'object' ? to.pathname : to;

        const match = path ? matchPath(path, { exact, strict }) : null;
        const isActive = !!(getIsActive ? getIsActive(match, context.location) : match);

        return React.cloneElement(
          child,
          {
            ...props,
            active: isActive,
            href,
            onClick: (e: React.MouseEvent<any>) => handleClick(e, context),
          }
        );
      }}
    </RouterContext.Consumer>
  );
}

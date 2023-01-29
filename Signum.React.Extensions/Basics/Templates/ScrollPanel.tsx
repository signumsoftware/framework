import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes } from '@framework/Globals'

export interface ScrollPanelsProps {
  children: React.ReactElement<ScrollPanelProps>[];
  backId?: string;
}


export class ScrollPanels extends React.Component<ScrollPanelsProps> {

  render() {
    return (
      <div>
        <ul className="nav nav-pills" role="tablist" id={this.props.backId}>
          {
            this.props.children.map(p =>
              <li className="nav-item" role="presentation" key={p.props.id}>
                <Scrollchor to={p.props.id}>{p.props.title}</Scrollchor>
              </li>
            )
          }
        </ul>
        {
          React.Children.map(this.props.children,
            (p, i) => React.cloneElement((p as React.ReactElement<any>), { eventKey: i, key: i, backId: this.props.backId }))
        }
      </div>);
  }
}

export interface ScrollPanelProps {
  id: string;
  title: React.ReactNode;
  backId?: string;
  children: React.ReactNode;
}

export class ScrollPanel extends React.Component<ScrollPanelProps> {
  render() {
    return (
      <div>
        <h3 id={this.props.id}>{this.props.title}&nbsp;{this.props.backId && <Scrollchor to={this.props.backId} className="float-end flip"><small><FontAwesomeIcon icon="arrow-turn-up" /></small></Scrollchor>}</h3>
        {this.props.children}
      </div>
    );
  }
}

export interface ScrollchorProps extends React.AnchorHTMLAttributes<HTMLAnchorElement> {
  to: string;
}

export class Scrollchor extends React.Component<ScrollchorProps> {

  handleClick = (event: React.MouseEvent<any>) => {
    event && event.preventDefault();
    const id = animateScroll(this.props.to, { offset: 0, duration: 500, easing: easeOutQuad });
  }

  render() {
    const { to, ...props } = this.props; // eslint-disable-line no-unused-vars

    return !this.props.children
      ? null
      : <a className="nav-link" {...props} href={'#' + this.props.to} onClick={this.handleClick} />;
  }
}

function easeOutQuad(t: number, start: number, change: number, duration: number) {
  return -change * (t /= duration) * (t - 2) + start;
}

interface AnimateScroll {
  offset: number;
  duration: number;
  easing: (t: number, start: number, change: number, duration: number) => number;
}

export function animateScroll(id: string, animate: AnimateScroll) {
  const element = id ? document.getElementById(id) : document.body;

  if (!element) {
    console.warn(`Cannot find element: #${id}`);
    return null;
  }

  const { offset, duration, easing } = animate;
  const parent = getScrollParent(element);
  if (!parent) {
    console.warn(`Element #${id} has no scroll parent`);
    return null;
  }
  const start = getScrollTop(parent);
  const to = getOffsetTop(element, parent) + offset;
  const change = to - start;

  function animateFn(elapsedTime = 0) {
    const increment = 20;
    const elapsed = elapsedTime + increment;
    const position = easing(elapsed, start, change, duration);
    setScrollTop(parent, position);
    elapsed < duration &&
      setTimeout(function () {
        animateFn(elapsed);
      }, increment);
  }

  animateFn();
  return id;
}

function getScrollTop(element: HTMLElement): number {
  if (element == document.documentElement)
    return document.documentElement.scrollTop || document.body.scrollTop /*Edge*/;
  else
    return element.scrollTop;
}

function setScrollTop(element: HTMLElement, value: number) {
  if (element == document.documentElement) {
    document.documentElement.scrollTop = value;
    document.body.scrollTop = value;/*Edge*/
  } else {
    element.scrollTop = value;
  }
}

function getScrollParent(element: HTMLElement, includeHidden: boolean = false) {
  var style = getComputedStyle(element);
  var excludeStaticParent = style.position === "absolute";
  var overflowRegex = includeHidden ? /(auto|scroll|hidden)/ : /(auto|scroll)/;

  if (style.position === "fixed") return document.body;
  for (var parent: HTMLElement = element; (parent = parent.parentElement!);) {
    style = getComputedStyle(parent);
    if (excludeStaticParent && style.position === "static") {
      continue;
    }
    if (overflowRegex.test(style.overflow! + style.overflowY! + style.overflowX!))
      return parent;
  }

  return document.documentElement;
}

function getOffsetTop(element: HTMLElement, scrollParent: HTMLElement) {
  const { top } = element.getBoundingClientRect();
  return top + getScrollTop(scrollParent);
}

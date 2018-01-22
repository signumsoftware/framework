import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'

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
                            <li role="presentation" key={p.props.id}>
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
}

export class ScrollPanel extends React.Component<ScrollPanelProps> {
    render() {
        return (
            <div>
                <h3 id={this.props.id}>{this.props.title}&nbsp;{this.props.backId && <Scrollchor to={this.props.backId} className="pull-right flip"><small><i className="fa fa-level-up" /></small></Scrollchor>}</h3>
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
            : <a {...props} href={'#' + this.props.to} onClick={this.handleClick} />;
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
    console.warn(`Cannot find element: #${id}`);

    if (!element) {
        return null;
    }

    const { offset, duration, easing } = animate;
    const start = getScrollTop();
    const to = getOffsetTop(element) + offset;
    const change = to - start;

    function animateFn(elapsedTime = 0) {
        const increment = 20;
        const elapsed = elapsedTime + increment;
        const position = easing(elapsed, start, change, duration);
        setScrollTop(position);
        elapsed < duration &&
            setTimeout(function () {
                animateFn(elapsed);
            }, increment);
    }

    animateFn();
    return id;
}

export function updateHistory(id: string) {
    window.location.hash = id;
}

function getScrollTop() {
    // like jQuery -> $('html, body').scrollTop
    return document.documentElement.scrollTop || document.body.scrollTop;
}

function setScrollTop(position: number) {
    document.documentElement.scrollTop = document.body.scrollTop = position;
}

function getOffsetTop(element: HTMLElement) {
    const { top } = element.getBoundingClientRect();
    return top + getScrollTop();
}
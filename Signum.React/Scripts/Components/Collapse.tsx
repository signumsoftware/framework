import * as React from 'react';
import * as PropTypes from 'prop-types';
import Transition, { TransitionProps, EnterHandler, ExitHandler } from 'react-transition-group/Transition';
import { classes, Dic } from '../Globals';

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

}

const transitionStatusToClassHash = {
    entering: 'collapsing',
    entered: 'collapse show',
    exiting: 'collapsing',
    exited: 'collapse',
};

function getHeight(node: HTMLElement) {
    return node.scrollHeight;
}

export class Collapse extends React.Component<CollapseProps, { height: number | null }> {

    static defaultProps = {
        ...(Transition as any).defaultProps,
        isOpen: false,
        appear: false,
        enter: true,
        exit: true,
        tag: 'div',
        timeout: 600,
    };

    constructor(props: CollapseProps) {
        super(props);

        this.state = {
            height: null
        };
    }

    onEntering = (node: HTMLElement, isAppearing: boolean) => {
        this.setState({ height: getHeight(node) });
        this.props.onEntering!(node, isAppearing);
    }

    onEntered = (node: HTMLElement, isAppearing: boolean) => {
        this.setState({ height: null });
        this.props.onEntered!(node, isAppearing);
    }

    onExit = (node: HTMLElement) => {
        this.setState({ height: getHeight(node) });
        this.props.onExit!(node);
    }

    onExiting = (node: HTMLElement) => {
        // getting this variable triggers a reflow
        const _unused = node.offsetHeight; // eslint-disable-line no-unused-vars
        this.setState({ height: 0 });
        this.props.onExiting!(node);
    }

    onExited = (node: HTMLElement) => {
        this.setState({ height: null });
        this.props.onExited!(node);
    }

    render() {
        const {
            tag,
            isOpen,
            navbar,
            children,
            attrs,
            timeout,
            onEnter
        } = this.props;

        const { height } = this.state;

        // In NODE_ENV=production the Transition.propTypes are wrapped which results in an
        // empty object "{}". This is the result of the `react-transition-group` babel
        // configuration settings. Therefore, to ensure that production builds work without
        // error, we can either explicitly define keys or use the Transition.defaultProps.
        // Using the Transition.defaultProps excludes any required props. Thus, the best
        // solution is to explicitly define required props in our utilities and reference these.
        // This also gives us more flexibility in the future to remove the prop-types
        // dependency in distribution builds (Similar to how `react-transition-group` does).
        // Note: Without omitting the `react-transition-group` props, the resulting child
        // Tag component would inherit the Transition properties as attributes for the HTML
        // element which results in errors/warnings for non-valid attributes.
     
        var Tag = tag!; 

        return (
            <Transition
                timeout={timeout!}
                in={isOpen}
                onEnter={onEnter}
                onEntering={this.onEntering}
                onEntered={this.onEntered}
                onExit={this.onExit}
                onExiting={this.onExiting}
                onExited={this.onExited}
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
}
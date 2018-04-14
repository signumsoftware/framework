import * as React from 'react'
import * as PropTypes from 'prop-types'
import * as H from 'history';
import { Route, match } from 'react-router';

const isModifiedEvent = (event: React.MouseEvent<any>) =>
    !!(event.metaKey || event.altKey || event.ctrlKey || event.shiftKey)


interface LinkContainerProps extends React.AnchorHTMLAttributes<HTMLAnchorElement> {
    to: H.LocationDescriptor;
    replace?: boolean;
    onClick?: (e: React.MouseEvent<any>) => void;
    innerRef?: (e: any) => void;
    strict?: boolean;
    exact?: boolean;
    isActive?: (m: match<any>, l: H.Location) => boolean;
}

export class LinkContainer extends React.Component<LinkContainerProps> {
    static propTypes = {
        onClick: PropTypes.func,
        target: PropTypes.string,
        replace: PropTypes.bool,
        to: PropTypes.oneOfType([
            PropTypes.string,
            PropTypes.object
        ]).isRequired,
        innerRef: PropTypes.oneOfType([
            PropTypes.string,
            PropTypes.func
        ])
    }

    static defaultProps = {
        replace: false
    }

    static contextTypes = {
        router: PropTypes.shape({
            history: PropTypes.shape({
                push: PropTypes.func.isRequired,
                replace: PropTypes.func.isRequired,
                createHref: PropTypes.func.isRequired
            }).isRequired
        }).isRequired
    }

    handleClick = (event: React.MouseEvent<any>) => {
        if (this.props.onClick)
            this.props.onClick(event)

        if (
            !event.defaultPrevented && // onClick prevented default
            event.button === 0 && // ignore everything but left clicks
            !this.props.target && // let browser handle "target=_blank" etc.
            !isModifiedEvent(event) // ignore clicks with modifier keys
        ) {
            event.preventDefault()

            const { history } = this.context.router
            const { replace, to } = this.props

            if (replace) {
                history.replace(to)
            } else {
                history.push(to)
            }
        }
    }


    render() {
        const { exact, strict, isActive: getIsActive, children, replace, to, innerRef, ...props } = this.props // eslint-disable-line no-unused-vars

        if (!this.context.router)
            throw new Error('You should not use <LinkContainer> outside a <Router>');

        const child = React.Children.only(children);

        if (!child)
            throw new Error("LinkContainer should contain a child");

        const href = this.context.router.history.createHref(
            typeof to === 'string' ? { pathname: to } : to
        )

        return (
            <Route
                path={typeof to === 'object' ? to.pathname : to}
                exact={exact}
                strict={strict}
                children={({ location, match }) => {
                    const isActive = !!(getIsActive ? getIsActive(match, location) : match);

                    return React.cloneElement(
                        child,
                        {
                            ...props,
                            active: isActive,
                            href,
                            onClick: this.handleClick,
                        }
                    );
                }}
            />
        );
    }
}
import * as React from 'react'
import { TabContent, TabPane, NavItem, Nav, NavLink } from 'reactstrap'
import { classes } from './Globals';


interface UncontrolledTabsProps extends React.HTMLAttributes<HTMLDivElement> {
    defaultEventKey?: string | number;
    onToggled?: (eventKey: string | number) => void;
    unmountOnExit?: boolean;
    children?: React.ReactFragment;
}

interface UncontrolledTabsState {
    activeEventKey: string | number | undefined;
}

export class UncontrolledTabs extends React.Component<UncontrolledTabsProps, UncontrolledTabsState> {

    constructor(props: UncontrolledTabsProps) {
        super(props);
        this.state = {
            activeEventKey: props.defaultEventKey != null ? props.defaultEventKey : getFirstEventKey(props.children)
        };
    } 

    componentWillReceiveProps(newProps: UncontrolledTabsProps) {
        if (this.state.activeEventKey == undefined) {
            const newEventKey = getFirstEventKey(newProps.children);
            if (newEventKey != null)
                this.setState({ activeEventKey: newEventKey });
        } else {
            var array = (React.Children.toArray(newProps.children) as React.ReactElement<TabProps>[]);

            if (!array.some(a => a.props.eventKey == this.state.activeEventKey)) {
                const newEventKey = getFirstEventKey(newProps.children);
                this.setState({ activeEventKey: newEventKey });
            }
        }
    }


    handleToggle = (eventKey: string | number) => {
        if (this.state.activeEventKey !== eventKey) {
            this.setState({ activeEventKey: eventKey }, () => {
                if (this.props.onToggled)
                    this.props.onToggled(eventKey);
            });
        }
    }

    render() {

        const { unmountOnExit, children, defaultEventKey } = this.props;

        return (
            <Tabs activeEventKey={this.state.activeEventKey} unmountOnExit={unmountOnExit} toggle={this.handleToggle}>
                {children}
            </Tabs>
        );
    }
}



interface TabsProps extends React.HTMLAttributes<HTMLDivElement> {
    activeEventKey: string | number | undefined;
    toggle: (eventKey: string | number) => void;
    unmountOnExit?: boolean;
}

function getFirstEventKey(children: React.ReactNode) {
    const array = React.Children.toArray(children);

    if (array && array.length)
        return (array[0] as React.ReactElement<TabProps>).props.eventKey;

    return undefined;
}

export class Tabs extends React.Component<TabsProps> {
    
    render() {

        var { activeEventKey, children, unmountOnExit, toggle, ...attrs } = this.props;

        var array = (React.Children.toArray(this.props.children) as React.ReactElement<TabProps>[]);

        return (
            <div {...attrs}>
                <Nav tabs>
                    {array.map(t =>
                        <NavItem key={t.props.eventKey}>
                            <NavLink
                                className={classes(this.props.activeEventKey == t.props.eventKey && "active")}
                                onClick={() => this.props.toggle(t.props.eventKey)}
                            >
                                {t.props.title}
                            </NavLink>
                        </NavItem>
                    )}
                </Nav>
                {array.filter(a => !this.props.unmountOnExit || a.props.eventKey == this.props.activeEventKey)}
            </div>
        );
    }
}

interface TabProps extends React.HTMLAttributes<any> {
    eventKey: string | number;
    title?: string /*| React.ReactChild*/;
}

export class Tab extends React.Component<TabProps> {
    render() {
        var { children, eventKey, title, ...rest } = this.props;

        return (
            <TabPane tabId={eventKey} {...rest}>
                {children}
            </TabPane>
        );
    }
}
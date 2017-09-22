import * as React from 'react'
import { TabContent, TabPane, NavItem, Nav, NavLink } from 'reactstrap'
import { classes } from './Globals';


interface TabsProps extends React.HTMLAttributes<HTMLDivElement> {
    defaultEventKey?: string | number;
    children: React.ReactNode;
    unmountOnExit?: boolean;
}

interface TabsState {
    activeEventKey: string | number | undefined;
}

function getFirstEventKey(children: React.ReactNode) {
    const array = React.Children.toArray(children);

    if (array && array.length)
        return (array[0] as React.ReactElement<TabProps>).props.eventKey;

    return undefined;

}

export class Tabs extends React.Component<TabsProps, TabsState> {
    constructor(props: TabsProps) {
        super(props);
        
        this.state = {
            activeEventKey: props.defaultEventKey != null ? props.defaultEventKey : getFirstEventKey(props.defaultEventKey)
        };
    } 

    componentWillReceiveProps(newProps: TabsProps) {
        if (this.state.activeEventKey == undefined) {
            const newEventKey = getFirstEventKey(newProps.defaultEventKey);
            if (newEventKey != null)
                this.setState({ activeEventKey: newEventKey });
        } else {
            var array = (React.Children.toArray(newProps.children) as React.ReactElement<TabProps>[]);

            if (!array.some(a => a.props.eventKey == this.state.activeEventKey)) {
                const newEventKey = getFirstEventKey(newProps.defaultEventKey);
                this.setState({ activeEventKey: newEventKey });
            }

        }
    }

    handleToggle = (tabId: string | number) => {
        if (this.state.activeEventKey !== tabId) {
            this.setState({ activeEventKey: tabId });
        }
    }

    render() {

        var { defaultEventKey, children, unmountOnExit, ...attrs } = this.props;

        var array = (React.Children.toArray(this.props.children) as React.ReactElement<TabProps>[]);

        return (
            <div {...attrs}>
                <Nav tabs>
                    {array.map(t =>
                        <NavItem key={t.props.eventKey}>
                            <NavLink
                                className={classes(this.state.activeEventKey == t.props.eventKey && "active")}
                                onClick={() => this.handleToggle(t.props.eventKey)}
                            >
                                {t.props.title}
                            </NavLink>
                        </NavItem>
                    )}
                </Nav>
                {array.filter(a => !this.props.unmountOnExit || a.props.eventKey == this.state.activeEventKey)}
            </div>
        );
    }
}

interface TabProps extends React.HTMLAttributes<any> {
    eventKey: string | number;
    title: string /*| React.ReactChild*/;
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
import * as React from 'react'
import * as PropTypes from 'prop-types'
import { classes } from '../Globals';
import { ErrorBoundary } from './ErrorBoundary';

interface UncontrolledTabsProps extends React.HTMLAttributes<HTMLDivElement> {
  defaultEventKey?: string | number;
  onToggled?: (eventKey: string | number) => void;
  children?: React.ReactFragment;
  hideOnly?: boolean;
  pills?: boolean;
  fill?: boolean;
}

interface UncontrolledTabsState {
  activeEventKey: string | number | undefined;
}

export class UncontrolledTabs extends React.Component<UncontrolledTabsProps, UncontrolledTabsState> {

  constructor(props: UncontrolledTabsProps) {
    super(props);
    this.state = {
      activeEventKey: getDefaultEventKey(props)
    };
  }

  componentWillReceiveProps(newProps: UncontrolledTabsProps) {
    if (this.props.defaultEventKey != newProps.defaultEventKey ||
      this.state.activeEventKey == null ||
      !(React.Children.toArray(newProps.children) as React.ReactElement<TabProps>[]).some(a => a.props.eventKey == this.state.activeEventKey)) {

      var newActiveKey = getDefaultEventKey(newProps);

      if (newActiveKey != this.state.activeEventKey)
        this.setState({ activeEventKey: newActiveKey });
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

    const { children, defaultEventKey, hideOnly, fill, pills } = this.props;

    return (
      <Tabs activeEventKey={this.state.activeEventKey} toggle={this.handleToggle} fill={fill} pills={pills} hideOnly={hideOnly}>
        {children}
      </Tabs>
    );
  }
}


function getDefaultEventKey(props: UncontrolledTabsProps) {
  const array = React.Children.toArray(props.children) as React.ReactElement<TabProps>[];

  if (array.some(a => a.props.eventKey == props.defaultEventKey))
    return props.defaultEventKey;

  if (array && array.length)
    return array[0].props.eventKey;

  return undefined;
}


interface TabsProps extends React.HTMLAttributes<HTMLDivElement> {
  activeEventKey: string | string[] | number | undefined;
  toggle: (eventKey: string | number) => void;
  hideOnly?: boolean;
  pills?: boolean;
  fill?: boolean;
}

export class Tabs extends React.Component<TabsProps> {

  render() {

    var { activeEventKey, children, toggle, hideOnly, pills, fill, ...attrs } = this.props;

    var allChildren = React.Children.toArray(this.props.children);

    var tabs = allChildren.filter(a => typeof a == "object" && (a as React.ReactElement<any>).type == Tab) as React.ReactElement<TabProps>[];

    var noTabs = allChildren.filter(a => !tabs.contains(a as any));

    return (
      <div {...attrs}>
        <ul className={"nav " + (pills ? "nav-pills" : "nav-tabs") + (fill ? " nav-fill" : "")}>
          {tabs.map(t =>
            <li className="nav-item" key={t.props.eventKey} data-eventkey={t.props.eventKey}>
              <a href="#"
                className={classes("nav-link", this.props.activeEventKey == t.props.eventKey && "active")}
                onClick={e => {
                  e.preventDefault();
                  this.props.toggle(t.props.eventKey)
                }}
                {...t.props.anchorHtmlProps}
              >
                {t.props.title}
              </a>
            </li>
          )}
          {
            noTabs.map((nt, i) => <li key={"nt" + i} className="nav-item no-tab">
              {nt}
            </li>)
          }
        </ul>
        {hideOnly ?
          tabs.map(elem => React.cloneElement(elem, ({ style: elem.props.eventKey == this.props.activeEventKey ? undefined : { display: "none" } }) as React.HTMLAttributes<any>)) :
          tabs.filter(elem => elem.props.eventKey == this.props.activeEventKey)
        }
      </div>
    );
  }
}

interface TabProps extends React.HTMLAttributes<any> {
  eventKey: string | number;
  title?: string /* | React.ReactChild*/;
  anchorHtmlProps?: React.HTMLAttributes<HTMLAnchorElement>;
}

export class Tab extends React.Component<TabProps> {

  static contextTypes = {
    activeTabId: PropTypes.any
  };

  render() {
    var { children, eventKey, title, anchorHtmlProps, ...rest } = this.props;

    return (
      <div className={classes("tab-pane", this.props.eventKey == this.context.activeTabId)} {...rest}>
        <ErrorBoundary>
          {children}
        </ErrorBoundary>
      </div>
    );
  }
}

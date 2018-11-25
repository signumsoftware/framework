import * as React from 'react'
import { classes } from '../Globals'
import { JavascriptMessage } from '../Signum.Entities'
import { Transition } from 'react-transition-group'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { IconProp } from '@fortawesome/fontawesome-svg-core';
import "./Notify.css"

type NotifyType = "warning" | "error" | "success" | "loading";

interface NotifyOptions {
  text: React.ReactChild;
  type: NotifyType;
}

interface NotifyState {
  text?: React.ReactChild;
  type?: NotifyType;
}

export default class Notify extends React.Component<{}, NotifyState>{

  static singleton: Notify;

  constructor(props: {}) {
    super(props);
    this.state = { text: undefined, type: undefined };

    Notify.singleton = this;
  }

  _isMounted: boolean = false;
  componentDidMount() {
    this._isMounted = true;
  }

  componentWillUnmount() {
    this._isMounted = false;
  }

  handler?: number;
  notifyTimeout(options: NotifyOptions, timeout: number = 2000) {
    this.notify(options);
    this.handler = setTimeout(() => this.clear(), timeout);
  }

  notify(options: NotifyOptions) {
    if (!this._isMounted)
      return;
    if (this.handler != undefined) {
      clearTimeout(this.handler);
      this.handler = undefined;
    }
    this.setState(options);
  }

  clear() {
    if (!this._isMounted)
      return;
    if (this.handler != undefined) {
      clearTimeout(this.handler);
      this.handler = undefined;
    }
    this.setState({ text: undefined, type: undefined })
  }


  notifyPendingRequest(pending: number) {
    if (pending)
      this.notify({ text: JavascriptMessage.loading.niceToString(), type: "loading" });
    else
      this.clear();
  }

  getIcon() {
    if (!this.state.type) {
      return undefined;
    }

    var icon: IconProp | undefined;
    switch (this.state.type) {
      case "loading":
        icon = "cog";
        break;
      case "error":
      case "warning":
        icon = "exclamation";
        break;
      case "success":
        icon = "check";
        break;
      default:
        break;
    }

    if (icon) {
      return <FontAwesomeIcon icon={icon} fixedWidth style={{ fontSize: "larger" }} spin={this.state.type === "loading"} />
    }
    else {
      return undefined;
    }
  }

  render() {
    return (
      <div id="sfNotify">
        <Transition in={this.state.text != undefined} timeout={200}>
          {(state: string) => <span className={classes(this.state.type, "notify", state == "entering" || state == "entered" ? "in" : undefined)}>{this.getIcon()}&nbsp;{this.state.text}</span>}
        </Transition>
      </div>
    );
  }
}


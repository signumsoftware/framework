import * as React from 'react'
import { classes } from '../Globals'
import { JavascriptMessage } from '../Signum.Entities'
import { Transition } from 'react-transition-group'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { IconProp } from '@fortawesome/fontawesome-svg-core';
import "./Notify.css"
import { namespace } from 'd3';

type NotifyType = "warning" | "error" | "success" | "loading";

interface NotifyOptions {
  text: React.ReactChild;
  type: NotifyType;
}


interface NotifyHandle {
  notify(options: NotifyOptions) : void;
  notifyTimeout(options: NotifyOptions, timeout?: number): void
}


export default function Notify() {

  const [options, setOptions] = React.useState<NotifyOptions | undefined>(undefined);

  const handler = React.useRef<number | undefined>(undefined);

  function notify(options: NotifyOptions) {
    if (handler.current != undefined) {
      clearTimeout(handler.current);
      handler.current = undefined;
    }
    setOptions(options);
  }

  function notifyTimeout(options: NotifyOptions, timeout: number = 2000) {
    notify(options);
    handler.current = setTimeout(() => clear(), timeout);
  }

  function notifyPendingRequest(pending: number) {
    if (pending)
      notify({ text: JavascriptMessage.loading.niceToString(), type: "loading" });
    else
      clear();
  }

  function clear() {
    if (handler.current != undefined) {
      clearTimeout(handler.current);
      handler.current = undefined;
    }
    setOptions(undefined);
  }

  React.useEffect(() => {

    Notify.singleton = {
      notify: notify,
      notifyTimeout: notifyTimeout
    };

    return () => Notify.singleton = undefined;
  }, [options, handler]);



  function getIcon() {
    if (!options) {
      return undefined;
    }

    var icon: IconProp | undefined;
    switch (options.type) {
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
      return <FontAwesomeIcon icon={icon} fixedWidth style={{ fontSize: "larger" }} spin={options.type === "loading"} />
    }
    else {
      return undefined;
    }
  }

  const styleLock: React.CSSProperties | undefined = (Notify.lockScreenOnNotify && options && options.type === "loading") ?
    { zIndex: 100000, position: "fixed", width: "100%", height: "100%" } : undefined;

  return (
    <div style={styleLock}>
      <div id="sfNotify" >
        <Transition in={options != undefined} timeout={200}>
          {(state: string) => <span className={classes(options && options.type, "notify", state == "entering" || state == "entered" ? "in" : undefined)}>{getIcon()}&nbsp;{options && options.text}</span>}
        </Transition>
      </div>
    </div>
  );
}


Notify.singleton = undefined as (NotifyHandle | undefined);
Notify.lockScreenOnNotify = false;

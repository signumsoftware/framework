import * as React from 'react'
import { classes } from '../Globals'
import { JavascriptMessage } from '../Signum.Entities'
import { Transition } from 'react-transition-group'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { IconProp } from '@fortawesome/fontawesome-svg-core';
import "./Notify.css"
import { useForceUpdate } from '../Hooks';

type NotifyType = "warning" | "error" | "success" | "loading";

interface NotifyOptions {
  text: React.ReactChild;
  type: NotifyType;
  priority?: number; 
  timeoutHandler?: number;
}

interface NotifyHandle {
  notify(options: NotifyOptions): NotifyOptions;
  notifyTimeout(options: NotifyOptions, timeout?: number): NotifyOptions
  notifyPendingRequest(pending: number): void;
  remove(options: NotifyOptions): void;
}

export default function Notify() {

  const forceUpdate = useForceUpdate();

  const optionsStack = React.useRef<NotifyOptions[]>([]);

  function notify(options: NotifyOptions) {

    if (options.priority == null)
      options.priority = 10;

    if (options.timeoutHandler)
      clearTimeout(options.timeoutHandler);

    optionsStack.current.extract(a => a == options);
    optionsStack.current.push(options);
    forceUpdate();
    return options;
  }

  function notifyTimeout(options: NotifyOptions, timeout: number = 2000): NotifyOptions {
    notify(options);

    options.timeoutHandler = setTimeout(() => remove(options), timeout);
    return options;
  }

  const loadingRef = React.useRef<NotifyOptions>({ text: JavascriptMessage.loading.niceToString(), type: "loading", priority: 0 });

  function notifyPendingRequest(pending: number) {
    if (pending)
      notify(loadingRef.current);
    else
      remove(loadingRef.current);
  }

  function remove(options: NotifyOptions) {
    if (options.timeoutHandler)
      clearTimeout(options.timeoutHandler);

    optionsStack.current.extract(a => a == options);
    forceUpdate();
  }

  React.useEffect(() => {

    Notify.singleton = {
      notify: notify,
      notifyTimeout: notifyTimeout,
      notifyPendingRequest: notifyPendingRequest,
      remove: remove,
    };

    return () => Notify.singleton = undefined;
  }, []);

  var opt = optionsStack.current.orderByDescending(a => a.priority).firstOrNull();

  function getIcon() {
    if (!opt) {
      return undefined;
    }

    var icon: IconProp | undefined;
    switch (opt.type) {
      case "loading":
        icon = "gear";
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
      return <FontAwesomeIcon icon={icon} fixedWidth style={{ fontSize: "larger" }} spin={opt.type === "loading"} />
    }
    else {
      return undefined;
    }
  }

  const styleLock: React.CSSProperties | undefined = (Notify.lockScreenOnLoading && optionsStack.current.some(o => o.type == "loading") ?
    { zIndex: 100000, position: "fixed", width: "100%", height: "100%" } : undefined);

  return (
    <div style={styleLock}>
      <div id="sfNotify" >
        <Transition in={opt != undefined} timeout={200}>
          {/*translation is made off for this span to avoid rendering error (Failed to execute 'removeChild' on 'Node': The node to be removed is not a child of this node.) 
           * that occurs with Google/Microsoft page translator extensions*/}
          {(state: string) => <span className={classes(opt?.type, "notify", state == "entering" || state == "entered" ? "in" : undefined, "notranslate")} translate="no">{getIcon()}&nbsp;{opt?.text}</span>}
        </Transition>
      </div>
    </div>
  );
}


Notify.singleton = undefined as (NotifyHandle | undefined);
Notify.lockScreenOnLoading = false;

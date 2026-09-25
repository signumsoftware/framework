import * as React from 'react'
import { classes } from '../Globals'
import { JavascriptMessage } from '../Signum.Entities'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { IconProp } from '@fortawesome/fontawesome-svg-core';
import "./Notify.css"
import { useForceUpdate } from '../Hooks';

type NotifyType = "warning" | "error" | "success" | "loading";

export interface NotifyOptions {
  text: string | React.ReactElement;
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

function Notify(): React.ReactElement {

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

    options.timeoutHandler = window.setTimeout(() => remove(options), timeout);
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

    Notify.setSingleton({
      notify: notify,
      notifyTimeout: notifyTimeout,
      notifyPendingRequest: notifyPendingRequest,
      remove: remove,
    });

    return () => Notify.setSingleton(undefined);
  }, []);


  const [visible, setVisible] = React.useState(false);
  const [currentOpt, setCurrentOpt] = React.useState<NotifyOptions | null>(null);

  React.useEffect(() => {
    const top = optionsStack.current.orderByDescending(a => a.priority).firstOrNull();
    setCurrentOpt(top);
    setVisible(!!top);
  }, [optionsStack.current.length]);

  function getIcon(opt: NotifyOptions) {
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
      return <FontAwesomeIcon icon={icon} className="fa-fw" style={{ fontSize: "larger" }} spin={opt.type === "loading"} />
    }
    else {
      return undefined;
    }
  }

  const styleLock: React.CSSProperties | undefined = (Notify.Options.lockScreenOnLoading && optionsStack.current.some(o => o.type == "loading") ?
    { zIndex: 100000, position: "fixed", width: "100%", height: "100%" } : undefined);

  const isError = currentOpt?.type == "error";

  // "loading" is raised for every pending request, so announcing it would be constant chatter for no
  // information; the spinner stays a visual cue only.
  const liveText = currentOpt && currentOpt.type != "loading" ? currentOpt.text : null;

  return (
    <div style={styleLock}>
      {/* The visible message is hidden from assistive technology and the same text is given once through a
          live region below, so it is not read twice. The regions themselves are always in the document,
          empty when there is nothing to say: a live region has to exist before content appears inside it,
          or the insertion is not announced at all — which is why putting the role on the message element
          itself, which mounts and unmounts, would not work. */}
      <div id="sfNotify" >
        {currentOpt && (
          <span aria-hidden={true} className={classes(currentOpt.type, "notify", "in", "notranslate")} translate="no" key={currentOpt.text.toString()}>
            {getIcon(currentOpt)}&nbsp;{currentOpt.text}
          </span>
        )}
      </div>
      <span className="visually-hidden" role="status">{isError ? null : liveText}</span>
      <span className="visually-hidden" role="alert">{isError ? liveText : null}</span>
    </div>
  );
}

namespace Notify {
  let singleton = undefined as (NotifyHandle | undefined);
  export function getSingleton(): NotifyHandle | undefined { return singleton; }
  export function setSingleton(v: NotifyHandle | undefined): void { singleton = v; }

  export const Options = { lockScreenOnLoading: false };
}

export default Notify;


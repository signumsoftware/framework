import * as React from 'react'
import * as History from 'history'
import { FindOptions, ResultTable } from './Search';
import * as Finder from './Finder';
import * as AppContext from './AppContext';
import { Entity, Lite, liteKey, isEntity } from './Signum.Entities';
import { Type, QueryTokenString, newLite } from './Reflection';

export function useForceUpdate(): () => void {
  const [count, setCount] = React.useState(0);
  const forceUpdate = React.useCallback(() => {
    setCount(c => c + 1);
  }, []);

  return forceUpdate
}

export function useUpdatedRef<T>(newValue: T): React.MutableRefObject<T> {
  const ref = React.useRef(newValue);
  ref.current = newValue;
  return ref;
}

export function useForceUpdatePromise(): () => Promise<void> {
  var [count, setCount] = useStateWithPromise(0);
  return () => setCount(c => c + 1) as Promise<any>;
}

export function useInterval<T>(interval: number | undefined | null, initialState: T, newState: (oldState: T) => T, deps?: ReadonlyArray<any>) {
  const [val, setVal] = React.useState(initialState);

  React.useEffect(() => {
    if (interval) {
      var handler = setInterval(() => {
        setVal(s => newState(s));
      }, interval);
      return () => clearInterval(handler);
    }
  }, [interval, ...(deps ?? [])]);

  return val;
}

export function usePrevious<T>(value: T): T | undefined {
  var ref = React.useRef<T | undefined>();
  React.useEffect(() => {
    ref.current = value;
  }, [value]);

  return ref.current;
}

export interface Size {
  width: number;
  height: number;
}

export function whenVisible<T extends HTMLElement>(element: T, callback: (visible: boolean) => void, options?: IntersectionObserverInit) {

  var observer = new IntersectionObserver((entries, observer) => {
    entries.forEach(entry => {
      callback(entry.intersectionRatio > 0);
    });
  }, options);

  observer.observe(element);

  return observer;
}

export function useSize<T extends HTMLElement = HTMLDivElement>(initialTimeout = 0, resizeTimeout = 300): { size: Size | undefined, setContainer: (element: T | null) => void } {
  const [size, setSize] = React.useState<Size | undefined>();
  const divElement = React.useRef<T | null>(null);

  function setNewSize() {
    const rect = divElement.current!.getBoundingClientRect();
    if (size == null || size.width != rect.width || size.height != rect.height)
      setSize({ width: rect.width, height: rect.height });
  }

  const initialHandle = React.useRef<number | null>(null);
  const visibleObserver = React.useRef<IntersectionObserver | null>(null);

  function setContainer(div: T | null) {

    if (initialHandle.current)
      clearTimeout(initialHandle.current);

    if (visibleObserver.current)
      visibleObserver.current.disconnect();

    if (divElement.current = div) {

      if (div.clientHeight == 0 && div.clientWidth == 0)
        setSize(undefined);

      visibleObserver.current = whenVisible(div, (visible) => {
        if (visible) {
          if (initialTimeout)
            initialHandle.current = setTimeout(setNewSize, initialTimeout);
          else
            setNewSize();
        }
        else
          setSize(undefined);
      });
    }
  }

  const setContainerMemo = React.useCallback(setContainer, [divElement]);

  const resizeHandle = React.useRef<number | null>(null);
  React.useEffect(() => {
    function onResize() {
      if (resizeHandle.current != null)
        clearTimeout(resizeHandle.current);

      resizeHandle.current = setTimeout(() => {
        if (divElement.current) {
          setNewSize()
        }
      }, resizeTimeout);
    }

    window.addEventListener('resize', onResize);

    return () => {
      if (resizeHandle.current)
        clearTimeout(resizeHandle.current);

      if (initialHandle.current)
        clearTimeout(initialHandle.current);

      if (visibleObserver.current)
        visibleObserver.current.disconnect();

      window.removeEventListener("resize", onResize);
    };
  }, []);

  return { size, setContainer: setContainerMemo };
}

export function useDocumentEvent<K extends keyof DocumentEventMap>(type: K, listener: (this: Document, ev: DocumentEventMap[K]) => any, deps: any[]): void;
export function useDocumentEvent(type: string, listener: (this: Document, ev: Event) => any, deps: any[]): void;
export function useDocumentEvent(type: string, listener: (this: Document, ev: Event) => any, deps: any[]) : void {
  React.useEffect(() => {
    document.addEventListener(type, listener);
    return () => {
      document.removeEventListener(type, listener);
    }
  }, deps);
}

export function useWindowEvent<K extends keyof WindowEventMap>(type: K, listener: (this: Window, ev: WindowEventMap[K]) => any, deps: any[]): void;
export function useWindowEvent(type: string, listener: (this: Window, evt: Event) => void, deps: any[]): void;
export function useWindowEvent(type: string, listener: (this: Window, evt: Event) => void, deps: any[]): void {
  React.useEffect(() => {
    window.addEventListener(type, listener);
    return () => {
      window.removeEventListener(type, listener);
    }
  }, deps);
}

export function useStateWithPromise<T>(defaultValue: T): [T, (newValue: React.SetStateAction<T>) => Promise<T>] {
  const [state, setState] = React.useState({ value: defaultValue, resolve: (val: T) => { } });

  React.useEffect(() => state.resolve(state.value), [state]);

  return [
    state.value,
    updaterOrValue => new Promise(resolve => {
      setState(prevState => {
        let nextVal = typeof updaterOrValue == "function" ? (updaterOrValue as ((val: T) => T))(prevState.value) : updaterOrValue;
        return {
          value: nextVal,
          resolve: resolve
        };
      })
    })
  ]
}

export interface APIHookOptions {
  avoidReset?: boolean;
}

export function useAPIWithReload<T>(makeCall: (signal: AbortSignal, oldData: T | undefined) => Promise<T>, deps: ReadonlyArray<any>, options?: APIHookOptions): [T | undefined, () => void] {
  const [count, setCount] = React.useState(0);
  const value = useAPI<T>(makeCall, [...(deps || []), count], options);
  return [value, () => setCount(c => c + 1)];
}

export function useAPI<T>(makeCall: (signal: AbortSignal, oldData: T | undefined) => Promise<T>, deps: ReadonlyArray<any>, options?: APIHookOptions): T | undefined {

  const [data, setData] = React.useState<{ deps: ReadonlyArray<any>; result: T } | undefined>(undefined);

  React.useEffect(() => {
    var abortController = new AbortController();

    makeCall(abortController.signal, data && data.result)
      .then(result => !abortController.signal.aborted && setData({ result, deps }))
      .done();

    return () => {
      abortController.abort();
    }
  }, deps);

  if (!(options && options.avoidReset)) {
    if (data && !areEqual(data.deps, deps))
      return undefined;
  }

  return data && data.result;
}

function areEqual(depsA: ReadonlyArray<any>, depsB: ReadonlyArray<any>) {

  if (depsA.length !== depsB.length)
    return false;

  for (var i = 0; i < depsA.length; i++) {
    if (depsA[i] !== depsB[i])
      return false;
  }

  return true;
}

export function useMounted() {
  const mounted = React.useRef<boolean>(true);
  React.useEffect(() => {
    return () => { mounted.current = false; };
  }, []);
  return mounted;
}

export function useThrottle<T>(value: T, limit: number, options?: { enabled?: boolean }): T {
  const [throttledValue, setThrottledValue] = React.useState(value);

  const lastRequested = React.useRef<(undefined | { value: T })>(undefined);
  const handleRef = React.useRef<number | undefined>(undefined);

  function stop(){
    if (handleRef.current)
      clearTimeout(handleRef.current);

    lastRequested.current = undefined;
  }

  React.useEffect(
    () => {
      if (options && options.enabled == false) {
        stop();
      } else {
        if (lastRequested.current) {
          lastRequested.current.value = value;
        } else {
          lastRequested.current = { value };
          handleRef.current = setTimeout(function () {
            setThrottledValue(lastRequested.current!.value);
            stop();
          }, limit);
        }
      }
    },
    [value, options && options.enabled]
  );

  React.useEffect(() => {
    return () => stop();
  }, []);

  return throttledValue;
}

export function useLock<T>(): [/*isLocked:*/boolean, /*lock:*/(makeCall: () => Promise<T>) => Promise<T>] { //TODO: TS4

  const [isLocked, setIsLocked] = React.useState<boolean>(false);

  async function lock(makeCall: () => Promise<T>): Promise<T> {
    if (isLocked)
      throw new Error("Call in Progress");

    setIsLocked(true);
    try {
      return await makeCall();
    } finally {
      setIsLocked(false);
    }
  }

  return [isLocked, lock];
}

export function useHistoryListen(locationChanged: (location: History.Location, action: History.Action) => void, enabled: boolean = true, extraDeps?: ReadonlyArray<any>) {
  const unregisterCallback = React.useRef<History.UnregisterCallback | undefined>(undefined);
  React.useEffect(() => {
    if (!enabled)
      return;

    unregisterCallback.current = AppContext.history.listen(locationChanged);
    return () => { unregisterCallback.current!(); }
  }, [enabled, ...(extraDeps || [])]);
}

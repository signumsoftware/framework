import * as React from 'react';
import * as AppContext from './AppContext';

export function useForceUpdate(): () => void {
  const [count, setCount] = React.useState(0);
  const forceUpdate = React.useCallback(() => {
    setCount(c => c + 1);
  }, []);

  return forceUpdate
}

export function useVersion(): [version: number, updateVersion: () => void] {
  const [version, setVersion] = React.useState(0);
  const updateVersion = React.useCallback(() => {
    setVersion(c => c + 1);
  }, []);

  return [version, updateVersion]
}

export function useUpdatedRef<T>(newValue: T): React.MutableRefObject<T> {
  const ref = React.useRef(newValue);
  ref.current = newValue;
  return ref;
}

export function useForceUpdatePromise(): () => Promise<void> {
  const [ticks, setTick] = React.useState(0); // State used to trigger re-render
  const resolveRef = React.useRef<(value?: void) => void>(undefined); // Ref to store resolve function

  const forceUpdate = React.useCallback(() => new Promise<void>((resolve) => {
      resolveRef.current = resolve; // Store the resolve function
      setTick((tick) => tick + 1); // Trigger re-render
  }), []);

  React.useEffect(() => {
    if (resolveRef.current) {
      resolveRef.current();
      resolveRef.current = undefined;
    }
  }, [ticks]);

  return forceUpdate;
}

export function useInterval<T>(interval: number | undefined | null, initialState: T, newState: (oldState: T) => T, deps?: ReadonlyArray<any>): T {
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
  var ref = React.useRef<T | undefined>(undefined);
  React.useEffect(() => {
    ref.current = value;
  }, [value]);

  return ref.current;
}

export interface Size {
  width: number;
  height: number;
}

export function whenVisible<T extends HTMLElement>(element: T, callback: (visible: boolean) => void, options?: IntersectionObserverInit): IntersectionObserver {

  var observer = new IntersectionObserver((entries, observer) => {
    entries.forEach(entry => {
      callback(entry.intersectionRatio > 0);
    });
  }, options);

  observer.observe(element);

  return observer;
}

export function useSize<T extends HTMLElement = HTMLDivElement>(options?: { 
  initialTimeout?: number ,
   resizeTimeout ? : number, 
  autoResetOnHide?: boolean, 
  deps?: React.DependencyList
}):

  { size: Size | undefined, setContainer: (element: T | null) => void, element: T | null } {

  const initialTimeout = options?.initialTimeout  ?? 0;
  const resizeTimeout = options?.resizeTimeout  ?? 300;
  const autoResetOnHide = options?.autoResetOnHide  ?? false;
  const deps = options?.deps  ?? [];

  const [size, setSize] = React.useState<Size | undefined>();
  const sizeRef = useUpdatedRef(size);
  const htmlElement = React.useRef<T | null>(null);

  function setNewSize() {
    const rect = htmlElement.current!.getBoundingClientRect();
    if (size == null || size.width != rect.width || size.height != rect.height)
      setSize({ width: rect.width, height: rect.height });
  }

  React.useEffect(()=> {
    if(size&& deps.length)
    { 
      setSize(undefined);
      setTimeout(() => {
        setNewSize();
      }, resizeTimeout);
    }
}, deps);

  const initialHandle = React.useRef<number | null>(null);
  const visibleObserver = React.useRef<IntersectionObserver | null>(null);

  function setContainer(div: T | null) {

    if (initialHandle.current)
      clearTimeout(initialHandle.current);

    if (visibleObserver.current)
      visibleObserver.current.disconnect();

    if (htmlElement.current = div) {

      if (div.clientHeight == 0 && div.clientWidth == 0)
        setSize(undefined);

      visibleObserver.current = whenVisible(div, (visible) => {
        if (sizeRef.current != null && !autoResetOnHide)
          return;

        if (visible) {
          if (initialTimeout)
            initialHandle.current = window.setTimeout(setNewSize, initialTimeout);
          else
            setNewSize();
        }
        else 
          setSize(undefined);
      });
    }
  }

  const setContainerMemo = React.useCallback(setContainer, [htmlElement]);

  const resizeHandle = React.useRef<number | null>(null);
  React.useEffect(() => {
    function onResize() {
      if (resizeHandle.current != null)
        clearTimeout(resizeHandle.current);

      resizeHandle.current = window.setTimeout(() => {
        if (htmlElement.current) {
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

  return { size, setContainer: setContainerMemo, element: htmlElement.current };
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

export function useAPIWithReload<T>(makeCall: (signal: AbortSignal, oldData: T | undefined) => Promise<T> | T, deps: ReadonlyArray<any>, options?: APIHookOptions): [T | undefined, () => void] {
  const [count, setCount] = React.useState(0);
  const value = useAPI<T>(makeCall, [...(deps || []), count], options);
  return [value, () => setCount(c => c + 1)];
}

export function useAPI<T>(makeCall: (signal: AbortSignal, oldData: T | undefined) => Promise<T> | T, deps: ReadonlyArray<any>, options?: APIHookOptions): T | undefined {

  const [data, setData] = React.useState<{ deps: ReadonlyArray<any>; result: T } | undefined>(undefined);

  React.useEffect(() => {
    var abortController = new AbortController();

    var promiseOrValue = makeCall(abortController.signal, data && data.result);

    (promiseOrValue instanceof Promise ? promiseOrValue : Promise.resolve(promiseOrValue))
      .then(result => !abortController.signal.aborted && setData({ result, deps }),
        error => {
          if (error instanceof Error && error.name == "AbortError")
            return;

          throw error;
        });

    return () => {
      abortController.abort();
    }
  }, deps);

  if (!(options && options.avoidReset)) {
    if (data && !areEqualDeps(data.deps, deps))
      return undefined;
  }

  return data && data.result;
}

export function areEqualDeps(depsA: React.DependencyList, depsB: React.DependencyList): boolean {

  if (depsA.length !== depsB.length)
    return false;

  for (var i = 0; i < depsA.length; i++) {
    if (depsA[i] !== depsB[i])
      return false;
  }

  return true;
}

export function useMounted(): React.MutableRefObject<boolean> {
  const mounted = React.useRef<boolean>(true);
  React.useEffect(() => {
    return () => { mounted.current = false; };
  }, []);
  return mounted;
}

export function useThrottle<T>(value: T, timeout: number, options?: { enabled?: boolean }): T {
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
          handleRef.current = window.setTimeout(function () {
            setThrottledValue(lastRequested.current!.value);
            stop();
          }, timeout);
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


export const useDoubleClick = (doubleClick: React.MouseEventHandler, click: React.MouseEventHandler, options?: { timeout?: number }): (event: React.MouseEvent) => void => {
  options = {
    timeout: 200,
    ...options,
  };

  /** @type {{ current: number }} */
  const handleRef = React.useRef<number | undefined>(undefined);

  const clearClickTimeout = () => {
    if (handleRef) {
      clearTimeout(handleRef.current);
      handleRef.current = undefined;
    }
  };

  return React.useCallback((event: React.MouseEvent) => {
    clearClickTimeout();
    if (click && event.detail === 1) {
      handleRef.current = window.setTimeout(() => {
        click(event);
      }, options!.timeout);
    }
    if (event.detail % 2 === 0) {
      doubleClick(event);
    }
  }, [click, doubleClick, options.timeout]);
};

export const Breakpoints = {
  sm: 576, /*Phone*/
  md: 768, /*Tablet*/
  lg: 992,
  xl: 1200,
  xxl: 1400,
}

export function getBreakpoint(): number {
  var width = window.innerWidth;
  if (width <= Breakpoints.sm) return 0;
  if (width <= Breakpoints.md) return Breakpoints.sm;
  if (width <= Breakpoints.lg) return Breakpoints.md;
  if (width <= Breakpoints.xl) return Breakpoints.lg;
  if (width <= Breakpoints.xxl) return Breakpoints.xl;
  return Breakpoints.xxl;
}

export function useBreakpoint(): number {

  var breakpointRef = useUpdatedRef(getBreakpoint());

  const forceUpdate = useForceUpdate();

  useWindowEvent("resize", (ev) => {
    var newBreakpoint = getBreakpoint();
    if (breakpointRef.current != newBreakpoint) {
      forceUpdate();
    }
  }, []);

  return breakpointRef.current;
}

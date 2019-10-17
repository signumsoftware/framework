import * as React from 'react'
import { FindOptions, ResultTable } from './Search';
import * as Finder from './Finder';
import * as Navigator from './Navigator';
import { Entity, Lite, liteKey, isEntity } from './Signum.Entities';
import { Type, QueryTokenString } from './Reflection';

export function useForceUpdate(): () => void {
  var [count, setCount] = React.useState(0);
  return () => setCount(count + 1);
}

export function useForceUpdatePromise(): () => Promise<void> {
  var [count, setCount] = useStateWithPromise(0);
  return () => setCount(count + 1) as Promise<any>;
}

export function usePrevious<T>(value: T): T | undefined {
  var ref = React.useRef<T | undefined>();
  React.useEffect(() => {
    ref.current = value;
  }, [value]);

  return ref.current;
}

interface APIHookOptions{
  avoidReset?: boolean;
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

export function useTitle(title: string, deps?: readonly any[]) {
  React.useEffect(() => {
    Navigator.setTitle(title);
    return () => Navigator.setTitle();
  }, deps);
}

export function useAPI<T>(defaultValue: T, makeCall: (signal: AbortSignal, oldData: T) => Promise<T>, deps: ReadonlyArray<any> | undefined, options?: APIHookOptions): T {

  const [data, setData] = React.useState<T>(defaultValue);

  React.useEffect(() => {
    var abortController = new AbortController();

    if (options == null || !options.avoidReset)
      setData(defaultValue);

    makeCall(abortController.signal, data)
      .then(result => !abortController.signal.aborted && setData(result))
      .done();

    return () => {
      abortController.abort();
    }
  }, deps);

  return data;
}

export function useMounted() {
  const mounted = React.useRef<boolean>(false);
  React.useEffect(() => {
    return () => { mounted.current = false; };
  }, []);
  return mounted;
}

export function useThrottle<T>(value: T, limit: number) : T {
  const [throttledValue, setThrottledValue] = React.useState(value);
  const lastRan = React.useRef(Date.now());

  React.useEffect(
    () => {
      const handler = setTimeout(function () {
        if (Date.now() - lastRan.current >= limit) {
          setThrottledValue(value);
          lastRan.current = Date.now();
        }
      }, limit - (Date.now() - lastRan.current));

      return () => {
        clearTimeout(handler);
      };
    },
    [value, limit]
  );

  return throttledValue;
};

export function useQuery(fo: FindOptions | null): ResultTable | undefined | null {
  return useAPI(undefined, signal =>
    fo == null ? Promise.resolve<ResultTable | null>(null) :
      Finder.getQueryDescription(fo.queryName)
        .then(qd => Finder.parseFindOptions(fo!, qd))
        .then(fop => Finder.API.executeQuery(Finder.getQueryRequest(fop), signal)),
    [fo && Finder.findOptionsPath(fo)]);
}

export function useInDB<R>(entity: Entity | Lite<Entity> | null, token: QueryTokenString<R> | string): Finder.AddToLite<R> | null | undefined {
  var resultTable = useQuery(entity == null ? null : {
    queryName: isEntity(entity) ? entity.Type : entity.EntityType,
    filterOptions: [{ token: "Entity", value: entity }],
    pagination: { mode: "Firsts", elementsPerPage: 1 },
    columnOptions: [{ token: token }],
    columnOptionsMode: "Replace",
  });

  if (entity == null)
    return null;

  if (resultTable == null)
    return undefined;

  return resultTable.rows[0] && resultTable.rows[0].columns[0] || null; 
}



export function useFetchAndForget<T extends Entity>(lite: Lite<T> | null | undefined): T | null | undefined {
  return useAPI(undefined, signal =>
    lite == null ? Promise.resolve<T | null | undefined>(lite) :
      Navigator.API.fetchAndForget(lite),
    [lite && liteKey(lite)]);
}


export function useFetchAndRemember<T extends Entity>(lite: Lite<T> | null, onLoaded?: () => void): T | null | undefined {

  const forceUpdate = useForceUpdate();
  React.useEffect(() => {
    if (lite && !lite.entity)
      Navigator.API.fetchAndRemember(lite)
        .then(() => {
          onLoaded && onLoaded();
          forceUpdate();
        })
        .done();
  }, [lite && liteKey(lite)]);


  if (lite == null)
    return null;

  if (lite.entity == null)
    return undefined;

  return lite.entity;
}

export function useFetchAll<T extends Entity>(type: Type<T>): T[] | undefined {
  return useAPI(undefined, signal => Navigator.API.fetchAll(type), []);
}

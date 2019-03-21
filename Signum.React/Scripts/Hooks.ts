import * as React from 'react'
import { FindOptions, ResultTable } from './Search';
import * as Finder from './Finder';
import * as Navigator from './Navigator';
import { Entity, Lite, liteKey } from './Signum.Entities';
import { EntityBase } from './Lines/EntityBase';
import { Type } from './Reflection';

export function useForceUpdate(): () => void {
  var [count, setCount] = React.useState(0);
  return () => setCount(count + 1);
}

export function useAPI<T>(defaultValue: T, key: ReadonlyArray<any> | undefined, makeCall: (signal: AbortSignal) => Promise<T>): T {

  const [data, updateData] = React.useState<T>(defaultValue)

  React.useEffect(() => {
    var abortController = new AbortController();

    updateData(defaultValue);

    makeCall(abortController.signal)
      .then(result => !abortController.signal.aborted && updateData(result))
      .done();

    return () => {
      abortController.abort();
    }
  }, key);

  return data;
}

export function useQuery(fo: FindOptions | null): ResultTable | undefined | null {
  return useAPI(undefined, [fo && Finder.findOptionsPath(fo)], signal =>
    fo == null ? Promise.resolve<ResultTable | null>(null) :
      Finder.getQueryDescription(fo.queryName)
        .then(qd => Finder.parseFindOptions(fo!, qd))
        .then(fop => Finder.API.executeQuery(Finder.getQueryRequest(fop), signal)));
}


export function useFetchAndForget<T extends Entity>(lite: Lite<T> | null | undefined): T | null | undefined {
  return useAPI(undefined, [lite && liteKey(lite)], signal =>
    lite == null ? Promise.resolve<T | null | undefined>(lite) :
      Navigator.API.fetchAndForget(lite));
}


export function useFetchAndRemember<T extends Entity>(lite: Lite<T> | null): T | null | undefined {

  const forceUpdate = useForceUpdate();
  React.useEffect(() => {
    if (lite && !lite.entity)
      Navigator.API.fetchAndRemember(lite)
        .then(() => forceUpdate())
        .done();
  }, [lite && liteKey(lite)]);


  if (lite == null)
    return null;

  if (lite.entity == null)
    return undefined;

  return lite.entity;
}

export function useFetchAll<T extends Entity>(type: Type<T>): T[] | undefined {
  return useAPI(undefined, [], signal => Navigator.API.fetchAll(type));
}

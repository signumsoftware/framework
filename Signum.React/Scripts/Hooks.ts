import * as React from 'react'
import { FindOptions, ResultTable } from './Search';
import * as Finder from './Finder';
import * as Navigator from './Navigator';
import { Entity, Lite, liteKey, isEntity } from './Signum.Entities';
import { EntityBase } from './Lines/EntityBase';
import { Type, QueryTokenString } from './Reflection';

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

export function useInDB<R>(entity: Entity | Lite<Entity> | null, token: QueryTokenString<R> | string): AddToLite<R> | null | undefined {
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

type AddToLite<T> = T extends Entity ? Lite<T> : T;

export function useFetchAndForget<T extends Entity>(lite: Lite<T> | null | undefined): T | null | undefined {
  return useAPI(undefined, [lite && liteKey(lite)], signal =>
    lite == null ? Promise.resolve<T | null | undefined>(lite) :
      Navigator.API.fetchAndForget(lite));
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
  return useAPI(undefined, [], signal => Navigator.API.fetchAll(type));
}

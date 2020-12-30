import * as React from 'react';
import { Entity, Lite } from "../Signum.Entities";
import { useFetchAndRemember, useFetchInState } from "../Navigator";

export function FetchInState<T extends Entity>(p: { lite: Lite<T> | null, children: (val: T | null | undefined) => React.ReactElement | null | undefined}) {

  var entity = useFetchInState(p.lite);

  var res = p.children(entity);

  if (res == null)
    return null;

  return res;
}

export function FetchAndRemember<T extends Entity>(p: { lite: Lite<T> | null, children: (val: T | null | undefined) => React.ReactElement | null | undefined }) {

  var entity = useFetchAndRemember(p.lite);

  var res = p.children(entity);

  if (res == null)
    return null;

  return res;
}

import * as React from 'react';
import { Entity, Lite } from "../Signum.Entities";
import { useFetchAndRemember } from "../Hooks";

export function Retrieve<T extends Entity>(p: { lite: Lite<T> | null, children: (val: T | null | undefined) => React.ReactElement }) {

  var entity = useFetchAndRemember(p.lite);

  return p.children(entity);
}

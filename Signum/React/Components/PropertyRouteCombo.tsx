import * as React from 'react'
import { Dic } from '../Globals'
import { TypeEntity, PropertyRouteEntity } from '../Signum.Basics'
import { TypeContext } from '../Lines'
import { getTypeInfo, MemberInfo, PropertyRoute } from "../Reflection";
import { useForceUpdate } from '../Hooks'


export interface PropertyRouteComboProps {
  ctx: TypeContext<PropertyRouteEntity | undefined | null>;
  type: TypeEntity;
  filter?: (m: MemberInfo) => boolean;
  routes?: PropertyRoute[];
  onChange?: () => void;
}

export default function PropertyRouteCombo(p : PropertyRouteComboProps): React.ReactElement{
  const forceUpdate = useForceUpdate();


  function handleChange(e: React.FormEvent<any>) {
    var currentValue = (e.currentTarget as HTMLSelectElement).value;
    p.ctx.value = currentValue ? PropertyRouteEntity.New({ path: currentValue, rootType: p.type }) : null;
    forceUpdate();
    if (p.onChange)
      p.onChange();
  }

  var ctx = p.ctx;

  var routes = p.routes ?? Dic.getValues(getTypeInfo(p.type.cleanName).members).filter(p.filter!).map(mi => PropertyRoute.parse(p.type.cleanName, mi.name));

  return (
    <select className={ctx.formSelectClass} value={ctx.value?.path ?? ""} onChange={handleChange} >
      <option value=""> - </option>
      {routes.map(r => r.propertyPath()).map(path =>
        <option key={path} value={path}>{path}</option>
      )}
    </select>
  );;
}

(PropertyRouteCombo as any).defaultProps = {
  filter: a => a.name != "Id"
} as Partial<PropertyRouteComboProps>;

import { ModifiableEntity } from '@framework/Signum.Entities';
import { getLambdaMembers } from '@framework/Reflection';

export function translated<T extends ModifiableEntity, S extends string | null | undefined>(entity: T, field: (e: T) => S): S {
  var members = getLambdaMembers(field);

  if (members.length != 1 || members[0].type != 'Member')
    throw new Error("Invalid lambda");

  const prop = members[0].name;

  return (entity as any)[prop + "_translated"] as S ?? (entity as any)[prop] as S;
}

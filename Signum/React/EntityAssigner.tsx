import * as Navigator from './Navigator'
import { getTypeName, GraphExplorer } from './Reflection'
import { Entity, is, isEntity, isModifiableEntity, MListElement, ModifiableEntity } from './Signum.Entities';


export function assignServerChanges<T extends ModifiableEntity>(local: T, server: T) {
  
  if (!isModifiableEntity(local))
    return;

  if (isEntity(local) && getTypeName(local) != "FileType") {
    let serverEntity = server as ModifiableEntity as Entity;

    if (local.id != serverEntity.id && local.temporalId != serverEntity.temporalId)
      throw new Error("Temporal Id of local and server are not equal.");
    local.id = serverEntity.id;
    local.ticks = serverEntity.ticks;
    local.toStr = serverEntity.toStr;
    if (local.isNew && !server.isNew)
      delete local.isNew;

    let es = Navigator.getSettings(local.Type);
    if (es?.onAssignServerChanges)
      es.onAssignServerChanges(local, serverEntity);
  }

  for (const prop in local) {
    if (local.hasOwnProperty(prop) && !GraphExplorer.specialProperties.contains(prop)) {
      let localPropValue = (local as any)[prop];
      let serverPropValue = (server as any)[prop];

      if (prop == "mixins") {
        for (const mixinType in localPropValue) {
          assignServerChanges(localPropValue[mixinType], serverPropValue[mixinType]);
        }
      }
      else if (isModifiableEntity(localPropValue)) {
        if (serverPropValue != null)
          assignServerChanges(localPropValue, serverPropValue);
      }
      else if (localPropValue instanceof Array) {
        let serverArray = [...serverPropValue] as Array<any>;
        for (let i = 0; i < localPropValue.length; i++) {
          let lmle = localPropValue[i];
          if (lmle.hasOwnProperty && lmle.hasOwnProperty("rowId")) {

            if (lmle.rowId != null) {
              var smle = serverArray.filter(a => a.rowId == lmle.rowId).firstOrNull();
              if (smle) {
                assignServerChanges(lmle.element, smle.element);
                serverArray.remove(smle);
                continue;
              }
            }

            if (isModifiableEntity(lmle.element)) {
              smle = serverArray.filter(a => is(a.element, lmle.element)).firstOrNull();
              if (smle) {
                lmle.rowId = smle.rowId;
                assignServerChanges(lmle.element, smle.element);
                serverArray.remove(smle);
                continue;
              }

              smle = serverArray.filter(a => a.element.temporalId == lmle.element.temporalId).firstOrNull();
              if (smle) {
                lmle.rowId = smle.rowId;
                assignServerChanges(lmle.element, smle.element);
                serverArray.remove(smle);
                continue;
              }
            }
            else {
              smle = serverArray.filter(a => a.element == lmle.element).firstOrNull();
              if (smle) {
                lmle.rowId = smle.rowId;
                serverArray.remove(smle);
              }
            }
          }
        }
        continue;
      }
    }
  }
}

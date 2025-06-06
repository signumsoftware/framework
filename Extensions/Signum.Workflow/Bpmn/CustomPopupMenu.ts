/// <reference path="../bpmn-js.ts" />
import BpmnReplaceMenuProvider from "bpmn-js/lib/features/popup-menu/ReplaceMenuProvider"
import * as BpmnUtils from './BpmnUtils'

export class CustomReplaceMenuProvider extends BpmnReplaceMenuProvider {
  static $inject: string[] = ['popupMenu', 'modeling', 'moddle', 'bpmnReplace', 'rules', 'translate'];
  constructor(popupMenu: any, modeling: any, moddle: BPMN.ModdleElement, bpmnReplace: any, rules: any, translate: any) {
    super(popupMenu, modeling, moddle, bpmnReplace, rules, translate);
  }

  getHeaderEntries(element: BPMN.DiElement): never[] {
    return [];
  }

  getPopupMenuEntries(element: BPMN.DiElement): (entries: BPMN.EntriesObject) => BPMN.EntriesObject {

    if (BpmnUtils.isGatewayAnyKind(element.type))
      return this.entriesOrUpdaterGateways;

    if (element.type == "bpmn:IntermediateThrowEvent")
      return this.entriesOrUpdaterIntermediateEvents;

    if (element.type == "bpmn:BoundaryEvent")
      return this.entriesOrUpdaterBoundaryEvents;

    return this.entriesOrUpdaterEmpty;
  }

  entriesOrUpdaterGateways(entries: BPMN.EntriesObject): BPMN.EntriesObject {
    
    Object.keys(entries)
      .filter(key => key != "replace-with-parallel-gateway" &&
        key != "replace-with-inclusive-gateway" &&
        key != "replace-with-exclusive-gateway")
      .forEach(key => delete entries[key]);

    return entries;
  }

  entriesOrUpdaterIntermediateEvents(entries: BPMN.EntriesObject): BPMN.EntriesObject {

    Object.keys(entries)
      .filter(key => key != "replace-with-timer-intermediate-catch")
      .forEach(key => delete entries[key]);

    return entries;
  }

  entriesOrUpdaterBoundaryEvents(entries: BPMN.EntriesObject): BPMN.EntriesObject {

    Object.keys(entries)
      .filter(key => key != "replace-with-timer-boundary" &&
        key != "replace-with-non-interrupting-timer-boundary")
      .forEach(key => delete entries[key]);

    return entries;
  }

  entriesOrUpdaterEmpty(entries: BPMN.EntriesObject): BPMN.EntriesObject {

    Object.keys(entries).forEach(key => delete entries[key]);

    return entries;
  }
}

export var __init__: string[] = ['customReplaceMenuProvider'];
export var customReplaceMenuProvider: (string | typeof CustomReplaceMenuProvider)[] = ['type', CustomReplaceMenuProvider];

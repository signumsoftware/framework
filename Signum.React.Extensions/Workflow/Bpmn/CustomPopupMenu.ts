/// <reference path="../bpmn-js.d.ts" />
import * as Modeler from "bpmn-js/lib/Modeler"
import * as BpmnReplaceMenuProvider from "bpmn-js/lib/features/popup-menu/ReplaceMenuProvider"
import * as BpmnUtils from './BpmnUtils'
import { Lite, liteKey } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'

interface ReplaceOptions {
    actionName: string;
    className: string;
    label: string;
    target: BPMN.DiElement;
}

export class CustomReplaceMenuProvider extends BpmnReplaceMenuProvider {

    static $inject = ['popupMenu', 'modeling', 'moddle', 'bpmnReplace', 'rules', 'translate'];
    constructor(popupMenu: any, modeling: any, moddle: BPMN.ModdleElement, bpmnReplace: any, rules: any, translate: any) {
        super(popupMenu, modeling, moddle, bpmnReplace, rules, translate);
    }

    getHeaderEntries(element: BPMN.DiElement) {
        return [];
    }

    _createEntries(element: BPMN.DiElement, replaceOptions: ReplaceOptions[]) {
        if (BpmnUtils.isGatewayAnyKind(element.type))
            return super._createEntries(element, replaceOptions.filter(a =>
                a.actionName == "replace-with-parallel-gateway" ||
                a.actionName == "replace-with-inclusive-gateway" ||
                a.actionName == "replace-with-exclusive-gateway"));

        if (element.type == "bpmn:IntermediateThrowEvent")
            return super._createEntries(element, replaceOptions.filter(a =>
                a.actionName == "replace-with-timer-intermediate-catch"));

        if (element.type == "bpmn:BoundaryEvent")
            return super._createEntries(element, replaceOptions.filter(a =>
                a.actionName == "replace-with-timer-boundary" ||
                a.actionName == "replace-with-non-interrupting-timer-boundary"));

        return [];
    }
}

export var __init__ = ['customReplaceMenuProvider'];
export var customReplaceMenuProvider = ['type', CustomReplaceMenuProvider];
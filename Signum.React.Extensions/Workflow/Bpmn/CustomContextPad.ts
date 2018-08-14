/// <reference path="../bpmn-js.d.ts" />
import Modeler from "bpmn-js/lib/Modeler"
import BpmnContextPadProvider from "bpmn-js/lib/features/context-pad/ContextPadProvider"
import * as BpmnUtils from './BpmnUtils'
import { Lite, liteKey } from '@framework/Signum.Entities'

export class CustomContextPadProvider extends BpmnContextPadProvider {

    static $inject = ['config.contextPad', 'injector', 'eventBus', 'contextPad', 'modeling', 'elementFactory', 'connect', 'create', 'popupMenu', 'canvas', 'rules', 'translate'];
    constructor(config: any, injector: any, eventBus: any, contextPad: any, modeling: any, elementFactory: any, connect: any, create: any, popupMenu: any, canvas: any, rules: any, translate: any) {
        super(config, injector, eventBus, contextPad, modeling, elementFactory, connect, create, popupMenu, canvas, rules, translate);
    }

    getContextPadEntries(element: BPMN.DiElement) {
        var result = super.getContextPadEntries(element);

        delete result["append.text-annotation"];

        if (element.type == "bpmn:Lane" || element.type == "bpmn:Participant") {
            delete result["lane-divide-two"];
            delete result["lane-divide-three"];

            if (element.type == "bpmn:Participant") {
                delete result["connect"];
            }
        }

        return result;
    }
}

export var __init__ = ['contextPadProvider'];
export var contextPadProvider = ['type', CustomContextPadProvider];
/// <reference path="../bpmn-js.d.ts" />
import Modeler = require("bpmn-js/lib/Modeler");
import BpmnReplaceMenuProvider = require("bpmn-js/lib/features/popup-menu/ReplaceMenuProvider");
import {  } from '../Signum.Entities.Workflow'
import { Lite, liteKey } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'

export class CustomReplaceMenuProvider extends BpmnReplaceMenuProvider {

    constructor(popupMenu: any, modeling: any, moddle: BPMN.ModdleElement, bpmnReplace: any, rules: any, translate: any) {
        super(popupMenu, modeling, moddle, bpmnReplace, rules, translate);
    }

    _createMenuEntry(definition: any, element: BPMN.DiElement, action: any) {
        var result = super._createMenuEntry(definition, element, action);

        if (element.type == "bpmn:StartEvent")
        {
            if ((definition as any).label == "Intermediate Throw Event") {
                return null; 
            }
        }

        return result;
    }
}


export var __init__ = ['customReplaceMenuProvider'];
export var customReplaceMenuProvider = ['type', CustomReplaceMenuProvider];
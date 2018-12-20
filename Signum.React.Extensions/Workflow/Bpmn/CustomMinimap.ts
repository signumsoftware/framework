/// <reference path="../bpmn-js.d.ts" />
import Minimap from "diagram-js-minimap/lib/Minimap"

export class CustomMinimap extends Minimap {
  static $inject = ['config.minimap', 'injector', 'eventBus', 'canvas', 'elementRegistry'];
  constructor(config: any, injector: any, eventBus: any, canvas: any, elementRegistry: any) {
    super(config, injector, eventBus, canvas, elementRegistry);
  }
}


export var __init__ = ['minimap'];
export var minimap = ['type', CustomMinimap];

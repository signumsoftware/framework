/// <reference path="../bpmn-js.ts" />
import MMPair from "diagram-js-minimap"

const Minimap = MMPair.minimap[1];

export class CustomMinimap extends (Minimap as any) {
  static $inject: string[] = ['config.minimap', 'injector', 'eventBus', 'canvas', 'elementRegistry'];
  constructor(config: any, injector: any, eventBus: any, canvas: any, elementRegistry: any) {
    super(config, injector, eventBus, canvas, elementRegistry);
  }
}


export var __init__: string[] = ['minimap'];
export var minimap: (string | typeof CustomMinimap)[] = ['type', CustomMinimap];

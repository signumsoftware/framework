/// <reference path="../bpmn-js.ts" />
import { WorkflowConditionEntity, WorkflowActionEntity } from '../Signum.Workflow'
import { getToString, Lite, liteKey } from '@framework/Signum.Entities'

export function getOrientation(rect: BPMN.DiElement, reference: BPMN.DiElement, padding: number) {
  padding = padding || 0;

  var sPadding = { x: padding, y: padding };

  function asTRBL(bounds: { x: number; y: number; width: number; height: number }) {
    return {
      top: bounds.y,
      right: bounds.x + (bounds.width || 0),
      bottom: bounds.y + (bounds.height || 0),
      left: bounds.x
    };
  }

  var rectOrientation = asTRBL(rect),
    referenceOrientation = asTRBL(reference);

  var top = rectOrientation.bottom + sPadding.y <= referenceOrientation.top,
    right = rectOrientation.left - sPadding.x >= referenceOrientation.right,
    bottom = rectOrientation.top - sPadding.y >= referenceOrientation.bottom,
    left = rectOrientation.right + sPadding.x <= referenceOrientation.left;

  var vertical = top ? 'top' : (bottom ? 'bottom' : null),
    horizontal = left ? 'left' : (right ? 'right' : null);

  if (horizontal && vertical) {
    return vertical + '-' + horizontal;
  } else {
    return horizontal || vertical || 'intersect';
  }
}


export class ConnectionIcons {
  static $inject = ['elementRegistry', 'overlays', 'eventBus'];

  _overlays: BPMN.Overlays;
  _elementRegistry: BPMN.ElementRegistry;
  active: boolean;
  constructor(elementRegistry: BPMN.ElementRegistry, overlays: BPMN.Overlays, eventBus: BPMN.EventBus) {

    this._overlays = overlays;
    this._elementRegistry = elementRegistry;

    this.active = false;

    eventBus.on('elements.changed', () => {

      if (this.active) {
        this.hide();
        this.show();
      }
    });
  }




  _addOverlay(shape: BPMN.DiElement, waypoint: BPMN.DiElement, lite: Lite<WorkflowConditionEntity | WorkflowActionEntity>, color: string) {

    var orientation = getOrientation(waypoint, shape, -7);

    if (orientation === 'intersect') {
      // Try again using a bigger padding to get an orientation which is not 'intersect'.
      // Otherwise the boundary would not be visible if the connection is attached on the
      // diagonal edge of a gateway. Not perfect, but much better than showing no overlay at all.
      orientation = getOrientation(waypoint, shape, -20);
    }

    var strokeWidth = 5,
      defaultLength = 20,
      margin = 0;

    var position: BPMN.RelativePosition = {};
    var height;
    var width;

    // if orientation is either 'left', 'top-left' or 'bottom-left'
    if (/left/.test(orientation)) {

      width = strokeWidth;
      height = defaultLength;

      // horizontal position: at the left border respecting margin
      // vertical position: centered at the connection waypoint
      position.left = -width - margin;
      position.top = waypoint.y - shape.y - defaultLength / 2;

      // if orientation is either 'right', 'top-right' or 'bottom-right'
    } else if (/right/.test(orientation)) {

      width = strokeWidth;
      height = defaultLength;

      // horizontal position: at the right border respecting margin
      // vertical position: centered at the connection waypoint
      position.right = shape.x + shape.width - waypoint.x - margin;
      position.top = waypoint.y - shape.y - defaultLength / 2;

    } else if (orientation === 'top') {

      width = defaultLength;
      height = strokeWidth;

      // horizontal position: centered at the connection waypoint
      // vertical position: at the top border respecting margin
      position.left = waypoint.x - shape.x - defaultLength / 2;
      position.top = -height - margin;

    } else if (orientation === 'bottom') {

      width = defaultLength;
      height = strokeWidth;

      // horizontal position: centered at the connection waypoint
      // vertical position: at the bottom border respecting margin
      position.bottom = -margin;
      position.left = waypoint.x - shape.x - defaultLength / 2;
    }

    this._overlays.add(shape, 'transaction-boundaries', {
      position: position,
      html: `<div class="connection-icon" data-key="${liteKey(lite)}" title="${htmlEscape(lite.EntityType + ": " + getToString(lite) || "")}" style="width: ${width}px; height: ${height}px; background: ${color}; cursor:pointer;"> </div>`
    });
  };


  hasAction!: (con: BPMN.Connection) => Lite<WorkflowActionEntity> | undefined;
  hasCondition!: (con: BPMN.Connection) => Lite<WorkflowConditionEntity> | undefined;

  show() {

    this._elementRegistry.forEach(element => {
      if (element.type == "label")
        return;

      element.incoming.forEach(con => {
        var action = this.hasAction(con);
        if (action) {
          var waypoint = con.waypoints.last();

          this._addOverlay(element, waypoint, action, "#ffc800");
        }
      });

      element.outgoing.forEach(con => {
        var condition = this.hasCondition(con);
        if (condition) {
          var waypoint = con.waypoints.first();

          this._addOverlay(element, waypoint, condition, "#0000ff");
        }
      });

    });

    this.active = true;
  }

  hide() {
    this._overlays.remove({ type: 'transaction-boundaries' });

    this.active = false;
  }

  toggle() {
    this.active ? this.hide() : this.show();
  }
}

function htmlEscape(str: string) {
  return str
    .replace(/&/g, '&amp;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;');
}

export var __init__ = ['connectionIcons'];
export var connectionIcons = ['type', ConnectionIcons];

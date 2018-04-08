/// <reference path="../bpmn-js.d.ts" />
import * as React from 'react'
import { WorkflowEntitiesDictionary, WorkflowActivityModel, WorkflowActivityType, WorkflowPoolModel, WorkflowLaneModel, WorkflowConnectionModel, WorkflowEventModel, WorkflowEntity, IWorkflowNodeEntity, WorkflowMessage } from '../Signum.Entities.Workflow'
import * as Modeler from "bpmn-js/lib/Modeler"
import { ModelEntity, ValidationMessage, parseLite } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as connectionIcons from './ConnectionIcons'
import * as customRenderer from './CustomRenderer'
import * as customPopupMenu from './CustomPopupMenu'
import * as BpmnUtils from './BpmnUtils'

import "bpmn-js/dist/assets/bpmn-font/css/bpmn-embedded.css"
import "diagram-js/assets/diagram-js.css"
import "./Bpmn.css"
import { Button } from '../../../../Framework/Signum.React/Scripts/Components';

export interface BpmnModelerComponentProps {
    workflow: WorkflowEntity;
    diagramXML: string;
    entities: WorkflowEntitiesDictionary;
}

class CustomModeler extends Modeler {

}

CustomModeler.prototype._modules =
    CustomModeler.prototype._modules.concat([customRenderer, customPopupMenu]);

export default class BpmnModelerComponent extends React.Component<BpmnModelerComponentProps> {

    private modeler!: Modeler;
    private elementRegistry!: BPMN.ElementRegistry;
    private bpmnFactory!: BPMN.BpmnFactory;
    private divArea!: HTMLDivElement; 

    constructor(props: any) {
        super(props);
    }


    componentDidMount() {
        this.modeler = new CustomModeler({
            container: this.divArea,
            height: 1000,
            keyboard: {
                bindTo: document
            },
            additionalModules: [
                connectionIcons,
            ],
        });
        this.configureModules();
        this.elementRegistry = this.modeler.get<BPMN.ElementRegistry>('elementRegistry');
        this.bpmnFactory = this.modeler.get<BPMN.BpmnFactory>('bpmnFactory');
        this.modeler.on('element.dblclick', 1500, this.handleElementDoubleClick as (obj: BPMN.Event) => void);
        this.modeler.on('element.paste', 1500, this.handleElementPaste as (obj: BPMN.Event) => void);
        this.modeler.on('create.ended', 1500, this.handleCreateEnded as (obj: BPMN.Event) => void);
        this.modeler.on('shape.add', 1500, this.handleAddShapeOrConnection as (obj: BPMN.Event) => void);
        this.modeler.on('connection.add', 1500, this.handleAddShapeOrConnection as (obj: BPMN.Event) => void);
        this.modeler.on('label.add', 1500, () => this.lastPasted = undefined);
        this.modeler.importXML(this.props.diagramXML, this.handleOnModelError)
    }

    existsMainEntityTypeRelatedNodes(): boolean {

        var entities = this.props.entities;
        var result = false;
        this.elementRegistry.forEach(e => {

            var model = entities[e.id];

            if (!model)
                return;

            if (BpmnUtils.isLane(e.type) &&
                (model as WorkflowLaneModel).actorsEval != null)
                result = true;

            if (BpmnUtils.isStartEvent(e.type) &&
                (model as WorkflowEventModel).task != null &&
                ((model as WorkflowEventModel).task!.action != null || (model as WorkflowEventModel).task!.condition != null))
                result = true;

            if (BpmnUtils.isConnection(e.type) &&
                ((model as WorkflowConnectionModel).action != null) ||
                ((BpmnUtils.isExclusiveGateway(e.type) || BpmnUtils.isInclusiveGateway(e.type)) && (model as WorkflowConnectionModel).condition != null))
                result = true;

            if (BpmnUtils.isTaskAnyKind(e.type) && (
                (model as WorkflowActivityModel).script != null ||
                (model as WorkflowActivityModel).timers.some(t => t.element.action != null || t.element.condition != null) ||
                (model as WorkflowActivityModel).jumps.some(j => j.element.action != null || j.element.condition != null)))
                result = true;
        });

        return result;
    }

    private handleOnModelError = (err : string) => {
        if (err)
            throw new Error('Error rendering the model ' + err);
        else
            this.modeler.get<connectionIcons.ConnectionIcons>('connectionIcons').show();
    }

    configureModules() {
        var conIcons = this.modeler.get<connectionIcons.ConnectionIcons>('connectionIcons');
        conIcons.hasAction = con => {
            var mod = this.props.entities[con.id] as (WorkflowConnectionModel | undefined);
            return mod && mod.action || undefined;
        };

        conIcons.hasCondition = con => {
            var mod = this.props.entities[con.id] as (WorkflowConnectionModel | undefined);
            return mod && mod.condition || undefined;
        };

        var cusRenderer = this.modeler.get<customRenderer.CustomRenderer>('customRenderer');
        cusRenderer.getDecisionResult = con => {
            var mod = this.props.entities[con.id] as (WorkflowConnectionModel | undefined);
            return mod && mod.decisonResult || undefined;
        }

        conIcons.show();
    }

    private saveXmlAsync(options: BPMN.SaveOptions): Promise<string> {
        return new Promise<string>((resolve, reject) => {
            this.modeler.saveXML(options, (err, xml) => {
                if (err)
                    return reject(err);
                else
                    return resolve(xml);
            })
        });
    }

    private saveSvgAsync(options: BPMN.SaveOptions): Promise<string> {
        return new Promise<string>((resolve, reject) => {
            this.modeler.saveSVG(options, (err, svgStr) => {
                if (err)
                    return reject(err);
                else
                    return resolve(svgStr);
            })
        });
    }

    getXml(): Promise<string> {
        return this.saveXmlAsync({ });
    }

    getSvg(): Promise<string> {
        return this.saveSvgAsync({ });
    }

    private fireElementChanged(element: Object) {

        this.modeler._emit('elements.changed', { elements: [element] });
    }

    getMainType() {
        return this.props.workflow.mainEntityType;
    }

    newModel(element: BPMN.DiElement): ModelEntity | undefined {

        const elementType = element.type;
        const elementName = element.businessObject.name;

        if (BpmnUtils.isPool(elementType))
            return WorkflowPoolModel.New({
                name: elementName
            });

        if (BpmnUtils.isLane(elementType))
            return WorkflowLaneModel.New({
                name: elementName
            });

        if (BpmnUtils.isStartEvent(elementType)) {
            return WorkflowEventModel.New({
                name: elementName,
                type: "Start"
            });
        }

        if (BpmnUtils.isTaskAnyKind(elementType))
            return WorkflowActivityModel.New({
                name: elementName,
                type: "Task",
                workflow: this.props.workflow,
            });

        if (BpmnUtils.isConnection(elementType))
            return WorkflowConnectionModel.New({
                name: elementName,
            });

        return undefined;
    }


    handleElementDoubleClick = (obj: BPMN.DoubleClickEvent) => {
        if (BpmnUtils.isEndEvent(obj.element.type))
            return;

        var elementType = obj.element.type;
        var model = this.props.entities[obj.element.id] as (ModelEntity | undefined);
        if (!model) {
            model = this.newModel(obj.element);
            if (!model)
                return;

            this.props.entities[obj.element.id] = model;
        }
        else
            (model as any).name = obj.element.businessObject.name;

        if (BpmnUtils.isConnection(elementType)) {
            var sourceElementType = (obj.element.businessObject as BPMN.ConnectionModdleElemnet).sourceRef.$type;
            var connModel = (model as WorkflowConnectionModel);

            connModel.needDecisonResult = BpmnUtils.isExclusiveGateway(sourceElementType);
            connModel.needCondition = BpmnUtils.isExclusiveGateway(sourceElementType) || BpmnUtils.isInclusiveGateway(sourceElementType);
            connModel.needOrder = BpmnUtils.isExclusiveGateway(sourceElementType);
        }

        if (BpmnUtils.isLane(elementType) || BpmnUtils.isTaskAnyKind(elementType) || BpmnUtils.isStartEvent(elementType) || BpmnUtils.isConnection(elementType))
            (model as any).mainEntityType = this.getMainType();

        obj.preventDefault();
        obj.stopPropagation();

        Navigator.view(model).then(me => {

            if (me) {
                this.props.entities[obj.element.id] = me;

                obj.element.businessObject.name = (me as any).name;

                if (BpmnUtils.isTaskAnyKind(obj.element.type)) {
                    var dt = (me as WorkflowActivityModel).type;
                    obj.element.type = (dt == "CallWorkflow" || dt == "DecompositionWorkflow") ? "bpmn:CallActivity" :
                        dt == "Decision" ? "bpmn:UserTask" : dt == "Script" ? "bpmn:ScriptTask" : "bpmn:Task";
                } else if (BpmnUtils.isStartEvent(obj.element.type)) {
                    var et = (me as WorkflowEventModel).type;
                    obj.element.type = (et == "Start" || et == "TimerStart") ? "bpmn:StartEvent" : "bpmn:EndEvent";

                    var bo = obj.element.businessObject;
                    var shouldEvent =
                        (et == "Start" || et == "Finish") ? null :
                            (me as WorkflowEventModel).task!.triggeredOn == "Always" ? "bpmn:TimerEventDefinition" :
                                "bpmn:ConditionalEventDefinition";

                    if (shouldEvent) {
                        if (!bo.eventDefinitions)
                            bo.eventDefinitions = [];

                        bo.eventDefinitions.filter(a => a.$type != shouldEvent).forEach(a => bo.eventDefinitions!.remove(a));
                        if (bo.eventDefinitions.length == 0)
                            bo.eventDefinitions.push(this.bpmnFactory.create(shouldEvent, {}));
                    } else {
                        bo.eventDefinitions = undefined;
                    }
                }

                var newName = (me as any).name;

                if (WorkflowConnectionModel.isInstance(me)) {
                    newName = (newName.tryBeforeLast(":") || newName) + (me.order != null ? ": " + me.order! : "");
                }

                this.modeler.get<any>("modeling").updateProperties(obj.element, {
                    name: newName,
                });
            };
        }).done();
    }

    lastPasted?: { id: string; name?: string };
    handleElementPaste = (obj: BPMN.PasteEvent) => {
        if (this.lastPasted) {
            console.error("lastPasted not consumed: " + this.lastPasted.id);
        }

        if (obj.descriptor.type != "label")
            this.lastPasted = {
                id: obj.descriptor.id,
                name: obj.descriptor.name
            };
    }

    handleCreateEnded = (obj: BPMN.Event) => {
        console.log(obj);
    }

    handleAddShapeOrConnection = (obj: BPMN.AddClickEvent) => {
        if (this.lastPasted) {
            var model = this.props.entities[this.lastPasted.id];
            if (model) {
                var clone: ModelEntity = JSON.parse(JSON.stringify(model));
                if (WorkflowLaneModel.isInstance(clone))
                    clone.actors.forEach(a => a.rowId = null);

                this.props.entities[obj.element.id] = clone ;
            }

            if (this.lastPasted.name)
                obj.element.businessObject.name = this.lastPasted.name;

            this.lastPasted = undefined;
        }
    }

    componentWillUnmount() {
        this.modeler.destroy();
    }

    componentWillReceiveProps(nextProps: BpmnModelerComponentProps) {
        if (this.modeler) {
            if (nextProps.diagramXML !== undefined && this.props.diagramXML !== nextProps.diagramXML) {
                this.modeler.importXML(nextProps.diagramXML, this.handleOnModelError);
            }
        }
    }

    setDiv = (div: HTMLDivElement) => {
        if (this.divArea)
            this.divArea.removeEventListener("click", this.clickConnectionIconEvent as EventListener);

        this.divArea = div;

        if (this.divArea)
            this.divArea.addEventListener("click", this.clickConnectionIconEvent);
    }

    clickConnectionIconEvent = (e: MouseEvent) => {
        var d = e.target as HTMLDivElement;

        if (d.classList && d.classList.contains("connection-icon")) {
            const lite = parseLite(d.dataset["key"]!);
            Navigator.navigate(lite).done();
        }
    }

    handleZoomClick = (e: React.MouseEvent<any>) => {
        var zoomScroll = this.modeler.get<any>("zoomScroll");
        zoomScroll.reset();
    }

    render() {
        return (
            <div>
                <Button style={{ marginLeft: "20px" }} onClick={this.handleZoomClick}>{WorkflowMessage.ResetZoom.niceToString()}</Button>
                <div ref={this.setDiv} />
            </div>
        );
    }
}

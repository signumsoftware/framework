/// <reference path="../bpmn-js.d.ts" />
import * as React from 'react'
import { WorkflowEntitiesDictionary, WorkflowActivityModel, WorkflowActivityType, WorkflowPoolModel, WorkflowLaneModel, WorkflowConnectionModel, WorkflowEntity } from '../Signum.Entities.Workflow'
import Modeler = require("bpmn-js/lib/Modeler");
import { ModelEntity, ValidationMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'

require("!style!css!bpmn-js/assets/bpmn-font/css/bpmn-embedded.css");
require("!style!css!diagram-js/assets/diagram-js.css");
require("!style!css!./Workflow.css");

export interface BpmnModelerComponentProps {
    workflow: WorkflowEntity;
    diagramXML: string;
    entities: WorkflowEntitiesDictionary;
}

export default class BpmnModelerComponent extends React.Component<BpmnModelerComponentProps, void> {

    private modeler: Modeler;
    private elementRegistry: BPMN.DiModule;
    private divArea: HTMLDivElement; 

    constructor(props: any) {
        super(props);
    }

    private handleOnModelError = (err : string) => {
        if (err) {
            throw new Error('Error rendering the model ' + err);
        };
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

        this.modeler._emit('element.changed', { element });
    }

    private isPool(elementType: string): boolean {
        return (elementType == "bpmn:Participant");
    }

    private isLane(elementType: string): boolean {
        return (elementType == "bpmn:Lane");
    }

    private isEvent(elementType: string): boolean {
        return (elementType == "bpmn:StartEvent" || elementType == "bpmn:EndEvent");
    }

    private isTaskAnyway(elementType: string): boolean {
        return this.isTask(elementType) || this.isUserTask(elementType);
    }


    private isTask(elementType: string): boolean {
        return (elementType == "bpmn:Task");
    }

    private isUserTask(elementType: string): boolean {
        return (elementType == "bpmn:UserTask");
    }

    private isGateway(elementType: string): boolean {
        return (elementType == "bpmn:ExclusiveGateway" || elementType == "bpmn:InclusiveGateway" || elementType == "bpmn:ParallelGateway");
    }

    private isConnection(elementType: string): boolean {
        return (elementType == "bpmn:SequenceFlow" || elementType == "bpmn:MessageFlow");
    }

    private isLabel(elementType: string): boolean {
        return (elementType == "label");
    }

    handleElementDoubleClick = (obj: BPMN.Event) => {

        if (this.isConnection(obj.element.type))
            if (!this.isGateway((obj.element.businessObject as BPMN.ConnectionModdleElemnet).sourceRef.$type))
                return;

        var model = this.props.entities[obj.element.id] as (ModelEntity | undefined);
        if (!model) {
            model = this.newModel(obj.element.type, obj.element.businessObject.name);
            if (!model)
                return;

            this.props.entities[obj.element.id] = model;
        }
        else
            (model as any).name = obj.element.businessObject.name;

        obj.preventDefault();
        obj.stopPropagation();

        Navigator.view(model).then(me => {

            if (me) {
                this.props.entities[obj.element.id] = me;
                obj.element.businessObject.name = (me as any).name;

                if (this.isTaskAnyway(obj.element.type))
                    obj.element.type = (me as WorkflowActivityModel).type == "DecisionTask" ? "bpmn:UserTask" : "bpmn:Task";

                this.fireElementChanged(obj.element);

                if (obj.element.label)
                {
                    var labelObj = this.elementRegistry.get(obj.element.label.id);
                    this.fireElementChanged(labelObj);
                };
            };
        }).done();
    }

    getMainType() {
        var result = this.props.workflow.mainEntityType;
        if (!result)
            throw new Error(ValidationMessage._0IsNotSet.niceToString(WorkflowEntity.nicePropertyName(a => a.mainEntityType)));

        return result;
    }

    newModel(elementType: string, elementName: string): ModelEntity | undefined {

        if (this.isPool(elementType))
            return WorkflowPoolModel.New({ name : elementName });

        if (this.isLane(elementType))
            return WorkflowLaneModel.New({name : elementName });

        if (this.isTask(elementType) || this.isUserTask(elementType))
            return WorkflowActivityModel.New({
                    mainEntityType : this.getMainType(),
                    name : elementName,
                    type : (this.isUserTask(elementType) ? "DecisionTask" : "Task")
            });

        if (this.isConnection(elementType))
            return WorkflowConnectionModel.New({ name: elementName });

        return undefined;
    }

    componentDidMount() {
        this.modeler = new Modeler({ container: this.divArea, height: 1000 });
        this.elementRegistry = this.modeler.get('elementRegistry');
        this.modeler.on('element.dblclick', 1500, this.handleElementDoubleClick);
        this.modeler.importXML(this.props.diagramXML, this.handleOnModelError)
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

    render() {
        return (<div ref={ de => this.divArea = de } />);
    }
}

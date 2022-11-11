import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { WorkflowEntity, WorkflowModel, WorkflowEntitiesDictionary, WorkflowMessage } from '../Signum.Entities.Workflow'
import { TypeContext, ValueLine, EntityLine, LiteAutocompleteConfig, EnumCheckboxList } from '@framework/Lines'
import { is, JavascriptMessage, toLite, ModifiableEntity, Lite, Entity } from '@framework/Signum.Entities'
import { API } from '../WorkflowClient'
import { IconName, IconProp, IconPrefix } from "@fortawesome/fontawesome-svg-core";
import BpmnModelerComponent from '../Bpmn/BpmnModelerComponent'
import MessageModal from "@framework/Modals/MessageModal";
import CollapsableCard from '../../Basics/Templates/CollapsableCard';
import { BsColor } from '@framework/Components';

interface WorkflowProps {
  ctx: TypeContext<WorkflowEntity>;
}

interface WorkflowState {
  initialXmlDiagram: string;
  entities: WorkflowEntitiesDictionary;
}

export interface WorkflowHandle {
  workflowState: WorkflowState;
  setIssues: (value: Array<API.WorkflowIssue>) => void;
  getXml(): Promise<string>;
  getSvg(): Promise<string>;
}

export const Workflow = React.forwardRef(function Workflow(p: WorkflowProps, ref: React.Ref<WorkflowHandle>) {

  const bpmnModelerComponentRef = React.useRef<BpmnModelerComponent>(null);

  const [issues, setIssues] = React.useState<Array<API.WorkflowIssue> | undefined>(undefined);
  const [workflowState, setWorkflowState] = React.useState<WorkflowState | undefined>(undefined);

  function updateState(model: WorkflowModel) {
    setWorkflowState({
      initialXmlDiagram: model.diagramXml,
      entities: model.entities.toObject(mle => mle.element.bpmnElementId, mle => mle.element.model!)
    });
  }


  React.useEffect(() => {
    const w = p.ctx.value;
    if (w.isNew) {
      // @ts-ignore Cannot find name 'require'.
      require(["raw-loader!./InitialWorkflow.xml"], (xml) => {
        updateState(WorkflowModel.New({
          diagramXml: xml.default,
          entities: [],
        }));
        setIssues(undefined);
      });
    }
    else
      API.getWorkflowModel(toLite(w))
        .then(pair => {
          updateState(pair.model);
          setIssues(pair.issues);
        });
  }, [p.ctx.value.id, p.ctx.value.ticks]);

  React.useImperativeHandle(ref, () => ({
    workflowState: workflowState,
    setIssues: (value) => setIssues(value),
    getXml: () => bpmnModelerComponentRef.current!.getXml(),
    getSvg: () => bpmnModelerComponentRef.current!.getSvg()
  } as WorkflowHandle), [bpmnModelerComponentRef.current, workflowState]);

  function handleHighlightClick(e: React.MouseEvent<HTMLAnchorElement>, issue: API.WorkflowIssue) {
    e.preventDefault();
    if (bpmnModelerComponentRef)
      bpmnModelerComponentRef.current!.focusElement(issue.bpmnElementId);
  }

  function renderIssues() {
    if (issues == null)
      return null;

    var color = (issues.length == 0 ? "success" :
      issues.some(a => a.type == "Error") ? "danger" : "warning") as BsColor;

    return (
      <CollapsableCard
        cardStyle={{ border: color }}
        headerStyle={{ border: color, text: color }}
        header={renderIssuesHeader()} >

        <ul style={{ listStyleType: "none", marginBottom: "0px" }} >

          {issues.length == 0 ?
            <li>
              <FontAwesomeIcon icon="check" className="text-success me-1" />
              {"-- No issues --"}
            </li> :
            issues.orderBy(a => a.type).map((issue, i) =>

              <li key={i}>
                {issue.type == "Error" ?
                  <FontAwesomeIcon icon="circle-xmark" className="text-danger me-1" /> :
                  <FontAwesomeIcon icon="triangle-exclamation" className="text-warning me-1" />}

                {issue.bpmnElementId && <span className="me-1">(in <a href="#" onClick={e => handleHighlightClick(e, issue)}>{issue.bpmnElementId}</a>)</span>}
                {issue.message}

              </li>
            )}
        </ul>
      </CollapsableCard>
    );
  }

  function renderIssuesHeader(): React.ReactNode {
    const errorCount = (issues?.filter(a => a.type == "Error").length) ?? 0;
    const warningCount = (issues?.filter(a => a.type == "Warning").length) ?? 0;

    return (
      <div>
        <span className="display-7">{WorkflowMessage.WorkflowIssues.niceToString()}&nbsp;</span>
        {errorCount > 0 && <FontAwesomeIcon icon="circle-xmark" className="text-danger me-1" />}
        {errorCount > 0 && errorCount}
        {warningCount > 0 && <FontAwesomeIcon icon="triangle-exclamation" className="text-warning me-1" />}
        {warningCount > 0 && warningCount}
      </div>
    );
  }

  function handleMainEntityTypeChange(entity: ModifiableEntity | Lite<Entity>): Promise<boolean> {
    if (bpmnModelerComponentRef.current!.existsMainEntityTypeRelatedNodes()) {
      return MessageModal.show({
        title: JavascriptMessage.error.niceToString(),
        message: WorkflowMessage.ChangeWorkflowMainEntityTypeIsNotAllowedBecauseWeHaveNodesThatUseIt.niceToString(),
        buttons: "ok",
        icon: "warning",
        style: "warning",
      }).then(a => false)
    }
    else
      return Promise.resolve(true);
  }
  var ctx = p.ctx.subCtx({ labelColumns: 3 });
  return (
    <div>
      <CollapsableCard
        header={<span className="display-7">{WorkflowMessage.WorkflowProperties.niceToString()}</span>}
        cardStyle={{ background: "info" }}
        headerStyle={{ text: "light" }}
        bodyStyle={{ background: "light" }}
        defaultOpen={ctx.value.isNew} >
        <div className="row">
          <div className="col-sm-6">
            <ValueLine ctx={ctx.subCtx(d => d.name)} />
            <EntityLine ctx={ctx.subCtx(d => d.mainEntityType)}
              autocomplete={new LiteAutocompleteConfig((signal, str) => API.findMainEntityType({ subString: str, count: 5 }))}
              find={false}
              onRemove={handleMainEntityTypeChange} />
            <ValueLine ctx={ctx.subCtx(d => d.expirationDate)} />
          </div>
          <div className="col-sm-6">
            <EnumCheckboxList ctx={ctx.subCtx(d => d.mainEntityStrategies)} columnCount={1} formGroupHtmlAttributes={{ style: { marginTop: "-15px" } }} />
          </div>
        </div>
      </CollapsableCard>
      {renderIssues()}
      <fieldset>
        {workflowState?
          <div>
            <BpmnModelerComponent ref={bpmnModelerComponentRef}
              workflow={ctx.value}
              diagramXML={workflowState.initialXmlDiagram}
              entities={workflowState.entities!}
            /></div> :
          <h3>{JavascriptMessage.loading.niceToString()}</h3>}
      </fieldset>
    </div>
  );
});

export default Workflow;

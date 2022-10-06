import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { ValueLine, EntityTable } from '@framework/Lines'
import { ModifiableEntity, JavascriptMessage, SaveChangesMessage } from '@framework/Signum.Entities'
import { classes } from '@framework/Globals'
import { getTypeInfo } from '@framework/Reflection'
import * as Navigator from '@framework/Navigator'
import MessageModal from '@framework/Modals/MessageModal'
import { TypeContext } from '@framework/TypeContext'
import * as Operations from '@framework/Operations'
import { BaseNode } from './Nodes'
import { DesignerContext, DesignerNode, RenderWithViewOverrides } from './NodeUtils'
import * as DynamicViewClient from '../DynamicViewClient'
import { DynamicViewTabs } from './DynamicViewTabs'
import { DynamicViewInspector, CollapsableTypeHelp } from './Designer'
import ShowCodeModal from './ShowCodeModal'
import { DynamicViewEntity, DynamicViewOperation, DynamicViewMessage, DynamicViewPropEmbedded } from '../Signum.Entities.Dynamic'
import { Dropdown, DropdownButton, Tabs, Tab } from 'react-bootstrap';
import "./DynamicView.css"
import { AutoFocus } from '@framework/Components/AutoFocus';
import { useAPI, useUpdatedRef } from '@framework/Hooks'

export interface DynamicViewComponentProps {
  ctx: TypeContext<ModifiableEntity>;
  initialDynamicView: DynamicViewEntity;
  //...extraProps
}

export interface DynamicViewComponentState {
  isDesignerOpen: boolean;
  rootNode: BaseNode;
  selectedNode: DesignerNode<BaseNode>;
  dynamicView: DynamicViewEntity;
  viewOverrides?: Navigator.ViewOverride<ModifiableEntity>[];
  
}

export default function DynamicViewComponent(p: DynamicViewComponentProps) {

  const [isDesignerOpen, setIsDesignerOpen] = React.useState<boolean>(false);
  const rootNodeMemo = React.useMemo(() => JSON.parse(p.initialDynamicView.viewContent!) as BaseNode, []);

  const [rootNode, setRootNode] = React.useState<BaseNode>(() => rootNodeMemo);
  const [selectedNode, setSelectedNode] = React.useState<DesignerNode<BaseNode>>(() => getZeroNode().createChild(rootNodeMemo));
  const selectedNodeRef = useUpdatedRef(selectedNode);
  const [dynamicView, setDynamicView] = React.useState<DynamicViewEntity>(p.initialDynamicView);

  const viewOverrides = useAPI(() => Navigator.viewDispatcher.getViewOverrides(p.ctx.value.Type), []);

  function getZeroNode() {
    var { ctx, initialDynamicView, ...extraProps } = p;

    var context: DesignerContext = {
      onClose: handleClose,
      refreshView: () => { setSelectedNode(selectedNodeRef.current.reCreateNode()); },
      getSelectedNode: () => isDesignerOpen ? selectedNodeRef.current : undefined,
      setSelectedNode: (newNode) => setSelectedNode(newNode),
      props: extraProps,
      propTypes: initialDynamicView.props.toObject(mle => mle.element.name, mle => mle.element.type),
      locals: {},
      localsCode: initialDynamicView.locals,
    };

    return DesignerNode.zero(context, ctx.value.Type);
  }

  function handleReload(dynamicView: DynamicViewEntity) {
    setDynamicView(dynamicView);
    setRootNode(JSON.parse(dynamicView.viewContent!) as BaseNode);
    setSelectedNode(getZeroNode().createChild(rootNode));
  }

  function handleOpen() {
    setIsDesignerOpen(true);
  }

  function handleClose() {
    setIsDesignerOpen(false);
  }

  function handleLoseChanges() {
    const node = JSON.stringify(rootNode);

    if (dynamicView.isNew || node != dynamicView.viewContent) {
      return MessageModal.show({
        title: SaveChangesMessage.ThereAreChanges.niceToString(),
        message: JavascriptMessage.loseCurrentChanges.niceToString(),
        buttons: "yes_no",
        style: "warning",
        icon: "warning"
      }).then(result => { return result == "yes"; });
    }

    return Promise.resolve(true);
  }

  const desRootNode = getZeroNode().createChild(rootNode);
  const ctx = p.ctx;

  if (viewOverrides == null)
    return null;

  var vos = viewOverrides.filter(a => a.viewName == dynamicView.viewName);

  if (Navigator.isReadOnly(DynamicViewEntity)) {
    return (
      <div className="design-content">
        <RenderWithViewOverrides dn={desRootNode} parentCtx={ctx} vos={vos} />
      </div>
    );
  }

  return (<div className="design-main">
      <div className={classes("design-left", isDesignerOpen && "open")}>
        {!isDesignerOpen ?
          <span onClick={handleOpen}><FontAwesomeIcon icon={["fas", "pen-to-square"]} className="design-open-icon" /></span> :
          <DynamicViewDesigner
            rootNode={desRootNode}
            dynamicView={dynamicView}
            onReload={handleReload}
            onLoseChanges={handleLoseChanges}
            typeName={ctx.value.Type} />
        }
      </div>
    <div className={classes("design-content", isDesignerOpen && "open")}>
        <RenderWithViewOverrides dn={desRootNode} parentCtx={ctx} vos={vos} />
    </div>
  </div>);
}

interface DynamicViewDesignerProps {
  rootNode: DesignerNode<BaseNode>;
  dynamicView: DynamicViewEntity;
  onLoseChanges: () => Promise<boolean>;
  onReload: (dynamicView: DynamicViewEntity) => void;
  typeName: string;
}

function DynamicViewDesigner(p: DynamicViewDesignerProps) {

  const [viewNames, setViewNames] = React.useState<string[] | undefined>(undefined);
  const [isDropdownOpen, setIsDropdownOpen] = React.useState<boolean>(false);


  function reload(entity: DynamicViewEntity) {
    setViewNames(undefined);
    p.onReload(entity);
  }

  function handleSave() {
    p.dynamicView.viewContent = JSON.stringify(p.rootNode.node);
    p.dynamicView.modified = true;

    Operations.API.executeEntity(p.dynamicView, DynamicViewOperation.Save)
      .then(pack => {
        reload(pack.entity);
        DynamicViewClient.cleanCaches();
        return Operations.notifySuccess();
      });
  }

  function handleCreate() {
    p.onLoseChanges().then(goahead => {
      if (!goahead)
        return;

      DynamicViewClient.createDefaultDynamicView(p.typeName)
        .then(entity => { reload(entity); return Operations.notifySuccess(); });

    });
  }

  function handleClone() {
    p.onLoseChanges().then(goahead => {
      if (!goahead)
        return;

      Operations.API.constructFromEntity(p.dynamicView, DynamicViewOperation.Clone)
        .then(pack => { reload(pack!.entity); return Operations.notifySuccess(); });
    });
  }

  function handleChangeView(viewName: string) {
    p.onLoseChanges().then(goahead => {
      if (!goahead)
        return;

      DynamicViewClient.API.getDynamicView(p.typeName, viewName)
        .then(entity => { reload(entity!); });
    });
  }

  function handleOnToggle() {
    if (!isDropdownOpen && !viewNames)
      DynamicViewClient.API.getDynamicViewNames(p.typeName)
        .then(viewNames => setViewNames(viewNames));


    setIsDropdownOpen(!isDropdownOpen);
  }

  function handleShowCode() {
    ShowCodeModal.showCode(p.typeName, p.rootNode.node);
  }

  function renderButtonBar() {
    var operations = Operations.operationInfos(getTypeInfo(DynamicViewEntity)).toObject(a => a.key);

    return (
      <div className="btn-group btn-group-sm" role="group" style={{ marginBottom: "5px" }}>
        {operations[DynamicViewOperation.Save.key] && <button type="button" className="btn btn-primary" onClick={handleSave}>{operations[DynamicViewOperation.Save.key].niceName}</button>}
        <button type="button" className="btn btn-success" onClick={handleShowCode}>Show code</button>
        <Dropdown onToggle={handleOnToggle} show={isDropdownOpen} >
          <Dropdown.Toggle id="bg-nested-dropdown" size="sm">
            {" … "}
          </Dropdown.Toggle>
          <Dropdown.Menu>
            {operations[DynamicViewOperation.Create.key] && <Dropdown.Item onClick={handleCreate}>{operations[DynamicViewOperation.Create.key].niceName}</Dropdown.Item>}
            {operations[DynamicViewOperation.Clone.key] && !p.dynamicView.isNew && <Dropdown.Item onClick={handleClone}>{operations[DynamicViewOperation.Clone.key].niceName}</Dropdown.Item>}
            {viewNames && viewNames.length > 0 && <Dropdown.Divider />}
            {viewNames?.map(vn => <Dropdown.Item key={vn}
              className={classes("sf-dynamic-view", vn == p.dynamicView.viewName && "active")}
              onClick={() => handleChangeView(vn)}>
              {vn}
            </Dropdown.Item>)}
          </Dropdown.Menu>
        </Dropdown>
      </div >
    );
  }
  var dv = p.dynamicView;
  var ctx = TypeContext.root(dv);

  return (
    <div className="code-container">
      <button type="button" className="btn-close" aria-label="Close" style={{ float: "right" }} onClick={p.rootNode.context.onClose}/>
      <h3>
        <small>{Navigator.getTypeSubTitle(p.dynamicView, undefined)}</small>
      </h3>
      <ValueLine ctx={ctx.subCtx(e => e.viewName)} formGroupStyle="SrOnly" placeholderLabels={true} />
      {renderButtonBar()}
      <DynamicViewTabs ctx={ctx} rootNode={p.rootNode}/>
      <CollapsableTypeHelp initialTypeName={dv.entityType!.cleanName} />
    </div>
  );
}


import { RouteObject } from 'react-router'
import { Navigator, EntitySettings } from '@framework/Navigator'
import { Operations, EntityOperationSettings } from '@framework/Operations'
import { DynamicViewClient } from '../Signum.Dynamic/DynamicViewClient'
import { WorkflowClient } from '../Signum.Workflow/WorkflowClient'
import { CaseActivityEntity, WorkflowActivityModel } from '../Signum.Workflow/Signum.Workflow'
import { QuickLinkClient } from '@framework/QuickLinkClient'
import { PropertyRoute, getQueryKey } from '@framework/Reflection'
import { CodeContext } from '../Signum.Dynamic/View/NodeUtils'
import { WorkflowActivityModelOptions } from '../Signum.Workflow/Workflow/WorkflowActivityModel'

export namespace WorkflowDynamicClient {
  
  export function start(options: { routes: RouteObject[] }): void {
    registerCustomContexts();
  
    WorkflowActivityModelOptions.getViewProps = (typeName, viewName) => DynamicViewClient.API.getDynamicViewProps(typeName, viewName);
    WorkflowActivityModelOptions.navigateToView = (typeName, viewName, props) => DynamicViewClient.API.getDynamicView(typeName, viewName)
      .then(async dv => { await Navigator.view(dv, { extraProps: props }); });
  
  }
  
  function registerCustomContexts() {
  
    function addActx(cc: CodeContext) {
      if (!cc.assignments["actx"]) {
        cc.assignments["actx"] = "WorkflowClient.getCaseActivityContext(ctx)";
        cc.imports.push("import { WorkflowClient.getCaseActivityContext } as WorkflowClient from '../../Workflow/WorkflowClient'");
      }
    }
  
    DynamicViewClient.registeredCustomContexts["caseActivity"] = {
      getTypeContext: ctx => {
        var actx = WorkflowClient.getCaseActivityContext(ctx);
        return actx;
      },
      getCodeContext: cc => {
        addActx(cc);
        return cc.createNewContext("actx");
      },
      getPropertyRoute: dn => PropertyRoute.root(CaseActivityEntity)
    };
  
    DynamicViewClient.registeredCustomContexts["case"] = {
      getTypeContext: ctx => {
        var actx = WorkflowClient.getCaseActivityContext(ctx);
        return actx?.subCtx(a => a.case);
      },
      getCodeContext: cc => {
        addActx(cc);
        cc.assignments["cctx"] = "actx?.subCtx(a => a.case)";
        return cc.createNewContext("cctx");
      },
      getPropertyRoute: dn => CaseActivityEntity.propertyRouteAssert(a => a.case)
    };
  
  
    DynamicViewClient.registeredCustomContexts["parentCase"] = {
      getTypeContext: ctx => {
        var actx = WorkflowClient.getCaseActivityContext(ctx);
        return actx?.value.case.parentCase ? actx.subCtx(a => a.case.parentCase) : undefined;
      },
      getCodeContext: cc => {
        addActx(cc);
        cc.assignments["pcctx"] = "actx?.value.case.parentCase && actx.subCtx(a => a.case.parentCase)";
        return cc.createNewContext("pcctx");
      },
      getPropertyRoute: dn => CaseActivityEntity.propertyRouteAssert(a => a.case.parentCase)
    };
  }
}

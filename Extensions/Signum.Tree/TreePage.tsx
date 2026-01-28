import * as React from 'react'
import { getQueryNiceName, getTypeInfo, tryGetOperationInfo } from '@framework/Reflection'
import * as AppContext from '@framework/AppContext'
import { Finder } from '@framework/Finder'
import { Operations } from '@framework/Operations'
import { TreeViewer } from './TreeViewer'
import { useLocation, useParams } from "react-router";
import { TreeOperation } from "./Signum.Tree";
import { QueryString } from '@framework/QueryString'
import { FrameMessage } from '@framework/Signum.Entities'
import { TreeClient, TreeOptions } from './TreeClient'
import { useTitle } from '@framework/AppContext'
import { LinkButton } from '@framework/Basics/LinkButton'


export default function TreePage(): React.JSX.Element {
  const params = useParams() as { typeName: string };
  const location = useLocation();

  useTitle(getQueryNiceName(params.typeName));

  const to = TreeClient.parseTreeOptionsPath(params.typeName, QueryString.parse(location.search));

  const treeViewRef = React.useRef<TreeViewer>(null);


  function changeUrl() {
    var newPath = treeViewRef.current!.getCurrentUrl();

    if (location.pathname + location.search != newPath)
      AppContext.navigate(newPath, { replace : true });
  }

  var ti = getTypeInfo(params.typeName);

  return (
    <div id="divSearchPage">
      <h2>
        <span className="sf-entity-title">{ti.nicePluralName}</span>
        &nbsp;
        <LinkButton className="sf-popup-fullscreen" title={FrameMessage.Fullscreen.niceToString()} onClick={e => treeViewRef.current!.handleFullScreenClick(e)}>
          <span className="fa fa-external-link"></span>
        </LinkButton>
      </h2>
      <TreeViewer ref={treeViewRef}
        treeOptions={to}
        initialShowFilters={true}
        allowMove={tryGetOperationInfo(TreeOperation.Move, ti.name) != null}
        showToolbar={true}
        showExpandCollapseButtons={true}
        key={ti.name}
        onSearch={(top) => changeUrl()} />
    </div>
  );
}

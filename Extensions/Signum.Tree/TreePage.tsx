import * as React from 'react'
import { getTypeInfo } from '@framework/Reflection'
import * as AppContext from '@framework/AppContext'
import * as Finder from '@framework/Finder'
import * as Operations from '@framework/Operations'
import { TreeViewer } from './TreeViewer'
import { useLocation, useParams } from "react-router";
import { TreeOperation } from "./Signum.Tree";
import { QueryString } from '@framework/QueryString'
import { FrameMessage } from '@framework/Signum.Entities'


export default function TreePage() {
  const params = useParams() as { typeName: string };
  const location = useLocation();
  var query = QueryString.parse(location.search);

  const filterOptions = React.useMemo(() => Finder.Decoder.decodeFilters(query), [query]);

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
        <a className="sf-popup-fullscreen" href="#" title={FrameMessage.Fullscreen.niceToString()} onClick={e => treeViewRef.current!.handleFullScreenClick(e)}>
          <span className="fa fa-external-link"></span>
        </a>
      </h2>
      <TreeViewer ref={treeViewRef}
        initialShowFilters={true}
        typeName={ti.name}
        allowMove={Operations.tryGetOperationInfo(TreeOperation.Move, ti.name) != null}
        filterOptions={filterOptions}
        showToolbar={true}
        key={ti.name}
        onSearch={() => changeUrl()} />
    </div>
  );
}

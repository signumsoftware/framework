import * as React from 'react'
import { getTypeInfo } from '@framework/Reflection'
import * as AppContext from '@framework/AppContext'
import * as Finder from '@framework/Finder'
import * as Operations from '@framework/Operations'
import { TreeViewer } from './TreeViewer'
import { RouteComponentProps } from "react-router";
import { TreeOperation } from "./Signum.Entities.Tree";
import { QueryString } from '@framework/QueryString'


export default function TreePage(p: RouteComponentProps<{ typeName: string }>) {
  var query = QueryString.parse(p.location.search);

  const filterOptions = React.useMemo(() => Finder.Decoder.decodeFilters(query), [query]);

  const treeViewRef = React.useRef<TreeViewer>(null);

  function changeUrl() {
    var newPath = treeViewRef.current!.getCurrentUrl();

    var currentLocation = AppContext.history.location;

    if (currentLocation.pathname + currentLocation.search != newPath)
      AppContext.history.replace(newPath);
  }

  var ti = getTypeInfo(p.match.params.typeName);

  return (
    <div id="divSearchPage">
      <h2>
        <span className="sf-entity-title">{ti.nicePluralName}</span>
        &nbsp;
                  <a className="sf-popup-fullscreen" href="#" onClick={e => treeViewRef.current!.handleFullScreenClick(e)}>
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

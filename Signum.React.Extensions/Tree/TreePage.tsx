import * as React from 'react'
import { getTypeInfo } from '@framework/Reflection'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import * as Operations from '@framework/Operations'
import { TreeViewer } from './TreeViewer'
import { RouteComponentProps } from "react-router";
import { FilterOption } from "@framework/FindOptions";
import * as QueryString from 'query-string'
import { TreeOperation } from "./Signum.Entities.Tree";
import { useAPI } from '../../../Framework/Signum.React/Scripts/Hooks'

interface TreePageProps extends RouteComponentProps<{ typeName: string }> {

}

export default function TreePage(p: TreePageProps) {
  var query = QueryString.parse(p.location.search);

  const filterOptions = React.useMemo(() => Finder.Decoder.decodeFilters(query), [query]);

  const treeViewRef = React.useRef<TreeViewer>(null);

  function changeUrl() {
    var newPath = treeViewRef.current!.getCurrentUrl();

    var currentLocation = Navigator.history.location;

    if (currentLocation.pathname + currentLocation.search != newPath)
      Navigator.history.replace(newPath);
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
        allowMove={Operations.isOperationAllowed(TreeOperation.Move, ti.name)}
        filterOptions={filterOptions}
        key={ti.name}
        onSearch={() => changeUrl()} />
    </div>
  );
}

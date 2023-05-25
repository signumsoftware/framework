import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import * as Finder from '@framework/Finder'
import * as AppContext from '@framework/AppContext'
import { default as SearchControlLoaded } from '@framework/SearchControl/SearchControlLoaded'
import { TreeMessage } from './Signum.Tree'
import * as TreeClient from './TreeClient'
import { Button } from 'react-bootstrap';

export interface TreeButtonProps {
  searchControl: SearchControlLoaded;
}

export default function TreeButton(p : TreeButtonProps){
  function handleClick(e: React.MouseEvent<any>) {
    const fo = p.searchControl.props.findOptions;

    const path = TreeClient.treePath(fo.queryKey, Finder.toFilterOptions(fo.filterOptions));

    if (p.searchControl.props.avoidChangeUrl)
      window.open(AppContext.toAbsoluteUrl(path));
    else
      AppContext.pushOrOpenInTab(path, e);
  }

  var label = p.searchControl.props.largeToolbarButtons == true ? " " + TreeMessage.Tree.niceToString() : undefined;
  return (
    <Button onClick={handleClick} variant="light" title={TreeMessage.Tree.niceToString()}><FontAwesomeIcon icon="sitemap" />&nbsp; { label }</Button >
  );
}




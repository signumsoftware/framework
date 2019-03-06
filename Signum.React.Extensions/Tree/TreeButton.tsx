import * as React from 'react'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import { default as SearchControlLoaded } from '../../../Framework/Signum.React/Scripts/SearchControl/SearchControlLoaded'
import { TreeMessage } from './Signum.Entities.Tree'
import * as TreeClient from './TreeClient'
import { Button } from '../../../Framework/Signum.React/Scripts/Components';

export interface TreeButtonProps {
  searchControl: SearchControlLoaded;
}

export default function TreeButton(p : TreeButtonProps){
  function handleClick(e: React.MouseEvent<any>) {
    const fo = p.searchControl.props.findOptions;

    const path = TreeClient.treePath(fo.queryKey, Finder.toFilterOptions(fo.filterOptions));

    if (p.searchControl.props.avoidChangeUrl)
      window.open(Navigator.toAbsoluteUrl(path));
    else
      Navigator.pushOrOpenInTab(path, e);
  }

  var label = p.searchControl.props.largeToolbarButtons == true ? " " + TreeMessage.Tree.niceToString() : undefined;
  return (
    <Button onClick={handleClick} color="light"><i className="fa fa-sitemap"></i>&nbsp;{label}</Button>
  );
}




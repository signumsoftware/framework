import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Dic, classes } from '@framework/Globals'
import { getQueryKey } from '@framework/Reflection'
import * as Finder from '@framework/Finder'
import { Lite, toLite } from '@framework/Signum.Entities'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '@framework/FindOptions'
import { SearchMessage, JavascriptMessage, parseLite, is } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import { default as SearchControlLoaded } from '@framework/SearchControl/SearchControlLoaded'
import { TreeMessage } from './Signum.Entities.Tree'
import * as TreeClient from './TreeClient'
import { Button } from '@framework/Components';

export interface TreeButtonProps {
    searchControl: SearchControlLoaded;
}

export default class TreeButton extends React.Component<TreeButtonProps> {

    handleClick = (e: React.MouseEvent<any>) => {

        const fo = this.props.searchControl.props.findOptions;

        const path = TreeClient.treePath(fo.queryKey, Finder.toFilterOptions(fo.filterOptions));

        if (this.props.searchControl.props.avoidChangeUrl)
            window.open(Navigator.toAbsoluteUrl(path));
        else
            Navigator.pushOrOpenInTab(path, e);
    }
    
    render() {
        var label = this.props.searchControl.props.largeToolbarButtons == true ? " " + TreeMessage.Tree.niceToString() : undefined;
        return (
            <Button onClick={this.handleClick} color="light"><FontAwesomeIcon icon="sitemap" />&nbsp;{label}</Button>
        );
    }
}




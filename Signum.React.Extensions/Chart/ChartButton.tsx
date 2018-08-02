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
import { ChartMessage, ChartRequest } from './Signum.Entities.Chart'
import * as ChartClient from './ChartClient'
import { Button } from '@framework/Components';

export interface ChartButtonProps {
    searchControl: SearchControlLoaded;
}

export default class ChartButton extends React.Component<ChartButtonProps> {

    handleOnMouseUp = (e: React.MouseEvent<any>) => {
        
        const fo = this.props.searchControl.props.findOptions;

        const path = ChartClient.Encoder.chartPath(ChartRequest.New({
            queryKey : fo.queryKey,
            orderOptions: [],
            filterOptions: fo.filterOptions.filter(a => a.token != undefined && a.operation != undefined)
        }));

        if (this.props.searchControl.props.avoidChangeUrl)
            window.open(Navigator.toAbsoluteUrl(path));
        else
            Navigator.pushOrOpenInTab(path, e);
    }
    
    render() {
        var label = this.props.searchControl.props.largeToolbarButtons == true ? " " + ChartMessage.Chart.niceToString() : undefined;
        return (
            <Button onMouseUp={this.handleOnMouseUp} color="light"><FontAwesomeIcon icon="chart-bar" />&nbsp;{label}</Button>
        );
    }
 
}




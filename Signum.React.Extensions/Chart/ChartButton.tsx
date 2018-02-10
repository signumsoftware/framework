
import * as React from 'react'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals'
import { getQueryKey } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { Lite, toLite } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '../../../Framework/Signum.React/Scripts/FindOptions'
import { SearchMessage, JavascriptMessage, parseLite, is } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import { default as SearchControlLoaded } from '../../../Framework/Signum.React/Scripts/SearchControl/SearchControlLoaded'
import { ChartMessage, ChartRequest } from './Signum.Entities.Chart'
import * as ChartClient from './ChartClient'
import { Button } from '../../../Framework/Signum.React/Scripts/Components';

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
            <Button onMouseUp={this.handleOnMouseUp} color="light"><i className="fa fa-bar-chart"></i>&nbsp;{label}</Button>
        );
    }
 
}




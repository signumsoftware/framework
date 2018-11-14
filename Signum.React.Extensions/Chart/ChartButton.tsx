import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { FilterOptionParsed, FilterGroupOptionParsed, isFilterGroupOptionParsed } from '@framework/FindOptions'
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

    function simplifyFilters(filterOption: FilterOptionParsed[]): FilterOptionParsed[] {
      return filterOption.map(f => isFilterGroupOptionParsed(f) ?
        { groupOperation: f.groupOperation, token: f.token, filters: simplifyFilters(f.filters) } as FilterGroupOptionParsed :
        f.token && f.operation && f
      ).filter(f => f != null) as FilterOptionParsed[];
    }

    const path = ChartClient.Encoder.chartPath(ChartRequest.New({
      queryKey: fo.queryKey,
      orderOptions: [],
      filterOptions: simplifyFilters(fo.filterOptions)
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




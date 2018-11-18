import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { FilterOptionParsed, FilterGroupOptionParsed, isFilterGroupOptionParsed } from '@framework/FindOptions'
import * as Navigator from '@framework/Navigator'
import { default as SearchControlLoaded } from '@framework/SearchControl/SearchControlLoaded'
import { ChartMessage, ChartRequestModel } from './Signum.Entities.Chart'
import * as ChartClient from './ChartClient'
import { Button } from '@framework/Components';
import { toFilterOptions } from '@framework/Finder';

export interface ChartButtonProps {
  searchControl: SearchControlLoaded;
}

export default class ChartButton extends React.Component<ChartButtonProps> {

  handleOnMouseUp = (e: React.MouseEvent<any>) => {

    const fo = this.props.searchControl.props.findOptions;

    const path = ChartClient.Encoder.chartPath({
      queryName: fo.queryKey,
      orderOptions: [],
      filterOptions: toFilterOptions(fo.filterOptions)
    })

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




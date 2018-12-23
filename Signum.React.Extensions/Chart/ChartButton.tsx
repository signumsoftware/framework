import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { FilterOptionParsed, FilterGroupOptionParsed, isFilterGroupOptionParsed } from '@framework/FindOptions'
import * as Navigator from '@framework/Navigator'
import { default as SearchControlLoaded } from '@framework/SearchControl/SearchControlLoaded'
import { ChartMessage, ChartRequestModel } from './Signum.Entities.Chart'
import * as ChartClient from './ChartClient'
import { Button } from '@framework/Components';
import * as Finder from '@framework//Finder';

export interface ChartButtonProps {
  searchControl: SearchControlLoaded;
}

export default class ChartButton extends React.Component<ChartButtonProps> {

  handleOnMouseUp = (e: React.MouseEvent<any>) => {

    const sc = this.props.searchControl;

    Finder.getQueryDescription(sc.props.findOptions.queryKey).then(qd => {

      const fo = Finder.toFindOptions(sc.props.findOptions, qd);

      const path = ChartClient.Encoder.chartPath({
        queryName: fo.queryName,
        orderOptions: [],
        filterOptions: fo.filterOptions
      })

      if (sc.props.avoidChangeUrl)
        window.open(Navigator.toAbsoluteUrl(path));
      else
        Navigator.pushOrOpenInTab(path, e);
    }).done();
  }

  render() {
    var label = this.props.searchControl.props.largeToolbarButtons == true ? " " + ChartMessage.Chart.niceToString() : undefined;
    return (
      <Button onMouseUp={this.handleOnMouseUp} color="light"><FontAwesomeIcon icon="chart-bar" />&nbsp;{label}</Button>
    );
  }

}




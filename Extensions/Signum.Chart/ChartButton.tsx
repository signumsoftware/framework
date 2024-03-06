import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { FilterOptionParsed, FilterGroupOptionParsed, isFilterGroup } from '@framework/FindOptions'
import * as AppContext from '@framework/AppContext'
import { Navigator } from '@framework/Navigator'
import { default as SearchControlLoaded } from '@framework/SearchControl/SearchControlLoaded'
import { ChartMessage, ChartRequestModel } from './Signum.Chart'
import * as ChartClient from './ChartClient'
import { Button } from 'react-bootstrap'
import * as Finder from '@framework//Finder';

export interface ChartButtonProps {
  searchControl: SearchControlLoaded;
}

export default class ChartButton extends React.Component<ChartButtonProps> {

  handleOnMouseUp = (e: React.MouseEvent<any>) => {
    e.preventDefault();

    if (e.button == 2)
      return;


    const sc = this.props.searchControl;

    Finder.getQueryDescription(sc.props.findOptions.queryKey).then(qd => {

      const fo = Finder.toFindOptions(sc.props.findOptions, qd, false);

      const path = ChartClient.Encoder.chartPath({
        queryName: fo.queryName,
        orderOptions: [],
        filterOptions: fo.filterOptions
      })

      if (sc.props.avoidChangeUrl)
        window.open(AppContext.toAbsoluteUrl(path));
      else
        AppContext.pushOrOpenInTab(path, e);
    });
  }

  render() {
    var label = this.props.searchControl.props.largeToolbarButtons == true ? <span className="d-none d-sm-inline">{" " + ChartMessage.Chart.niceToString()}</span> : undefined;
    return (
      <Button variant="light" onMouseUp={this.handleOnMouseUp} title={ChartMessage.Chart.niceToString()}><FontAwesomeIcon icon="chart-bar" />&nbsp;{label}</Button>
    );
  }

}




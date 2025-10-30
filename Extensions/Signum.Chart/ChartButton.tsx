import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { FilterOptionParsed, FilterGroupOptionParsed, isFilterGroup } from '@framework/FindOptions'
import * as AppContext from '@framework/AppContext'
import { Navigator } from '@framework/Navigator'
import { default as SearchControlLoaded } from '@framework/SearchControl/SearchControlLoaded'
import { ChartMessage, ChartRequestModel, ChartTimeSeriesEmbedded } from './Signum.Chart'
import { ChartClient } from './ChartClient'
import { Button } from 'react-bootstrap'
import { Finder } from '@framework/Finder';

export interface ChartButtonProps {
  searchControl: SearchControlLoaded;
}

export default class ChartButton extends React.Component<ChartButtonProps> {

  handleOnMouseUp = (e: React.MouseEvent<any>): void => {

    if (e.button == 2)
      return;


    const sc = this.props.searchControl;

    Finder.getQueryDescription(sc.props.findOptions.queryKey).then(qd => {

      const fo = Finder.toFindOptions(sc.props.findOptions, qd, false);

      const path = ChartClient.Encoder.chartPath({
        queryName: fo.queryName,
        orderOptions: [],
        filterOptions: fo.filterOptions,
        timeSeries: ChartClient.cloneChartTimeSeries(fo.systemTime as any),
      })

      if (sc.props.avoidChangeUrl)
        window.open(AppContext.toAbsoluteUrl(path));
      else
        AppContext.pushOrOpenInTab(path, e);
    });
  }

  render(): React.JSX.Element {
    var label = this.props.searchControl.props.largeToolbarButtons == true ? <span className="d-none d-sm-inline">{" " + ChartMessage.Chart.niceToString()}</span> : undefined;
    return (
      <button className="btn btn-tertiary" onMouseDown={this.handleOnMouseUp} title={ChartMessage.Chart.niceToString()}><FontAwesomeIcon aria-hidden={true} icon="chart-bar" />&nbsp;{label}</button>
    );
  }

}




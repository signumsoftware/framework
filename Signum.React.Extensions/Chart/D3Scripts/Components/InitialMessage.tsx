import * as React from 'react'
import * as d3 from 'd3'
import {  ChartTable } from '../../ChartClient';
import { Rule } from './Rule';
import { JavascriptMessage, SearchMessage } from '@framework/Signum.Entities';
import { SearchControl } from '@framework/Search';

interface InitialMessageProps {
  x?: number;
  y?: number;
  loading: boolean;
  data?: ChartTable;
}

export default class InitialMessage extends React.Component<InitialMessageProps, { dots: number | null }> {

  intervalHandle?: number 

  constructor(props: InitialMessageProps) {
    super(props);
    this.state = { dots: null };
  }

  componentWillMount() {
    if (this.props.loading)
      this.startTimer();
  }

  componentWillUnmount() {
    this.stopTimer();
  }

  componentWillReceiveProps(newProps: InitialMessageProps) {
    if (this.props.loading != newProps.loading) {
      if (newProps.loading) {
        this.startTimer();
      } else {
        this.stopTimer();
        this.setState({ dots: null });
      }
    }
  }

  startTimer() {
    this.intervalHandle = setInterval(() =>
      this.intervalHandle != null && this.setState({ dots: ((this.state.dots || 0) + 1) % 4 }),
      1000);
  }

  stopTimer() {
    if (this.intervalHandle) {
      clearInterval(this.intervalHandle);
      this.intervalHandle = undefined;
    }
  }

  render() {

    if (this.state.dots)
      return (
        <text x={this.props.x} y={this.props.y} style={{ fontSize: "22px", textAnchor: "middle" }} fill="#aaa">
          {JavascriptMessage.loading.niceToString() + ".".repeat(this.state.dots) + " ".repeat(3 - this.state.dots)}
        </text >
      );

    if (this.props.data == null)
      return (
        <text x={this.props.x} y={this.props.y} style={{ fontSize: "22px", textAnchor: "middle" }} fill="#ddd">
          {JavascriptMessage.searchForResults.niceToString()}
        </text >
      );

    if (this.props.data.rows.length == 0)
      return (
        <text x={this.props.x} y={this.props.y} style={{ fontSize: "22px", textAnchor: "middle" }} fill="#ffb5b5">
          {SearchMessage.NoResultsFound.niceToString()}
        </text >
      );

    return null;
  }
}

import * as React from 'react'
import * as d3 from 'd3'
import * as ChartUtils from './ChartUtils';
import { translate } from './ChartUtils';


export default class TextEllipsis extends React.Component<{ maxWidth: number, padding?: number, etcText?: string; } & React.SVGProps<SVGTextElement>> {

  txt?: SVGTextElement | null;

  render() {

    var { maxWidth, padding, children, etcText, ...atts } = this.props;

    return (
      <text ref={t => this.txt = t} {...atts} >
        {children || ""}
      </text>
    );
  }


  componentDidMount() {
    var etcText = this.props.etcText == null ? "…" : this.props.etcText;

    var width = this.props.maxWidth;
    if (this.props.padding)
      width -= this.props.padding * 2;

    let txtElement = this.txt!;
    let textLength = txtElement.getComputedTextLength();
    let text = txtElement.textContent!;
    while (textLength > width && text.length > 0) {
      text = text.slice(0, -1);
      while (text[text.length - 1] == ' ' && text.length > 0)
        text = text.slice(0, -1);
      txtElement.textContent = text + etcText;
      textLength = txtElement.getComputedTextLength();
    }
  }
}

import * as React from 'react'
import * as d3 from 'd3'
import * as ChartUtils from './ChartUtils';
import { translate } from './ChartUtils';


export interface TextEllipsisProps extends React.SVGProps<SVGTextElement>{
  maxWidth: number;
  padding?: number;
  etcText?: string;
}

export default class TextEllipsis extends React.Component<TextEllipsisProps> {

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
    this.recalculate(this.props);
  }



  componentWillReceiveProps(nextProps: TextEllipsisProps) {
    if (this.props.maxWidth != nextProps.maxWidth ||
      this.props.padding != nextProps.padding ||
      this.props.etcText != nextProps.etcText ||
      getString(this.props) != getString(nextProps))
      this.recalculate(nextProps);
  }

  recalculate(props: TextEllipsisProps) {
    
    var etcText = props.etcText == null ? "…" : props.etcText;

    var width = props.maxWidth;
    if (props.padding)
      width -= props.padding * 2;

    let txtElem = this.txt!;
    txtElem.textContent = getString(this.props);
    let textLength = txtElem.getComputedTextLength();
    let text = txtElem.textContent!;
    while (textLength > width && text.length > 0) {
      text = text.slice(0, -1);
      while (text[text.length - 1] == ' ' && text.length > 0)
        text = text.slice(0, -1);
      txtElem.textContent = text + etcText;
      textLength = txtElem.getComputedTextLength();
    }
  }
}


function getString(props: TextEllipsisProps) {
  return React.Children.toArray(props.children)[0] as string;
}

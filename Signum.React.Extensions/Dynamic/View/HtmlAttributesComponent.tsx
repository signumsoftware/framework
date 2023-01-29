import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes, Dic } from '@framework/Globals'
import { Binding } from '@framework/Reflection'
import { EntityControlMessage } from '@framework/Signum.Entities'
import { ExpressionOrValueComponent, DesignerModal } from './Designer'
import { DesignerNode, ExpressionOrValue, isExpression } from './NodeUtils'
import { BaseNode } from './Nodes'
import { HtmlAttributesExpression } from './HtmlAttributesExpression'
import { Typeahead } from '@framework/Components';
import { useForceUpdate } from '@framework/Hooks'

interface HtmlAttributesLineProps {
  binding: Binding<HtmlAttributesExpression | undefined>;
  dn: DesignerNode<BaseNode>;
}

export function HtmlAttributesLine(p: HtmlAttributesLineProps) {
  function renderMember(expr: HtmlAttributesExpression | undefined): React.ReactNode {
    return (<span
      className={expr === undefined ? "design-default" : "design-changed"}>
      {p.binding.member}
    </span>);
  }

  function handleRemove(e: React.MouseEvent<any>) {
    e.preventDefault();
    p.binding.deleteValue();
    p.dn.context.refreshView();
  }

  function handleCreate(e: React.MouseEvent<any>) {
    e.preventDefault();
    modifyExpression({} as HtmlAttributesExpression);
  }

  function handleView(e: React.MouseEvent<any>) {
    e.preventDefault();
    var hae = JSON.parse(JSON.stringify(p.binding.getValue())) as HtmlAttributesExpression;
    modifyExpression(hae);
  }

  function modifyExpression(hae: HtmlAttributesExpression) {

    if (hae.style == undefined)
      hae.style = {};

    DesignerModal.show("HtmlAttributes", () => <HtmlExpressionComponent dn={p.dn} htmlAttributes={hae} />).then(result => {
      if (result) {
        if ((hae as Object).hasOwnProperty("style") && Dic.getKeys(hae.style!).length == 0)
          delete hae.style;

        if (Dic.getKeys(hae).length == 0)
          p.binding.deleteValue();
        else
          p.binding.setValue(hae);
      }

      p.dn.context.refreshView();
    });
  }


  function getDescription(hae: HtmlAttributesExpression) {

    var { style, ...cleanHae } = hae;


    var keys = Dic.map(cleanHae, (key, value) => key + ":" + getValue(value));

    if (style)
      keys.push("style: {\n" +
        Dic.map(style, (key, value) => "   " + key + ":" + getValue(value)).join("\n") +
        "\n}");

    return keys.join("\n");
  }

  function getValue(value: any) {
    return (isExpression(value) ? "{" + value.__code__ + "}" : value)
  }
  const val = p.binding.getValue();

  return (
    <div className="form-group form-group-xs">
      <label className="control-label label-xs">
        {renderMember(val)}

        {val && " "}
        {val && <a href="#" className={classes("sf-line-button", "sf-remove")}
          onClick={handleRemove}
          title={EntityControlMessage.Remove.niceToString()}>
          <FontAwesomeIcon icon="xmark" />
        </a>}
      </label>
      <div>
        {val ?
          <a href="#" onClick={handleView}><pre style={{ padding: "0px", border: "none" }}>{getDescription(val)}</pre></a>
          :
          <a href="#" title={EntityControlMessage.Create.niceToString()}
            className="sf-line-button sf-create"
            onClick={handleCreate}>
            <FontAwesomeIcon icon="plus" className="sf-create" />&nbsp;{EntityControlMessage.Create.niceToString()}
          </a>}
      </div>
    </div>
  );
}

export interface HtmlExpressionComponentProps {
  dn: DesignerNode<BaseNode>;
  htmlAttributes: HtmlAttributesExpression
}

const htmlAttributeList = ["accept", "accept-charset", "accesskey", "action", "align", "alt", "async", "autocomplete", "autofocus", "autoplay", "autosave", "bgcolor", "border",
  "buffered", "challenge", "charset", "checked", "cite", "class", "code", "codebase", "color", "cols", "colspan", "content", "contenteditable", "contextmenu", "controls",
  "coords", "data", "data-*", "datetime", "default", "defer", "dir", "dirname", "disabled", "download", "draggable", "dropzone", "enctype", "for", "form", "formaction",
  "headers", "height", "hidden", "high", "href", "hreflang", "http-equiv", "icon", "id", "ismap", "itemprop", "keytype", "kind", "label", "lang", "language", "list",
  "loop", "low", "manifest", "max", "maxlength", "media", "method", "min", "multiple", "muted", "name", "novalidate", "open", "optimum", "pattern", "ping", "placeholder",
  "poster", "preload", "radiogroup", "readonly", "rel", "required", "reversed", "rows", "rowspan", "sandbox", "scope", "scoped", "seamless", "selected", "shape", "size",
  "sizes", "span", "spellcheck", "src", "srcdoc", "srclang", "srcset", "start", "step", "style", "summary", "tabindex", "target", "title", "type", "usemap", "value", "width",
  "wrap"].sort();

const cssPropertyList = ["color", "opacity", "background", "background-attachment", "background-blend-mode", "background-color", "background-image", "background-position",
  "background-repeat", "background-clip", "background-origin", "background-size", "border", "border-bottom", "border-bottom-color", "border-bottom-left-radius",
  "border-bottom-right-radius", "border-bottom-style", "border-bottom-width", "border-color", "border-image", "border-image-outset", "border-image-repeat",
  "border-image-slice", "border-image-source", "border-image-width", "border-left", "border-left-color", "border-left-style", "border-left-width", "border-radius",
  "border-right", "border-right-color", "border-right-style", "border-right-width", "border-style", "border-top", "border-top-color", "border-top-left-radius",
  "border-top-right-radius", "border-top-style", "border-top-width", "border-width", "box-decoration-break", "box-shadow", "bottom", "clear", "clip", "display", "float",
  "height", "left", "margin", "margin-bottom", "margin-left", "margin-right", "margin-top", "max-height", "max-width", "min-height", "min-width", "overflow",
  "overflow-x", "overflow-y", "padding", "padding-bottom", "padding-left", "padding-right", "padding-top", "position", "right", "top", "visibility", "width", "vertical-align",
  "z-index", "align-content", "align-items", "align-self", "flex", "flex-basis", "flex-direction", "flex-flow", "flex-grow", "flex-shrink", "flex-wrap", "justify-content",
  "order", "hanging-punctuation", "hyphens", "letter-spacing", "line-break", "line-height", "overflow-wrap", "tab-size", "text-align", "text-align-last", "text-combine-upright",
  "text-indent", "text-justify", "text-transform", "white-space", "word-break", "word-spacing", "word-wrap", "text-decoration", "text-decoration-color", "text-decoration-line",
  "text-decoration-style", "text-shadow", "text-underline-position", "@font-face", "@font-feature-values", "font", "font-family", "font-feature-settings", "font-kerning",
  "font-language-override", "font-size", "font-size-adjust", "font-stretch", "font-style", "font-synthesis", "font-variant", "font-variant-alternates", "font-variant-caps",
  "font-variant-east-asian", "font-variant-ligatures", "font-variant-numeric", "font-variant-position", "font-weight", "direction", "text-orientation", "text-combine-upright",
  "unicode-bidi", "writing-mode", "border-collapse", "border-spacing", "caption-side", "empty-cells", "table-layout", "counter-increment", "counter-reset", "list-style",
  "list-style-image", "list-style-position", "list-style-type", "@keyframes", "animation", "animation-delay", "animation-direction", "animation-duration", "animation-fill-mode",
  "animation-iteration-count", "animation-name", "animation-play-state", "animation-timing-function", "backface-visibility", "perspective", "perspective-origin", "transform",
  "transform-origin", "transform-style", "transition", "transition-property", "transition-duration", "transition-timing-function", "transition-delay", "box-sizing", "content",
  "cursor", "ime-mode", "nav-down", "nav-index", "nav-left", "nav-right", "nav-up", "outline", "outline-color", "outline-offset", "outline-style", "outline-width", "resize",
  "text-overflow", "break-after", "break-before", "break-inside", "column-count", "column-fill", "column-gap", "column-rule", "column-rule-color", "column-rule-style",
  "column-rule-width", "column-span", "column-width", "columns", "widows", "orphans", "page-break-after", "page-break-before", "page-break-inside", "marks", "quotes", "filter",
  "image-orientation", "image-rendering", "image-resolution", "object-fit", "object-position", "mask", "mask-type", "mark", "mark-after", "mark-before", "phonemes", "rest",
  "rest-after", "rest-before", "voice-balance", "voice-duration", "voice-pitch", "voice-pitch-range", "voice-rate", "voice-stress", "voice-volume", "marquee-direction",
  "marquee-play-count", "marquee-speed", "marquee-style"].sort();

export function HtmlExpressionComponent(p: HtmlExpressionComponentProps) {
  return (
    <div className="form-sm code-container">
      <fieldset>
        <legend>HTML Attributes</legend>
        <ExpressionOrValueStrip object={p.htmlAttributes} filterKey={key => key != "style"} dn={p.dn} possibleKeys={htmlAttributeList} />
        <fieldset>
          <legend>CSS Properties</legend>
          <ExpressionOrValueStrip object={p.htmlAttributes.style!} filterKey={key => true} dn={p.dn} possibleKeys={cssPropertyList} />
        </fieldset>
      </fieldset>
    </div>
  );
}



export interface ExpressionOrValueStripProps {
  possibleKeys: string[];
  dn: DesignerNode<BaseNode>;
  object: { [key: string]: ExpressionOrValue<any> }
  filterKey: (key: string) => boolean;
}

export function ExpressionOrValueStrip(p: ExpressionOrValueStripProps) {
  const forceUpdate = useForceUpdate();

  function handleOnRemove(e: React.MouseEvent<any>, key: string) {
    e.preventDefault();
    delete p.object[key];
    forceUpdate();
  }


  function renderList() {
    return (
      <ul className="expression-list">
        {
          Dic.getKeys(p.object).filter(p.filterKey).map(key => <li key={key}>
            <a href="#" className="sf-line-button sf-remove"
              onClick={e => handleOnRemove(e, key)}
              title={EntityControlMessage.Remove.niceToString()}>
              <FontAwesomeIcon icon="xmark" />
            </a>
            <ExpressionOrValueComponent dn={p.dn} refreshView={() => forceUpdate()}
              binding={new Binding(p.object, key)} type="string" defaultValue={null} avoidDelete={true} />
          </li>)
        }
      </ul>
    );
  }

  function handleGetItems(query: string) {
    const result = p.possibleKeys
      .filter(k => k.toLowerCase().contains(query.toLowerCase()) &&
        !(p.object as Object).hasOwnProperty(k))
      .orderBy(a => a.length)
      .filter((k, i) => i < 5);

    return Promise.resolve(result);
  }

  function handleSelect(item: unknown) {
    p.object[item as string] = undefined;
    forceUpdate();
    return "";
  }

  function renderTypeahead() {
    return (
      <Typeahead
        inputAttrs={{ className: "form-control form-control-xs sf-entity-autocomplete" }}
        getItems={handleGetItems}
        onSelect={handleSelect} />
    );
  }

  return (
    <div>
      {renderList()}
      {renderTypeahead()}
    </div>
  );
}

import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes } from '@framework/Globals'
import { isTypeEnum, PropertyRoute } from '@framework/Reflection'
import { Typeahead } from '@framework/Components'
import { TypeHelpClient } from './TypeHelpClient'
import ContextMenu, { getMouseEventPosition } from '@framework/SearchControl/ContextMenu'
import { ContextMenuPosition } from '@framework/SearchControl/ContextMenu'
import "./TypeHelpComponent.css"
import { useAPI } from '@framework/Hooks'
import { TypeHelpMessage } from './Signum.Eval.TypeHelp'
import { LinkButton } from '@framework/Basics/LinkButton'

interface TypeHelpComponentProps {
  initialType?: string;
  mode: TypeHelpClient.TypeHelpMode;
  onMemberClick?: (pr: PropertyRoute) => void;
  renderContextMenu?: (pr: PropertyRoute) => React.ReactElement<any>;
}

function TypeHelpComponent(p: TypeHelpComponentProps): React.JSX.Element {

  const history = React.useMemo<string[]>(() => [], []);

  const [historyIndex, setHistoryIndex] = React.useState<number>(-1);

  const [tempQuery, setTempQuery] = React.useState<string | undefined>(undefined);

  React.useEffect(() => {
    if (p.initialType)
      goTo(p.initialType);
  }, [p.initialType]);

  const typeName = historyIndex == -1 ? undefined : history[historyIndex];

  const help = useAPI<false | TypeHelpClient.TypeHelp | undefined>(() => typeName == null ? Promise.resolve(undefined) : TypeHelpClient.API.typeHelp(typeName, p.mode).then(a => a || false), [typeName]);

  function goTo(type: string) {
    while (history.length - 1 > historyIndex)
      history.removeAt(history.length - 1);
    history.push(type);

    setHistoryIndex(historyIndex + 1);
  }

  function handleGoHistory(e: React.MouseEvent<any>, newIndex: number) {
    e.preventDefault();
    setHistoryIndex(newIndex);
  }


  const [contextMenuPosition, setContextMenuPosition] = React.useState<ContextMenuPosition | undefined>(undefined);
  const [selected, setSelected] = React.useState<PropertyRoute | undefined>(undefined)

  function renderContextualMenu() {
    if (!p.renderContextMenu || !selected)
      return null;

    let menu = p.renderContextMenu(selected);
    return (menu && <ContextMenu position={contextMenuPosition!} onHide={handleContextOnHide}>
      {menu.props.children}
    </ContextMenu>)
  }

  function handleContextOnHide() {
    setSelected(undefined);
    setContextMenuPosition(undefined);
  }

  function handleGetItems(query: string) {
    return TypeHelpClient.API.autocompleteEntityCleanType({
      query: query,
      limit: 5,
    });
  }

  function handleSelect(item: unknown) {
    setTempQuery(undefined);
    goTo(item as string);
    return item as string;
  }

  function currentType(): string {
    return historyIndex == -1 ? "" : history[historyIndex];
  }

  function canBack() {
    return historyIndex > 0;
  }

  function canForth() {
    return historyIndex < history.length - 1;
  }

  function renderHeader() {
    return (
      <div className="sf-type-help-bar">

        <Typeahead
          inputAttrs={{ className: "form-control form-control-sm sf-entity-autocomplete" }}
          getItems={handleGetItems}
          value={tempQuery == undefined ? currentType() : tempQuery}
          onBlur={() => setTempQuery(undefined)}
          onChange={newValue => setTempQuery(newValue)}
          onSelect={handleSelect}
          renderInput={input => <div className="input-group input-group-sm" style={{ position: "initial" }}>
            <button className="btn input-group-text" disabled={!canBack()}              
              onClick={e => handleGoHistory(e, historyIndex - 1)} type="button"
              title={TypeHelpMessage.Previous.niceToString()}
              aria-label={TypeHelpMessage.Previous.niceToString()}>
              <FontAwesomeIcon aria-hidden={true} icon="circle-arrow-left"/>
            </button>
            <button className="btn input-group-text" disabled={!canForth()}              
              onClick={e => handleGoHistory(e, historyIndex + 1)} type="button"
              title={TypeHelpMessage.Next.niceToString()}
              aria-label={TypeHelpMessage.Next.niceToString()}>
              <FontAwesomeIcon aria-hidden={true} icon="circle-arrow-right" />
            </button>
            {input}
            <div className="input-group-text" style={{ color: "white", backgroundColor: p.mode == "CSharp" ? "#007e01" : "#017acc" }}>
              {p.mode == "CSharp" ? "C#" : p.mode}
            </div>
          </div>
          } />
      </div>
    );
  }


  function renderHelp(h: TypeHelpClient.TypeHelp) {
    return (
      <div>
        <h1 className="mb-1 mt-2 h4">{h.type}</h1>

        <ul className="sf-members" style={{ paddingLeft: "0px" }}>
          {h.members.map((m, i) => renderMember(h, m, i))}
        </ul>
      </div>
    );
  }

  function handleOnMemberClick(m: TypeHelpClient.TypeMemberHelp) {
    if (p.onMemberClick && m.propertyString) {
      var pr = PropertyRoute.parse((help as TypeHelpClient.TypeHelp).cleanTypeName, m.propertyString);
      p.onMemberClick(pr);
    }
  }

  function handleOnContextMenuClick(m: TypeHelpClient.TypeMemberHelp, e: React.MouseEvent<any>) {
    if (!m.propertyString)
      return;

    e.preventDefault();
    e.stopPropagation();
    var pr = PropertyRoute.parse((help as TypeHelpClient.TypeHelp).cleanTypeName, m.propertyString);
    setSelected(pr);
    setContextMenuPosition(getMouseEventPosition(e));
  }

  function renderMember(h: TypeHelpClient.TypeHelp, m: TypeHelpClient.TypeMemberHelp, index: number): React.ReactNode {

    var className = "sf-member-name";
    var onClick: React.MouseEventHandler<any> | undefined;
    if (p.onMemberClick) {
      className = classes(className, "sf-member-click");
      onClick = () => handleOnMemberClick(m);
    }

    var onContextMenu: React.MouseEventHandler<any> | undefined;
    if (p.renderContextMenu) {
      onContextMenu = (e) => handleOnContextMenuClick(m, e);
    }

    return (
      <li key={index}>
        {h.isEnum ?
          <span className={className} onClick={onClick} onContextMenu={onContextMenu}>{m.name}</span>
          :
          <div>
            {p.mode == "CSharp" ?
              <span>
                {renderType(m.type, m.cleanTypeName)}{" "}<span className={className} onClick={onClick} onContextMenu={onContextMenu}>{m.name}{m.name && (m.isExpression ? "()" : "")}</span>
              </span> :
              <span>
                <span className={className} onClick={onClick} onContextMenu={onContextMenu}>{m.name ? m.name + ": " : ""}</span>{renderType(m.type, m.cleanTypeName)}
              </span>}

            {m.subMembers.length > 0 &&
              <ul className="sf-members">
                {m.subMembers.map((sm, i) => renderMember(h, sm, i))}
              </ul>}
          </div>}
      </li>
    );
  }

  function renderType(type: string, cleanType?: string | null): React.ReactNode {

    var startIndex = type.indexOf("<");
    var endIndex = type.lastIndexOf(">");

    if (startIndex != -1) {
      return (
        <span>
          {renderType(type.substr(0, startIndex))}
          {"<"}
          {renderType(type.substr(startIndex + 1, endIndex - startIndex - 1), cleanType)}
          {">"}
          {type.startsWith("Lite") && type.endsWith("?") ?
            (p.mode == "TypeScript" ? <span> | <span className="sf-member-primitive">null</span></span> : "?") : ""}
        </span>
      );
    }

    if (type.endsWith("?")) {
      return (
        <span>
          {renderType(type.substr(0, type.length - 1), cleanType)}
          {p.mode == "TypeScript" ? " | " : "?"}
          {p.mode == "TypeScript" && <span className="sf-member-primitive">null</span>}
        </span>
      );
    }

    if (cleanType != null)
      return (
        <span>
          <LinkButton title={undefined} className={"sf-member-" + (isTypeEnum(type) ? "enum" : "class")}
            onClick={(e) => { goTo(cleanType); }}>
            {type}
          </LinkButton>
        </span>
      );

    var kind = type.firstLower() == type ? "primitive" :
      type == "DateTime" || type == "DateOnly" || type == "TimeSpan" || type == "TimeOnly" ? "date" :
        type == "Lite" ? "lite" :
          type == "IEnumerable" || type == "IQueryable" || type == "List" || type == "MList" ? "collection" :
            isTypeEnum(type) ? "enum" : "others";

    return <span className={"sf-member-" + kind} title={kind}>{type}</span>;
  }
  return (
    <div className="sf-type-help">
      {renderHeader()}
      {help == undefined ? <h1 className="h4">Loading {currentType()}…</h1> :
        help == false ? <h1 className="h4">Not found {currentType()}</h1> :
          renderHelp(help)}
      {renderContextualMenu && renderContextualMenu()}
    </div>
  );
}

namespace TypeHelpComponent {
  export function getExpression(initial: string, pr: PropertyRoute | string, mode: TypeHelpClient.TypeHelpMode, options?: { stronglyTypedMixinTS?: boolean }): string {

    if (pr instanceof PropertyRoute)
      pr = pr.propertyPath();

    return pr.split(".").reduce((prev, curr) => {
      if (curr.startsWith("[") && curr.endsWith("]")) {
        const mixin = curr.after("[").beforeLast("]");
        return mode == "CSharp" ?
          `${prev}.Mixin<${mixin}>()` :
          options?.stronglyTypedMixinTS ?
            `getMixin(${prev}, ${mixin})` :
            `${prev}.mixins["${mixin}"]`;
      }
      else
        return mode == "TypeScript" ?
          `${prev}.${curr.firstLower()}` :
          `${prev}.${curr}`;
    }, initial);
  }
}

export default TypeHelpComponent;

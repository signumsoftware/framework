
import * as React from 'react'
import { FindOptions } from '@framework/FindOptions'
import { getQueryNiceName } from '@framework/Reflection'
import { Entity, Lite, is, JavascriptMessage } from '@framework/Signum.Entities'
import { SearchControl, ValueSearchControl } from '@framework/Search'
import * as UserQueryClient from '../../UserQueries/UserQueryClient'
import { UserQueryPartEntity, PanelPartEmbedded, PanelStyle } from '../Signum.Entities.Dashboard'
import { classes } from '@framework/Globals';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { parseIcon } from '../Admin/Dashboard';

export interface UserQueryPartProps {
  partEmbedded: PanelPartEmbedded;
  part: UserQueryPartEntity;
  entity?: Lite<Entity>;
}

export default class UserQueryPart extends React.Component<UserQueryPartProps, { fo?: FindOptions }> {

  constructor(props: any) {
    super(props);
    this.state = { fo: undefined };
  }

  componentWillMount() {
    this.loadFindOptions(this.props);
  }

  componentWillReceiveProps(newProps: UserQueryPartProps) {

    if (is(this.props.part.userQuery, newProps.part.userQuery) &&
      is(this.props.entity, newProps.entity))
      return;

    this.loadFindOptions(newProps);
  }

  loadFindOptions(props: UserQueryPartProps) {

    UserQueryClient.Converter.toFindOptions(props.part.userQuery!, props.entity)
      .then(fo => this.setState({ fo: fo }))
      .done();
  }

  render() {

    if (!this.state.fo)
      return <span>{JavascriptMessage.loading.niceToString()}</span>;

    if (this.props.part.renderMode == "BigValue") {
      return <BigValueSearchCounter
        findOptions={this.state.fo}
        text={this.props.partEmbedded.title || undefined}
        style={this.props.partEmbedded.style!}
        iconName={this.props.partEmbedded.iconName || undefined}
        iconColor={this.props.partEmbedded.iconColor || undefined}
      />;
    }

    return (
      <SearchControl
        findOptions={this.state.fo}
        showHeader={"PinnedFilters"}
        showFooter={false}
        allowSelection={this.props.part.renderMode == "SearchControl"} />
    );
  }
}


interface BigValueBadgeProps {
  findOptions: FindOptions;
  text?: string;
  style: PanelStyle;
  iconName?: string;
  iconColor?: string;
}

export class BigValueSearchCounter extends React.Component<BigValueBadgeProps, { isRTL: boolean; }> {

  constructor(props: BigValueBadgeProps) {
    super(props);

    this.state = { isRTL: document.body.classList.contains("rtl") };
  }

  vsc!: ValueSearchControl;

  render() {

    return (
      <div className={classes(
        "card",
        this.props.style != "Light" && "text-white",
        "bg-" + this.props.style.toLowerCase(),
        "o-hidden"
      )}>
        <div className={classes("card-body", "bg-" + this.props.style.toLowerCase())} onClick={e => this.vsc.handleClick(e)} style={{ cursor: "pointer" }}>
          <div className="row">
            <div className="col-3">
              {this.props.iconName &&
                <FontAwesomeIcon icon={parseIcon(this.props.iconName)!} color={this.props.iconColor} size="4x" />}
            </div>
            <div className={classes("col-9 flip", this.state.isRTL ? "text-left" : "text-right")}>
              <h1>
                <ValueSearchControl
                  ref={vsc => {
                    if (this.vsc == null && vsc) {
                      this.vsc = vsc;
                    }
                  }}
                  findOptions={this.props.findOptions} isLink={false} isBadge={false} />
              </h1>
            </div>
          </div>
          <div className={classes("flip", this.state.isRTL ? "text-left" : "text-right")}>
            <h6 className="large">{this.props.text || getQueryNiceName(this.props.findOptions.queryName)}</h6>
          </div>
        </div>
      </div>
    );
  }
}






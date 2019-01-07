import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { RouteComponentProps } from 'react-router'
import * as Finder from '../Finder'
import { FindOptions, FilterOption, isFilterGroupOption } from '../FindOptions'
import { getQueryNiceName } from '../Reflection'
import * as Navigator from '../Navigator'
import SearchControl from './SearchControl'
import * as QueryString from 'query-string'

interface SearchPageProps extends RouteComponentProps<{ queryName: string }> {

}

interface SearchPageState {
  findOptions: FindOptions;
}

export default class SearchPage extends React.Component<SearchPageProps, SearchPageState> {
  static marginDown = 130;
  static minHeight = 600;
  static showFilters = (fo: FindOptions) => !(fo.filterOptions == undefined || fo.filterOptions.length == 0 || anyPinned(fo.filterOptions));

  constructor(props: SearchPageProps) {
    super(props);
    this.state = this.calculateState(this.props);
  }

  searchControl!: SearchControl;

  componentWillReceiveProps(nextProps: SearchPageProps) {
    this.setState(this.calculateState(nextProps));
  }

  componentWillMount() {
    window.addEventListener('resize', this.onResize);
  }

  componentWillUnmount() {
    window.removeEventListener('resize', this.onResize);

    Navigator.setTitle();
  }

  onResize = () => {
    var sc = this.searchControl;
    var scl = sc && sc.searchControlLoaded;
    var containerDiv = scl && scl.containerDiv;
    if (containerDiv) {

      var marginTop = containerDiv.offsetTop;

      var maxHeight = (window.innerHeight - (marginTop + SearchPage.marginDown));

      containerDiv.style.maxHeight = Math.max(maxHeight, SearchPage.minHeight) + "px";
    }
  }

  calculateState(props: SearchPageProps): SearchPageState {

    Navigator.setTitle(getQueryNiceName(props.match.params.queryName));

    return {
      findOptions: {
        ...Finder.parseFindOptionsPath(props.match.params.queryName, QueryString.parse(props.location.search))
      },
    };
  }

  changeUrl() {
    const scl = this.searchControl.searchControlLoaded!;
    const findOptions = Finder.toFindOptions(scl.props.findOptions, scl.props.queryDescription);
    const newPath = Finder.findOptionsPath(findOptions);
    const currentLocation = Navigator.history.location;

    if (currentLocation.pathname + currentLocation.search != newPath)
      Navigator.history.replace(newPath);
  }

  render() {
    const fo = this.state.findOptions;
    if (!Finder.isFindable(fo.queryName, true))
      return (
        <div id="divSearchPage">
          <h3>
            <span className="display-6 sf-query-title">{getQueryNiceName(fo.queryName)}</span>
            <small>Error: Query not allowed {Finder.isFindable(fo.queryName, false) ? "in full screen" : ""}</small>
          </h3>
        </div>
      );

    return (
      <div id="divSearchPage">
        <h3 className="display-6 sf-query-title">
          <span>{getQueryNiceName(fo.queryName)}</span>
          &nbsp;
            <a className="sf-popup-fullscreen" href="#" onClick={(e) => this.searchControl.handleFullScreenClick(e)}>
            <FontAwesomeIcon icon="external-link-alt" />
          </a>
        </h3>
        <SearchControl ref={e => this.searchControl = e!}
          findOptions={fo}
          tag="SearchPage"
          throwIfNotFindable={true}
          showBarExtension={true}
          hideFullScreenButton={true}
          largeToolbarButtons={true}
          showFilters={SearchPage.showFilters(fo)}
          showGroupButton={true}
          avoidChangeUrl={false}
          maxResultsHeight={"none"}
          onHeighChanged={this.onResize}
          onSearch={result => this.changeUrl()}
        />
      </div>
    );
  }
}


function anyPinned(filterOptions?: FilterOption[]): boolean {
  if (filterOptions == null)
    return false;

  return filterOptions.some(a => Boolean(a.pinned) || isFilterGroupOption(a) && anyPinned(a.filters));
}

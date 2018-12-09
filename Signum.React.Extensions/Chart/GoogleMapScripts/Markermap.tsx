/// <reference path="../../node_modules/@types/googlemaps/index.d.ts" />
import * as React from 'react'
import * as d3 from 'd3'
import * as Navigator from '@framework/Navigator';
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../D3Scripts/Components/ChartUtils';
import * as GoogleMapsChartUtils from './GoogleMapsChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from '../D3Scripts/Components/ChartUtils';
import { ChartRow } from '../ChartClient';
import googleMapStyles from "./GoogleMapStyles"


export default class MarkermapChart extends React.Component<ChartClient.ChartComponentProps> {

  componentDidMount() {
    GoogleMapsChartUtils.loadGoogleMapsScript(() => {
      GoogleMapsChartUtils.loadGoogleMapsMarkerCluster(() => {
        this.drawChart(this.props);
      })
    });
  }

  divElement?: HTMLDivElement | null;

  render() {
    return (
      <div className="sf-chart-container" ref={d => this.divElement = d}>
      </div>
    );
  }

  drawChart({ data, parameters }: ChartClient.ChartComponentProps) {

    var mapType = parameters["MapType"] == "Roadmap" ? google.maps.MapTypeId.ROADMAP : google.maps.MapTypeId.SATELLITE;

    var latitudeColumn = data && data.columns.c0! as ChartClient.ChartColumn<number> | undefined;
    var longitudeColumn = data && data.columns.c1! as ChartClient.ChartColumn<number> | undefined;

    var centerMap = new google.maps.LatLng(
      data && data.rows.length > 0 ? latitudeColumn!.getValue(data.rows[0]) : 0,
      data && data.rows.length > 0 ? longitudeColumn!.getValue(data.rows[0]) : 0);

    var mapOptions = {
      center: centerMap,
      zoom: 2,
      mapTypeControlOptions: {
        mapTypeIds: ["roadmap", "satellite", "hybrid", "terrain",
          "styled_map"]
      },
      mapTypeId: mapType
    } as google.maps.MapOptions;

    var map = new google.maps.Map(this.divElement!, mapOptions);

    if (parameters["MapStyle"] != null &&
      parameters["MapStyle"] != "Standard") {

      var json = googleMapStyles[parameters["MapStyle"]];

      if (json != null) {
        map.mapTypes.set("styled_map", new google.maps.StyledMapType(json, { name: 'Styled Map' }));
        map.setMapTypeId("styled_map");
      }
    }

    var bounds = new google.maps.LatLngBounds();

    var clusterMap = parameters["ClusterMap"] == "Yes";

    var animateOnClick = parameters["AnimateOnClick"] == "Yes" && !clusterMap;

    var markers: google.maps.Marker[] = [];

    function toggleBounce(marker: google.maps.Marker) {
      if (marker.getAnimation() !== null && marker.getAnimation() != undefined) {
        marker.setAnimation(null);
      } else {
        marker.setAnimation(google.maps.Animation.BOUNCE);
      }
    }

    if (data) {
      var labelColumn = data.columns.c2;
      var iconColumn = data.columns.c3 as ChartClient.ChartColumn<string> | undefined;
      var titleColumn = data.columns.c4;
      var infoColumn = data.columns.c5;
      var colorScaleColumn = data.columns.c6 as ChartClient.ChartColumn<number> | undefined;
      var colorSchemeColumn = data.columns.c7;

      var color: ((r: ChartRow) => string) | null = null;
      if (colorScaleColumn != null) {
        var scaleFunc = scaleFor(colorScaleColumn, data.rows.map(r => colorScaleColumn!.getValue(r)), 0, 100, parameters["ColorScale"]);
        var colorInterpolator = ChartUtils.getColorInterpolation(parameters["ColorSet"])!;
        color = r => colorInterpolator!(scaleFunc(colorScaleColumn!.getValue(r)));
      }
      else if (colorSchemeColumn != null) {
        var scheme = ChartUtils.getColorScheme(parameters["ColorCategory"])!;
        var categoryColor = d3.scaleOrdinal(scheme).domain(data.rows.map(colorSchemeColumn.getValueKey));
        color = r => colorSchemeColumn!.getValueColor(r) || categoryColor(colorSchemeColumn!.getValueKey(r));
      }

      data.rows.forEach(r => {
        if (latitudeColumn!.getValue(r) != null && longitudeColumn!.getValue(r) != null) {
          var position = new google.maps.LatLng(latitudeColumn!.getValue(r), longitudeColumn!.getValue(r));
          bounds.extend(position);
          var marker = new google.maps.Marker({
            position: position,
            label: labelColumn && labelColumn.getValueNiceName(r),
            icon: iconColumn ? iconColumn.getValue(r) : color ?
              {
                anchor: new google.maps.Point(16, 16),
                url: 'data:image/svg+xml;utf-8, \
    																<svg width="16" height="16" viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg"> \
    																<circle cx="8" cy="8" r="8" fill="'  + color(r) + '" /> \
    																</svg>'
              } : undefined,
            title: titleColumn && titleColumn.getValueNiceName(r)
          });

          if (infoColumn) {

            marker.addListener("click", () => {

              var html = `<div>
                                ${infoColumn!.getValueNiceName(r)}
                                ${(parameters["InfoLinkPosition"] == "Below" ? "<br/>" : "")} +
                                <a ´${(parameters["InfoLinkPosition"] == "Inline" ? " style='margin-left: 10px;'" : "")}>
                                    <svg aria-hidden='true' data-prefix='fas' data-icon='external-link-alt' class='svg-inline--fa fa-external-link-alt fa-w-18 ' role='img' xmlns='http://www.w3.org/2000/svg' viewBox='0 0 576 512' style='shape-rendering: auto;'>
                                        <path fill='currentColor' d='M576 24v127.984c0 21.461-25.96 31.98-40.971 16.971l-35.707-35.709-243.523 243.523c-9.373 9.373-24.568 9.373-33.941 0l-22.627-22.627c-9.373-9.373-9.373-24.569 0-33.941L442.756 76.676l-35.703-35.705C391.982 25.9 402.656 0 424.024 0H552c13.255 0 24 10.745 24 24zM407.029 270.794l-16 16A23.999 23.999 0 0 0 384 303.765V448H64V128h264a24.003 24.003 0 0 0 16.97-7.029l16-16C376.089 89.851 365.381 64 344 64H48C21.49 64 0 85.49 0 112v352c0 26.51 21.49 48 48 48h352c26.51 0 48-21.49 48-48V287.764c0-21.382-25.852-32.09-40.971-16.97z'></path>
                                    </svg>
                                </a>
                            </div>`;

              var d = document.createElement("div");
              d.innerHTML = html;
              d.querySelector("a")!.onclick = () => {
                this.props.onDrillDown(r);
              };


              var infow = new google.maps.InfoWindow({
                content: d!.innerHTML
              });


              infow.open(map, marker);
            });
          }
          else {
            marker.addListener("click", () => {
              this.props.onDrillDown(r);
            });
          }


          if (animateOnClick) {
            marker.addListener("click", function () {
              toggleBounce(marker);
            });
          }

          markers.push(marker);
        }
      });

      map.fitBounds(bounds);
      map.panToBounds(bounds);

      if (!clusterMap) {
        if (parameters["AnimateDrop"] == "Yes" && markers.length <= 25) {

          for (var i = 0; i < markers.length; i++) {
            const marker = markers[i];
            window.setTimeout(function () {
              marker.setAnimation(google.maps.Animation.DROP);
              marker.setMap(map);
            }, i * 200);
          }
        }
        else {
          markers.forEach(function (marker) {
            marker.setMap(map);
          })
        }
      }
      else {
        var markerCluster = new window.MarkerClusterer(map, markers,
          { imagePath: GoogleMapsChartUtils.urlCdnClusterImages });
      }
    }
  }
}

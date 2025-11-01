/// <reference path="../../../../node_modules/@types/googlemaps/index.d.ts" />
import * as React from 'react'
import * as d3 from 'd3'
import { Navigator } from '@framework/Navigator';
import * as ChartUtils from '../D3Scripts/Components/ChartUtils';
import * as GoogleMapsChartUtils from './GoogleMapsChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from '../D3Scripts/Components/ChartUtils';
import { ChartClient, ChartColumn, ChartRow, ChartScriptProps } from '../ChartClient';
import googleMapStyles from "./GoogleMapStyles"


export default function renderMarkermapChart(p: ChartScriptProps): React.JSX.Element {
  return <MarkermapChartImp {...p} />
}

export function MarkermapChartImp({ data, parameters, onDrillDown, memo }: ChartScriptProps): React.JSX.Element {

  const divElement = React.useRef<HTMLDivElement>(null);

  React.useEffect(() => {
    GoogleMapsChartUtils.loadGoogleMapsScript(() => {
      GoogleMapsChartUtils.loadGoogleMapsMarkerCluster(() => {
        drawChart()
      });
    });
  })

  return (
    <div style={{ width: "100%", height: "100%" }} ref={divElement}>
    </div>
  );




  function drawChart() {

    var mapType = parameters["MapType"] == "Roadmap" ? google.maps.MapTypeId.ROADMAP : google.maps.MapTypeId.SATELLITE;
    var zoom = parameters["Zoom"];
    var minZoom = parameters["MinZoom"];
    var maxZoom = parameters["MaxZoom"];

    var latitudeColumn = data?.columns.c0! as ChartColumn<number> | undefined;
    var longitudeColumn = data?.columns.c1! as ChartColumn<number> | undefined;

    var centerMap = new google.maps.LatLng(
      data?.rows.length ? latitudeColumn!.getValue(data.rows[0]) : 0,
      data?.rows.length ? longitudeColumn!.getValue(data.rows[0]) : 0);

    var mapOptions = {
      center: centerMap,
      zoom: zoom && parseInt(zoom),
      minZoom: minZoom && parseInt(minZoom),
      maxZoom: maxZoom && parseInt(maxZoom),
      mapTypeControlOptions: {
        mapTypeIds: ["roadmap", "satellite", "hybrid", "terrain",
          "styled_map"]
      },
      mapTypeId: mapType
    } as google.maps.MapOptions;

    var map = new google.maps.Map(divElement.current!, mapOptions);

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
      var iconColumn = data.columns.c3 as ChartColumn<string> | undefined;
      var titleColumn = data.columns.c4;
      var infoColumn = data.columns.c5;
      var colorScaleColumn = data.columns.c6 as ChartColumn<number> | undefined;
      var colorSchemeColumn = data.columns.c7;

      var color: ((r: ChartRow) => string) | null = null;
      if (colorScaleColumn != null) {
        var scaleFunc = scaleFor(colorScaleColumn, data.rows.map(r => colorScaleColumn!.getValue(r)), 0, 100, parameters["ColorScale"]);
        var colorInterpolator = ChartUtils.getColorInterpolation(parameters["ColorInterpolation"])!;
        color = r => colorInterpolator!(scaleFunc(colorScaleColumn!.getValue(r))!);
      }
      else if (colorSchemeColumn != null) {
        var categoryColor = ChartUtils.colorCategory(parameters, data.rows.map(colorSchemeColumn.getValueKey), memo);
        color = r => colorSchemeColumn!.getValueColor(r) ?? categoryColor(colorSchemeColumn!.getValueKey(r));
      }

      data.rows.forEach(r => {
        if (latitudeColumn!.getValue(r) != null && longitudeColumn!.getValue(r) != null) {
          var position = new google.maps.LatLng(latitudeColumn!.getValue(r), longitudeColumn!.getValue(r));
          bounds.extend(position);
          var marker = new google.maps.Marker({
            position: position,
            label: labelColumn?.getValueNiceName(r),
            icon: iconColumn ? iconColumn.getValue(r) : color ?
              {
                anchor: new google.maps.Point(16, 16),
                url: 'data:image/svg+xml;utf-8, \
    																<svg width="16" height="16" viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg"> \
    																<circle cx="8" cy="8" r="8" fill="'  + color(r) + '" /> \
    																</svg>'
              } : undefined,
            title: titleColumn?.getValueNiceName(r)
          });

          if (infoColumn) {

            marker.addListener("click", (e: any) => {

              var html = 
                infoColumn!.getValueNiceName(r) +
                (parameters["InfoLinkPosition"] == "Below" ? "<br/>" : "");

              var d = document.createElement("div");
              d.innerHTML = html;
              var link = document.createElement("a");
              link.style.cursor = "pointer";
              link.style.color = "blue";
              if (parameters["InfoLinkPosition"] == "Inline") {
                link.style.marginLeft = "10px";
                link.style.marginRight = "20px";
              }
              link.innerHTML = "<svg aria-hidden='true' data-prefix='fas' data-icon='up-right-from-square' class='svg-inline--fa fa-external-link-alt fa-w-18 ' role='img' xmlns='http://www.w3.org/2000/svg' viewBox='0 0 576 512' style='shape-rendering: auto;'>" +
                "<path fill='currentColor' d='M576 24v127.984c0 21.461-25.96 31.98-40.971 16.971l-35.707-35.709-243.523 243.523c-9.373 9.373-24.568 9.373-33.941 0l-22.627-22.627c-9.373-9.373-9.373-24.569 0-33.941L442.756 76.676l-35.703-35.705C391.982 25.9 402.656 0 424.024 0H552c13.255 0 24 10.745 24 24zM407.029 270.794l-16 16A23.999 23.999 0 0 0 384 303.765V448H64V128h264a24.003 24.003 0 0 0 16.97-7.029l16-16C376.089 89.851 365.381 64 344 64H48C21.49 64 0 85.49 0 112v352c0 26.51 21.49 48 48 48h352c26.51 0 48-21.49 48-48V287.764c0-21.382-25.852-32.09-40.971-16.97z'></path>" +
                "</svg>";
              link.addEventListener("click", (e) => {
                e.preventDefault();
                onDrillDown(r, e);
              });

              d.append(link);

              var infow = new google.maps.InfoWindow({
                content: d
              });

              infow.open(map, marker);
            });
          }
          else {
            marker.addListener("click", e => {
              onDrillDown(r, e as any as MouseEvent);
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
            window.window.setTimeout(function () {
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

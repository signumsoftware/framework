import * as React from 'react'
import * as d3 from 'd3'
import * as ChartClient from '../ChartClient';
import * as ChartUtils from '../D3Scripts/Components/ChartUtils';
import * as GoogleMapsChartUtils from './GoogleMapsChartUtils';
import googleMapStyles from "./GoogleMapStyles"
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from '../D3Scripts/Components/ChartUtils';


export default class HeatmapChart extends React.Component<ChartClient.ChartComponentProps> {

  componentDidMount() {
    GoogleMapsChartUtils.loadGoogleMapsScript(() => {
      this.drawChart(this.props)
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
    
    var bounds = new google.maps.LatLngBounds();
    
    var coords: any[] = [];

    if (data) {
      var latitudeColumn = data.columns.c0! as ChartClient.ChartColumn<number>;
      var longitudeColumn = data.columns.c1! as ChartClient.ChartColumn<number>;
      var weightColumn = data.columns.c2! as ChartClient.ChartColumn<number> | undefined;

      data.rows.forEach(r => {
        if (latitudeColumn.getValue(r) != null &&
          longitudeColumn.getValue(r) != null) {
          var position = new google.maps.LatLng(latitudeColumn.getValue(r), longitudeColumn.getValue(r));
          bounds.extend(position);
          coords.push(weightColumn && weightColumn.getValue(r) != null ? { location: position, weight: weightColumn.getValue(r) } : position);
        }
      });
    }

    var mapType = parameters["MapType"] == "Roadmap" ? google.maps.MapTypeId.ROADMAP : google.maps.MapTypeId.SATELLITE;

    var map = new google.maps.Map(this.divElement!, {
      center: new google.maps.LatLng(coords[0].lat, coords[0].lng),
      zoom: 2,
      mapTypeControlOptions: {
        mapTypeIds: ["roadmap", "satellite", "hybrid", "terrain", "styled_map"]
      },
      mapTypeId: mapType
    });

    if (parameters["MapStyle"] != null &&
      parameters["MapStyle"] != "Standard") {

      var json = googleMapStyles[parameters["MapStyle"]];

      if (json != null) {
        map.mapTypes.set("styled_map", new google.maps.StyledMapType(json, { name: 'Styled Map' }));
        map.setMapTypeId("styled_map");
      }
    }

    map.fitBounds(bounds);
    map.panToBounds(bounds);

    var heatmap = new google.maps.visualization.HeatmapLayer({
      data: coords,
      map: map
    });

    if (parameters["Opacity"] != null) {
      heatmap.set("opacity", parseFloat(parameters["Opacity"]));
    }

    if (parameters["Radius(px)"] != null) {
      heatmap.set("radius", parseFloat(parameters["Radius(px)"]));
    }

    if (parameters["ColorGradient"] != null &&
      parameters["ColorGradient"] != "Default") {
      var gradient;

      switch (parameters["ColorGradient"]) {
        case "Blue-Red":
          gradient = [
            "rgba(0, 255, 255, 0)",
            "rgba(0, 255, 255, 1)",
            "rgba(0, 191, 255, 1)",
            "rgba(0, 127, 255, 1)",
            "rgba(0, 63, 255, 1)",
            "rgba(0, 0, 255, 1)",
            "rgba(0, 0, 223, 1)",
            "rgba(0, 0, 191, 1)",
            "rgba(0, 0, 159, 1)",
            "rgba(0, 0, 127, 1)",
            "rgba(63, 0, 91, 1)",
            "rgba(127, 0, 63, 1)",
            "rgba(191, 0, 31, 1)",
            "rgba(255, 0, 0, 1)"
          ];
          break;
        case "Fire":
          gradient = [
            "rgba(255, 29, 29, 0)",
            "rgba(255, 29, 29, 1)",
            "rgba(255, 74, 29, 1)",
            "rgba(255, 89, 29, 1)",
            "rgba(255, 93, 29, 1)",
            "rgba(255, 104, 29, 1)",
            "rgba(255, 111, 29, 1)",
            "rgba(255, 153, 29, 1)",
            "rgba(255, 202, 29, 1)",
            "rgba(255, 255, 29, 1)",
            "rgba(255, 249, 147, 1)",
            "rgba(255, 255, 255, 1)"
          ];
          break;
        case "Emerald":
          gradient = [
            "rgba(83, 255, 83, 0)",
            "rgba(83, 255, 83, 1)",
            "rgba(104, 255, 104, 1)",
            "rgba(117, 255, 117, 1)",
            "rgba(160, 255, 163, 1)",
            "rgba(216, 255, 218, 1)",
            "rgba(244, 255, 245, 1)",
            "rgba(252, 252, 252, 1)"
          ];
          break;
        case "Cobalt":
          gradient = [
            "rgba(3, 5, 255, 0)",
            "rgba(3, 5, 255, 1)",
            "rgba(2, 158, 225, 1)",
            "rgba(90, 255, 255, 1)",
            "rgba(127, 255, 255, 1)",
            "rgba(170, 255, 255, 1)",
            "rgba(211, 255, 255, 1)",
            "rgba(255, 255, 255, 1)"
          ];
          break;
        case "Purple-Blue":
          gradient = [
            "rgba(255, 247, 251, 0)",
            "#fff7fb",
            "#ece2f0",
            "#d0d1e6",
            "#a6bddb",
            "#67a9cf",
            "#3690c0",
            "#02818a",
            "#016c59",
            "#014636"
          ];
          break;
        case "Orange-Red":
          gradient = [
            "rgba(255, 255, 229, 0)",
            "#ffffe5",
            "#fff7bc",
            "#fee391",
            "#fec44f",
            "#fe9929",
            "#ec7014",
            "#cc4c02",
            "#993404",
            "#662506"
          ];
          break;
        case "Purples":
          gradient = [
            "rgba(252, 251, 253, 0)",
            "#fcfbfd",
            "#efedf5",
            "#dadaeb",
            "#bcbddc",
            "#9e9ac8",
            "#807dba",
            "#6a51a3",
            "#54278f",
            "#3f007d"
          ];
          break;
        case "Greys":
          gradient = [
            "rgba(255, 255, 255, 0)",
            "#ffffff",
            "#f0f0f0",
            "#d9d9d9",
            "#bdbdbd",
            "#969696",
            "#737373",
            "#525252",
            "#252525",
            "#000000"
          ];
          break;
        default:
          gradient = null;
      }

      if (gradient != null) {
        heatmap.set("gradient", gradient);
      }
    }
  }
}

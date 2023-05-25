import googleMapStyles from "./GoogleMapStyles"


export function getScript(source: string, onload?: () => void) {
  var script = document.createElement('script');
  var prior = document.getElementsByTagName('script')[0];
  script.async = true;
  script.src = source;
  script.onload = onload ?? null;
  prior.parentNode!.insertBefore(script, prior);
}

export function getMapStyles() {
  return googleMapStyles;
}

declare global {
  interface Window {
    __google_api_key: string;
    googleMapsCallback?: () => void;
    MarkerClusterer: any;
  }
}

//export var urlCdnClusterJs = "https://developers.google.com/maps/documentation/javascript/examples/markerclusterer/markerclusterer.js";
export var urlCdnClusterJs = "https://cdn.rawgit.com/googlemaps/js-marker-clusterer/gh-pages/src/markerclusterer.js";
export var urlCdnClusterImages = "https://cdn.rawgit.com/googlemaps/js-marker-clusterer/gh-pages/images/m";

export function loadGoogleMapsScript(onReady: () => void) {

  if (!(typeof google === 'object' && typeof google.maps === 'object')) {

    if (window.__google_api_key == null)
      throw Error("You need to set window.__google_api_key to use this map");

    var oldFunction = window.googleMapsCallback;
    window.googleMapsCallback = function () {
      if (oldFunction)
        oldFunction();

      onReady();
    };

    getScript("https://maps.googleapis.com/maps/api/js?key=" + window.__google_api_key + "&libraries=visualization&callback=window.googleMapsCallback");
  } else {
    onReady();
  }

}


export function loadGoogleMapsMarkerCluster(onReady: () => void) {
  if (!window.MarkerClusterer) {
    getScript(urlCdnClusterJs, function () {
      onReady();
    });
  }
  else {
    onReady();
  }
}

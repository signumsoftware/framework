# Finder class

The main responsibilities of `Finder` class in the server-side is to return the `ActionResults` that are necessary to open a `SearchPage`, `SearchPopup` or the `SearchResults` after making the database query. 

By default the custom views provided by the framework are used, but theoretically they could be overridden to open any other custom  `SearchControl`. .
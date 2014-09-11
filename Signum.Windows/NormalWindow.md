# NormalWindow 

`NormalWindow` is just a window frame around the custom controls you designed for your entities, this way, we simplify writing custom controls by factoring out all the common functionality for every entity.

This frame contains:

* **ButtonBar:** Where the operations will be placed and with some default buttons already available: 
   * Reload: To get a clean new instance of the entity from the server, loosing changes. Like F5 in a browser. 
   * Ok: To confirm changes and return the entity in a modal navigation, to cancel just close the window.  
* **EntityTitle:** Showing the `ToString` and the type `NiceName` and `Id`. 
* **WidgetPanel:** A left panel that shows an extensible list of widgets, like QuickLinks, Notes and Alerts, Documents, etc...
* **ErrorSummary:** A control in the button that shows the errors of the entities. 

In the center of all this is the custom control for your particular entity.

`NormalControl` is just an implementation detail, and should be transparent to the developer, not something the you should use on a day-to-day basis. Do not instantiate and show `NormalWindow` directly,  use [Navigator](../Facades/Navigator.md) actions instead!. This way you'll be able to, for example, change the UI to a tabbed UI in the future. 

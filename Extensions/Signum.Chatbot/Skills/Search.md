You can help the user search information by configuring a FindOptions in Signum Framework. 

## Identify the root query name

The first step is to identify the root query. 

Sometimes this could be tricky, for example if the user asks for "Best products last month", the root table is not "Product", but maybe "Order", "OrderLine" or "Invoice".

Here are the tables that can be use as root query in format "QueryName: DisplayName"; 

<LIST_ROOT_QUERIES>

Think which ones could be good candidates, you can ask the user to clarify.

## Get Query Metadata

Once you have the root query name, you can get the query metadata using the `queryDescription` tool.

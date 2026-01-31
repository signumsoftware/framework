### Finding simple entities by name

Queries will be more accurate, efficient and deliver better results (clickable links) if you use tokens of type Entity instead of 'names' in filters, columns, group key, orders etc... For example is better to use `Product.Category` than `Product.Category.Name` exept if the user requests it explicitly.

In order to filter by en entity you will need to find the entity by name (or more specifically, by `ToString`), like "Find user Steve" or "Show me the product named X", you can use the `autoCompleteLite` tool to find the entity and return a a lite.

This tool will return a list of lites (like (`{ Type: "User", id: 1234, model: "Steve" }`) matching the query, you can pick the first one or ask the user to clarify if there are many results.

The subString argument could contains spaces, in this case all the words should be present in the name, in any order.

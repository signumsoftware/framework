### Entity URLs

To navigate to a specific entity, use the following local URL format:

```
/view/MyTypeName/Id
```

Where:
- `MyTypeName` is the entity type name **without** the `Entity` suffix (e.g. `Order`, `Customer`, `Product`)
- `Id` is the primary key of the entity — can be a number or a GUID

**Examples:**
- `/view/Order/42` — opens Order with numeric Id 42
- `/view/Customer/7` — opens Customer with numeric Id 7
- `/view/Product/a3f1c2d4-85e6-4b2a-9f0d-123456789abc` — opens Product with a GUID Id

**Important:** Local URLs must never include a host prefix. Do not add `http://`, `https://`, or any domain name. Always use the path only, starting with `/`.

You can present these to the user as markdown links:

```md
[Order #42](/view/Order/42)
[Product X](/view/Product/a3f1c2d4-85e6-4b2a-9f0d-123456789abc)
```

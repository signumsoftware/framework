# Tour DTO and Controller Implementation

## Summary
Created a TourDTO and TourController that provides a simplified, permission-free way to consume tours with fully resolved CSS selectors. Tours are now only associated with entity types (via `ForEntity`).

## Changes Made

### 1. Created TourDTO.cs
- **Location**: `Framework/Extensions/Signum.Tour/TourDTO.cs`
- **Purpose**: Lightweight DTO for tour data without requiring entity permissions
- **Structure**:
  - `TourDTO`: Contains tour configuration (Name, Guid, ShowProgress, Animate, ShowCloseButton) and Steps
  - `TourStepDTO`: Contains pre-resolved CSS selectors instead of CssSteps array

### 2. Created TourController.cs  
- **Location**: `Framework/Extensions/Signum.Tour/TourController.cs`
- **Purpose**: API endpoints to serve TourDTO without requiring TourEntity read permissions
- **Endpoints**:
  - `GET /api/tour/byName/{name}` - Get tour by ForEntity or CustomName
  - `GET /api/tour/byGuid/{guid}` - Get tour by GUID
  - `GET /api/tour/byEntity/{typeName}` - Get tour by entity type name
- **CSS Selector Resolution**:
  - `CSSSelector` type ? Direct CSS selector string
  - `Property` type ? `[data-property-path='{PropertyPath}']`
  - `ToolbarContent` type ? `[data-toolbar-content='{TypeName};{Id}']`

### 3. Updated TourClient.tsx
- **Location**: `Framework/Extensions/Signum.Tour/TourClient.tsx`
- **Changes**: Added API namespace with methods to call the new controller endpoints
- **Added TypeScript interfaces**: `TourDTO` and `TourStepDTO`

### 4. Updated TourComponent.tsx
- **Location**: `Framework/Extensions/Signum.Tour/TourComponent.tsx`
- **Changes**: 
  - Now supports both `TourEntity` and `TourDTO`
  - Added `useDTO` prop (default: true) to control whether to use DTO API or entity API
  - Updated logic to handle both DTO and Entity formats
  - DTO approach doesn't require read permissions on TourEntity

### 5. Updated Toolbar Rendering
- **Location**: `Framework/Extensions/Signum.Toolbar/Renderers/ToolbarRenderer.tsx`
- **Changes**: Added `data-toolbar-content` attribute to toolbar navigation items
- **Format**: `data-toolbar-content="{TypeName};{Id}"` (e.g., `"UserQuery;02d57d02-8a19-4fe4-b740-e980da256122"`)
- **Also updated**: `Framework/Extensions/Signum.Toolbar/ToolbarConfig.tsx` to pass content info to ToolbarNavItem

## How CSS Selectors Are Resolved

The TourController's `ResolveCssSelector` method combines multiple CssSteps into a single CSS selector string:

1. **CSSSelector Type**: Uses the raw CSS selector
   - Example: `.my-custom-class`

2. **Property Type**: Creates attribute selector for data-property-path
   - Retrieves PropertyRouteEntity to get the path
   - Example: `[data-property-path='Name']`

3. **ToolbarContent Type**: Creates attribute selector for data-toolbar-content
   - Uses entity type name (without "Entity" suffix) and ID
   - Example: `[data-toolbar-content='UserQuery;02d57d02-8a19-4fe4-b740-e980da256122']`

Multiple CSS steps are combined with spaces (descendant combinator).

## Usage

### Using the DTO API (Recommended - No Permissions Required)
```tsx
const tourDTO = useAPI(() => TourClient.API.getTourByEntity("MyEntityType"), []);
return tourDTO && <TourComponent tour={tourDTO} />;
```

### Using the Entity API (Legacy - Requires Permissions)
```tsx
const tourEntity = useAPI(() => 
  Finder.fetchEntities({
    queryName: TourEntity,
    filterOptions: [{ token: TourEntity.token(e => e.forEntity!.entity), value: myEntityType }],
    count: 1,
  }).then(r => r[0] as TourEntity | undefined)
, []);
return tourEntity && <TourComponent tour={tourEntity} />;
```

## Benefits

1. **No Permission Required**: Users can consume tours without needing read permission on TourEntity
2. **Performance**: Lighter payload with pre-resolved CSS selectors
3. **Simplicity**: No need to understand CssSteps structure on the client
4. **Backward Compatible**: Existing code using TourEntity still works
5. **Toolbar Support**: Tours can now target specific toolbar items using data-toolbar-content attribute

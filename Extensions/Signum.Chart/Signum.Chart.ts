//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as DynamicQuery from '../../Signum/React/Signum.DynamicQuery'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Queries from '../Signum.UserAssets/Signum.UserAssets.Queries'

import { FilterOptionParsed, OrderOptionParsed, FilterRequest, OrderRequest } from '@framework/FindOptions'

//Partial
export interface ChartRequestModel {
  queryKey: string;
  
  filterOptions: FilterOptionParsed[];

  filters: FilterRequest[];
}

export interface ChartScriptParameterEmbedded {
  enumValues: { name: string, typeFilter : ChartColumnType }[];
}



export const ChartColumnEmbedded = new Type<ChartColumnEmbedded>("ChartColumnEmbedded");
export interface ChartColumnEmbedded extends Entities.EmbeddedEntity {
  Type: "ChartColumnEmbedded";
  token: Queries.QueryTokenEmbedded | null;
  displayName: string | null;
  format: string | null;
  orderByIndex: number | null;
  orderByType: DynamicQuery.OrderType | null;
}

export module ChartColumnMessage {
  export const SplitLines = new MessageKey("ChartColumnMessage", "SplitLines");
  export const Height = new MessageKey("ChartColumnMessage", "Height");
  export const Height2 = new MessageKey("ChartColumnMessage", "Height2");
  export const Height3 = new MessageKey("ChartColumnMessage", "Height3");
  export const Height4 = new MessageKey("ChartColumnMessage", "Height4");
  export const Height5 = new MessageKey("ChartColumnMessage", "Height5");
  export const Line = new MessageKey("ChartColumnMessage", "Line");
  export const Dimension1 = new MessageKey("ChartColumnMessage", "Dimension1");
  export const Dimension2 = new MessageKey("ChartColumnMessage", "Dimension2");
  export const Dimension3 = new MessageKey("ChartColumnMessage", "Dimension3");
  export const Dimension4 = new MessageKey("ChartColumnMessage", "Dimension4");
  export const Dimension5 = new MessageKey("ChartColumnMessage", "Dimension5");
  export const Dimension6 = new MessageKey("ChartColumnMessage", "Dimension6");
  export const Dimension7 = new MessageKey("ChartColumnMessage", "Dimension7");
  export const Dimension8 = new MessageKey("ChartColumnMessage", "Dimension8");
  export const Angle = new MessageKey("ChartColumnMessage", "Angle");
  export const Sections = new MessageKey("ChartColumnMessage", "Sections");
  export const Areas = new MessageKey("ChartColumnMessage", "Areas");
  export const ColorCategory = new MessageKey("ChartColumnMessage", "ColorCategory");
  export const LocationCode = new MessageKey("ChartColumnMessage", "LocationCode");
  export const Location = new MessageKey("ChartColumnMessage", "Location");
  export const ColorScale = new MessageKey("ChartColumnMessage", "ColorScale");
  export const Opacity = new MessageKey("ChartColumnMessage", "Opacity");
  export const Date = new MessageKey("ChartColumnMessage", "Date");
  export const Latitude = new MessageKey("ChartColumnMessage", "Latitude");
  export const Longitude = new MessageKey("ChartColumnMessage", "Longitude");
  export const Weight = new MessageKey("ChartColumnMessage", "Weight");
  export const Bubble = new MessageKey("ChartColumnMessage", "Bubble");
  export const Size = new MessageKey("ChartColumnMessage", "Size");
  export const Parent = new MessageKey("ChartColumnMessage", "Parent");
  export const Columns = new MessageKey("ChartColumnMessage", "Columns");
  export const Label = new MessageKey("ChartColumnMessage", "Label");
  export const Icon = new MessageKey("ChartColumnMessage", "Icon");
  export const Title = new MessageKey("ChartColumnMessage", "Title");
  export const Info = new MessageKey("ChartColumnMessage", "Info");
  export const SplitBars = new MessageKey("ChartColumnMessage", "SplitBars");
  export const Width = new MessageKey("ChartColumnMessage", "Width");
  export const Width2 = new MessageKey("ChartColumnMessage", "Width2");
  export const Width3 = new MessageKey("ChartColumnMessage", "Width3");
  export const Width4 = new MessageKey("ChartColumnMessage", "Width4");
  export const Width5 = new MessageKey("ChartColumnMessage", "Width5");
  export const SplitColumns = new MessageKey("ChartColumnMessage", "SplitColumns");
  export const HorizontalAxis = new MessageKey("ChartColumnMessage", "HorizontalAxis");
  export const HorizontalAxis2 = new MessageKey("ChartColumnMessage", "HorizontalAxis2");
  export const HorizontalAxis3 = new MessageKey("ChartColumnMessage", "HorizontalAxis3");
  export const HorizontalAxis4 = new MessageKey("ChartColumnMessage", "HorizontalAxis4");
  export const VerticalAxis2 = new MessageKey("ChartColumnMessage", "VerticalAxis2");
  export const VerticalAxis = new MessageKey("ChartColumnMessage", "VerticalAxis");
  export const VerticalAxis3 = new MessageKey("ChartColumnMessage", "VerticalAxis3");
  export const VerticalAxis4 = new MessageKey("ChartColumnMessage", "VerticalAxis4");
  export const Value = new MessageKey("ChartColumnMessage", "Value");
  export const Value2 = new MessageKey("ChartColumnMessage", "Value2");
  export const Value3 = new MessageKey("ChartColumnMessage", "Value3");
  export const Value4 = new MessageKey("ChartColumnMessage", "Value4");
  export const Point = new MessageKey("ChartColumnMessage", "Point");
  export const Bars = new MessageKey("ChartColumnMessage", "Bars");
  export const Color = new MessageKey("ChartColumnMessage", "Color");
  export const InnerSize = new MessageKey("ChartColumnMessage", "InnerSize");
  export const Order = new MessageKey("ChartColumnMessage", "Order");
  export const Square = new MessageKey("ChartColumnMessage", "Square");
}

export const ChartColumnType = new EnumType<ChartColumnType>("ChartColumnType");
export type ChartColumnType =
  "Integer" |
  "Real" |
  "DateOnly" |
  "DateTime" |
  "String" |
  "Lite" |
  "Enum" |
  "RealGroupable" |
  "Time" |
  "Magnitude" |
  "Groupable" |
  "Positionable" |
  "Any";

export module ChartMessage {
  export const _0CanOnlyBeCreatedFromTheChartWindow = new MessageKey("ChartMessage", "_0CanOnlyBeCreatedFromTheChartWindow");
  export const _0CanOnlyBeCreatedFromTheSearchWindow = new MessageKey("ChartMessage", "_0CanOnlyBeCreatedFromTheSearchWindow");
  export const Chart = new MessageKey("ChartMessage", "Chart");
  export const ChartToken = new MessageKey("ChartMessage", "ChartToken");
  export const ChartSettings = new MessageKey("ChartMessage", "ChartSettings");
  export const Dimension = new MessageKey("ChartMessage", "Dimension");
  export const DrawChart = new MessageKey("ChartMessage", "DrawChart");
  export const Group = new MessageKey("ChartMessage", "Group");
  export const Query0IsNotAllowed = new MessageKey("ChartMessage", "Query0IsNotAllowed");
  export const ToggleInfo = new MessageKey("ChartMessage", "ToggleInfo");
  export const ColorsFor0 = new MessageKey("ChartMessage", "ColorsFor0");
  export const CreatePalette = new MessageKey("ChartMessage", "CreatePalette");
  export const MyCharts = new MessageKey("ChartMessage", "MyCharts");
  export const CreateNew = new MessageKey("ChartMessage", "CreateNew");
  export const Edit = new MessageKey("ChartMessage", "Edit");
  export const ApplyChanges = new MessageKey("ChartMessage", "ApplyChanges");
  export const ViewPalette = new MessageKey("ChartMessage", "ViewPalette");
  export const ChartFor = new MessageKey("ChartMessage", "ChartFor");
  export const ChartOf0 = new MessageKey("ChartMessage", "ChartOf0");
  export const _0IsKeyBut1IsAnAggregate = new MessageKey("ChartMessage", "_0IsKeyBut1IsAnAggregate");
  export const _0ShouldBeAnAggregate = new MessageKey("ChartMessage", "_0ShouldBeAnAggregate");
  export const _0ShouldBeSet = new MessageKey("ChartMessage", "_0ShouldBeSet");
  export const _0ShouldBeNull = new MessageKey("ChartMessage", "_0ShouldBeNull");
  export const _0IsNot1 = new MessageKey("ChartMessage", "_0IsNot1");
  export const _0IsAnAggregateButTheChartIsNotGrouping = new MessageKey("ChartMessage", "_0IsAnAggregateButTheChartIsNotGrouping");
  export const _0IsNotOptional = new MessageKey("ChartMessage", "_0IsNotOptional");
  export const SavePalette = new MessageKey("ChartMessage", "SavePalette");
  export const NewPalette = new MessageKey("ChartMessage", "NewPalette");
  export const Data = new MessageKey("ChartMessage", "Data");
  export const ChooseABasePalette = new MessageKey("ChartMessage", "ChooseABasePalette");
  export const DeletePalette = new MessageKey("ChartMessage", "DeletePalette");
  export const Preview = new MessageKey("ChartMessage", "Preview");
  export const TypeNotFound = new MessageKey("ChartMessage", "TypeNotFound");
  export const Type0NotFoundInTheDatabase = new MessageKey("ChartMessage", "Type0NotFoundInTheDatabase");
  export const Reload = new MessageKey("ChartMessage", "Reload");
  export const Maximize = new MessageKey("ChartMessage", "Maximize");
  export const Minimize = new MessageKey("ChartMessage", "Minimize");
  export const ShowChartSettings = new MessageKey("ChartMessage", "ShowChartSettings");
  export const HideChartSettings = new MessageKey("ChartMessage", "HideChartSettings");
  export const QueryResultReachedMaxRows0 = new MessageKey("ChartMessage", "QueryResultReachedMaxRows0");
}

export const ChartParameterEmbedded = new Type<ChartParameterEmbedded>("ChartParameterEmbedded");
export interface ChartParameterEmbedded extends Entities.EmbeddedEntity {
  Type: "ChartParameterEmbedded";
  name: string;
  value: string | null;
}

export module ChartParameterGroupMessage {
  export const Stroke = new MessageKey("ChartParameterGroupMessage", "Stroke");
  export const Number = new MessageKey("ChartParameterGroupMessage", "Number");
  export const Opacity = new MessageKey("ChartParameterGroupMessage", "Opacity");
  export const ColorScale = new MessageKey("ChartParameterGroupMessage", "ColorScale");
  export const ColorCategory = new MessageKey("ChartParameterGroupMessage", "ColorCategory");
  export const Margin = new MessageKey("ChartParameterGroupMessage", "Margin");
  export const Circle = new MessageKey("ChartParameterGroupMessage", "Circle");
  export const Shape = new MessageKey("ChartParameterGroupMessage", "Shape");
  export const Color = new MessageKey("ChartParameterGroupMessage", "Color");
  export const Arrange = new MessageKey("ChartParameterGroupMessage", "Arrange");
  export const ShowValue = new MessageKey("ChartParameterGroupMessage", "ShowValue");
  export const Url = new MessageKey("ChartParameterGroupMessage", "Url");
  export const Location = new MessageKey("ChartParameterGroupMessage", "Location");
  export const Fill = new MessageKey("ChartParameterGroupMessage", "Fill");
  export const Map = new MessageKey("ChartParameterGroupMessage", "Map");
  export const Label = new MessageKey("ChartParameterGroupMessage", "Label");
  export const ColorGradient = new MessageKey("ChartParameterGroupMessage", "ColorGradient");
  export const Margins = new MessageKey("ChartParameterGroupMessage", "Margins");
  export const Points = new MessageKey("ChartParameterGroupMessage", "Points");
  export const Numbers = new MessageKey("ChartParameterGroupMessage", "Numbers");
  export const Performance = new MessageKey("ChartParameterGroupMessage", "Performance");
  export const Zoom = new MessageKey("ChartParameterGroupMessage", "Zoom");
  export const FillColor = new MessageKey("ChartParameterGroupMessage", "FillColor");
  export const Size = new MessageKey("ChartParameterGroupMessage", "Size");
  export const InnerSize = new MessageKey("ChartParameterGroupMessage", "InnerSize");
  export const ShowPercent = new MessageKey("ChartParameterGroupMessage", "ShowPercent");
  export const Scale = new MessageKey("ChartParameterGroupMessage", "Scale");
}

export module ChartParameterMessage {
  export const CompleteValues = new MessageKey("ChartParameterMessage", "CompleteValues");
  export const Scale = new MessageKey("ChartParameterMessage", "Scale");
  export const Labels = new MessageKey("ChartParameterMessage", "Labels");
  export const LabelsMargin = new MessageKey("ChartParameterMessage", "LabelsMargin");
  export const NumberOpacity = new MessageKey("ChartParameterMessage", "NumberOpacity");
  export const NumberColor = new MessageKey("ChartParameterMessage", "NumberColor");
  export const ColorCategory = new MessageKey("ChartParameterMessage", "ColorCategory");
  export const HorizontalScale = new MessageKey("ChartParameterMessage", "HorizontalScale");
  export const VerticalScale = new MessageKey("ChartParameterMessage", "VerticalScale");
  export const UnitMargin = new MessageKey("ChartParameterMessage", "UnitMargin");
  export const NumberMinWidth = new MessageKey("ChartParameterMessage", "NumberMinWidth");
  export const CircleStroke = new MessageKey("ChartParameterMessage", "CircleStroke");
  export const CircleRadius = new MessageKey("ChartParameterMessage", "CircleRadius");
  export const CircleAutoReduce = new MessageKey("ChartParameterMessage", "CircleAutoReduce");
  export const CircleRadiusHover = new MessageKey("ChartParameterMessage", "CircleRadiusHover");
  export const Color = new MessageKey("ChartParameterMessage", "Color");
  export const Interpolate = new MessageKey("ChartParameterMessage", "Interpolate");
  export const MapType = new MessageKey("ChartParameterMessage", "MapType");
  export const MapStyle = new MessageKey("ChartParameterMessage", "MapStyle");
  export const AnimateDrop = new MessageKey("ChartParameterMessage", "AnimateDrop");
  export const AnimateOnClick = new MessageKey("ChartParameterMessage", "AnimateOnClick");
  export const InfoLinkPosition = new MessageKey("ChartParameterMessage", "InfoLinkPosition");
  export const ClusterMap = new MessageKey("ChartParameterMessage", "ClusterMap");
  export const ColorScale = new MessageKey("ChartParameterMessage", "ColorScale");
  export const ColorInterpolation = new MessageKey("ChartParameterMessage", "ColorInterpolation");
  export const LabelMargin = new MessageKey("ChartParameterMessage", "LabelMargin");
  export const NumberSizeLimit = new MessageKey("ChartParameterMessage", "NumberSizeLimit");
  export const FillOpacity = new MessageKey("ChartParameterMessage", "FillOpacity");
  export const ColorInterpolate = new MessageKey("ChartParameterMessage", "ColorInterpolate");
  export const StrokeColor = new MessageKey("ChartParameterMessage", "StrokeColor");
  export const StrokeWidth = new MessageKey("ChartParameterMessage", "StrokeWidth");
  export const Scale1 = new MessageKey("ChartParameterMessage", "Scale1");
  export const Scale2 = new MessageKey("ChartParameterMessage", "Scale2");
  export const Scale3 = new MessageKey("ChartParameterMessage", "Scale3");
  export const Scale4 = new MessageKey("ChartParameterMessage", "Scale4");
  export const Scale5 = new MessageKey("ChartParameterMessage", "Scale5");
  export const Scale6 = new MessageKey("ChartParameterMessage", "Scale6");
  export const Scale7 = new MessageKey("ChartParameterMessage", "Scale7");
  export const Scale8 = new MessageKey("ChartParameterMessage", "Scale8");
  export const InnerRadious = new MessageKey("ChartParameterMessage", "InnerRadious");
  export const Sort = new MessageKey("ChartParameterMessage", "Sort");
  export const ValueAsNumberOrPercent = new MessageKey("ChartParameterMessage", "ValueAsNumberOrPercent");
  export const SvgUrl = new MessageKey("ChartParameterMessage", "SvgUrl");
  export const LocationSelector = new MessageKey("ChartParameterMessage", "LocationSelector");
  export const LocationAttribute = new MessageKey("ChartParameterMessage", "LocationAttribute");
  export const LocationMatch = new MessageKey("ChartParameterMessage", "LocationMatch");
  export const ColorScaleMaxValue = new MessageKey("ChartParameterMessage", "ColorScaleMaxValue");
  export const NoDataColor = new MessageKey("ChartParameterMessage", "NoDataColor");
  export const StartDate = new MessageKey("ChartParameterMessage", "StartDate");
  export const Opacity = new MessageKey("ChartParameterMessage", "Opacity");
  export const RadiousPx = new MessageKey("ChartParameterMessage", "RadiousPx");
  export const SizeScale = new MessageKey("ChartParameterMessage", "SizeScale");
  export const TopMargin = new MessageKey("ChartParameterMessage", "TopMargin");
  export const RightMargin = new MessageKey("ChartParameterMessage", "RightMargin");
  export const ShowLabel = new MessageKey("ChartParameterMessage", "ShowLabel");
  export const LabelColor = new MessageKey("ChartParameterMessage", "LabelColor");
  export const ForceColor = new MessageKey("ChartParameterMessage", "ForceColor");
  export const SubTotal = new MessageKey("ChartParameterMessage", "SubTotal");
  export const Placeholder = new MessageKey("ChartParameterMessage", "Placeholder");
  export const MultiValueFormat = new MessageKey("ChartParameterMessage", "MultiValueFormat");
  export const Complete = new MessageKey("ChartParameterMessage", "Complete");
  export const Order = new MessageKey("ChartParameterMessage", "Order");
  export const Gradient = new MessageKey("ChartParameterMessage", "Gradient");
  export const CSSStyle = new MessageKey("ChartParameterMessage", "CSSStyle");
  export const CSSStyleDiv = new MessageKey("ChartParameterMessage", "CSSStyleDiv");
  export const MaxTextLength = new MessageKey("ChartParameterMessage", "MaxTextLength");
  export const ShowCreateButton = new MessageKey("ChartParameterMessage", "ShowCreateButton");
  export const ShowAggregateValues = new MessageKey("ChartParameterMessage", "ShowAggregateValues");
  export const PointSize = new MessageKey("ChartParameterMessage", "PointSize");
  export const DrawingMode = new MessageKey("ChartParameterMessage", "DrawingMode");
  export const MinZoom = new MessageKey("ChartParameterMessage", "MinZoom");
  export const MaxZoom = new MessageKey("ChartParameterMessage", "MaxZoom");
  export const CompleteHorizontalValues = new MessageKey("ChartParameterMessage", "CompleteHorizontalValues");
  export const CompleteVerticalValues = new MessageKey("ChartParameterMessage", "CompleteVerticalValues");
  export const Shape = new MessageKey("ChartParameterMessage", "Shape");
  export const XMargin = new MessageKey("ChartParameterMessage", "XMargin");
  export const HorizontalLineColor = new MessageKey("ChartParameterMessage", "HorizontalLineColor");
  export const VerticalLineColor = new MessageKey("ChartParameterMessage", "VerticalLineColor");
  export const XSort = new MessageKey("ChartParameterMessage", "XSort");
  export const YSort = new MessageKey("ChartParameterMessage", "YSort");
  export const FillColor = new MessageKey("ChartParameterMessage", "FillColor");
  export const OpacityScale = new MessageKey("ChartParameterMessage", "OpacityScale");
  export const InnerSizeType = new MessageKey("ChartParameterMessage", "InnerSizeType");
  export const InnerFillColor = new MessageKey("ChartParameterMessage", "InnerFillColor");
  export const Stack = new MessageKey("ChartParameterMessage", "Stack");
  export const ValueAsPercent = new MessageKey("ChartParameterMessage", "ValueAsPercent");
  export const HorizontalMargin = new MessageKey("ChartParameterMessage", "HorizontalMargin");
  export const Padding = new MessageKey("ChartParameterMessage", "Padding");
  export const Zoom = new MessageKey("ChartParameterMessage", "Zoom");
}

export const ChartParameterType = new EnumType<ChartParameterType>("ChartParameterType");
export type ChartParameterType =
  "Enum" |
  "Number" |
  "String" |
  "Special";

export module ChartPermission {
  export const ViewCharting : Basics.PermissionSymbol = registerSymbol("Permission", "ChartPermission.ViewCharting");
}

export const ChartRequestModel = new Type<ChartRequestModel>("ChartRequestModel");
export interface ChartRequestModel extends Entities.ModelEntity {
  Type: "ChartRequestModel";
  chartScript: ChartScriptSymbol;
  columns: Entities.MList<ChartColumnEmbedded>;
  parameters: Entities.MList<ChartParameterEmbedded>;
  maxRows: number | null;
}

export const ChartScriptSymbol = new Type<ChartScriptSymbol>("ChartScript");
export interface ChartScriptSymbol extends Basics.Symbol {
  Type: "ChartScript";
}

export module D3ChartScript {
  export const Bars : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.Bars");
  export const Columns : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.Columns");
  export const Line : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.Line");
  export const MultiBars : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.MultiBars");
  export const MultiColumns : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.MultiColumns");
  export const MultiLines : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.MultiLines");
  export const StackedBars : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.StackedBars");
  export const StackedColumns : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.StackedColumns");
  export const StackedLines : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.StackedLines");
  export const Pie : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.Pie");
  export const BubblePack : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.BubblePack");
  export const Scatterplot : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.Scatterplot");
  export const Bubbleplot : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.Bubbleplot");
  export const ParallelCoordinates : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.ParallelCoordinates");
  export const Punchcard : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.Punchcard");
  export const CalendarStream : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.CalendarStream");
  export const Treemap : ChartScriptSymbol = registerSymbol("ChartScript", "D3ChartScript.Treemap");
}

export module GoogleMapsChartScript {
  export const Heatmap : ChartScriptSymbol = registerSymbol("ChartScript", "GoogleMapsChartScript.Heatmap");
  export const Markermap : ChartScriptSymbol = registerSymbol("ChartScript", "GoogleMapsChartScript.Markermap");
}

export module HtmlChartScript {
  export const PivotTable : ChartScriptSymbol = registerSymbol("ChartScript", "HtmlChartScript.PivotTable");
}

export const SpecialParameterType = new EnumType<SpecialParameterType>("SpecialParameterType");
export type SpecialParameterType =
  "ColorCategory" |
  "ColorInterpolate";

export module SvgMapsChartScript {
  export const SvgMap : ChartScriptSymbol = registerSymbol("ChartScript", "SvgMapsChartScript.SvgMap");
}



using Signum.Logic.Chart;
using Signum.Entities.Chart;
using Signum.Engine.Chart;
using System.Collections.Generic;

namespace Signum.Logic.Chart.Scripts 
{
    public class LineChartScript : ChartScript                
    {
        public LineChartScript() : base(D3ChartScript.Line)
        {
            this.Icon = ChartScriptLogic.LoadIcon("lines.png");
            this.GroupBy = GroupByChart.Always;
            this.Columns = new List<ChartScriptColumn>
            {
                new ChartScriptColumn("Horizontal Axis", ChartColumnType.Groupable) { IsGroupKey = true },
                new ChartScriptColumn("Height", ChartColumnType.Positionable) 
            };
            this.Parameters = new List<ChartScriptParameter>
            {
                new ChartScriptParameter("UnitMargin", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 20m } },
                new ChartScriptParameter("Scale", ChartParameterType.Enum) { ColumnIndex = 1,  ValueDefinition = EnumValueList.Parse("ZeroMax (M)|MinMax|Log (M)") },
                new ChartScriptParameter("Color", ChartParameterType.String) {  ValueDefinition = new StringValue("#fff") },
                new ChartScriptParameter("Interpolate", ChartParameterType.Enum) {  ValueDefinition = EnumValueList.Parse("linear|step-before|step-after|cardinal|monotone|basis|bundle|catmull-rom") },
                new ChartScriptParameter("NumberOpacity", ChartParameterType.Number) {  ValueDefinition = new NumberInterval { DefaultValue = 0.8m } }
            };
        }      
    }                
}

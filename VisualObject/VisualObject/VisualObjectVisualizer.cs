using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.VisualStudio.DebuggerVisualizers;
using System.Drawing;
using System.Collections;
using System.Runtime.Serialization;

[assembly: System.Diagnostics.DebuggerVisualizer(
typeof(VisualObject.VisualObjectVisualizer), typeof(VisualizerObjectSource),
 Target = typeof(ArrayList), Description = "VisualObject")]

namespace VisualObject
{
    // TODO: Add the following to SomeType's defintion to see this visualizer when debugging instances of SomeType:
    // 
    //  [DebuggerVisualizer(typeof(ImageDebugVisualizer))]
    //  [Serializable]
    //  public class SomeType
    //  {
    //   ...
    //  }
    // 
    /// <summary>
    /// A Visualizer for SomeType.  
    /// </summary>
    public class VisualObjectVisualizer : DialogDebuggerVisualizer
    {
        protected override void Show(IDialogVisualizerService windowService, IVisualizerObjectProvider objectProvider)
        {
            
            object objeto = objectProvider.GetObject();
            if (objeto != null)
            {
                using ( VisualObjectFrm displayform = new VisualObjectFrm())
                {
                    displayform.CurrentObject = objeto;
                    displayform.ShowDialog();
                }
            }
            

        }

        // TODO: Add the following to your testing code to test the visualizer:
        // 
        //    ImageDebugVisualizer.TestShowVisualizer(new SomeType());
        // 
        /// <summary>
        /// Tests the visualizer by hosting it outside of the debugger.
        /// </summary>
        /// <param name="objectToVisualize">The object to display in the visualizer.</param>
        public static void TestShowVisualizer(object objectToVisualize)
        {
            VisualizerDevelopmentHost visualizerHost = new VisualizerDevelopmentHost(objectToVisualize, typeof(VisualObjectVisualizer));
            visualizerHost.ShowVisualizer();
        }
    }
}
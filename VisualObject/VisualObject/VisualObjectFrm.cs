using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using VisualObject;

namespace VisualObject
{
    public partial class VisualObjectFrm : Form
    {

        private Estado estado = Estado.Stopped;

        object currentObject;

        public object CurrentObject
        {
            get { return currentObject; }
            set { currentObject = value; }
        }         
                
        public VisualObjectFrm()
        {
            InitializeComponent();
            tbViento_Scroll(null, null);
            tbGravedad_Scroll(null, null);
            tbMuelles_Scroll(null, null);
            tbDistMuelles_Scroll(null, null);
            tbRepulsion_Scroll(null, null);
            
            //MainLoop.Enabled = false; 
            //MainLoop.Go += new EventHandler(MainLoop_Go);
            Application.EnableVisualStyles();
        }

        public int LimiteDeep
        {
            get
            {
                return cbLimitDeep.Checked ? (int)nUDLimitDeep.Value : int.MaxValue;
            }
        }

        public int LimiteObjetos
        {
            get
            {
                return cbLimitObjts.Checked ? (int)nUDLimitObjts.Value : int.MaxValue;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (estado == Estado.Stopped)
            {
                visualObjectCtrl1.VisualObjectEngine = new VisualObjectEngine(currentObject, LimiteDeep, LimiteObjetos,visualObjectCtrl1.Size);

                //MainLoop.Enabled = true;
                button1.ImageIndex = 1;
                estado = Estado.Play;
                Simulacion();

            }
            else if(estado == Estado.Pause){

                //MainLoop.Enabled = true;
                button1.ImageIndex = 1;
                estado = Estado.Play;
                Simulacion();
            }
            else if(estado == Estado.Play)
            {
                //MainLoop.Enabled = false;
                button1.ImageIndex = 0;
                estado = Estado.Pause;
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {           
            visualObjectCtrl1.VisualObjectEngine = null;
            button1.ImageIndex = 0;
            estado = Estado.Stopped;
        }

      


        private void tbViento_Scroll(object sender, EventArgs e)
        {
            ConjuntoDibujables.KteViento = (float)tbViento.Value;
        }

        private void tbGravedad_Scroll(object sender, EventArgs e)
        {
            ConjuntoDibujables.KteRozamiento = (float)tbGravedad.Value/100.0f;
        }

        private void tbRepulsion_Scroll(object sender, EventArgs e)
        {
            ConjuntoDibujables.KteElectromagnetica = (float)tbRepulsion.Value;
        }

        private void tbMuelles_Scroll(object sender, EventArgs e)
        {
            ConjuntoDibujables.KteMuelles = (float)tbMuelles.Value/10.0f;
        }

        private void tbDistMuelles_Scroll(object sender, EventArgs e)
        {
            ConjuntoDibujables.KteDistMuelles = (float)tbDistMuelles.Value / 10.0f;
        }


        private void Simulacion()
        {
            try
            {
                while (this.estado == Estado.Play)
                {
                    visualObjectCtrl1.MoverTodas();
                    visualObjectCtrl1.Invalidate();
                    Application.DoEvents();
                }
            }
            catch (Exception e)
            {
                int a = 2; 
            }
        }

        private void VisualObjectFrm_FormClosing(object sender, FormClosingEventArgs e)
        {
            button2_Click(null, null);
        }




      
    }

    public enum Estado {Play, Pause, Stopped}; 
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Sharp3D.Math.Core;

namespace VisualObject
{
    public partial class VisualObjectCtrl : UserControl
    {
        private class Arrastrable
        {
            public TipoRefDibujable trd;
            public Vector2F dx;
            public Vector2F firtsPosition;
            public bool procesarMovimientoDespues; 
            public Arrastrable(TipoRefDibujable trd, Vector2F dx, Vector2F firtsPosition,bool procesarMovimientoDespues)
            {
                this.trd = trd;
                this.dx = dx;
                this.firtsPosition = firtsPosition;
                this.procesarMovimientoDespues = procesarMovimientoDespues;
            }
        }

        Arrastrable arr = null; 
        VisualObjectEngine ve;

        public VisualObjectEngine VisualObjectEngine
        {
            get { return ve; }
            set
            {
                ve = value;
                this.Invalidate();
            }
        }
 
        public VisualObjectCtrl()
        {
            InitializeComponent();
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics gr = e.Graphics;
            gr.Clear(this.BackColor);
            if (ve != null)
            {
                ve.Dibujar(gr, this.Size);
            }
        }
       
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (arr == null)
            {
                if (ve != null)
                    if (ve.TipoEn(e.X, e.Y) != null)
                        Cursor = Cursors.Hand;
                    else
                        Cursor = Cursors.Default;
            }
            else
            {
                arr.trd.Posicion = new Vector2F(e.X - arr.dx.X, e.Y - arr.dx.Y);
                this.Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (ve != null)
            {
                TipoRefDibujable trd = ve.TipoEn(e.X, e.Y);
                if (trd != null)
                {
                    if (e.Button == MouseButtons.Left)
                    {

                        Vector2F loc = trd.Posicion;
                        arr = new Arrastrable(trd, new Vector2F(e.X - loc.X, e.Y - loc.Y), loc, trd.ProcesarMovimiento);
                        trd.ProcesarMovimiento = false;
                        Cursor = Cursors.Hand;

                    }
                    else if(e.Button == MouseButtons.Right)
                    {
                        
                          trd.ProcesarMovimiento = !trd.ProcesarMovimiento;  
                    }
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (ve != null)
            {
                if (arr != null)
                {
                    arr.trd.Posicion = new Vector2F(e.X - arr.dx.X, e.Y - arr.dx.Y);
                    arr.trd.ProcesarMovimiento = arr.procesarMovimientoDespues; 
                    
                    arr = null; 
                    this.Invalidate();
                }
            }
        }
        
        protected override void OnMouseLeave(EventArgs e)
        {
            if (arr != null)
            {
                arr.trd.Posicion = arr.firtsPosition;
                arr.trd.ProcesarMovimiento = arr.procesarMovimientoDespues; 
                arr = null;
                this.Invalidate();
                Cursor = Cursors.Default;

            }
        }

        public void MoverTodas()
        {
            ve.MoverTodos();
        }
    }
}

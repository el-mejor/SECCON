using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SEC2ON.LBSecconBusinessLogic.Dialogs.Controlls
{
    public partial class dayscale : UserControl
    {
        public double Maximum { get; set; }

        public List<DateTime> Marker = new List<DateTime>();
        public enum ScaleOrientation { Horizontal, Vertical }
        public ScaleOrientation Orientation { get; set; }
        public override Color ForeColor { get; set; }
        public override String Text { get; set; }
        public String Unit { get; set; }
        public Color MarkerColor { get; set; }
        public int border { get; set; }
        public int rectangles { get; set; }
        public int Spacing { get; set; }
        public double PercentRedZone { get; set; }

        int count = 0;

        Graphics m_Scale = null;

        public dayscale()
        {
            InitializeComponent();

            ResizeRedraw = true;

            //default values
            border = 0;
            rectangles = 50;
            Maximum = 360;
            ForeColor = Color.Black;
            MarkerColor = Color.Blue;
            Spacing = 8;
            PercentRedZone = 0.2;

            m_Scale = ScaleBox.CreateGraphics();
        }

        //Highlight listviewitem depending on its passwords value
        public static void highlightpasswordage(ListViewItem listviewitem, double value, double Maximum)
        {
            listviewitem.BackColor = GetColorFromvalue(value, Maximum);            
        }

        //returns a color value depending on the value and the maximum time span
        public static Color GetColorFromvalue(double value, double maximum)
        {
            if (value <= 0.0) value = 1.0;

            Color ColorFromvalue = new Color();

            if (value > 0 && value < maximum / 3) //green
            {
                ColorFromvalue = Color.FromArgb(128, 255, 128);
            }
            if (value >= maximum / 3 && value < (maximum / 3) * 2) //transform to yellow
            {
                ColorFromvalue = Color.FromArgb(128 + Convert.ToInt16((value - maximum / 3) * (360 / maximum)), 255, 128);
            }
            if (value >= (maximum / 3) * 2 && value <= maximum) //transform to orange and red
            {
                ColorFromvalue = Color.FromArgb(255, 255 - Convert.ToInt16((value - (maximum / 3) * 2) * (360 / maximum)), 128);
            }
            if (value > maximum) //very old password
            {
                ColorFromvalue = Color.FromArgb(255, 80, 80);                
            }
            return ColorFromvalue;
        }

        //returns the y coordinate for a given value
        private int GetYFromvalue(double value)
        {
            int scalelength = this.Height;
            int scalewidth = this.Width;

            if (Orientation == ScaleOrientation.Horizontal)
            {                
                scalelength = this.Width;
                scalewidth = this.Height;
            }
            else
            {

            }

            return border + Convert.ToInt16(((Convert.ToDouble(scalelength * (1.0 - PercentRedZone)) - 2 * border) / Maximum) * value);
        }

        //Redraw
        private void ScaleBox_Paint(object sender, PaintEventArgs e)
        {
            if (Maximum == 0) Maximum = 365;

            int scalelength = this.Height;
            int scalewidth = this.Width;

            if (Orientation == ScaleOrientation.Horizontal)
            {
                e.Graphics.TranslateTransform(0, this.Height - border);
                e.Graphics.RotateTransform(-90);
                scalelength = this.Width;
                scalewidth = this.Height;
            }
            else
            {

            }

            //draw                                   
            e.Graphics.Clear(BackColor);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;            
            //draw scale
            for (int RectangleCount = 0; RectangleCount <= rectangles; RectangleCount++)
            {
                double value = ((RectangleCount) / (rectangles - rectangles * PercentRedZone) * Maximum);
                using (SolidBrush brush = new SolidBrush(GetColorFromvalue(((RectangleCount + 1) / (rectangles - rectangles * PercentRedZone) * Maximum), Maximum)))
                {
                    e.Graphics.FillRectangle(brush, 
                        border, 
                        GetYFromvalue(value),
                        scalewidth / 4,
                        Convert.ToInt16((Convert.ToDouble(scalelength * (1.0 - PercentRedZone)) - (2 * border)) / rectangles) + 5);
                }
            }
            
            //draw marker
            foreach (DateTime valuedatetime in Marker)
            {
                double value = Convert.ToDouble((DateTime.Now - valuedatetime).Days);
                using (SolidBrush brush = new SolidBrush(MarkerColor))
                {
                    if (value <= 0) value = 1.0;
                    System.Drawing.Drawing2D.GraphicsPath triangle = new System.Drawing.Drawing2D.GraphicsPath();
                    int x = scalewidth / 4;
                    int y = GetYFromvalue(value);
                    triangle.AddLine(x, y, x + 5, y - 5);
                    triangle.AddLine(x + 5, y - 5, x + 5, y + 5);
                    triangle.AddLine(x + 5, y + 5, x, y);
                    e.Graphics.FillPath(brush, triangle);
                }
            }

            //draw scales text
            StringFormat UpperFormat = new StringFormat();
            UpperFormat.Alignment = StringAlignment.Center;

            StringFormat LowerFormat = new StringFormat();
            LowerFormat.Alignment = StringAlignment.Center;
            LowerFormat.LineAlignment = StringAlignment.Far;


            e.Graphics.DrawString(Text,
                new Font("Arial Narrow", 8, FontStyle.Bold),
                new SolidBrush(ForeColor),
                scalewidth / 2,
                border - 1,
                UpperFormat);

            e.Graphics.DrawString(Unit,
                new Font("Arial Narrow", 8, FontStyle.Bold),
                new SolidBrush(ForeColor),
                scalewidth / 2,
                scalelength - border - 4,
                LowerFormat);

            double[] valuescale = new double[Spacing];
            
            for (int spacing = 0; spacing < Spacing; spacing++ )
            {
                valuescale[spacing] = Maximum * (1 / Convert.ToDouble(Spacing)) + spacing * Maximum * (1 / Convert.ToDouble(Spacing));
            }

            foreach (double value in valuescale)
            {
                e.Graphics.DrawString(Convert.ToInt16(value).ToString(),
                    new Font("Arial Narrow", 8),
                    new SolidBrush(ForeColor),
                    scalewidth / 4 + border + 8,
                    GetYFromvalue(value) - 8);

                e.Graphics.DrawLine(new Pen(new SolidBrush(ForeColor), 1.0f),
                    border,
                    GetYFromvalue(value),
                    scalewidth / 4 + border + 6,
                    GetYFromvalue(value));                
            }

            

            count++;            
        }

        


        
    }
}

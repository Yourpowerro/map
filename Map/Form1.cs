using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.IO;

namespace Map
{
    public partial class Form1 : Form
    {
        double power = 5.0;
        double cont = 1.0;
        double brig = 0.0;

        Bitmap bmp;
        Pen pen = new Pen(Color.FromArgb(250, 250, 250));
        SolidBrush brush = new SolidBrush(Color.FromArgb(200, 200, 200));
        Font font = new Font("Calibri", 15.0f);

        int width;
        int height;

        double elev_min = +100000.0;
        double elev_max = -100000.0;

        struct ele_data
        {
            public double elev;

            public double lat;
            public double lon;
        };

        ele_data[] samp;
        int samps;
            

        GraphicsPath path = new GraphicsPath();

        double offset_x;
        double offset_y;

        double scale;

        double half_w;
        double half_h;

        struct obj_data
        {
            public string file;
            public int type;
            public string name;
            public double thick;
            public int pts;
            public double[] lat;
            public double[] lon;
            public double cx;
            public double cy;
        };

        struct mark_data
        {
            public double lat;
            public double lon;
            public string name;
        };

        int marks;
        mark_data[] mark;

        int objs;
        obj_data[] obj;

        int mouse_x;
        int mouse_y;

        int lock_i = -1;
        int lock_j = -1;

        public Form1()
        {

       /*     half_w = (double)pictureBox1.Width * 0.5f;
            half_h = (double)pictureBox1.Height * 0.5f;

            scale = Math.Min(half_h, half_w) * 100.0f; */
            InitializeComponent();
        }

        void loadObject(int idx, string objfile)
        {
            obj[idx].file = objfile;
            StreamReader reader = new StreamReader(objfile);

            string s_type = reader.ReadLine();
            switch (s_type)
            {
                case "building":
                    obj[idx].type = 0;
                    break;
                case "water":
                    obj[idx].type = 1;
                    break;
                case "grass":
                    obj[idx].type = 2;
                    break;
                case "road":
                    obj[idx].type = 3;
                    break;
            }
            obj[idx].name = reader.ReadLine();
            obj[idx].thick = double.Parse(reader.ReadLine());

            string s_pts = reader.ReadLine();
            int pts = int.Parse(s_pts);

            obj[idx].pts = pts;
            obj[idx].lat = new double[pts];
            obj[idx].lon = new double[pts];

            double cx = 0.0;
            double cy = 0.0;

            for (int i = 0; i != pts; i++)
            {
                string s_coords = reader.ReadLine();
                string[] field = s_coords.Split(' ');

                double lat = double.Parse(field[0]);
                double lon = double.Parse(field[0]);

                obj[idx].lat[i] = lat;
                obj[idx].lon[i] = lon;

                offset_x += lon;
                offset_y += lat;

                cx += lon;
                cy += lat;
            }
            obj[idx].cx = cx / (double)pts;
            obj[idx].cy = cy / (double)pts;

            reader.Close();
        }

        void saveObject (int idx, string objfile)
        {
            StreamWriter writer = new StreamWriter(objfile);

            switch (obj[idx].type)
            {
                case 0:
                    writer.WriteLine("building");
                    break;
                case 1:
                    writer.WriteLine("water");
                    break;
                case 2:
                    writer.WriteLine("grass");
                    break;
                case 3:
                    writer.WriteLine("road");
                    break;
            }

            writer.WriteLine(obj[idx].name);
            writer.WriteLine(obj[idx].thick.ToString("0.000000000000"));

            writer.WriteLine(obj[idx].pts.ToString());

            for (int i = 0; i < obj[idx].pts; i++)
                writer.WriteLine(obj[idx].lat[i].ToString("0.000000000000") + " " + obj[idx].lon[i].ToString("0.000000000000"));

            writer.Close();
        }

        double unit(double x, double x0, double x1)
        {
            return (x - x0) / (x1 - x0);
        }

        double lerp (double a, double b, double t)
        {
            return a + (b - a) * t;
        }

        double map (double x, double x0, double x1, double a, double b)
        {
            double t = unit(x, x0, x1);
            return lerp(a, b, t);
        }

        void loadElevationSamples(string filename)
        {
            StreamReader reader = new StreamReader(filename);

            string s_samps = reader.ReadLine();
            samps = int.Parse(s_samps);
            samp = new ele_data[samps];

            for(int i = 0; i < samps; i++)
            {
                string s_data = reader.ReadLine();
                string[] field = s_data.Split(' ');
                double elev = double.Parse(field[0]);

                samp[i].elev = elev;
                samp[i].lat = double.Parse(field[1]);
                samp[i].lon = double.Parse(field[2]);

                elev_min = Math.Min(elev_min, elev);
                elev_max = Math.Max(elev_max, elev);
            }

            reader.Close();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }



        void drawLine(int idx, int p0, int p1, GraphicsPath path)
        {
            float x0 = lon2x(obj[idx].lon[p0]);
            float y0 = lat2y(obj[idx].lat[p0]);

            float x1 = lon2x(obj[idx].lon[p1]);
            float y1 = lat2y(obj[idx].lat[p1]);

            path.AddLine(x0, y0, x1, y1);
        }

        double lon2x(double lon)
        {
            return half_w + (lon + offset_x) * scale;
        }

        double lat2y(double lat)
        {
            return half_h - (lat + offset_y) * scale;
        }

       
  
    }
}

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
        double power = 1.0;
        double cont = 1.5;
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

            if (s_type == "building") reader.ReadLine();

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
                double lon = double.Parse(field[1]);
                
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

        void to_color(double t, out double r, out double g, out double b)
        {
            if (t < 0.5)
            {
                r = map(t, 0.0, 0.5, 0.0, 0.0);
                g = map(t, 0.0, 0.5, 0.0, 0.5);
                b = map(t, 0.0, 0.5, 1.0, 1.0);
            }
            else
            {
                r = map(t, 0.5, 1.0, 0.0, 1.0);
                g = map(t, 0.5, 1.0, 0.5, 1.0);
                b = map(t, 0.5, 1.0, 1.0, 1.0);
            }
            r = (r - 0.5) * cont + 0.5 + brig;
            g = (g - 0.5) * cont + 0.5 + brig;
            b = (b - 0.5) * cont + 0.5 + brig;
        }

        void saveAllObjects()
        {
            for (int i = 0; i < objs; i++)
                saveObject(i, obj[i].file);
        }

        double y2lat(double y)
        {
            return (half_h - y) / scale - offset_y;
        }

        double x2lon(double x)
        {
            return (x - half_w) / scale - offset_x;
        }

        void loadMap(string mapfile)
        {
            StreamReader reader = new StreamReader(mapfile);
            
            string s_objs = reader.ReadLine();
            objs = int.Parse(s_objs);
            obj = new obj_data[objs];
            
            int pts = 0;
            
            for (int i = 0; i < objs; i++)
            {                
                string objfile = reader.ReadLine();              
                loadObject(i, "../../" + objfile);
                
                pts += obj[i].pts;
            }
            
            offset_x = -offset_x / (double)pts;
            offset_y = -offset_y / (double)pts;

            offset_x = -obj[0].cx;
            offset_y = -obj[0].cy;

            reader.Close();
        }

        void loadMarkers(string markerfile)
        {
            StreamReader reader = new StreamReader(markerfile);

            string s_marks = reader.ReadLine();
            marks = int.Parse(s_marks);

            mark = new mark_data[marks];
            
            for (int i = 0; i < marks; i++)
            {
                string s_info = reader.ReadLine();
                string[] field = s_info.Split(' ');

                mark[i].lat = double.Parse(field[0]);           
                mark[i].lon = double.Parse(field[1]);
                mark[i].name = field[2];
            }

            reader.Close();
        }

        private void Form1_Load(object sender, EventArgs e)
        {           
            loadMap("../../map.txt");           
            loadMarkers("../../markers.txt");
            loadElevationSamples("../../height.txt");
            
            width = pictureBox1.Width;
            height = pictureBox1.Height;

            bmp = new Bitmap(width, height);

            half_w = (double)width * 0.5;
            half_h = (double)height * 0.5;

            scale = Math.Min(half_w, half_h) * 100;

            MouseWheel += new MouseEventHandler(Form1_MouseWheel);
        }

        void Form1_MouseWheel(object sender, MouseEventArgs e)
        {
            double new_scale = scale + e.Delta * 0.001 * scale;
            scale = Math.Min(Math.Max(new_scale, 0.001), 3000000);

            pictureBox1.Invalidate();
        }

        double lon2x(double lon)
        {
            return half_w + (lon + offset_x) * scale;
        }

        double lat2y(double lat)
        {
            return half_h - (lat + offset_y) * scale;
        }

        double idw (double lat, double lon)
        {
            double stop = 0;
            double sbot = 0;

            for(int i = 0; i < samps; i++)
            {
                double dlat = lat - samp[i].lat;
                double dlon = lon - samp[i].lon;

                double dist = Math.Sqrt(dlat * dlat + dlon * dlon);
                if (dist < 0.0000000000001) return samp[i].elev;

                double w = 1 / Math.Pow(dist, power);

                stop += w * samp[i].elev;
                sbot += w;
            }
            return stop / sbot;
        }

        void estimateElevation()
        {
            for (int y = 0; y < height; y++)
                for(int x = 0; x < width; x++)
                {
                    double lat = y2lat(y);
                    double lon = x2lon(x);

                    double elev = idw(lat, lon);
                    double t = unit(elev, elev_min, elev_max);

                    double rf, gf, bf;
                    to_color(t, out rf, out gf, out bf);

                    int r = (int)(rf * 255);
                    int g = (int)(gf * 255);
                    int b = (int)(bf * 255);

                    r = Math.Min(Math.Max(r, 0), 255);
                    g = Math.Min(Math.Max(g, 0), 255);
                    b = Math.Min(Math.Max(b, 0), 255);
                    
                    bmp.SetPixel(x, y, Color.FromArgb(r, g, b));
                }
        }

        void drawLine(int idx, int p0, int p1, GraphicsPath path)
        {
            float x0 = (float) lon2x(obj[idx].lon[p0]);
            float y0 = (float) lat2y(obj[idx].lat[p0]);

            float x1 = (float) lon2x(obj[idx].lon[p1]);
            float y1 = (float) lat2y(obj[idx].lat[p1]);

            path.AddLine(x0, y0, x1, y1);
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Color.FromArgb(50, 50, 50));
            
            estimateElevation();
            
            e.Graphics.DrawImage(bmp, 0, 0);

            for (int i = 0; i < objs; i++)
            {
                int pts = obj[i].pts;
                
                for (int j = 0; j < pts - 1; j++)
                    drawLine(i, j, j + 1, path);
                
                if (obj[i].type != 3) drawLine(i, pts - 1, 0, path);

                switch (obj[i].type)
                {
                    case 0:
                        brush.Color = Color.FromArgb(200, 250, 250);
                        break;
                    case 1:
                        brush.Color = Color.FromArgb(50, 200, 200);
                        break;
                    case 2:
                        brush.Color = Color.FromArgb(50, 100, 50);
                        break;
                    case 3:
                        brush.Color = Color.FromArgb(0, 100, 150);
                        break;
                }
                pen.Color = brush.Color;
                pen.Width = (float)(obj[i].thick * scale);

                if (obj[i].type != 3) e.Graphics.FillPath(brush, path);
                else e.Graphics.DrawPath(pen, path);

                if(obj[i].type == 0)
                {
                    float tx = (float)lon2x(obj[i].cx);
                    float ty = (float)lat2y(obj[i].cy);

                    SizeF ts = e.Graphics.MeasureString(obj[i].name, font);

                    brush.Color = Color.FromArgb(250, 250, 250);
                    e.Graphics.DrawString(obj[i].name, font, brush, tx - ts.Width * 0.5f, ty - ts.Height * 0.5f);
                }
                
                path.Reset();
            }

            for (int i = 0; i < marks; i++)
            {
                float mx = (float)lon2x(mark[i].lon);
                float my = (float)lat2y(mark[i].lat);

                brush.Color = Color.FromArgb(250, 250, 250);
                e.Graphics.DrawString(mark[i].name, font, brush, mx, my);

                brush.Color = Color.FromArgb(250, 250, 250);
                e.Graphics.FillEllipse(brush, mx - 5.0f, my - 5.0f, 8.0f, 8.0f);
            }

            brush.Color = Color.FromArgb(40, 50, 250, 250);
            pen.Color = Color.FromArgb(100, 50, 250, 250);

            pen.Width = 1;

            for(int i = 0; i< objs; i++)
            {
                int pts = obj[i].pts;

                for(int j = 0; j < pts; j++)
                {
                    float mx = (float)lon2x(obj[i].lon[j]);
                    float my = (float)lat2y(obj[i].lat[j]);
                    e.Graphics.FillEllipse(brush, mx - 5.0f, my - 5.0f, 8.0f, 8.0f);
                    e.Graphics.DrawEllipse(pen, mx - 5.0f, my - 5.0f, 8.0f, 8.0f);
                }
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Left)
            {
                int dx = e.X - mouse_x;
                int dy = mouse_y - e.Y;

                if (lock_i == -1 && lock_j == -1)
                {
                    offset_x += (double)dx / scale;
                    offset_y += (double)dy / scale;
                }
                else
                {
                    obj[lock_i].lon[lock_j] += (double)dx / scale;
                    obj[lock_i].lat[lock_j] += (double)dy / scale;
                }
                pictureBox1.Invalidate();
            }
            mouse_x = e.X;
            mouse_y = e.Y;
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            for(int i = 0; i < objs; i++)
            {
                int pts = obj[i].pts;
                for (int j = 0; j < pts; j++)
                {
                    float mx = (float)lon2x(obj[i].lon[j]);
                    float my = (float)lat2y(obj[i].lat[j]);

                    double dx = mx - e.X;
                    double dy = my - e.Y;
                    if (dx * dx + dy * dy >= 5 * 5) continue;

                    lock_i = i;
                    lock_j = j;
                }
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            lock_i = -1;
            lock_j = -1;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            saveAllObjects();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            power = trackBar1.Value;
            label1.Text = "Power: " + power;
            pictureBox1.Invalidate();
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            cont = (double)trackBar2.Value/1000;
            label2.Text = "Cont: " + cont;
            pictureBox1.Invalidate();
        }

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            brig = (double)trackBar3.Value / 100;
            label3.Text = "Brig: " + brig;
            pictureBox1.Invalidate();
        }
    }
}

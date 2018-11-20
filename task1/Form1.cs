using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace indtask
{
    public partial class Form1 : Form
    {
        private Graphics g;
        private PointF movePoint = PointF.Empty;

        public Form1()
        {
            InitializeComponent();
            pictureBox1.Image = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            g = Graphics.FromImage(pictureBox1.Image);
            g.Clear(Color.White);
            comboBox1.SelectedIndex = 0;
        }

        private LinkedList<PolygonNode> polygon1 = new LinkedList<PolygonNode>();
        private LinkedList<PolygonNode> polygon2 = new LinkedList<PolygonNode>();
        private LinkedList<PolygonNode> intersection = new LinkedList<PolygonNode>();

        public class PolygonNode
        {
            public PointF p;
            public bool isIntersection;
            public float angle;
            public LinkedListNode<PolygonNode> otherNode;

            public PolygonNode(PointF _p, bool _is_intersecton = false, float _angle = 0)
            {
                p = _p;
                isIntersection = _is_intersecton;
                angle = _angle;
            }
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (comboBox1.SelectedIndex == 0 && polygon1.Count == 0)
                    polygon1.AddLast(new PolygonNode(e.Location));
                else if (comboBox1.SelectedIndex == 1 && polygon2.Count == 0)
                    polygon2.AddLast(new PolygonNode(e.Location));
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                movePoint = e.Location;
                pictureBox1.Invalidate();
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (comboBox1.SelectedIndex == 0)
                    polygon1.AddLast(new PolygonNode(movePoint));
                else if (comboBox1.SelectedIndex == 1)
                    polygon2.AddLast(new PolygonNode(movePoint));
            }
            pictureBox1.Invalidate();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            findIntersections();
            var cur_node = polygon1.First;
            for (var node = polygon1.First; node != null; node = node.Next)
            {
                if (isInside(polygon2.Select(el => el.p).ToArray(), node.Value.p))
                {
                    cur_node = node;
                    break;
                }
            }

            PointF start_point = cur_node.Value.p;

            while (true)
            {
                intersection.AddLast(new PolygonNode(cur_node.Value.p));
                var next_node = cur_node.Next ?? cur_node.List.First;
                if (next_node.Value.p == start_point)
                    break;
                if (next_node.Value.isIntersection)
                    cur_node = next_node.Value.otherNode;
                else
                    cur_node = next_node;
            }
            pictureBox1.Invalidate();
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (polygon1.Count > 2)
                e.Graphics.DrawPolygon(new Pen(Color.Red), polygon1.Select(el => el.p).ToArray());
            if (polygon2.Count > 2)
                e.Graphics.DrawPolygon(new Pen(Color.Blue), polygon2.Select(el => el.p).ToArray());
            if (intersection.Count > 2)
                e.Graphics.DrawPolygon(new Pen(Color.Black, 3), intersection.Select(el => el.p).ToArray());
        }

        void getEquationCoeffs(PointF p1, PointF p2, out float a, out float b, out float c)
        {
            a = p1.Y - p2.Y;
            b = p2.X - p1.X;
            c = p1.X * p2.Y - p2.X * p1.Y;
        }

        bool isBetweenCoords(float z, float z1, float z2)
        {
            return Math.Min(z1, z2) - float.Epsilon <= z && z <= Math.Max(z1, z2) + float.Epsilon;
        }

        bool isBeetweenSegmentsEnds(float x, float y, PointF p1, PointF p2, PointF p3, PointF p4)
        {
            return isBetweenCoords(x, p1.X, p2.X) && isBetweenCoords(x, p3.X, p4.X) &&
                   isBetweenCoords(y, p1.Y, p2.Y) && isBetweenCoords(y, p3.Y, p4.Y);
        }

        PointF findIntersection(PointF p1, PointF p2, PointF p3, PointF p4)
        {
            PointF intersection_point = new PointF(-1, -1);
            float a1, b1, c1, a2, b2, c2;
            getEquationCoeffs(p1, p2, out a1, out b1, out c1);
            getEquationCoeffs(p3, p4, out a2, out b2, out c2);
            //параллельны
            if (Math.Abs(a1 * b2 - a2 * b1) < float.Epsilon)
                return intersection_point;
            float x = (c2 * b1 - c1 * b2) / (a1 * b2 - a2 * b1);
            float y = (c2 * a1 - c1 * a2) / (b1 * a2 - b2 * a1);
            if (isBeetweenSegmentsEnds(x, y, p1, p2, p3, p4))
            {
                intersection_point.X = x;
                intersection_point.Y = y;
                return intersection_point;
            }
            else
                return intersection_point;
        }

        bool isInside(PointF[] polygon, PointF p)
        {
            int n = polygon.Length;
            if (n < 3) return false;

            PointF extreme = new PointF(pictureBox1.Width, p.Y);

            int count = 0, i = 0;
            do
            {
                int next = (i + 1) % n;
                PointF intersection = findIntersection(polygon[i], polygon[next], p, extreme);
                if (intersection.X != -1)
                    count++;
                i = next;
            } while (i != 0);

            return count % 2 == 1;
        }

        private void sortPointsClockwise(ref LinkedList<PolygonNode> pointArr, out PointF avgCenter)
        {
            avgCenter = new PointF(pointArr.Average(el => el.p.X), pointArr.Average(el => el.p.Y));

            for (var curNode = pointArr.First; curNode != null; curNode = curNode.Next)
                curNode.Value.angle = (float)Math.Atan2(curNode.Value.p.Y - avgCenter.Y, curNode.Value.p.X - avgCenter.X);

            LinkedList<PolygonNode> tempLinkedList = new LinkedList<PolygonNode>(pointArr);
            pointArr.Clear();
            IEnumerable<PolygonNode> orderedEnumerable = tempLinkedList.OrderByDescending(p => p.angle).AsEnumerable();
            foreach (var oe in orderedEnumerable)
                pointArr.AddLast(oe);
        }

        private float distance(PointF p1, PointF p2)
        {
            return (float)Math.Sqrt((p2.X - p1.X) * (p2.X - p1.X) + (p2.Y - p1.Y) * (p2.Y - p1.Y));
        }

        private void findIntersections()
        {
            PointF pol1center, pol2center;
            sortPointsClockwise(ref polygon1, out pol1center);
            sortPointsClockwise(ref polygon2, out pol2center);

            PolygonNode[] pol1 = new PolygonNode[polygon1.Count + 1];
            PolygonNode[] pol2 = new PolygonNode[polygon2.Count + 1];

            polygon1.CopyTo(pol1, 0);
            polygon2.CopyTo(pol2, 0);

            pol1[polygon1.Count] = pol1.First();
            pol2[polygon2.Count] = pol2.First();

            for (int i = 0; i < pol1.Count() - 1; ++i)
                for (int j = 0; j < pol2.Count() - 1; ++j)
                {
                    PointF intersection_point = findIntersection(pol1[i].p, pol1[i + 1].p, pol2[j].p, pol2[j + 1].p);
                    if (intersection_point.X != -1)
                    {
                        PolygonNode ip1 = new PolygonNode(intersection_point, true);
                        PolygonNode ip2 = new PolygonNode(intersection_point, true);

                        var t1 = polygon1.Find(pol1[i]);
                        while (t1.Next != null && t1.Next.Value.isIntersection && distance(pol1[i + 1].p, t1.Next.Value.p) > distance(pol1[i + 1].p, ip1.p))
                            t1 = t1.Next;
                        var n1 = polygon1.AddAfter(t1, ip1);

                        var t2 = polygon2.Find(pol2[j]);
                        while (t2.Next != null && t2.Next.Value.isIntersection && distance(pol2[j + 1].p, t2.Next.Value.p) > distance(pol2[j + 1].p, ip2.p))
                            t2 = t2.Next;
                        var n2 = polygon2.AddAfter(t2, ip2);

                        n1.Value.otherNode = n2;
                        n2.Value.otherNode = n1;
                    }
                }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            g.Clear(Color.White);
            polygon1.Clear();
            polygon2.Clear();
            intersection.Clear();
            movePoint = Point.Empty;
            pictureBox1.Invalidate();
        }
    }
}

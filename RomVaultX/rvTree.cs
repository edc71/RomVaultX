﻿using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace RomVaultX
{

    public partial class RvTree : UserControl
    {
        private RvTreeRow _lSelected;

        public event MouseEventHandler RvSelected;

        private List<RvTreeRow> _rows;

        public RvTree()
        {
            _rows = new List<RvTreeRow>();
            InitializeComponent();
        }

        #region "Setup"
        
        public void Setup(List<RvTreeRow> rows)
        {
            _rows = rows;

            int yPos = 0;
            int treeCount = _rows.Count;
            for (int i = 0; i < treeCount; i++)
            {
                RvTreeRow pTree = _rows[i];
                int nodeDepth = pTree.dirFullName.Count(x => x == '\\') - 1;
                if (pTree.MultiDatDir) nodeDepth += 1;
                pTree.RTree = new Rectangle(0, yPos - 8, nodeDepth * 18, 16);
                if (pTree.DatId == null)
                    pTree.RExpand = new Rectangle(5 + nodeDepth * 18, yPos + 4, 9, 9);
                pTree.RIcon = new Rectangle(20 + nodeDepth * 18, yPos, 16, 16);
                pTree.RText = new Rectangle(36 + nodeDepth * 18, yPos, 500, 16);
                yPos = yPos + 16;
            }
            AutoScrollMinSize = new Size(500, yPos);


            string lastBranch = "";
            for (int i = treeCount - 1; i >= 0; i--)
            {
                RvTreeRow pTree = _rows[i];
                int nodeDepth = pTree.dirFullName.Count(x => x == '\\');
                if (pTree.MultiDatDir) nodeDepth += 1;

                string thisBranch;
                if (nodeDepth - 1 == 0)
                    thisBranch = "";
                else if (nodeDepth - 1 > lastBranch.Length)
                    thisBranch = lastBranch + new string(' ', nodeDepth - 1 - lastBranch.Length);
                else
                    thisBranch = lastBranch.Substring(0, nodeDepth - 1);

                thisBranch = thisBranch.Replace("└", "│");
                thisBranch += "└";

                pTree.TreeBranches = thisBranch;
                lastBranch = thisBranch;
            }
            Refresh();
        }


        #endregion

        #region "Paint"

        private int _hScroll;
        private int _vScroll;

        protected override void OnPaint(PaintEventArgs e)
        {

            Graphics g = e.Graphics;

            _hScroll = HorizontalScroll.Value;
            _vScroll = VerticalScroll.Value;

            Rectangle t = new Rectangle(e.ClipRectangle.Left + _hScroll, e.ClipRectangle.Top + _vScroll, e.ClipRectangle.Width, e.ClipRectangle.Height);

            g.FillRectangle(Brushes.White, e.ClipRectangle);


            int treeCount = _rows.Count;
            for (int i = 0; i < treeCount; i++)
            {
                RvTreeRow pTree = _rows[i];
                PaintTree(pTree, g, t);
            }



        }

        private void PaintTree(RvTreeRow pTree, Graphics g, Rectangle t)
        {
            int y = pTree.RTree.Top - _vScroll;

            if (pTree.RTree.IntersectsWith(t))
            {
                Pen p = new Pen(Brushes.Gray, 1) { DashStyle = DashStyle.Dot };

                string lTree = pTree.TreeBranches;
                for (int j = 0; j < lTree.Length; j++)
                {
                    int x = j * 18 - _hScroll;
                    string cTree = lTree.Substring(j, 1);
                    switch (cTree)
                    {
                        case "│":
                            g.DrawLine(p, x + 9, y, x + 9, y + 16);
                            break;

                        case "└":
                            g.DrawLine(p, x + 9, y, x + 9, y + 16);
                            g.DrawLine(p, x + 9, y + 16, x + 27, y + 16);
                            break;
                    }
                }
            }

            if (!pTree.RExpand.IsEmpty)
                if (pTree.RExpand.IntersectsWith(t))
                {
                    g.DrawImage(pTree.TreeExpanded ? RvImages.ExpandBoxMinus : RvImages.ExpandBoxPlus, RSub(pTree.RExpand, _hScroll, _vScroll));
                }

            if (pTree.RIcon.IntersectsWith(t))
            {
                int icon = 1;
                /*
                if (pTree.DirStatus.HasInToSort())
                {
                    icon = 4;
                }
                else if (!pTree.DirStatus.HasCorrect())
                {
                    icon = 1;
                }
                else if (!pTree.DirStatus.HasMissing())
                {
                    icon = 3;
                }
                */


                Bitmap bm;
                //if (pTree.Dat == null && pTree.DirDatCount != 1) // Directory above DAT's in Tree
                if (string.IsNullOrEmpty(pTree.datName))
                    bm = RvImages.GetBitmap("DirectoryTree" + icon);
                else
                    bm = RvImages.GetBitmap("Tree" + icon);

                if (bm != null)
                {
                    g.DrawImage(bm, RSub(pTree.RIcon, _hScroll, _vScroll));
                }
            }



            Rectangle recBackGround = new Rectangle(pTree.RText.X, pTree.RText.Y, Width - pTree.RText.X + _hScroll, pTree.RText.Height);

            if (recBackGround.IntersectsWith(t))
            {
                string thistxt = pTree.dirName;
                if (!string.IsNullOrEmpty(pTree.datName))
                    thistxt += ": " + pTree.datName;

                if (_lSelected == pTree)
                {
                    g.FillRectangle(new SolidBrush(Color.FromArgb(51, 153, 255)), RSub(recBackGround, _hScroll, _vScroll));
                    g.DrawString(thistxt, new Font("Microsoft Sans Serif", 8), Brushes.White, pTree.RText.Left - _hScroll, pTree.RText.Top + 1 - _vScroll);
                }
                else
                    g.DrawString(thistxt, new Font("Microsoft Sans Serif", 8), Brushes.Black, pTree.RText.Left - _hScroll, pTree.RText.Top + 1 - _vScroll);


            }
        }


        private static Rectangle RSub(Rectangle r, int h, int v)
        {
            Rectangle ret = new Rectangle(r.Left - h, r.Top - v, r.Width, r.Height);
            return ret;
        }

        #endregion


        #region"Mouse Events"
        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            bool mousehit = false;

            int x = mevent.X + HorizontalScroll.Value;
            int y = mevent.Y + VerticalScroll.Value;

            if (_rows != null)
                for (int i = 0; i < _rows.Count; i++)
                {
                    RvTreeRow tDir = _rows[i];
                    if (CheckMouseDown(tDir, x, y, mevent))
                    {
                        mousehit = true;
                        break;
                    }
                }

            if (mousehit) return;

            base.OnMouseDown(mevent);
        }

        private bool CheckMouseDown(RvTreeRow pTree, int x, int y, MouseEventArgs mevent)
        {
            if (pTree.RExpand.Contains(x, y))
            {
                SetExpanded(pTree, mevent.Button);
                return true;
            }

            if (pTree.RText.Contains(x, y))
            {

                if (RvSelected != null)
                    RvSelected(pTree, mevent);

                _lSelected = pTree;
                Refresh();
                return true;
            }

            return false;
        }



        private static void SetExpanded(RvTreeRow pTree, MouseButtons mouseB)
        {
            if (mouseB == MouseButtons.Left)
            {
                pTree.TreeExpanded = !pTree.TreeExpanded;
            }
        }

        #endregion

    }
}
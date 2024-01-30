using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;

namespace Drawing_Coeff_generator
{
    public partial class mainForm : Form
    {
        private struct TextFormat
        {
            public readonly string text;
            public readonly Font font;
            public readonly Brush brush;
            public readonly Point point;
            public readonly Rectangle bound;

            public TextFormat(string text, Font font, Brush brush, Point point, Rectangle bound)
            {
                this.text = text;
                this.font = font;
                this.brush = brush;
                this.point = point;
                this.bound = bound;
            }
        }

        private struct Line
        {
            public Point start;
            public Point end;

            public Line(Point point1, Point point2) : this()
            {
                this.start = point1;
                this.end = point2;
            }
        }

        //TODO : make every graphics into point to draw but Text
        private const int width = 960, height = 540;

        private bool move = true, rectangle = false, textMode = false;
        private int x = 0, y = 0, sheetOriginX = 0, sheetOriginY = 0;
        private Rectangle tempRectangle;
        private Point beforeMove = new Point(0, 0);
        private int side = 0;
        private int limitDisp = 0;
        private List<TextFormat> textToKeep = new List<TextFormat>();
        private Point sheetOffset = new Point(0, 0);
        private int rectAlignAidX;
        private int rectAlignAidY;
        private bool drawAid = false, xFound = false, yFound = false;
        private bool penMode;
        private bool eraseMode;
        private PrivateFontCollection collection;
        private bool line;
        private Line tempLine;
        private List<Point> squareToDraw = new List<Point>();
        private List<ToolStripButton> tsbRadioList;
        private List<Point> tempLineRaster = new List<Point>();
        private List<int> squareToMove = new List<int>();
        private bool selectMode;
        private Rectangle selectRectangle;
        private bool squareMoveMode;

        public mainForm()
        {
            InitializeComponent();
            tsbMove.Checked = true;
            pictureBox1.MouseWheel += PictureBox1_MouseWheel;
            if ((pictureBox1.Size.Width / width) < (pictureBox1.Size.Height / height))
            {
                side = (pictureBox1.Size.Width / width);
            }
            else
            {
                side = (pictureBox1.Size.Height / height);
            }
            if (side <= 2)
            {
                side = 2;
            }

            collection = new PrivateFontCollection();
            foreach (string path in Directory.EnumerateFiles(@".\PixelFont\"))
            {
                collection.AddFontFile(path);
                tscbFont.Items.Add(collection.Families[collection.Families.Length - 1].Name);
            }

            //using (InstalledFontCollection col = new InstalledFontCollection())
            //{
            //    foreach (FontFamily fa in col.Families)
            //    {
            //        tscbFont.Items.Add(fa.Name);
            //    }
            //}
            tscbFont.Sorted = true;
            this.Size = new Size(side * width + 150, side * height + 100);
            toolTip1.UseFading = false;
            toolTip1.UseAnimation = false;
            tsbRadioList = new List<ToolStripButton> { tsbSelect, tsbRect, tsbLine, tsbPen, tsbText, tsbEraser, tsbMove };
            SetInitialSize();
#if DEBUG
            tslDebugSide.Text = side.ToString();
#endif
        }

        private void PictureBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            Boolean zoom = false;
            //zoom func
            int sideBefore = side;
            if (e.Delta > 0)//Zoom in
            {
                zoom = true;
                side++;
            }
            else//Zoom out
            {
                zoom = false;
                side--;
                if (side <= 2)
                {
                    side = 2;
                }
            }
#if DEBUG
            tslDebugSide.Text = side.ToString();
#endif

            for (int i = 0; i < textToKeep.Count; i++)
            {
                if (zoom)
                {
                    //textToKeep[i].font = new Font(textToKeep[i].font.FontFamily, (textToKeep[i].font.Size / sideBefore) * side, GraphicsUnit.Pixel);

                    textToKeep[i] = new TextFormat(textToKeep[i].text,
                        new Font(textToKeep[i].font.FontFamily, (textToKeep[i].font.Size / sideBefore) * side, GraphicsUnit.Pixel),
                        textToKeep[i].brush,
                        new Point(textToKeep[i].point.X + (textToKeep[i].point.X / sideBefore), textToKeep[i].point.Y + (textToKeep[i].point.Y / sideBefore)),
                        textToKeep[i].bound);
                }
                else
                {
                    textToKeep[i] = new TextFormat(textToKeep[i].text,
                        new Font(textToKeep[i].font.FontFamily, (textToKeep[i].font.Size / sideBefore) * side, GraphicsUnit.Pixel),
                        textToKeep[i].brush,
                        new Point(textToKeep[i].point.X - (textToKeep[i].point.X / sideBefore), textToKeep[i].point.Y - (textToKeep[i].point.Y / sideBefore)),
                        textToKeep[i].bound);
                }
            }
            pictureBox1.Invalidate();
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (rectangle && e.Button == MouseButtons.Left)
            {
                squareToDraw.AddRange(RasterRectangle(tempRectangle));
                rectangle = false;
                drawAid = false; xFound = false; yFound = false; rectAlignAidX = 0; rectAlignAidY = 0;
                pictureBox1.Invalidate();
            }
            if (line && e.Button == MouseButtons.Left)
            {
                line = false;
                tempLineRaster.Clear();
                squareToDraw.AddRange(RasterLine(tempLine));
                tempLine = new Line();
                pictureBox1.Invalidate();
            }
            if (penMode && e.Button == MouseButtons.Left)
            {
                penMode = false;
            }
            if (eraseMode && e.Button == MouseButtons.Left)
            {
                eraseMode = false;
            }
            if (selectMode && e.Button == MouseButtons.Left)
            {
                squareMoveMode = false;
                foreach (Point point in squareToDraw)
                {
                    if (selectRectangle.Contains(new Point(point.X * side, point.Y * side)))
                    {
                        squareMoveMode = true;
                        Cursor = Cursors.Hand;
                        squareToMove.Add(squareToDraw.IndexOf(point));
                    }
                }
                selectMode = false;
            }
        }

        private void tsbExport_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Bitmap bmp = new Bitmap(width * side, height * side);
                pictureBox1.DrawToBitmap(bmp, new Rectangle(sheetOriginX, sheetOriginY,
                    width * side, height * side));
                StreamWriter sw = new StreamWriter(File.Open(saveFileDialog1.FileName, FileMode.Create));
                int compteur = 0;
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        if (bmp.GetPixel(((((j / 8) + 1) * 8) - ((j % 8) + 1)) * side + 1, i * side + 1).ToArgb() == Color.Black.ToArgb())
                        {
                            sw.Write("1");
                        }
                        else
                        {
                            sw.Write("0");
                        }
                        compteur++;
                        if (compteur == 8)
                        {
                            compteur = 0;
                            sw.Write(" ");
                        }
                    }
                    sw.WriteLine();
                }
                sw.Close();

                System.Diagnostics.Process.Start(saveFileDialog1.FileName);
            }
        }

        private void tsbClear_Click(object sender, EventArgs e)
        {
            textToKeep.Clear();
            squareToDraw.Clear();
            pictureBox1.Invalidate();
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (tsbMove.Checked && e.Button == MouseButtons.Left)
            {
                move = true;
                beforeMove = new Point(e.X, e.Y);
            }
            else if (tsbRect.Checked && e.Button == MouseButtons.Left)
            {
                if (e.X > sheetOriginX && e.X < sheetOriginX + width * side && e.Y > sheetOriginY && e.Y < sheetOriginY + height * side)
                {
                    tempRectangle = new Rectangle(((e.X - sheetOriginX) / side * side + side / 2 + sheetOriginX),
                        ((e.Y - sheetOriginY) / side * side + side / 2 + sheetOriginY),
                        side,
                        side);
                    rectangle = true;
                }
            }
            else if (tsbLine.Checked && e.Button == MouseButtons.Left)
            {
                if (e.X > sheetOriginX && e.X < sheetOriginX + width * side && e.Y > sheetOriginY && e.Y < sheetOriginY + height * side)
                {
                    tempLine = new Line(new Point((e.X - sheetOriginX) / side * side + side / 2, (e.Y - sheetOriginY) / side * side + side / 2), new Point(0, 0));
                    line = true;
                }
            }
            else if (tsbText.Checked && e.Button == MouseButtons.Left)
            {
                //textMode = true;
            }
            else if (tsbPen.Checked && e.Button == MouseButtons.Left)
            {
                penMode = true;
            }
            else if (tsbEraser.Checked && e.Button == MouseButtons.Left)
            {
                eraseMode = true;
            }
            else if (tsbSelect.Checked && e.Button == MouseButtons.Left)
            {
                selectRectangle = new Rectangle(e.X, e.Y, 0, 0);
                selectMode = true;
            }
            else if (squareMoveMode)
            {
                squareMoveMode = false;
                squareToMove.Clear();
            }
        }

        private void tsbMove_Click(object sender, EventArgs e)
        {
            if (tsbMove.Checked)
            {
                tsbMove.Checked = false;
                pictureBox1.Cursor = Cursors.Hand;
            }
            else
            {
                tsbPen.Checked = false;
                tsbMove.Checked = true;
                tsbText.Checked = false;
                tsbLine.Checked = false;
                tsbRect.Checked = false;
                tsbEraser.Checked = false;
                pictureBox1.Cursor = Cursors.Default;
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (textMode)
            {
                textToKeep[textToKeep.Count - 1] =
                    new TextFormat(textToKeep[textToKeep.Count - 1].text,
                    textToKeep[textToKeep.Count - 1].font,
                    Brushes.Black,
                    textToKeep[textToKeep.Count - 1].point,
                    textToKeep[textToKeep.Count - 1].bound);
                textMode = false;
                tsbText.Checked = false;
                tsbMove.Checked = true;
                pictureBox1.Invalidate();
            }
        }

        private void tscbFont_SelectedIndexChanged(object sender, EventArgs e)
        {
            //if (tscbFont.Text == pixelFont.Name)
            //{
            FontFamily fontFamily;
            foreach (FontFamily ff in collection.Families)
            {
                if (tscbFont.Text == ff.Name)
                {
                    fontFamily = ff;
                    textToKeep[textToKeep.Count - 1] = new TextFormat(
                        textToKeep[textToKeep.Count - 1].text,
                        new Font(fontFamily, side, GraphicsUnit.Pixel),
                        textToKeep[textToKeep.Count - 1].brush,
                        textToKeep[textToKeep.Count - 1].point,
                        textToKeep[textToKeep.Count - 1].bound);
                }
            }

            //}
            //else
            //{
            //    textToKeep[textToKeep.Count - 1] = new TextFormat(
            //        textToKeep[textToKeep.Count - 1].text,
            //        new Font(tscbFont.Text, side),
            //        textToKeep[textToKeep.Count - 1].brush,
            //        textToKeep[textToKeep.Count - 1].point);
            //}
            pictureBox1.Invalidate();
        }

        private void tsbLine_Click(object sender, EventArgs e)
        {
            if (tsbLine.Checked)
            {
                line = false;
                tsbLine.Checked = false;
            }
            else
            {
                tsbLine.Checked = true;
                tsbRect.Checked = false;
                tsbText.Checked = false;
                tsbMove.Checked = false;
                tsbEraser.Checked = false;
                tsbPen.Checked = false;
                textMode = false;
                move = false;
            }
        }

        private void tstbSizeFont_TextChanged(object sender, EventArgs e)
        {
            if (Int32.TryParse(tstbSizeFont.Text, out int result))
            {
                textToKeep[textToKeep.Count - 1] = new TextFormat(
                        textToKeep[textToKeep.Count - 1].text,
                        new Font(textToKeep[textToKeep.Count - 1].font.FontFamily, result, GraphicsUnit.Pixel),
                        textToKeep[textToKeep.Count - 1].brush,
                        textToKeep[textToKeep.Count - 1].point,
                        textToKeep[textToKeep.Count - 1].bound);
                pictureBox1.Invalidate();
            }
        }

        private void mainForm_Resize(object sender, EventArgs e)
        {
            //pictureBox1.Invalidate();
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            int[] lnposYWR = new int[32]; int[] lnposYRAM = new int[32];
            lnposYWR[0] = 850; lnposYRAM[0] = 1040;
            String txt = "lnposYWR:", txt2 = "lnposYRAM:";
            for (int i = 1; i < lnposYWR.Length; i++)
            {
                lnposYWR[i] = lnposYWR[i - 1] + 1200;
                lnposYRAM[i] = lnposYRAM[i - 1] + 1200;
                txt += lnposYWR[i - 1] + ",";
                txt2 += lnposYRAM[i - 1] + ",";
            }
            txt += lnposYWR[31] + "END";
            txt2 += lnposYRAM[31] + "END";
            Console.WriteLine(txt + "\n" + txt2);
        }

        private void tsbRadio_Click(object sender, EventArgs e)
        {
            ToolStripButton tsb = sender as ToolStripButton;
            if (tsb.Checked)
            {
                tsb.Checked = false;
                if (tsb.Name == "tsbMove")
                {
                    pictureBox1.Cursor = Cursors.Default;
                }
            }
            else
            {
                foreach (ToolStripButton tsbb in tsbRadioList)
                {
                    tsbb.Checked = false;
                }
                if (tsb.Name == "tsbMove")
                {
                    pictureBox1.Cursor = Cursors.Hand;
                }
                tsb.Checked = true;
            }
        }

        private void tsbText_Click(object sender, EventArgs e)
        {
            if (tsbText.Checked)
            {
                textMode = false;
                tsbText.Checked = false;
            }
            else
            {
                TextPrompt txt = new TextPrompt();

                if (txt.ShowDialog() == DialogResult.OK)
                {
                    TextFormat textToWrite = new TextFormat(
                        txt.Controls.Find("txtPrompt", false)[0].Text,
                        new Font(collection.Families[0], 5 * side, GraphicsUnit.Pixel),
                        Brushes.Gray,
                        new Point(0, 0),
                        new Rectangle());
                    textToKeep.Add(textToWrite);

                    tscbFont.SelectedItem = collection.Families[0].Name;

                    tscbFont.Enabled = true;
                    tstbSizeFont.Enabled = true;
                    tstbSizeFont.Text = (5 * side).ToString();
                    tsbText.Checked = true;
                    tsbMove.Checked = false;
                    tsbRect.Checked = false;
                    tsbEraser.Checked = false;
                    rectangle = false;
                    move = false;
                    textMode = true;
                    pictureBox1.Invalidate();
                }
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            int cursorposX = e.X, cursorposY = e.Y;
            Point moveDelta = new Point(e.X - beforeMove.X, e.Y - beforeMove.Y);
            if (e.X < sheetOriginX)
            {
                cursorposX = sheetOriginX;
            }
            if (e.X > sheetOriginX + (width * side))
            {
                cursorposX = width * side;
            }
            if (e.Y <= sheetOriginY)
            {
                cursorposY = sheetOriginY;
            }
            if (e.Y > sheetOriginY + (height * side))
            {
                cursorposY = height * side;
            }

            if (tsbMove.Checked && e.Button == MouseButtons.Left)
            {
                sheetOriginX += moveDelta.X;
                sheetOriginY += moveDelta.Y;

                //TempRectangle = new Rectangle(new Point(TempRectangle.X + moveDelta.X, TempRectangle.Y + moveDelta.Y), TempRectangle.Size);

                for (int i = 0; i < textToKeep.Count; i++)
                {
                    textToKeep[i] = new TextFormat(textToKeep[i].text, textToKeep[i].font, textToKeep[i].brush, new Point(textToKeep[i].point.X + moveDelta.X, textToKeep[i].point.Y + moveDelta.Y), textToKeep[i].bound);
                }

                sheetOffset = new Point(sheetOriginX - ((sheetOriginX / side) * side), sheetOriginY - ((sheetOriginY / side) * side));
                pictureBox1.Invalidate();
            }
            else if (tsbRect.Checked)
            {
                tempRectangle =
                    new Rectangle(tempRectangle.X,
                    tempRectangle.Y,
                    ((cursorposX - tempRectangle.X + (Math.Sign(tempRectangle.Width) * side / 2)) / side * side),
                    ((cursorposY - tempRectangle.Y + (Math.Sign(tempRectangle.Height) * side / 2)) / side * side));

                //if (rectToKeep.Count >= 2)//If there are other rectangle than the one being drawn
                //{//Draw guide lines
                //    xFound = false; yFound = false; drawAid = false;
                //    for (int i = 0; i < rectToKeep.Count - 1; i++)
                //    {
                //        if (!xFound)
                //        {
                //            if (((cursorposX / side) == rectToKeep[i].X / side))
                //            {
                //                rectAlignAidX = rectToKeep[i].X;
                //                xFound = true;
                //                drawAid = true;
                //            }
                //            if (((cursorposX / side) == (rectToKeep[i].X + rectToKeep[i].Width) / side))
                //            {
                //                rectAlignAidX = (rectToKeep[i].X + rectToKeep[i].Width);
                //                drawAid = true;
                //                xFound = true;
                //            }
                //        }
                //        if (!yFound)
                //        {
                //            if (((cursorposy / side) == (rectToKeep[i].Y / side)))
                //            {
                //                rectAlignAidY = (rectToKeep[i].Y);
                //                yFound = true;
                //                drawAid = true;
                //            }
                //            if (((cursorposy / side) == (rectToKeep[i].Y + rectToKeep[i].Height) / side))
                //            {
                //                rectAlignAidY = (rectToKeep[i].Y + rectToKeep[i].Height);
                //                yFound = true;
                //                drawAid = true;
                //            }
                //        }
                //        if (xFound && yFound)
                //        {
                //            break;
                //        }
                //    }
                //}

                pictureBox1.Invalidate();
            }
            else if (tsbLine.Checked && e.Button == MouseButtons.Left)
            {
                tempLine =
                    new Line(tempLine.start,
                    new Point((cursorposX - sheetOriginX) / side * side + side / 2, (cursorposY - sheetOriginY) / side * side + side / 2));
                tempLineRaster = RasterLine(tempLine);
                pictureBox1.Invalidate();
            }
            else if (tsbText.Checked)
            {
                textToKeep[textToKeep.Count - 1] =
                    new TextFormat(textToKeep[textToKeep.Count - 1].text,
                    textToKeep[textToKeep.Count - 1].font,
                    textToKeep[textToKeep.Count - 1].brush,
                    new Point(((cursorposX - sheetOriginX) / side) * side, ((cursorposY - sheetOriginY) / side) * side),
                    textToKeep[textToKeep.Count - 1].bound);
                pictureBox1.Invalidate();
            }
            else if (tsbPen.Checked && e.Button == MouseButtons.Left)
            {
                bool newPoint = true;
                foreach (Point point in squareToDraw)
                {
                    if (((cursorposX - sheetOriginX) / side == point.X) && (((cursorposY - sheetOriginY) / side) == point.Y))
                    {
                        newPoint = false;
                    }
                }
                if (newPoint)
                {
                    squareToDraw.Add(new Point((cursorposX - sheetOriginX) / side,
                                (cursorposY - sheetOriginY) / side));
                }

                pictureBox1.Invalidate();
            }
            else if (tsbEraser.Checked && e.Button == MouseButtons.Left)
            {
                squareToDraw.Remove(new Point((cursorposX - sheetOriginX) / side, (cursorposY - sheetOriginY) / side));

                //foreach (Rectangle rectangleToRemove in rectToKeep)
                //{
                //    if (rectangleToRemove.X == ((cursorposX / side) * side + side / 2))
                //    {
                //        rectToKeep.Remove(rectangleToRemove);
                //        break;
                //    }
                //    else if (rectangleToRemove.Y == ((cursorposy / side) * side + side / 2))
                //    {
                //        rectToKeep.Remove(rectangleToRemove);
                //        break;
                //    }
                //    else if ((rectangleToRemove.X + rectangleToRemove.Width) == ((cursorposX / side) * side + side / 2))
                //    {
                //        rectToKeep.Remove(rectangleToRemove);
                //        break;
                //    }
                //    else if ((rectangleToRemove.Y + rectangleToRemove.Height) == ((cursorposy / side) * side + side / 2))
                //    {
                //        rectToKeep.Remove(rectangleToRemove);
                //        break;
                //    }
                //}
                foreach (TextFormat text in textToKeep)
                {
                    if (text.bound.Contains(new Point(cursorposX, cursorposY)))
                    {
                        textToKeep.Remove(text);
                        break;
                    }
                }
                pictureBox1.Invalidate();
            }
            else if (tsbSelect.Checked && e.Button == MouseButtons.Left)
            {
                selectRectangle =
                    new Rectangle(selectRectangle.X,
                    selectRectangle.Y,
                    cursorposX - selectRectangle.X + Math.Sign(selectRectangle.Width),
                    cursorposY - selectRectangle.Y + Math.Sign(selectRectangle.Height));
                pictureBox1.Invalidate();
            }
            else if (squareMoveMode)
            {
                foreach (int index in squareToMove)
                {
                    squareToDraw[index] = new Point(((squareToDraw[index].X * side) + moveDelta.X) / side,
                            ((squareToDraw[index].Y * side) + moveDelta.Y) / side);
                }
                pictureBox1.Invalidate();
            }
            else
            {
                x = 0; y = 0;
            }
            limitDisp++;
            if (limitDisp == 10)//performance condition
            {
                limitDisp = 0;
                toolTip1.SetToolTip(pictureBox1, (e.X - sheetOriginX) / side + "," + (e.Y - sheetOriginY) / side);//Drawing this is slooooow
                lblCoordinates.Text = "X:" + (e.X - sheetOriginX) / side + ";Y:" + (e.Y - sheetOriginY) / side;
            }
            beforeMove = new Point(e.X, e.Y);
        }

        private void SetInitialSize()
        {
            if ((pictureBox1.Size.Width / width) < (pictureBox1.Size.Height / height))
            {
                side = (pictureBox1.Size.Width / width);
            }
            else
            {
                side = (pictureBox1.Size.Height / height);
            }
            if (side <= 2)
            {
                side = 2;
            }
            pictureBox1.Invalidate();
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.White);
            Pen selectPen = new Pen(Color.CornflowerBlue, 2);
            selectPen.EndCap = System.Drawing.Drawing2D.LineCap.SquareAnchor;
            selectPen.DashStyle = System.Drawing.Drawing2D.DashStyle.DashDot;
            e.Graphics.DrawRectangle(selectPen, selectRectangle);
            if (rectangle)
            {
                if (tempRectangle.Width < 0 && tempRectangle.Height < 0)
                {
                    e.Graphics.DrawRectangle(new Pen(Color.Black, side / 2),
                        new Rectangle(tempRectangle.X + tempRectangle.Width, tempRectangle.Y + tempRectangle.Height,
                        -tempRectangle.Width, -tempRectangle.Height));
                }
                else if (tempRectangle.Width < 0)
                {
                    e.Graphics.DrawRectangle(new Pen(Color.Black, side / 2),
                        new Rectangle(tempRectangle.X + tempRectangle.Width, tempRectangle.Y,
                        -tempRectangle.Width, tempRectangle.Height));
                }
                else if (tempRectangle.Height < 0)
                {
                    e.Graphics.DrawRectangle(new Pen(Color.Black, side / 2),
                        new Rectangle(tempRectangle.X, tempRectangle.Y + tempRectangle.Height,
                        tempRectangle.Width, -tempRectangle.Height));
                }
                else
                {
                    e.Graphics.DrawRectangle(new Pen(Color.Black, side / 2),
                        new Rectangle(tempRectangle.X, tempRectangle.Y,
                        tempRectangle.Width, tempRectangle.Height));
                }
            }
            foreach (Point points in squareToDraw)
            {
                DrawFullSquareNormalized(e.Graphics, points);
            }
            foreach (Point points in tempLineRaster)
            {
                DrawFullSquareNormalized(e.Graphics, points);
            }
            for (int i = 0; i < textToKeep.Count; i++)
            {
                if (textToKeep[i].font.Size > Math.Pow(2, 15))
                {
                    e.Graphics.DrawString(textToKeep[i].text,
                        new Font(textToKeep[i].font.FontFamily,
                        (float)(Math.Pow(2, 15) - 1)), textToKeep[i].brush, textToKeep[i].point);
                }
                else
                {
                    //TextRenderer.DrawText(e.Graphics, txt.text, txt.font, txt.point, Color.Black, TextFormatFlags.Left & TextFormatFlags.NoPadding);
                    SizeF size = e.Graphics.MeasureString(textToKeep[i].text, textToKeep[i].font, textToKeep[i].point, StringFormat.GenericTypographic);
                    e.Graphics.DrawString(textToKeep[i].text, textToKeep[i].font, textToKeep[i].brush, textToKeep[i].point, StringFormat.GenericTypographic);
                    textToKeep[i] = new TextFormat(textToKeep[i].text, textToKeep[i].font, textToKeep[i].brush, textToKeep[i].point,
                        new Rectangle(textToKeep[i].point.X, textToKeep[i].point.Y, (int)size.Width, (int)size.Height));
                }
            }
            if (drawAid)
            {
                if (xFound)
                {
                    e.Graphics.DrawLine(Pens.Green, new Point(rectAlignAidX, 0), new Point(rectAlignAidX, e.ClipRectangle.Height));
                }
                if (yFound)
                {
                    e.Graphics.DrawLine(Pens.Green, new Point(0, rectAlignAidY), new Point(e.ClipRectangle.Width, rectAlignAidY));
                }
            }

            for (int i = 0; i <= width; i++)//vertical lines
            {
                e.Graphics.DrawLine(Pens.Red, i * side + sheetOriginX, sheetOriginY, i * side + sheetOriginX, height * side + sheetOriginY);
            }
            for (int i = 0; i <= height; i++)//horizontal lines
            {
                e.Graphics.DrawLine(Pens.Blue, sheetOriginX, i * side + sheetOriginY, width * side + sheetOriginX, i * (side) + sheetOriginY);
            }
        }

        private List<Point> RasterLine(Line line)
        {
            List<Point> points = new List<Point>();
            points.Add(new Point(line.start.X / side, line.start.Y / side));
            int xCoord = line.start.X; float yCoord = line.start.Y;
            int tempX = line.start.X, tempY = line.start.Y;
            int diffx = line.end.X - line.start.X;
            int diffy = line.end.Y - line.start.Y;
            float coeffDirecteur = (float)(diffy) / (diffx); float origin = line.start.Y - coeffDirecteur * line.start.X;
            if (diffx == 0) { coeffDirecteur = 1; origin = 0; }
            for (int i = 0; i <= Math.Abs(diffx == 0 ? diffy : diffx); i++)
            {
                xCoord += Math.Sign(diffx);
                yCoord = diffx != 0 ? coeffDirecteur * xCoord + origin : yCoord + Math.Sign(diffy);

                if (((int)(Math.Round(yCoord)) / side != points[points.Count - 1].Y) && ((xCoord) / side != points[points.Count - 1].X))
                {
                    points.Add(new Point(tempX / side, tempY / side));
                    tempY = (int)yCoord;
                    tempX = xCoord;
                }
                else if (((int)(Math.Round(yCoord)) / side != points[points.Count - 1].Y))
                {
                    points.Add(new Point(tempX / side, (int)yCoord / side));
                    tempY = (int)yCoord;
                }
                else if (((xCoord) / side != points[points.Count - 1].X))
                {
                    points.Add(new Point(xCoord / side, tempY / side));
                    tempX = xCoord;
                }
            }
            points.Add(new Point(line.end.X / side, line.end.Y / side));
            return points;
        }

        private void DrawFullSquare(Graphics e, Point point)
        {
            e.DrawRectangle(new Pen(Color.Black, side / 2),
                (point.X / side) * side + side / 4,
                (point.Y / side) * side + side / 4,
                side / 2,
                side / 2);
        }

        private void DrawFullSquareNormalized(Graphics e, Point point)
        {
            e.DrawRectangle(new Pen(Color.Black, side / 2),
                ((point.X * side) + side / 4) + sheetOriginX,
                ((point.Y * side) + side / 4) + sheetOriginY,
                side / 2,
                side / 2);
        }

        private Point FindCenter(Point pt)
        {
            return new Point(pt.X / side * side + side / 2, pt.Y / side * side + side / 2);
        }

        private List<Point> RasterRectangle(Rectangle rectangle)
        {
            List<Point> points = new List<Point>();
            for (int i = 0; Math.Abs(i) < Math.Abs(rectangle.Width); i += Math.Sign(rectangle.Width) * side)
            {
                points.Add(new Point((rectangle.X - sheetOriginX + i) / side, (rectangle.Y - sheetOriginY) / side));
                points.Add(new Point((rectangle.X - sheetOriginX + i) / side, (rectangle.Y - sheetOriginY + rectangle.Height) / side));
            }
            points.Add(new Point((rectangle.X - sheetOriginX + rectangle.Width) / side, (rectangle.Y - sheetOriginY + rectangle.Height) / side));
            for (int i = 0; Math.Abs(i) < Math.Abs(rectangle.Height); i += Math.Sign(rectangle.Height) * side)
            {
                points.Add(new Point((rectangle.X - sheetOriginX) / side, (rectangle.Y - sheetOriginY + i) / side));
                points.Add(new Point((rectangle.X - sheetOriginX + rectangle.Width) / side, (rectangle.Y - sheetOriginY + i) / side));
            }
            return points;
        }

        private List<Point> ScanDrawingArea(PictureBox e)
        {
            List<Point> points = new List<Point>();

            Bitmap bmp = new Bitmap(width * side, height * side);
            e.DrawToBitmap(bmp, new Rectangle(sheetOriginX, sheetOriginY,
                width * side, height * side));

            for (int w = 0; w < width; w++)//vertical lines
            {
                for (int h = 0; h < height; h++)//horizontal lines
                {
                    if (bmp.GetPixel(w + side / 2, h + side / 2) == Color.Black)
                    {
                        points.Add(FindCenter(new Point(w + side / 2, h + side / 2)));
                    }
                }
            }

            return points;
        }

        private List<Point> ScanTextArea(PictureBox e, TextFormat tf)
        {
            List<Point> points = new List<Point>();

            Bitmap bmp = new Bitmap(tf.bound.Width, tf.bound.Height);
            e.DrawToBitmap(bmp, new Rectangle(tf.point.X, tf.point.Y,
                tf.bound.Width, tf.bound.Height));
            //TODO: create and verify the function
            for (int w = 0; w < width; w++)//vertical lines
            {
                for (int h = 0; h < height; h++)//horizontal lines
                {
                    if (bmp.GetPixel(w + side / 2, h + side / 2) == Color.Black)
                    {
                        points.Add(FindCenter(new Point(w + side / 2, h + side / 2)));
                    }
                }
            }

            return points;
        }
    }
}
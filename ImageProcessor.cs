using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace EmguTesseract
{
    /// <summary>
    /// 方向枚举
    /// </summary>
    enum Direction
    {
        Up = 1,
        Down = 2,
        Left = 4,
        Right = 8,
    }

    class ImageProcessor
    {
        #region 字段

        #endregion

        #region 属性


        #endregion

        /// <summary>
        /// 取得图像的一个连续的块
        /// 既是：连通分量（极大连通子图），Connected Component
        /// </summary>
        /// <param name="bm"></param>
        /// <param name="x">x起点</param>
        /// <param name="y">y起点</param>
        /// <returns>tslw</returns>
        private static Dictionary<string, Point> GetBlock(Bitmap bm, int x, int y)
        {
            // 极大连通分量的点的集合
            Dictionary<string, Point> Track = new Dictionary<string, Point>();
            string strKeyOfPoint;
            // 工作栈
            Stack<Point> stk = new Stack<Point>();

            Color Cr = bm.GetPixel(x, y);
            if (ArgbEqual(Cr, Color.White) == true)
            {
                // 测试点不是黑色
                return Track;
            }
            // 入栈起始位置
            stk.Push(new Point(x, y));

            // 深度优先搜索
            for (; stk.Count != 0; )
            {
                // 弹出栈顶元素
                Point Pt = stk.Pop();
                // 加入访问过的路径集合中
                strKeyOfPoint = Pt.X + "#" + Pt.Y;
                Track[strKeyOfPoint] = new Point(Pt.X, Pt.Y);

                #region 取得邻接点集合

                List<Point> lstAdjacency = new List<Point>();

                // 右
                Point ptTest = new Point(Pt.X + 1, Pt.Y);
                if (ptTest.X < bm.Width)
                {
                    Color crTest = bm.GetPixel(ptTest.X, ptTest.Y);
                    if (ArgbEqual(crTest, Color.Black))
                    {
                        lstAdjacency.Add(ptTest);
                    }
                }

                // 左
                ptTest = new Point(Pt.X - 1, Pt.Y);
                if (ptTest.X >= 0)
                {
                    Color crTest = bm.GetPixel(ptTest.X, ptTest.Y);
                    if (ArgbEqual(crTest, Color.Black))
                    {
                        lstAdjacency.Add(ptTest);
                    }
                }

                // 下
                ptTest = new Point(Pt.X, Pt.Y + 1);
                if (ptTest.Y < bm.Height)
                {
                    Color crTest = bm.GetPixel(ptTest.X, ptTest.Y);
                    if (ArgbEqual(crTest, Color.Black))
                    {
                        lstAdjacency.Add(ptTest);
                    }
                }

                // 上
                ptTest = new Point(Pt.X, Pt.Y - 1);
                if (ptTest.Y >= 0)
                {
                    Color crTest = bm.GetPixel(ptTest.X, ptTest.Y);
                    if (ArgbEqual(crTest, Color.Black))
                    {
                        lstAdjacency.Add(ptTest);
                    }
                }

                #endregion

                #region 遍历邻接点，加入路径栈

                for (int i = 0; i < lstAdjacency.Count; ++i)
                {
                    Point ptAdjacency = lstAdjacency[i];
                    strKeyOfPoint = ptAdjacency.X + "#" + ptAdjacency.Y;
                    if (Track.ContainsKey(strKeyOfPoint) == false)
                    {
                        stk.Push(ptAdjacency);
                    }
                }

                #endregion

            }
            // end for


            return Track;
        }

        /// <summary>
        /// 去除块。降噪
        /// </summary>
        /// <param name="bm">要操作的位图对象</param>
        /// /// <param name="nBelowBlockSize">块大小，低于指定的大小的块，将被抹成白色</param>
        public static void RemoveBlock(Bitmap bm, int nBlockSize)
        {
            // 曾经遍历过的点
            Dictionary<string, Point> Track = new Dictionary<string, Point>();

            for (int i = 0; i < bm.Width; ++i)
            {
                for (int j = 0; j < bm.Height; ++j)
                {
                    if (Track.ContainsKey(i + "#" + j) == true)
                        continue;

                    Dictionary<string, Point> Block = GetBlock(bm, i, j);
                    foreach (string strkey in Block.Keys)
                    {
                        //if (Track.ContainsKey(strkey))
                        //{

                        //}
                        // Track[strkey] = Block[strkey];
                        if (!Track.ContainsKey(strkey))
                        {
                            Track.Add(strkey, Block[strkey]);
                        }
                    }

                    if (Block.Count < nBlockSize)
                    {
                        foreach (KeyValuePair<string, Point> Item in Block)
                        {
                            Point pt = Item.Value;
                            bm.SetPixel(pt.X, pt.Y, Color.White);
                        }

                        //foreach (string strkey in Block.Keys)
                        //{
                        //    Point pt = Block[strkey];
                        //    bm.SetPixel(pt.X, pt.Y, Color.White);
                        //}
                    }
                }
            }
        }

        /// <summary>
        /// 水平切割
        /// </summary>
        /// <param name="bm"></param>
        ///  <param name="nThickness">可切断的粗细度</param>
        public static void CutHorizontally(Bitmap bm, int nThickness)
        {
            // 状态标志。0白色区域状态，1黑色区域状态
            int nState = 0;
            int nPosStart = 0;

            for (int idxRow = 0; idxRow < bm.Height; ++idxRow)
            {
                nState = 0; // 初始化状态

                for (int idxCol = 0; idxCol < bm.Width; ++idxCol)
                {
                    Color Cr = bm.GetPixel(idxCol, idxRow);

                    #region 状态处理

                    switch (nState)
                    {
                        case 0: // 白色
                            {
                                if (ArgbEqual(Cr, Color.Black) == true)
                                {
                                    nPosStart = idxCol;
                                    nState = 1;
                                }
                            }
                            break;

                        case 1: // 黑色
                            {
                                if (ArgbEqual(Cr, Color.White) == true)
                                {
                                    int nThicknessTemp = idxCol - nPosStart;    // 宽度粗细

                                    if (nThicknessTemp <= nThickness)
                                    {
                                        // 切断
                                        for (int i = nPosStart; i < idxCol; ++i)
                                        {
                                            bm.SetPixel(i, idxRow, Color.White);
                                        }
                                    }

                                    nState = 0;
                                }
                            }
                            break;
                    }

                    #endregion

                }
                // end for

                if (nState == 1)
                {
                    int nThicknessTemp = bm.Width - nPosStart;    // 宽度粗细

                    if (nThicknessTemp <= nThickness)
                    {
                        // 切断
                        for (int i = nPosStart; i < bm.Width; ++i)
                        {
                            bm.SetPixel(i, idxRow, Color.White);
                        }
                    }
                }
            }
            // end for
        }

        /// <summary>
        /// 垂直切割
        /// </summary>
        /// <param name="bm"></param>
        /// <param name="nThickness">可切断的粗细度</param>
        public static void CutVerticality(Bitmap bm, int nThickness)
        {
            // 状态标志。0白色区域状态，1黑色区域状态
            int nState = 0;
            int nPosStart = 0;

            for (int idxCol = 0; idxCol < bm.Width; ++idxCol)   // 列
            {
                nState = 0; // 初始化状态

                for (int idxRow = 0; idxRow < bm.Height; ++idxRow) // 行
                {
                    Color Cr = bm.GetPixel(idxCol, idxRow);

                    #region 状态处理

                    switch (nState)
                    {
                        case 0: // 白色
                            {
                                if (ArgbEqual(Cr, Color.Black) == true)
                                {
                                    nPosStart = idxRow;
                                    nState = 1;
                                }
                            }
                            break;

                        case 1: // 黑色
                            {
                                if (ArgbEqual(Cr, Color.White) == true)
                                {
                                    int nThicknessTemp = idxRow - nPosStart;    // 宽度粗细

                                    if (nThicknessTemp <= nThickness)
                                    {
                                        // 切断
                                        for (int i = nPosStart; i < idxRow; ++i)
                                        {
                                            bm.SetPixel(idxCol, i, Color.White);
                                        }
                                    }

                                    nState = 0;
                                }
                            }
                            break;
                    }

                    #endregion

                }
                // end for

                if (nState == 1)
                {
                    int nThicknessTemp = bm.Height - nPosStart;    // 宽度粗细

                    if (nThicknessTemp <= nThickness)
                    {
                        // 切断
                        for (int i = nPosStart; i < bm.Height; ++i)
                        {
                            bm.SetPixel(idxCol, i, Color.White);
                        }
                    }
                }
            }
            // end for
        }

        /// <summary>
        /// Argb值判等
        /// </summary>
        /// <param name="cr1"></param>
        /// <param name="cr2"></param>
        /// <returns></returns>
        private static bool ArgbEqual(Color cr1, Color cr2)
        {
            if (cr1.A == cr2.A &&
                cr1.R == cr2.R &&
                cr1.G == cr2.G &&
                cr1.B == cr2.B)
            {
                return true;
            }

            return false;

        }

        /// <summary>
        /// 腐蚀
        /// </summary>
        /// <param name="bm">要腐蚀的图像</param>
        /// <param name="nDeep">腐蚀的深度</param>
        public static void Corrode(Bitmap bm, Direction Direction)
        {
            using (Bitmap bmOld = (Bitmap)bm.Clone())
            {
                switch (Direction)
                {
                    case Direction.Up:
                        {
                            #region

                            for (int idxCol = 0; idxCol < bm.Width; ++idxCol)
                            {
                                for (int idxRow = 1; idxRow < bm.Height; ++idxRow)
                                {
                                    // 先设置目标图像的像素点
                                    bm.SetPixel(idxCol, idxRow, Color.White);

                                    if (
                                        ArgbEqual(bmOld.GetPixel(idxCol, idxRow), Color.Black) &&
                                        ArgbEqual(bmOld.GetPixel(idxCol, idxRow - 1), Color.Black)
                                        )
                                    {
                                        bm.SetPixel(idxCol, idxRow, Color.Black);
                                    }

                                }
                                // end for
                            }

                            #endregion
                        }
                        break;

                    case Direction.Left:
                        {
                            #region

                            for (int idxCol = 1; idxCol < bm.Width; ++idxCol)
                            {
                                for (int idxRow = 0; idxRow < bm.Height; ++idxRow)
                                {
                                    // 先设置目标图像的像素点
                                    bm.SetPixel(idxCol, idxRow, Color.White);

                                    if (
                                        ArgbEqual(bmOld.GetPixel(idxCol, idxRow), Color.Black) &&
                                        ArgbEqual(bmOld.GetPixel(idxCol - 1, idxRow), Color.Black)
                                        )
                                    {
                                        bm.SetPixel(idxCol, idxRow, Color.Black);
                                    }

                                }
                                // end for
                            }

                            #endregion
                        }
                        break;

                    case Direction.Down:
                        {
                            #region

                            for (int idxCol = 0; idxCol < bm.Width; ++idxCol)
                            {
                                for (int idxRow = 0; idxRow < bm.Height - 1; ++idxRow)
                                {
                                    // 先设置目标图像的像素点
                                    bm.SetPixel(idxCol, idxRow, Color.White);

                                    if (
                                        ArgbEqual(bmOld.GetPixel(idxCol, idxRow), Color.Black) &&
                                        ArgbEqual(bmOld.GetPixel(idxCol, idxRow + 1), Color.Black)
                                        )
                                    {
                                        bm.SetPixel(idxCol, idxRow, Color.Black);
                                    }

                                }
                                // end for
                            }

                            #endregion
                        }
                        break;

                    case Direction.Right:
                        {
                            #region

                            for (int idxCol = 0; idxCol < bm.Width - 1; ++idxCol)
                            {
                                for (int idxRow = 0; idxRow < bm.Height; ++idxRow)
                                {
                                    // 先设置目标图像的像素点
                                    bm.SetPixel(idxCol, idxRow, Color.White);

                                    if (
                                        ArgbEqual(bmOld.GetPixel(idxCol, idxRow), Color.Black) &&
                                        ArgbEqual(bmOld.GetPixel(idxCol + 1, idxRow), Color.Black)
                                        )
                                    {
                                        bm.SetPixel(idxCol, idxRow, Color.Black);
                                    }

                                }
                                // end for
                            }

                            #endregion
                        }
                        break;

                    case Direction.Left | Direction.Right:
                        {
                            #region

                            for (int idxCol = 1; idxCol < bm.Width - 1; ++idxCol)
                            {
                                for (int idxRow = 0; idxRow < bm.Height; ++idxRow)
                                {
                                    // 先设置目标图像的像素点
                                    bm.SetPixel(idxCol, idxRow, Color.White);

                                    if (
                                        ArgbEqual(bmOld.GetPixel(idxCol, idxRow), Color.Black) &&
                                        ArgbEqual(bmOld.GetPixel(idxCol - 1, idxRow), Color.Black) &&
                                        ArgbEqual(bmOld.GetPixel(idxCol + 1, idxRow), Color.Black)
                                        )
                                    {
                                        bm.SetPixel(idxCol, idxRow, Color.Black);
                                    }

                                }
                                // end for
                            }

                            #endregion
                        }
                        break;

                    case Direction.Up | Direction.Down:
                        {
                            #region

                            for (int idxCol = 0; idxCol < bm.Width; ++idxCol)
                            {
                                for (int idxRow = 1; idxRow < bm.Height - 1; ++idxRow)
                                {
                                    // 先设置目标图像的像素点
                                    bm.SetPixel(idxCol, idxRow, Color.White);

                                    if (
                                        ArgbEqual(bmOld.GetPixel(idxCol, idxRow), Color.Black) &&
                                        ArgbEqual(bmOld.GetPixel(idxCol, idxRow - 1), Color.Black) &&
                                        ArgbEqual(bmOld.GetPixel(idxCol, idxRow + 1), Color.Black)
                                        )
                                    {
                                        bm.SetPixel(idxCol, idxRow, Color.Black);
                                    }

                                }
                                // end for
                            }

                            #endregion
                        }
                        break;
                }
            }
            // end for
        }

        /// <summary>
        /// 平移图像
        /// </summary>
        /// <param name="bm"></param>
        /// <param name="nOffset">平移的方向</param>
        /// <param name="nOffset">平移的偏移量</param>
        /// <param name="nOffset">平移的填充像素</param>
        public static void Translate(Bitmap bm, Direction Direction, int nOffset, Color crFill)
        {
            switch (Direction)
            {
                case Direction.Left:
                    {
                        // 向左平移 nDeep 位
                        for (int idxCol = nOffset; idxCol < bm.Width; ++idxCol)
                        {
                            int idxColDst = idxCol - nOffset;

                            for (int idxRow = 0; idxRow < bm.Height; ++idxRow)
                            {
                                Color crSrc = bm.GetPixel(idxCol, idxRow);
                                bm.SetPixel(idxColDst, idxRow, crSrc);
                            }
                        }
                        // 被移空的地方填充crFill设定的背景
                        FillRect(bm,
                            new Point(bm.Width - nOffset, 0),
                            new Point(bm.Width - 1, bm.Height - 1),
                            crFill);
                    }
                    break;

                case Direction.Right:
                    {
                        // 向右平移 
                        for (int idxCol = bm.Width - nOffset - 1; idxCol >= 0; --idxCol)
                        {
                            int idxColDst = idxCol + nOffset;

                            for (int idxRow = 0; idxRow < bm.Height; ++idxRow)
                            {
                                Color crSrc = bm.GetPixel(idxCol, idxRow);
                                bm.SetPixel(idxColDst, idxRow, crSrc);
                            }
                        }
                        // 填充
                        FillRect(bm,
                            new Point(0, 0),
                            new Point(nOffset - 1, bm.Height - 1),
                            crFill);
                    }
                    break;

                case Direction.Down:
                    {
                        // 向下平移 
                        for (int idxRow = bm.Height - nOffset - 1; idxRow >= 0; --idxRow)
                        {
                            int idxRowDst = idxRow + nOffset;

                            for (int idxCol = 0; idxCol < bm.Width; ++idxCol)
                            {
                                Color crSrc = bm.GetPixel(idxCol, idxRow);
                                bm.SetPixel(idxCol, idxRowDst, crSrc);
                            }
                        }
                        // 填充
                        FillRect(bm,
                            new Point(0, 0),
                            new Point(bm.Width - 1, nOffset - 1),
                            crFill);
                    }
                    break;

                case Direction.Up:
                    {
                        // 向上平移 
                        for (int idxRow = nOffset; idxRow < bm.Height; ++idxRow)
                        {
                            int idxRowDst = idxRow - nOffset;

                            for (int idxCol = 0; idxCol < bm.Width; ++idxCol)
                            {
                                Color crSrc = bm.GetPixel(idxCol, idxRow);
                                bm.SetPixel(idxCol, idxRowDst, crSrc);
                            }
                        }
                        // 填充
                        FillRect(bm,
                            new Point(0, bm.Height - nOffset),
                            new Point(bm.Width - 1, bm.Height - 1),
                            crFill);
                    }
                    break;

            }


        }

        /// <summary>
        /// 填充矩形
        /// </summary>
        /// <param name="bm">要填充的图像</param>
        /// <param name="ptStart">填充开始位置</param>
        /// <param name="ptEnd">填充结束位置</param>
        /// <param name="ptEnd">填充的像素值</param>
        public static void FillRect(Bitmap bm, Point ptStart, Point ptEnd, Color crFill)
        {
            for (int idxCol = ptStart.X; idxCol <= ptEnd.X; ++idxCol)
            {
                for (int idxRow = ptStart.Y; idxRow <= ptEnd.Y; ++idxRow)
                {
                    bm.SetPixel(idxCol, idxRow, crFill);
                }
            }
        }

    }
}

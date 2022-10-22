/*************************************************************************
 * 文件名称 ：CompointControl.cs                          
 * 描述说明 ：飞行仪表控件
 * 
 * 创建信息 : create by  on 2012-01-10
 * 修订信息 : modify by (person) on (date) for (reason)
 * 
 * 原始代码来源 : https://download.csdn.net/download/qq_42237381/10877536
 * 原代码作者 : CSDN博主「渡之」
 * 本控件主要代码来源于CSDN网站(见上), 由湖南创智艾泰克科技有限公司 王文庆做出完善和改造
 * 遵循 CC 4.0 BY-SA 版权协议，转载请附上原文出处链接及本声明。
 **************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FlyMeter
{
    public partial class HoriCt : UserControl
    {
        private Image imgtmp, imgori;
        private Bitmap bitmp, bm;

        public HoriCt()
        {
            InitializeComponent();
        }
        /// <summary>
        /// 地平仪刻度划线函数
        /// </summary>
        /// <returns></returns>
        private Image Hori_Line()
        {
            bitmp = new Bitmap(this.Width, this.Width);//350
            System.Drawing.Graphics gscale = System.Drawing.Graphics.FromImage(bitmp);
            #region 准心绘线
            Pen p1 = new Pen(Color.Red, 3);
            Pen p2 = new Pen(Color.Green, 2);
            gscale.DrawLine(p2, (float)(this.Width * 29 / 70), (float)(this.Width / 2), (float)(this.Width * 41 / 70), (float)(this.Width / 2));
            gscale.DrawLine(p1, (float)(this.Width * 29 / 70), (float)(this.Width * 37 / 70), (float)(this.Width / 2), (float)(this.Width / 2));
            gscale.DrawLine(p1, (float)(this.Width * 41 / 70), (float)(this.Width * 37 / 70), (float)(this.Width / 2), (float)(this.Width / 2));
            #endregion
            #region 滚转刻度线
            //画圆
            gscale.DrawEllipse(Pens.White, (float)(this.Width * 0.1), (float)(this.Width * 0.1), (float)(this.Width * 0.8), (float)(this.Width * 0.8));
            #region 在圆上画刻度
            int i, i1, j, j1, k;
            for (k = 0; k < 73; k++)
            {
                i = Convert.ToInt32(this.Width * 0.4 * Math.Cos(k * Math.PI / 36) + this.Width / 2);
                j = Convert.ToInt32(this.Width * 0.4 * Math.Sin(k * Math.PI / 36) + this.Width / 2);
                if (k % 2 == 0)
                {
                    i1 = Convert.ToInt32(this.Width * 31 / 70 * Math.Cos(k * Math.PI / 36) + this.Width / 2);
                    j1 = Convert.ToInt32(this.Width * 31 / 70 * Math.Sin(k * Math.PI / 36) + this.Width / 2);
                }
                else
                {
                    i1 = Convert.ToInt32(this.Width * 30 / 70 * Math.Cos(k * Math.PI / 36) + this.Width / 2);
                    j1 = Convert.ToInt32(this.Width * 30 / 70 * Math.Sin(k * Math.PI / 36) + this.Width / 2);
                }
                gscale.DrawLine(Pens.White, i, j, i1, j1);
            }
            #endregion
            #endregion
            gscale.Dispose();
            return bitmp;
        }
        /// <summary>
        /// 地平仪显示函数
        /// </summary>
        /// <param name="pitch_angle">俯仰角 pitch_angle 范围-90~90 度 </param>
        /// <param name="row_angle">滚动角 row_angle   范围-90~90 度</param>
        public void Hori_Disp(double pitch_angle, double row_angle)
        {
            //1地平仪图像载入带平移
            //int pic_position = Convert.ToInt32(pitch_angle * 3.86);
            int pic_position = Convert.ToInt32(pitch_angle * this.Height / 90.67);
            try
            {
                //取得水平仪背景图--从ErrorImage中取得
                imgtmp = new Bitmap(HoriBox.ErrorImage);
                row_angle = row_angle % 360;
                ////弧度转换  
                //double ar = 2;
                //double radian = (row_angle - 90) * Math.PI / 180.0;
                //double radiana = (row_angle - 90 - ar) * Math.PI / 180.0;
                //double radianc = (row_angle - 90 + ar) * Math.PI / 180.0;
                //double cos = Math.Cos(radian);
                //double cosa = Math.Cos(radiana);
                //double cosc = Math.Cos(radianc);
                //double sin = Math.Sin(radian);
                //double sina = Math.Sin(radiana);
                //double sinc = Math.Sin(radianc);
                //目标位图
                Bitmap dsImage = new Bitmap(this.Width, this.Height);
                System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(dsImage);
                //双线性插值
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                //抗锯齿
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                //计算偏移量
                Rectangle rect = new Rectangle(-this.Width / 2, -this.Height / 2 + pic_position, this.Width * 2, this.Height * 2);
                //平移图形
                g.TranslateTransform(this.Width / 2, this.Height / 2);
                //旋转图形
                g.RotateTransform((float)row_angle);
                //恢复图像在水平和垂直方向的平移 
                g.TranslateTransform(-this.Width / 2, -this.Height / 2);
                g.DrawImage(imgtmp, rect);
                //重至绘图的所有变换  
                g.ResetTransform();
                g.Dispose();
                //保存旋转后的图片
                bm = dsImage;
                //调用imgori 已经是画好的刻度盘
                imgori = Hori_Line();
                //把刻度盘图形从Img格式转换为Bmp格式
                Bitmap bitmp = new Bitmap(imgori);
                //重合背景图和刻度盘
                bm = CommFunClass.Overlap(bitmp, bm, 0, 0, this.Width, this.Height);
                #region 指针设计
                Bitmap pointImage = new Bitmap(this.Width, this.Height);
                System.Drawing.Graphics gPoint = System.Drawing.Graphics.FromImage(pointImage);
                //红色
                SolidBrush h = new SolidBrush(Color.Red);
                //Point a = new Point(Convert.ToInt32(this.Width / 2 + this.Width * 131 / 350 * cosa), Convert.ToInt32(this.Width * 180 / 350 + this.Width * 131 / 350 * sina));
                //Point b = new Point(Convert.ToInt32(this.Width * 141 / 350 * cos + this.Width / 2), Convert.ToInt32(this.Width * 141 / 350 * sin + this.Height / 2));
                //Point c = new Point(Convert.ToInt32(this.Width / 2 + this.Width * 131 / 350 * cosc), Convert.ToInt32(this.Width * 180 / 350 + this.Width * 131 / 350 * sinc));
                //Point[] pointer = { a, b, c };
                ////填充点所围的区域
                //gPoint.FillPolygon(h, pointer);
                Point a = new Point(Convert.ToInt32(this.Width / 2), Convert.ToInt32(this.Height * 0.05));
                Point b = new Point(a.X - Convert.ToInt32(this.Height * 0.05), a.Y - Convert.ToInt32(this.Height * 0.05));
                Point c = new Point(a.X + Convert.ToInt32(this.Height * 0.05), a.Y - Convert.ToInt32(this.Height * 0.05));
                Point[] pointer = { a, b, c };
                //填充点所围的区域
                gPoint.FillPolygon(h, pointer);
                Bitmap aaa = CommFunClass.RotateBmp(pointImage, row_angle, this.Width, this.Height);
                #endregion
                //重合【指针】与【背景图和刻度盘】
                bm = CommFunClass.Overlap(aaa, bm, 0, 0, this.Width, this.Height);
                HoriBox.Image = bm;
                g.Dispose();
                imgtmp.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            #region 截出控件大小的一个圆
            HoriBox.Region = CommFunClass.ComshellRegion(this.Width, this.Height);
            #endregion
        }
        /// <summary>
        /// 保持正方形
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HoriCt_Resize(object sender, EventArgs e)
        {
            this.Height = this.Width;
            //画刻度盘
            imgori = Hori_Line();
            Hori_Disp(0, 0);
        }
    }
}

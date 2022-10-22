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
    /// <summary>
    /// 磁罗盘控件
    /// </summary>
    public partial class CompointCt : UserControl
    {
        private double dir_angle;
        /// <summary>
        /// 航向角
        /// </summary>
        public double DirAngle
        {
            get
            {
                return dir_angle;
            }
            set
            {
                dir_angle = value;
                Compass_Disp(dir_angle);
            }
        }
        public CompointCt()
        {
            InitializeComponent();
        }

        //-------------------磁罗盘显示函数-------------------//
        /// <summary>
        /// 航向角设置
        /// 航向角 dir_angle 范围0~360 度
        /// </summary>
        /// <param name="dir_angle">航向角(0~360)</param>
        public void Compass_Disp(double dir_angle)
        {
            CompointBox.Image = CommFunClass.RotateBmp(CompointBox.ErrorImage, dir_angle, this.Width, this.Height);
        }
        /// <summary>
        /// 保持正方形
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CompointCt_Resize(object sender, EventArgs e)
        {
            this.Height = this.Width;
            ComshellBox.Region = CommFunClass.ComshellRegion(this.Width, Height);
            CompointBox.Region = CommFunClass.CompointRegion(this.Width, Height, 0.13);
        }
    }
}

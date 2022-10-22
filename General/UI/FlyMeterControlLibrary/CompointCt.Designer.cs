namespace FlyMeter
{
    partial class CompointCt
    {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.CompointBox = new System.Windows.Forms.PictureBox();
            this.ComshellBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.CompointBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ComshellBox)).BeginInit();
            this.SuspendLayout();
            // 
            // CompointBox
            // 
            this.CompointBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CompointBox.ErrorImage = global::FlyMeter.Properties.Resources.point;
            this.CompointBox.Image = global::FlyMeter.Properties.Resources.point;
            this.CompointBox.InitialImage = global::FlyMeter.Properties.Resources.point;
            this.CompointBox.Location = new System.Drawing.Point(0, 0);
            this.CompointBox.Name = "CompointBox";
            this.CompointBox.Size = new System.Drawing.Size(237, 237);
            this.CompointBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.CompointBox.TabIndex = 0;
            this.CompointBox.TabStop = false;
            // 
            // ComshellBox
            // 
            this.ComshellBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ComshellBox.Image = global::FlyMeter.Properties.Resources.point;
            this.ComshellBox.Location = new System.Drawing.Point(0, 0);
            this.ComshellBox.Name = "ComshellBox";
            this.ComshellBox.Size = new System.Drawing.Size(237, 237);
            this.ComshellBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.ComshellBox.TabIndex = 1;
            this.ComshellBox.TabStop = false;
            // 
            // CompointCt
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.CompointBox);
            this.Controls.Add(this.ComshellBox);
            this.Name = "CompointCt";
            this.Size = new System.Drawing.Size(237, 237);
            this.Resize += new System.EventHandler(this.CompointCt_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.CompointBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ComshellBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox CompointBox;
        private System.Windows.Forms.PictureBox ComshellBox;
    }
}

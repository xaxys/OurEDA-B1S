namespace FlyMeter
{
    partial class HoriCt
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
            this.HoriBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.HoriBox)).BeginInit();
            this.SuspendLayout();
            // 
            // HoriBox
            // 
            this.HoriBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.HoriBox.ErrorImage = global::FlyMeter.Properties.Resources._20181226105533979;
            this.HoriBox.InitialImage = global::FlyMeter.Properties.Resources._20181226105533979;
            this.HoriBox.Location = new System.Drawing.Point(0, 0);
            this.HoriBox.Name = "HoriBox";
            this.HoriBox.Size = new System.Drawing.Size(265, 265);
            this.HoriBox.TabIndex = 0;
            this.HoriBox.TabStop = false;
            // 
            // HoriCt
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.HoriBox);
            this.Name = "HoriCt";
            this.Size = new System.Drawing.Size(265, 265);
            this.Resize += new System.EventHandler(this.HoriCt_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.HoriBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox HoriBox;
    }
}

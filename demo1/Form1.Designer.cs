
namespace demo1
{
    partial class Form1
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

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.leftLayout = new System.Windows.Forms.TableLayoutPanel();
            this.imageGroup = new System.Windows.Forms.GroupBox();
            this.cogDisplayImage = new Cognex.VisionPro.Display.CogDisplay();
            this.logGroup = new System.Windows.Forms.GroupBox();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.buttonsGroup = new System.Windows.Forms.GroupBox();
            this.buttonTable = new System.Windows.Forms.TableLayoutPanel();
            this.btnDisplayLive = new System.Windows.Forms.Button();
            this.btnCloseCamera = new System.Windows.Forms.Button();
            this.btnSingleRun = new System.Windows.Forms.Button();
            this.btnContinuousRun = new System.Windows.Forms.Button();
            this.statusGroup = new System.Windows.Forms.GroupBox();
            this.txtStatus = new System.Windows.Forms.TextBox();
            this.rootLayout.SuspendLayout();
            this.leftLayout.SuspendLayout();
            this.imageGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.cogDisplayImage)).BeginInit();
            this.logGroup.SuspendLayout();
            this.buttonsGroup.SuspendLayout();
            this.buttonTable.SuspendLayout();
            this.statusGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 2;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.rootLayout.Controls.Add(this.leftLayout, 0, 0);
            this.rootLayout.Controls.Add(this.buttonsGroup, 1, 0);
            this.rootLayout.Controls.Add(this.statusGroup, 1, 1);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.MinimumSize = new System.Drawing.Size(1600, 900);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 2;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 75F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.rootLayout.Size = new System.Drawing.Size(1904, 1041);
            this.rootLayout.TabIndex = 0;
            // 
            // leftLayout
            // 
            this.leftLayout.ColumnCount = 1;
            this.leftLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftLayout.Controls.Add(this.imageGroup, 0, 0);
            this.leftLayout.Controls.Add(this.logGroup, 0, 1);
            this.leftLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftLayout.Location = new System.Drawing.Point(3, 3);
            this.leftLayout.Name = "leftLayout";
            this.leftLayout.RowCount = 2;
            this.rootLayout.SetRowSpan(this.leftLayout, 2);
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 85F));
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.leftLayout.Size = new System.Drawing.Size(1041, 1035);
            this.leftLayout.TabIndex = 0;
            // 
            // imageGroup
            // 
            this.imageGroup.Controls.Add(this.cogDisplayImage);
            this.imageGroup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.imageGroup.Location = new System.Drawing.Point(3, 3);
            this.imageGroup.Name = "imageGroup";
            this.imageGroup.Size = new System.Drawing.Size(1035, 873);
            this.imageGroup.TabIndex = 0;
            this.imageGroup.TabStop = false;
            this.imageGroup.Text = "图像";
            // 
            // cogDisplayImage
            // 
            this.cogDisplayImage.ColorMapLowerClipColor = System.Drawing.Color.Black;
            this.cogDisplayImage.ColorMapLowerRoiLimit = 0D;
            this.cogDisplayImage.ColorMapPredefined = Cognex.VisionPro.Display.CogDisplayColorMapPredefinedConstants.None;
            this.cogDisplayImage.ColorMapUpperClipColor = System.Drawing.Color.Black;
            this.cogDisplayImage.ColorMapUpperRoiLimit = 1D;
            this.cogDisplayImage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cogDisplayImage.DoubleTapZoomCycleLength = 2;
            this.cogDisplayImage.DoubleTapZoomSensitivity = 2.5D;
            this.cogDisplayImage.Location = new System.Drawing.Point(3, 17);
            this.cogDisplayImage.Margin = new System.Windows.Forms.Padding(0);
            this.cogDisplayImage.MouseWheelMode = Cognex.VisionPro.Display.CogDisplayMouseWheelModeConstants.Zoom1;
            this.cogDisplayImage.MouseWheelSensitivity = 1D;
            this.cogDisplayImage.Name = "cogDisplayImage";
            this.cogDisplayImage.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("cogDisplayImage.OcxState")));
            this.cogDisplayImage.Size = new System.Drawing.Size(1029, 853);
            this.cogDisplayImage.TabIndex = 0;
            // 
            // logGroup
            // 
            this.logGroup.Controls.Add(this.txtLog);
            this.logGroup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.logGroup.Location = new System.Drawing.Point(3, 882);
            this.logGroup.Name = "logGroup";
            this.logGroup.Size = new System.Drawing.Size(1035, 150);
            this.logGroup.TabIndex = 1;
            this.logGroup.TabStop = false;
            this.logGroup.Text = "日志";
            // 
            // txtLog
            // 
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.Font = new System.Drawing.Font("宋体", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.txtLog.Location = new System.Drawing.Point(3, 17);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(1029, 130);
            this.txtLog.TabIndex = 0;
            this.txtLog.WordWrap = false;
            // 
            // buttonsGroup
            // 
            this.buttonsGroup.Controls.Add(this.buttonTable);
            this.buttonsGroup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonsGroup.Location = new System.Drawing.Point(1050, 3);
            this.buttonsGroup.Name = "buttonsGroup";
            this.buttonsGroup.Size = new System.Drawing.Size(851, 774);
            this.buttonsGroup.TabIndex = 1;
            this.buttonsGroup.TabStop = false;
            this.buttonsGroup.Text = "操作";
            // 
            // buttonTable
            // 
            this.buttonTable.ColumnCount = 1;
            this.buttonTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.buttonTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.buttonTable.Controls.Add(this.btnDisplayLive, 0, 0);
            this.buttonTable.Controls.Add(this.btnCloseCamera, 0, 1);
            this.buttonTable.Controls.Add(this.btnSingleRun, 0, 2);
            this.buttonTable.Controls.Add(this.btnContinuousRun, 0, 3);
            this.buttonTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonTable.Location = new System.Drawing.Point(3, 17);
            this.buttonTable.Name = "buttonTable";
            this.buttonTable.RowCount = 4;
            this.buttonTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.buttonTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.buttonTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.buttonTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.buttonTable.Size = new System.Drawing.Size(845, 754);
            this.buttonTable.TabIndex = 0;
            // 
            // btnDisplayLive
            // 
            this.btnDisplayLive.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDisplayLive.Enabled = false;
            this.btnDisplayLive.Font = new System.Drawing.Font("微软雅黑", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnDisplayLive.Location = new System.Drawing.Point(12, 12);
            this.btnDisplayLive.Margin = new System.Windows.Forms.Padding(12);
            this.btnDisplayLive.Name = "btnDisplayLive";
            this.btnDisplayLive.Size = new System.Drawing.Size(821, 164);
            this.btnDisplayLive.TabIndex = 0;
            this.btnDisplayLive.Text = "显示图像";
            this.btnDisplayLive.UseVisualStyleBackColor = true;
            this.btnDisplayLive.Click += new System.EventHandler(this.btnDisplayLive_Click);
            // 
            // btnCloseCamera
            // 
            this.btnCloseCamera.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCloseCamera.Enabled = false;
            this.btnCloseCamera.Font = new System.Drawing.Font("微软雅黑", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnCloseCamera.Location = new System.Drawing.Point(12, 200);
            this.btnCloseCamera.Margin = new System.Windows.Forms.Padding(12);
            this.btnCloseCamera.Name = "btnCloseCamera";
            this.btnCloseCamera.Size = new System.Drawing.Size(821, 164);
            this.btnCloseCamera.TabIndex = 1;
            this.btnCloseCamera.Text = "关闭摄像头";
            this.btnCloseCamera.UseVisualStyleBackColor = true;
            this.btnCloseCamera.Click += new System.EventHandler(this.btnCloseCamera_Click);
            // 
            // btnSingleRun
            // 
            this.btnSingleRun.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSingleRun.Enabled = false;
            this.btnSingleRun.Font = new System.Drawing.Font("微软雅黑", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnSingleRun.Location = new System.Drawing.Point(12, 388);
            this.btnSingleRun.Margin = new System.Windows.Forms.Padding(12);
            this.btnSingleRun.Name = "btnSingleRun";
            this.btnSingleRun.Size = new System.Drawing.Size(821, 164);
            this.btnSingleRun.TabIndex = 2;
            this.btnSingleRun.Text = "单次运行";
            this.btnSingleRun.UseVisualStyleBackColor = true;
            this.btnSingleRun.Click += new System.EventHandler(this.btnSingleRun_Click);
            // 
            // btnContinuousRun
            // 
            this.btnContinuousRun.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnContinuousRun.Enabled = false;
            this.btnContinuousRun.Font = new System.Drawing.Font("微软雅黑", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnContinuousRun.Location = new System.Drawing.Point(12, 576);
            this.btnContinuousRun.Margin = new System.Windows.Forms.Padding(12);
            this.btnContinuousRun.Name = "btnContinuousRun";
            this.btnContinuousRun.Size = new System.Drawing.Size(821, 166);
            this.btnContinuousRun.TabIndex = 3;
            this.btnContinuousRun.Text = "持续运行";
            this.btnContinuousRun.UseVisualStyleBackColor = true;
            this.btnContinuousRun.Click += new System.EventHandler(this.btnContinuousRun_Click);
            // 
            // statusGroup
            // 
            this.statusGroup.Controls.Add(this.txtStatus);
            this.statusGroup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.statusGroup.Location = new System.Drawing.Point(1050, 783);
            this.statusGroup.Name = "statusGroup";
            this.statusGroup.Size = new System.Drawing.Size(851, 255);
            this.statusGroup.TabIndex = 2;
            this.statusGroup.TabStop = false;
            this.statusGroup.Text = "状态";
            // 
            // txtStatus
            // 
            this.txtStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtStatus.Font = new System.Drawing.Font("宋体", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.txtStatus.Location = new System.Drawing.Point(3, 17);
            this.txtStatus.Multiline = true;
            this.txtStatus.Name = "txtStatus";
            this.txtStatus.ReadOnly = true;
            this.txtStatus.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtStatus.Size = new System.Drawing.Size(845, 235);
            this.txtStatus.TabIndex = 0;
            this.txtStatus.WordWrap = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1904, 1041);
            this.Controls.Add(this.rootLayout);
            this.MinimumSize = new System.Drawing.Size(1280, 780);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "VisionPro Demo";
            this.Load += new System.EventHandler(this.Form1_Load_1);
            this.rootLayout.ResumeLayout(false);
            this.leftLayout.ResumeLayout(false);
            this.imageGroup.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.cogDisplayImage)).EndInit();
            this.logGroup.ResumeLayout(false);
            this.logGroup.PerformLayout();
            this.buttonsGroup.ResumeLayout(false);
            this.buttonTable.ResumeLayout(false);
            this.statusGroup.ResumeLayout(false);
            this.statusGroup.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.TableLayoutPanel leftLayout;
        private System.Windows.Forms.GroupBox imageGroup;
        private Cognex.VisionPro.Display.CogDisplay cogDisplayImage;
        private System.Windows.Forms.GroupBox logGroup;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.GroupBox buttonsGroup;
        private System.Windows.Forms.TableLayoutPanel buttonTable;
        private System.Windows.Forms.Button btnDisplayLive;
        private System.Windows.Forms.Button btnCloseCamera;
        private System.Windows.Forms.Button btnSingleRun;
        private System.Windows.Forms.Button btnContinuousRun;
        private System.Windows.Forms.GroupBox statusGroup;
        private System.Windows.Forms.TextBox txtStatus;
    }
}


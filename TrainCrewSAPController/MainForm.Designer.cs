namespace TrainCrewSAPController
{
    partial class MainForm
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.TrackBar_SAPValue = new System.Windows.Forms.TrackBar();
            this.Label_SAPValue = new System.Windows.Forms.Label();
            this.TextBox_SAPValue = new System.Windows.Forms.TextBox();
            this.Button_SendSAP = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.CheckBox_TopMost = new System.Windows.Forms.CheckBox();
            this.CheckBox_SendSAPEnable = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.TrackBar_SAPValue)).BeginInit();
            this.SuspendLayout();
            // 
            // TrackBar_SAPValue
            // 
            this.TrackBar_SAPValue.BackColor = System.Drawing.Color.White;
            this.TrackBar_SAPValue.Cursor = System.Windows.Forms.Cursors.Default;
            this.TrackBar_SAPValue.Location = new System.Drawing.Point(12, 81);
            this.TrackBar_SAPValue.Name = "TrackBar_SAPValue";
            this.TrackBar_SAPValue.Size = new System.Drawing.Size(276, 45);
            this.TrackBar_SAPValue.TabIndex = 2;
            this.TrackBar_SAPValue.ValueChanged += new System.EventHandler(this.TrackBar_SAPValue_ValueChanged);
            // 
            // Label_SAPValue
            // 
            this.Label_SAPValue.BackColor = System.Drawing.SystemColors.Control;
            this.Label_SAPValue.Location = new System.Drawing.Point(12, 56);
            this.Label_SAPValue.Name = "Label_SAPValue";
            this.Label_SAPValue.Size = new System.Drawing.Size(276, 22);
            this.Label_SAPValue.TabIndex = 3;
            this.Label_SAPValue.Text = "SAP圧：0.00kPa";
            this.Label_SAPValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // TextBox_SAPValue
            // 
            this.TextBox_SAPValue.Font = new System.Drawing.Font("MS UI Gothic", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.TextBox_SAPValue.Location = new System.Drawing.Point(52, 141);
            this.TextBox_SAPValue.Name = "TextBox_SAPValue";
            this.TextBox_SAPValue.Size = new System.Drawing.Size(79, 22);
            this.TextBox_SAPValue.TabIndex = 4;
            this.TextBox_SAPValue.Text = "300.00";
            this.TextBox_SAPValue.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.TextBox_SAPValue.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TextBox_SAPValue_KeyDown);
            // 
            // Button_SendSAP
            // 
            this.Button_SendSAP.Location = new System.Drawing.Point(177, 140);
            this.Button_SendSAP.Name = "Button_SendSAP";
            this.Button_SendSAP.Size = new System.Drawing.Size(72, 25);
            this.Button_SendSAP.TabIndex = 5;
            this.Button_SendSAP.Text = "送信";
            this.Button_SendSAP.UseVisualStyleBackColor = true;
            this.Button_SendSAP.Click += new System.EventHandler(this.Button_SendSAP_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("MS UI Gothic", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.label2.Location = new System.Drawing.Point(137, 146);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(30, 15);
            this.label2.TabIndex = 6;
            this.label2.Text = "kPa";
            // 
            // CheckBox_TopMost
            // 
            this.CheckBox_TopMost.AutoSize = true;
            this.CheckBox_TopMost.Checked = true;
            this.CheckBox_TopMost.CheckState = System.Windows.Forms.CheckState.Checked;
            this.CheckBox_TopMost.Location = new System.Drawing.Point(12, 12);
            this.CheckBox_TopMost.Name = "CheckBox_TopMost";
            this.CheckBox_TopMost.Size = new System.Drawing.Size(84, 16);
            this.CheckBox_TopMost.TabIndex = 7;
            this.CheckBox_TopMost.Text = "最前面表示";
            this.CheckBox_TopMost.UseVisualStyleBackColor = true;
            this.CheckBox_TopMost.CheckedChanged += new System.EventHandler(this.CheckBox_TopMost_CheckedChanged);
            // 
            // CheckBox_SendSAPEnable
            // 
            this.CheckBox_SendSAPEnable.AutoSize = true;
            this.CheckBox_SendSAPEnable.Location = new System.Drawing.Point(102, 12);
            this.CheckBox_SendSAPEnable.Name = "CheckBox_SendSAPEnable";
            this.CheckBox_SendSAPEnable.Size = new System.Drawing.Size(106, 16);
            this.CheckBox_SendSAPEnable.TabIndex = 8;
            this.CheckBox_SendSAPEnable.Text = "SAP圧制御有効";
            this.CheckBox_SendSAPEnable.UseVisualStyleBackColor = true;
            this.CheckBox_SendSAPEnable.CheckedChanged += new System.EventHandler(this.CheckBox_SendSAPEnable_CheckedChanged);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(304, 181);
            this.Controls.Add(this.CheckBox_SendSAPEnable);
            this.Controls.Add(this.CheckBox_TopMost);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.Button_SendSAP);
            this.Controls.Add(this.TextBox_SAPValue);
            this.Controls.Add(this.Label_SAPValue);
            this.Controls.Add(this.TrackBar_SAPValue);
            this.Name = "MainForm";
            this.Text = "TrainCrewSAPController";
            this.TopMost = true;
            ((System.ComponentModel.ISupportInitialize)(this.TrackBar_SAPValue)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TrackBar TrackBar_SAPValue;
        private System.Windows.Forms.Label Label_SAPValue;
        private System.Windows.Forms.TextBox TextBox_SAPValue;
        private System.Windows.Forms.Button Button_SendSAP;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox CheckBox_TopMost;
        private System.Windows.Forms.CheckBox CheckBox_SendSAPEnable;
    }
}


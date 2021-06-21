
namespace DesktopReplacer
{
    partial class DesktopReplacerWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.browser = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.button1 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.browser)).BeginInit();
            this.SuspendLayout();
            // 
            // browser
            // 
            this.browser.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.browser.CreationProperties = null;
            this.browser.DefaultBackgroundColor = System.Drawing.Color.White;
            this.browser.Location = new System.Drawing.Point(0, 0);
            this.browser.Name = "browser";
            this.browser.Size = new System.Drawing.Size(400, 400);
            this.browser.Source = new System.Uri("about:empty", System.UriKind.Absolute);
            this.browser.TabIndex = 0;
            this.browser.ZoomFactor = 1D;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(200, 200);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(100, 30);
            this.button1.TabIndex = 1;
            this.button1.Text = "Close";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.Close_Click);
            // 
            // DesktopReplacerWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.browser);
            this.Cursor = System.Windows.Forms.Cursors.Default;
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "DesktopReplacerWindow";
            this.Text = "DesktopReplacerWindow";
            ((System.ComponentModel.ISupportInitialize)(this.browser)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Microsoft.Web.WebView2.WinForms.WebView2 browser;
        private System.Windows.Forms.Button button1;
    }
}
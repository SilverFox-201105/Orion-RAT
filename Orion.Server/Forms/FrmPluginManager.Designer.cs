using Orion.Server.Forms.DarkMode;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Orion.Server.Forms
{
    partial class FrmPluginManager
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmPluginManager));
            btnManage = new Button();
            btnRefresh = new Button();
            btnImport = new Button();
            btnBrowse = new Button();
            listView = new NonResizableListView();
            colName = new ColumnHeader();
            colVersion = new ColumnHeader();
            colStatus = new ColumnHeader();
            colType = new ColumnHeader();
            colActions = new ColumnHeader();
            rowHeightImages = new ImageList(components);
            btnClose = new Button();
            lblTitle = new Label();
            SuspendLayout();
            // 
            // btnManage
            // 
            btnManage.Location = new Point(12, 38);
            btnManage.Name = "btnManage";
            btnManage.Size = new Size(75, 23);
            btnManage.TabIndex = 0;
            btnManage.Text = "管理";
            btnManage.UseVisualStyleBackColor = true;
            // 
            // btnRefresh
            // 
            btnRefresh.Location = new Point(93, 38);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(75, 23);
            btnRefresh.TabIndex = 1;
            btnRefresh.Text = "刷新";
            btnRefresh.UseVisualStyleBackColor = true;
            // 
            // btnImport
            // 
            btnImport.Location = new Point(402, 365);
            btnImport.Name = "btnImport";
            btnImport.Size = new Size(90, 23);
            btnImport.TabIndex = 7;
            btnImport.Text = "导入插件包";
            btnImport.UseVisualStyleBackColor = true;
            // 
            // btnBrowse
            // 
            btnBrowse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowse.Location = new Point(498, 38);
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new Size(90, 23);
            btnBrowse.TabIndex = 2;
            btnBrowse.Text = "浏览插件";
            btnBrowse.UseVisualStyleBackColor = true;
            // 
            // listView
            // 
            listView.AllowDrop = true;
            listView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            // 关闭系统复选框：OwnerDraw 模式下原生 state image 与 item.Checked 不同步，
            // 会让已启用插件一直显示成未勾选。复选框改由 DrawSubItem 自绘（状态取自 PluginInfo.Enabled）。
            listView.CheckBoxes = false;
            listView.Columns.AddRange(new ColumnHeader[] { colName, colVersion, colStatus, colType, colActions });
            listView.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            listView.FullRowSelect = true;
            listView.Location = new Point(12, 70);
            listView.Name = "listView";
            listView.OwnerDraw = true;
            listView.Size = new Size(576, 288);
            listView.SmallImageList = rowHeightImages;
            listView.TabIndex = 3;
            listView.UseCompatibleStateImageBehavior = false;
            listView.View = View.Details;
            listView.ColumnClick += listView_ColumnClick;
            listView.DrawColumnHeader += listView_DrawColumnHeader;
            listView.DrawItem += listView_DrawItem;
            listView.DrawSubItem += listView_DrawSubItem;
            listView.SelectedIndexChanged += listView_SelectedIndexChanged;
            listView.DragDrop += listView_DragDrop;
            listView.DragEnter += listView_DragEnter;
            listView.DoubleClick += listView_DoubleClick;
            listView.MouseClick += listView_MouseClick;
            // 
            // colName
            // 
            colName.Text = "插件名称";
            colName.Width = 320;
            // 
            // colVersion
            // 
            colVersion.Text = "版本";
            colVersion.Width = 110;
            // 
            // colStatus
            // 
            colStatus.Text = "状态";
            colStatus.Width = 110;
            // 
            // colType
            // 
            colType.Text = "类型";
            colType.Width = 200;
            // 
            // colActions
            // 
            colActions.Text = "操作";
            colActions.TextAlign = HorizontalAlignment.Center;
            colActions.Width = 125;
            // 
            // rowHeightImages
            // 
            rowHeightImages.ColorDepth = ColorDepth.Depth32Bit;
            rowHeightImages.ImageSize = new Size(1, 24);
            rowHeightImages.TransparentColor = Color.Transparent;
            // 
            // btnClose
            // 
            btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnClose.Location = new Point(498, 365);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(90, 23);
            btnClose.TabIndex = 5;
            btnClose.Text = "关闭";
            btnClose.UseVisualStyleBackColor = true;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitle.Location = new Point(12, 12);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(100, 21);
            lblTitle.TabIndex = 6;
            lblTitle.Text = "插件管理器";
            // 
            // FrmPluginManager
            // 
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(600, 400);
            Controls.Add(lblTitle);
            Controls.Add(btnClose);
            Controls.Add(listView);
            Controls.Add(btnBrowse);
            Controls.Add(btnImport);
            Controls.Add(btnRefresh);
            Controls.Add(btnManage);
            Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(540, 360);
            Name = "FrmPluginManager";
            StartPosition = FormStartPosition.CenterParent;
            Text = "插件管理器";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.Windows.Forms.Button btnManage;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnImport;
        private System.Windows.Forms.Button btnBrowse;
        private NonResizableListView listView;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.ColumnHeader colVersion;
        private System.Windows.Forms.ColumnHeader colStatus;
        private System.Windows.Forms.ColumnHeader colType;
        private System.Windows.Forms.ColumnHeader colActions;
        private ImageList rowHeightImages;
    }
}
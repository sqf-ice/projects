﻿namespace ZZero.ZPlanner.UI.Help
{
    partial class HelpTopicsPanel
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HelpTopicsPanel));
            this.tutorialTopicsTree = new System.Windows.Forms.TreeView();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.SuspendLayout();
            // 
            // tutorialTopicsTree
            // 
            this.tutorialTopicsTree.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.tutorialTopicsTree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tutorialTopicsTree.ImageIndex = 0;
            this.tutorialTopicsTree.ImageList = this.imageList1;
            this.tutorialTopicsTree.Location = new System.Drawing.Point(0, 0);
            this.tutorialTopicsTree.Name = "tutorialTopicsTree";
            this.tutorialTopicsTree.SelectedImageIndex = 0;
            this.tutorialTopicsTree.Size = new System.Drawing.Size(473, 435);
            this.tutorialTopicsTree.TabIndex = 0;
            this.tutorialTopicsTree.AfterCollapse += new System.Windows.Forms.TreeViewEventHandler(this.tutorialTopicsTree_AfterCollapse);
            this.tutorialTopicsTree.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(this.tutorialTopicsTree_AfterExpand);
            this.tutorialTopicsTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tutorialTopicsTree_AfterSelect);
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "Collapsed");
            this.imageList1.Images.SetKeyName(1, "Expanded");
            this.imageList1.Images.SetKeyName(2, "Leaf");
            // 
            // HelpTopicsPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tutorialTopicsTree);
            this.Name = "HelpTopicsPanel";
            this.Size = new System.Drawing.Size(473, 435);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TreeView tutorialTopicsTree;
        private System.Windows.Forms.ImageList imageList1;
    }
}
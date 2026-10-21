namespace FeederCopy.App.Forms.Updates
{
    partial class UpdatesView
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Timer tmrActivity;

        private Panel pnlScroll;
        private Panel pnlCard;
        private TableLayoutPanel tlpForm;
        private Label lblUpdatesSection;
        private Label lblCurrentVersion;
        private Label lblUpdateTitle;
        private Label lblUpdateMessage;
        private ProgressBar prgDownload;
        private Label lblProgress;
        private FlowLayoutPanel flpUpdateButtons;
        private Button btnCheck;
        private Button btnUpdateNow;
        private Button btnCancelDownload;
        private Label lblSourceSection;
        private Label lblRepository;
        private TextBox txtRepository;
        private Label lblToken;
        private TextBox txtToken;
        private Label lblTokenHint;
        private FlowLayoutPanel flpSourceButtons;
        private Button btnSaveSource;
        private Label lblSourceMessage;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            tmrActivity = new System.Windows.Forms.Timer(components);
            pnlScroll = new Panel();
            pnlCard = new Panel();
            tlpForm = new TableLayoutPanel();
            lblUpdatesSection = new Label();
            lblCurrentVersion = new Label();
            lblUpdateTitle = new Label();
            lblUpdateMessage = new Label();
            prgDownload = new ProgressBar();
            lblProgress = new Label();
            flpUpdateButtons = new FlowLayoutPanel();
            btnCheck = new Button();
            btnUpdateNow = new Button();
            btnCancelDownload = new Button();
            lblSourceSection = new Label();
            lblRepository = new Label();
            txtRepository = new TextBox();
            lblToken = new Label();
            txtToken = new TextBox();
            lblTokenHint = new Label();
            flpSourceButtons = new FlowLayoutPanel();
            btnSaveSource = new Button();
            lblSourceMessage = new Label();
            pnlScroll.SuspendLayout();
            pnlCard.SuspendLayout();
            tlpForm.SuspendLayout();
            flpUpdateButtons.SuspendLayout();
            flpSourceButtons.SuspendLayout();
            SuspendLayout();

            pnlScroll.AutoScroll = true;
            pnlScroll.Controls.Add(pnlCard);
            pnlScroll.Dock = DockStyle.Fill;
            pnlScroll.Name = "pnlScroll";
            pnlScroll.Padding = new Padding(14);

            pnlCard.AutoSize = true;
            pnlCard.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlCard.Controls.Add(tlpForm);
            pnlCard.Location = new Point(14, 14);
            pnlCard.Name = "pnlCard";
            pnlCard.Padding = new Padding(22);

            tlpForm.AutoSize = true;
            tlpForm.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tlpForm.ColumnCount = 2;
            tlpForm.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpForm.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 460F));
            tlpForm.Controls.Add(lblUpdatesSection, 0, 0);
            tlpForm.Controls.Add(lblCurrentVersion, 0, 1);
            tlpForm.Controls.Add(lblUpdateTitle, 0, 2);
            tlpForm.Controls.Add(lblUpdateMessage, 0, 3);
            tlpForm.Controls.Add(prgDownload, 0, 4);
            tlpForm.Controls.Add(lblProgress, 0, 5);
            tlpForm.Controls.Add(flpUpdateButtons, 0, 6);
            tlpForm.Controls.Add(lblSourceSection, 0, 7);
            tlpForm.Controls.Add(lblRepository, 0, 8);
            tlpForm.Controls.Add(txtRepository, 1, 8);
            tlpForm.Controls.Add(lblToken, 0, 9);
            tlpForm.Controls.Add(txtToken, 1, 9);
            tlpForm.Controls.Add(lblTokenHint, 0, 10);
            tlpForm.Controls.Add(flpSourceButtons, 0, 11);
            tlpForm.Dock = DockStyle.Fill;
            tlpForm.Name = "tlpForm";
            tlpForm.RowCount = 12;
            tlpForm.SetColumnSpan(lblUpdatesSection, 2);
            tlpForm.SetColumnSpan(lblCurrentVersion, 2);
            tlpForm.SetColumnSpan(lblUpdateTitle, 2);
            tlpForm.SetColumnSpan(lblUpdateMessage, 2);
            tlpForm.SetColumnSpan(prgDownload, 2);
            tlpForm.SetColumnSpan(lblProgress, 2);
            tlpForm.SetColumnSpan(flpUpdateButtons, 2);
            tlpForm.SetColumnSpan(lblSourceSection, 2);
            tlpForm.SetColumnSpan(lblTokenHint, 2);
            tlpForm.SetColumnSpan(flpSourceButtons, 2);

            lblUpdatesSection.AutoSize = true;
            lblUpdatesSection.Margin = new Padding(0, 0, 0, 6);
            lblUpdatesSection.Name = "lblUpdatesSection";
            lblUpdatesSection.Text = "Program updates";

            lblCurrentVersion.AutoSize = true;
            lblCurrentVersion.Margin = new Padding(0, 0, 0, 12);
            lblCurrentVersion.Name = "lblCurrentVersion";

            lblUpdateTitle.AutoSize = true;
            lblUpdateTitle.Margin = new Padding(0, 0, 0, 4);
            lblUpdateTitle.Name = "lblUpdateTitle";

            lblUpdateMessage.AutoSize = true;
            lblUpdateMessage.Margin = new Padding(0, 0, 0, 10);
            lblUpdateMessage.MaximumSize = new Size(700, 0);
            lblUpdateMessage.Name = "lblUpdateMessage";

            prgDownload.Dock = DockStyle.Fill;
            prgDownload.Margin = new Padding(0, 0, 0, 4);
            prgDownload.MarqueeAnimationSpeed = 30;
            prgDownload.Name = "prgDownload";
            prgDownload.Size = new Size(700, 18);
            prgDownload.Visible = false;

            lblProgress.AutoSize = true;
            lblProgress.Margin = new Padding(0, 0, 0, 8);
            lblProgress.Name = "lblProgress";

            flpUpdateButtons.AutoSize = true;
            flpUpdateButtons.Controls.Add(btnCheck);
            flpUpdateButtons.Controls.Add(btnUpdateNow);
            flpUpdateButtons.Controls.Add(btnCancelDownload);
            flpUpdateButtons.Margin = new Padding(0);
            flpUpdateButtons.Name = "flpUpdateButtons";

            btnCheck.Name = "btnCheck";
            btnCheck.Size = new Size(180, 40);
            btnCheck.Text = "Check for updates";
            btnCheck.Click += btnCheck_Click;

            btnUpdateNow.Name = "btnUpdateNow";
            btnUpdateNow.Size = new Size(150, 40);
            btnUpdateNow.Text = "Update now";
            btnUpdateNow.Visible = false;
            btnUpdateNow.Click += btnUpdateNow_Click;

            btnCancelDownload.Name = "btnCancelDownload";
            btnCancelDownload.Size = new Size(170, 40);
            btnCancelDownload.Text = "Cancel download";
            btnCancelDownload.Visible = false;
            btnCancelDownload.Click += btnCancelDownload_Click;

            lblSourceSection.AutoSize = true;
            lblSourceSection.Margin = new Padding(0, 28, 0, 10);
            lblSourceSection.Name = "lblSourceSection";
            lblSourceSection.Text = "Update source (GitHub)";

            lblRepository.AutoSize = true;
            lblRepository.Margin = new Padding(0, 8, 18, 8);
            lblRepository.Name = "lblRepository";
            lblRepository.Text = "Repository (owner/name)";
            txtRepository.Dock = DockStyle.Fill;
            txtRepository.Name = "txtRepository";

            lblToken.AutoSize = true;
            lblToken.Margin = new Padding(0, 8, 18, 8);
            lblToken.Name = "lblToken";
            lblToken.Text = "Access token";
            txtToken.Dock = DockStyle.Fill;
            txtToken.Name = "txtToken";
            txtToken.UseSystemPasswordChar = true;

            lblTokenHint.AutoSize = true;
            lblTokenHint.Margin = new Padding(0, 4, 0, 8);
            lblTokenHint.MaximumSize = new Size(700, 0);
            lblTokenHint.Name = "lblTokenHint";
            lblTokenHint.Text = "Use a GitHub token that can only read this repository. It is stored encrypted for this Windows user. Leave the box empty to keep the saved token.";

            flpSourceButtons.AutoSize = true;
            flpSourceButtons.Controls.Add(btnSaveSource);
            flpSourceButtons.Controls.Add(lblSourceMessage);
            flpSourceButtons.Margin = new Padding(0, 6, 0, 0);
            flpSourceButtons.Name = "flpSourceButtons";

            btnSaveSource.Name = "btnSaveSource";
            btnSaveSource.Size = new Size(140, 38);
            btnSaveSource.Text = "Save";
            btnSaveSource.Click += btnSaveSource_Click;

            lblSourceMessage.AutoSize = true;
            lblSourceMessage.Margin = new Padding(12, 10, 0, 0);
            lblSourceMessage.Name = "lblSourceMessage";

            tmrActivity.Interval = 1000;
            tmrActivity.Tick += tmrActivity_Tick;

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(pnlScroll);
            Name = "UpdatesView";
            Size = new Size(1280, 720);
            pnlScroll.ResumeLayout(false);
            pnlScroll.PerformLayout();
            pnlCard.ResumeLayout(false);
            pnlCard.PerformLayout();
            tlpForm.ResumeLayout(false);
            tlpForm.PerformLayout();
            flpUpdateButtons.ResumeLayout(false);
            flpSourceButtons.ResumeLayout(false);
            flpSourceButtons.PerformLayout();
            ResumeLayout(false);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;
using Orion.Server.Forms.DarkMode;
using Orion.Server.Networking;

namespace Orion.Server.Forms
{
    public partial class FrmNickname : Form
    {
        private Label lblNickname;
        private TextBox txtNickname;
        private Button btnOk;
        private Button btnCancel;
        private readonly Client _client;
        private static readonly string OrionStuffDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OrionStuff");

        public event EventHandler NicknameSaved;

        public FrmNickname(Client client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            InitializeComponent();
            InitializeCustomComponents();
            DarkModeManager.ApplyDarkMode(this);
        }

        private void InitializeCustomComponents()
        {
            this.SuspendLayout();

            this.AutoScaleDimensions = new SizeF(96F, 96F);
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.Font = new Font("Segoe UI", 9F);

            this.lblNickname = new Label
            {
                AutoSize = true,
                Location = new Point(16, 12),
                TabIndex = 0,
                Text = "备注："
            };

            this.txtNickname = new TextBox
            {
                Location = new Point(16, 36),
                Size = new Size(388, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                TabIndex = 1
            };

            this.btnOk = new Button
            {
                Location = new Point(220, 72),
                Size = new Size(88, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                TabIndex = 2,
                Text = "确定",
                UseVisualStyleBackColor = true
            };
            this.btnOk.Click += BtnOk_Click;

            this.btnCancel = new Button
            {
                Location = new Point(316, 72),
                Size = new Size(88, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                TabIndex = 3,
                Text = "取消",
                UseVisualStyleBackColor = true
            };
            this.btnCancel.Click += BtnCancel_Click;

            this.ClientSize = new Size(420, 116);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.txtNickname);
            this.Controls.Add(this.lblNickname);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmNickname";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "备注设置";
            this.Load += FrmNickname_Load;

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNickname.Text))
            {
                ShowErrorMessage("请输入有效的备注。");
                return;
            }

            if (_client?.Value == null)
            {
                ShowErrorMessage("客户端信息不可用。");
                return;
            }

            try
            {
                string filePath = Path.Combine(OrionStuffDir, "client_info.json");
                
                if (!Directory.Exists(OrionStuffDir))
                {
                    Directory.CreateDirectory(OrionStuffDir);
                }

                SaveOrUpdateClientInfo(filePath, txtNickname.Text);

                OnNicknameSaved(EventArgs.Empty); // Trigger event  

                ShowSuccessMessage("备注保存成功！");
                this.Close(); // Close the form after saving the nickname  
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"保存备注时出错：{ex.Message}");
            }
        }

        protected virtual void OnNicknameSaved(EventArgs e)
        {
            NicknameSaved?.Invoke(this, e);
        }

        public Client GetClient()
        {
            return _client;
        }

        private void SaveOrUpdateClientInfo(string filePath, string nickname)
        {
            Dictionary<string, ClientInfo> clientInfos = new Dictionary<string, ClientInfo>();

            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        clientInfos = JsonConvert.DeserializeObject<Dictionary<string, ClientInfo>>(json) ?? new Dictionary<string, ClientInfo>();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading existing client info: {ex.Message}");
                }
            }

            string clientId = _client.Value.Id;
            clientInfos[clientId] = new ClientInfo
            {
                ClientId = clientId,
                Nickname = nickname
            };

            try
            {
                string updatedJson = JsonConvert.SerializeObject(clientInfos, Formatting.Indented);
                File.WriteAllText(filePath, updatedJson);
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"保存客户端信息失败：{ex.Message}");
                throw;
            }
        }

        private void ShowErrorMessage(string message)
        {
            MessageBox.Show(message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ShowSuccessMessage(string message)
        {
            MessageBox.Show(message, "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnCancel_Click(object sender, EventArgs e) => this.Close();
        private void FrmNickname_Load(object sender, EventArgs e) { }
    }
}

using Org.BouncyCastle.Crypto.Paddings;
using Orion.Server.Forms.DarkMode;
using Orion.Server.Helper;
using Orion.Server.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;

namespace Orion.Server.Forms
{
    public partial class FrmCertificate : Form
    {
        private X509Certificate2 _certificate;

        public FrmCertificate()
        {
            InitializeComponent();
            DarkModeManager.ApplyDarkMode(this);
        }

        private void SetCertificate(X509Certificate2 certificate)
        {
            _certificate = certificate;
            txtDetails.Text = _certificate.ToString(false);
            btnSave.Enabled = true;
        }

        private string GenerateRandomStringPair()
        {
            const string letters = "abcdefghijklmnopqrstuvwxyz";
            Random random = new Random();
            string GenerateRandomString(int length) => new string(Enumerable.Repeat(letters, length).Select(s => s[random.Next(s.Length)]).ToArray());

            string randomString1 = GenerateRandomString(random.Next(4, 7));
            string randomString2 = GenerateRandomString(random.Next(4, 7));

            return $"{randomString1} {randomString2}";
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            SetCertificate(CertificateHelper.CreateCertificateAuthority(GenerateRandomStringPair(), 4096));
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.CheckFileExists = true;
                ofd.Filter = "*.p12|*.p12";
                ofd.Multiselect = false;
                ofd.InitialDirectory = Application.StartupPath;
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        byte[] bytes = File.ReadAllBytes(ofd.FileName);
                        var cert = X509CertificateLoader.LoadPkcs12(bytes, null, X509KeyStorageFlags.Exportable);
                        SetCertificate(cert);

                        btnSave.PerformClick();

                        string importedDir = Path.GetDirectoryName(ofd.FileName);
                        string sourceOrionStuff = Path.Combine(importedDir, "OrionStuff");
                        string destOrionStuff = Path.Combine(Application.StartupPath, "OrionStuff");
                        if (Directory.Exists(sourceOrionStuff))
                        {
                            Directory.CreateDirectory(destOrionStuff);

                            foreach (string file in Directory.GetFiles(sourceOrionStuff, "*", SearchOption.AllDirectories))
                            {
                                string relativePath = file.Substring(sourceOrionStuff.Length + 1);
                                string destFile = Path.Combine(destOrionStuff, relativePath);
                                Directory.CreateDirectory(Path.GetDirectoryName(destFile));
                                File.Copy(file, destFile, true);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, $"导入证书时出错：\n{ex.Message}", "保存错误",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (_certificate == null)
                    throw new ArgumentNullException();

                if (!_certificate.HasPrivateKey)
                    throw new ArgumentException();

                File.WriteAllBytes(Settings.CertificatePath, _certificate.Export(X509ContentType.Pkcs12));

                MessageBox.Show(this,
                    "请立即备份证书。证书丢失将导致失去所有客户端的连接！",
                    "证书备份", MessageBoxButtons.OK, MessageBoxIcon.Information);

                string argument = "/select, \"" + Settings.CertificatePath + "\"";
                Process.Start("explorer.exe", argument);

                this.DialogResult = DialogResult.OK;
            }
            catch (ArgumentNullException)
            {
                MessageBox.Show(this, "请先创建或导入证书。", "保存错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (ArgumentException)
            {
                MessageBox.Show(this,
                    "导入的证书没有关联的私钥。请导入其他证书。",
                    "保存错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception)
            {
                MessageBox.Show(this,
                    "保存证书时出错，请确保您对 Orion 目录具有写入权限。",
                    "保存错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void FrmCertificate_Load(object sender, EventArgs e)
        {
            string disclaimer = "警告：本软件仅供教育和研究目的使用。\n" +
                                "未经授权在您不拥有或未获明确许可访问的计算机上使用是非法的。\n\n" +
                                "使用本软件，即表示您同意对自己的行为承担全部责任。\n\n" +
                                "您是否同意继续？";

            DialogResult result = MessageBox.Show(disclaimer, "法律免责声明",
                                                  MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.No)
            {
                // User did not agree – close the form
                this.Close();
            }
            // If Yes, the form stays open and continues loading
        }

    }
}

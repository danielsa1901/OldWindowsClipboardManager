using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using ClipboardManager;
using System.Security.Cryptography;
using System.Reflection;

namespace WindowsClipboardManager
{
    public partial class Form1 : Form
    {
        #region classMembers
        // Have the program in the system tray so that the main window can stay closed until user decides to open it
        private NotifyIcon trayIcon;
        private ContextMenu trayMenu;

        // Will look into get external DLL's to work with callbacks whenever the clipboard is changed, instead of polling
        private Timer _clipboardTimer;

        // Keep a list of clipboard history
        private List<ClipboardEntry> clipboardHistory = new List<ClipboardEntry>();
        FlowLayoutPanel historyPanel;

        // DLL imports, used to register the Win+V keyboard shortcut
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 1; // Unique ID for the hotkey
        private const uint MOD_WIN = 0x0008; // Windows key modifier
        private const uint VK_V = 0x56; // Virtual key code for 'V'

        #endregion

        public Form1()
        {
            //Load icon
            this.Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

            // Register the Win+V hotkey
            if (!RegisterHotKey(this.Handle, HOTKEY_ID, MOD_WIN, VK_V))
            {
                MessageBox.Show("Failed to register hotkey. It might already be in use.");
            }

            InitializeComponent();

            // Set up the system tray icon and context menu
            trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Open", OnOpen);
            trayMenu.MenuItems.Add("Exit", OnExit);

            trayIcon = new NotifyIcon()
            {
                Text = "Clipboard Monitor",
                Icon = this.Icon,
                ContextMenu = trayMenu,
                Visible = true
            };

            // Subscribe to the MouseClick event, clicking the sys. tray icon opens the window
            trayIcon.MouseClick += trayIcon_MouseClick;

            // Set up the FlowLayoutPanel for clipboard history
            historyPanel = new FlowLayoutPanel();
            historyPanel.Dock = DockStyle.Fill;
            historyPanel.AutoScroll = true;
            historyPanel.WrapContents = true;
            Controls.Add(historyPanel);

            // Set up the Timer to check clipboard every 1 second (1000 ms), hope to replace this
            _clipboardTimer = new Timer();
            _clipboardTimer.Interval = 1000;  // 1 second interval
            _clipboardTimer.Tick += ClipboardTimer_Tick;
            _clipboardTimer.Start();
        }

        ~Form1()
        {
            UnregisterHotKey(this.Handle, HOTKEY_ID);
        }

        // Timer Tick event to check clipboard content
        private void ClipboardTimer_Tick(object sender, EventArgs e)
        {
            bool changeOccured = false;

            // Handle text and image data
            if (Clipboard.ContainsText())
            {
                string currentText = Clipboard.GetText();

                if (currentText != null)
                {
                    // Check if the list is empty or the newest entry isn't the same text
                    if (clipboardHistory.Count == 0 || clipboardHistory[0].TextData != currentText)
                    {
                        // Add a new text entry
                        ClipboardEntry textEntry = new ClipboardEntry(currentText);
                        clipboardHistory.Insert(0, textEntry);
                        changeOccured = true;
                    }
                }
            }
            else if (Clipboard.ContainsImage())
            {
                Image currentImage = Clipboard.GetImage();

                if (currentImage != null)
                {
                    // Check if the list is empty or the newest entry isn't the same image
                    if (clipboardHistory.Count == 0 || !AreImagesEqual(clipboardHistory[0].OriginalImage, currentImage))
                    {
                        // Add a new image entry (with a scaled-down preview)
                        ClipboardEntry imageEntry = new ClipboardEntry(currentImage, 70); // Scale preview to 70px tall
                        clipboardHistory.Insert(0, imageEntry);

                        // Limit history size to 20 entries
                        if (clipboardHistory.Count > 20)
                            clipboardHistory.RemoveAt(20);

                        changeOccured = true;
                    }
                }
            }

            // Limit history size to 20 entries
            if (clipboardHistory.Count > 20)
            {
                ClipboardEntry entryToRemove = clipboardHistory[20];
                entryToRemove.Dispose();
                clipboardHistory.RemoveAt(20);
                changeOccured = true;
            }

            if (changeOccured)
            {
                // Update the display panel
                UpdateHistoryPanel();
            }
        }

        // Update the UI with new history
        private void UpdateHistoryPanel()
        {
            historyPanel.Controls.Clear();  // Clear the current controls

            // For each clipboard entry, create a new control (TextBox, PictureBox, etc.)
            foreach (var entry in clipboardHistory)
            {
                Label textLabel = null;
                PictureBox imageBox = null;

                // Create a new Label control for each clipboard entry
                Label label = new Label
                {
                    AutoSize = false,
                    Height = 70,  // Fixed height
                    Width = historyPanel.Width,
                    Padding = new Padding(10),
                    Margin = new Padding(0, 0, 0, 10),
                    BackColor = Color.White,
                    Cursor = Cursors.Hand
                };

                if (entry.IsImage)
                {
                    // Create the PictureBox to show the preview image
                    imageBox = new PictureBox
                    {
                        Image = entry.PreviewImage,  // Use the scaled-down preview image
                        SizeMode = PictureBoxSizeMode.Normal,  // Maintain aspect ratio while fitting in the box
                        Height = 70,  // Fixed height for image preview
                        Width = 70,  // Fixed width for image preview
                        Margin = new Padding(0, 0, 10, 0),
                        BackColor = Color.White,
                        Cursor = Cursors.Hand
                    };

                    // Add the PictureBox to the label
                    label.Controls.Add(imageBox);
                }

                else if (entry.IsText)
                {
                    textLabel = new Label
                    {
                        Text = entry.TextData,
                        AutoSize = true,  // Auto-size based on text content
                        Padding = new Padding(10, 0, 0, 0),
                        TextAlign = ContentAlignment.MiddleLeft,
                        Margin = new Padding(10, 0, 0, 0),
                        BackColor = Color.White,
                        Cursor = Cursors.Hand
                    };

                    label.Controls.Add(textLabel);  // Add text label to the container label
                }

                // Add the label (which contains the image or text) to the panel
                historyPanel.Controls.Add(label);

                // Add functionality for mouse hover and click events
                label.MouseEnter += (sender, e) =>
                {
                    label.BackColor = Color.LightGray;  // Change color on hover
                    label.BorderStyle = BorderStyle.FixedSingle;  // Add border on hover

                    //change the text label as well
                    if (textLabel != null)
                    {
                        textLabel.BackColor = Color.LightGray;
                    }
                };

                label.MouseLeave += (sender, e) =>
                {
                    label.BackColor = Color.White;  // Reset color on mouse leave
                    label.BorderStyle = BorderStyle.None;  // Remove border on mouse leave

                    //change the text label as well
                    if (textLabel != null)
                    {
                        textLabel.BackColor = Color.White;
                    }
                };

                // Handle the click event to copy to clipboard
                label.Click += (sender, e) =>
                {
                    if (entry.IsText)
                    {
                        Clipboard.SetText(entry.TextData);
                        MessageBox.Show($"Copied text: {entry.TextData}");
                    }
                    else if (entry.IsImage)
                    {
                        Clipboard.SetImage(entry.OriginalImage);
                        MessageBox.Show("Copied image to clipboard");
                    }
                };

                // We need to also add the same events if the mouse hovers over the image specifically
                if (imageBox != null)
                {
                    imageBox.MouseEnter += (sender, e) =>
                    {
                        label.BackColor = Color.LightGray;  // Change color on hover
                        label.BorderStyle = BorderStyle.FixedSingle;  // Add border on hover
                    };
                    imageBox.MouseLeave += (sender, e) =>
                    {
                        label.BackColor = Color.White;  // Reset color on mouse leave
                        label.BorderStyle = BorderStyle.None;  // Remove border on mouse leave
                    };
                    imageBox.Click += (sender, e) =>
                    {
                        Clipboard.SetImage(entry.OriginalImage);
                        MessageBox.Show("Copied image to clipboard");
                    };
                }
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;

            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                // Show the main window when the hotkey is pressed
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.BringToFront();
                this.Focus();
            }

            base.WndProc(ref m);
        }

        #region CompareImage

        private bool AreImagesEqual(Image img1, Image img2)
        {
            if (img1 == null || img2 == null)
                return false;

            string hash1 = GetImageHash(img1);
            string hash2 = GetImageHash(img2);

            return hash1 == hash2;
        }

        private string GetImageHash(Image img)
        {
            using (var ms = new System.IO.MemoryStream())
            {
                img.Save(ms, System.Drawing.Imaging.ImageFormat.Png); // Save as PNG to memory
                byte[] imgBytes = ms.ToArray();

                using (var md5 = MD5.Create())
                {
                    byte[] hashBytes = md5.ComputeHash(imgBytes);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        #endregion 

        #region WindowClosing
        // Clean up when the form is closed
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            trayIcon.Visible = false; // Hide tray icon
            base.OnFormClosed(e);
        }

        // Handle form closing (hides the window instead of exiting)
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Prevent the form from closing, just hide it
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            base.OnFormClosing(e);
        }
        #endregion

        #region SysTray
        private void trayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Show the form when the tray icon is clicked
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.Activate();
            }
        }

        private void OnOpen(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal; // Ensure the form is not minimized
            this.Activate(); // Bring the window to the front
        }

        // Method to handle the "Exit" menu item click
        private void OnExit(object sender, EventArgs e)
        {
            Application.Exit();
        }
        #endregion
    }
}

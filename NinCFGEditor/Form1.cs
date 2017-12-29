﻿using Be.Windows.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NinCFGEditor {
    public partial class Form1 : Form {
        private HexBox _hexBox;
        private Dictionary<NinCFGFlags, CheckBox> _checkboxes;

        private NIN_CFG _originalData;
        private NIN_CFG _workingData;

        private readonly Dictionary<NinCFGFlags, string> _flagDescriptions = new Dictionary<NinCFGFlags, string> {
            [NinCFGFlags.Memory_Card_Emulation] = @"Emulates a memory card in Slot A using a .raw file.
Disable this option if you want to use a real memory card on an original Wii.",
            [NinCFGFlags.Force_Widescreen] = @"Patch games to use a 16:9 aspect ratio. (widescreen)
Not all games support this option. The patches will not be applied to games that have built-in support for 16:9; use the game's options screen to configure the display mode.",
            [NinCFGFlags.Force_Progressive_Scan] = @"Patch games to always use 480p (progressive scan) output.
Requires component cables, or an HDMI cable on Wii U.",
            [NinCFGFlags.Unlock_Read_Speed] = @"Disc read speed is normally limited to the performance of the original GameCube disc drive.
Unlocking read speed can allow for faster load times, but it can cause problems with games that are extremely sensitive to disc read timing.",
            [NinCFGFlags.Wii_U_Widescreen] = @"On Wii U, Nintendont sets the display to 4:3, which results in bars on the sides of the screen. If playing a game that supports widescreen, enable this option to set the display back to 16:9.
This option has no effect on original Wii systems.",
            [NinCFGFlags.Drive_Access_LED] = @"Use the drive slot LED as a disk activity indicator.
The LED will be turned on when reading from or writing to the storage device.
This option has no effect on Wii U, since the Wii U does not have a drive slot LED.",
            [NinCFGFlags.Memory_Card_Multi] = @"Nintendont usually uses one emulated memory card image per game.
Enabling MULTI switches this to use one memory card image for all USA and PAL games, and one image for all JPN games.",
            [NinCFGFlags.Native_Control] = @"Native Control allows use of GBA link cables on original Wii systems.
NOTE: Enabling Native Control will disable Bluetooth and USB HID controllers.
This option is not available on Wii U, since it does not have built-in GameCube controller ports.",
            [NinCFGFlags.Triforce_Arcade_Mode] = @"Arcade Mode re-enables thecoin slot functionality ofTriforce games.
To insert a coin, move the C stick in any direction.",
            [NinCFGFlags.Classic_Controller_Rumble] = @"Enable rumble on Wii Remotes when using the Wii Classic Controller or Wii Classic Controller Pro.",
            [NinCFGFlags.Skip_IPL] = @"Skip loading the GameCube IPL, even if it's present on the storage device."
        };

        public Form1() {
            InitializeComponent();

            _hexBox = new HexBox() {
                Dock = DockStyle.Fill,
            };
            _hexBox.Font = new Font(_hexBox.Font.FontFamily, 8);
            pnlHex.Controls.Add(_hexBox);

            _checkboxes = new Dictionary<NinCFGFlags, CheckBox>();
                
            foreach (object o in Enum.GetValues(typeof(NinCFGFlags))) {
                NinCFGFlags v = (NinCFGFlags)o;
                
                CheckBox box = new CheckBox {
                    Text = v.ToString().Replace('_', ' '),
                    AutoSize = true
                };
                box.CheckedChanged += (sender, e) => {
                    _workingData.Flags &= ~v;
                    if (box.Checked) _workingData.Flags |= v;
                    UpdateHexBox();
                };
                flowLayoutPanel1.Controls.Add(box);
                _checkboxes.Add(v, box);

                if (_flagDescriptions.TryGetValue(v, out string desc)) {
                    flowLayoutPanel1.Controls.Add(new Label {
                        Text = desc,
                        Padding = new Padding(16, 0, 0, 4),
                        AutoSize = true
                    });
                }
            }
            
            _originalData = NIN_CFG.Default;
            Populate();
        }

        public void Populate() {
            _workingData = _originalData;
            UpdateHexBox();

            foreach (var pair in _checkboxes) {
                pair.Value.Checked = _workingData.Flags.HasFlag(pair.Key);
            }

            ddlVideoMode.Items.Clear();
            foreach (object o in Enum.GetValues(typeof(NinCFGVideoModeHigh))) {
                ddlVideoMode.Items.Add(o);
                if ((NinCFGVideoModeHigh)o == _workingData.VideoModeHigh) {
                    ddlVideoMode.SelectedIndex = ddlVideoMode.Items.Count - 1;
                }
            }
            if (ddlVideoMode.SelectedIndex == -1) {
                ddlVideoMode.Items.Add(_workingData.VideoModeHigh);
                ddlVideoMode.SelectedIndex = ddlVideoMode.Items.Count - 1;
            }

            ddlForceVideoMode.Items.Clear();
            foreach (NinCFGVideoModeLow o in new[] {
                NinCFGVideoModeLow.PAL50,
                NinCFGVideoModeLow.PAL60,
                NinCFGVideoModeLow.NTSC,
                NinCFGVideoModeLow.MPAL
            }) {
                ddlForceVideoMode.Items.Add(o);
                if (o == _workingData.ForcedVideoMode) {
                    ddlForceVideoMode.SelectedIndex = ddlForceVideoMode.Items.Count - 1;
                }
            }
            if (ddlForceVideoMode.SelectedIndex == -1) {
                ddlForceVideoMode.Items.Add(_workingData.ForcedVideoMode);
                ddlForceVideoMode.SelectedIndex = ddlForceVideoMode.Items.Count - 1;
            }

            chkPatchPAL50.Checked = _workingData.PatchPAL50;

            ddlLanguage.Items.Clear();
            foreach (object o in Enum.GetValues(typeof(NinCFGLanguage))) {
                ddlLanguage.Items.Add(o);
                if ((NinCFGLanguage)o == _workingData.Language) {
                    ddlLanguage.SelectedIndex = ddlLanguage.Items.Count - 1;
                }
            }

            txtGamePath.Text = _workingData.GamePath;
            txtCheatPath.Text = _workingData.CheatPath;
            numMaxPads.Value = _workingData.MaxPads;
            textBox1.Text = _workingData.GameID;

            ddlMemCardBlocks.Items.Clear();
            foreach (int size in new[] { 59, 251, _workingData.MemoryCardBlocks }.Distinct()) {
                ddlMemCardBlocks.Items.Add(size);
                if (size == _workingData.MemoryCardBlocks) {
                    ddlMemCardBlocks.SelectedIndex = ddlMemCardBlocks.Items.Count - 1;
                }
            }

            if (_workingData.VideoScale == 0) {
                chkVideoWidthAuto.Checked = true;
                numVideoScale.Value = 640;
            } else {
                chkVideoWidthAuto.Checked = false;
                numVideoScale.Value = _workingData.VideoScale + 600;
            }

            numVideoOffset.Value = _workingData.VideoOffset;
        }

        private void UpdateHexBox() {
            _hexBox.ByteProvider = new ReadOnlyByteProvider(_workingData.GetBytes());
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e) {
            _originalData = NIN_CFG.Default;
            Populate();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e) {
            using (var dialog = new OpenFileDialog()) {
                dialog.DefaultExt = "bin";
                if (dialog.ShowDialog(this) == DialogResult.OK) {
                    _originalData = NIN_CFG.Read(File.ReadAllBytes(dialog.FileName));
                    Populate();
                }
            }
        }

        private bool saveAs() {
            using (var dialog = new SaveFileDialog()) {
                dialog.DefaultExt = "bin";
                if (dialog.ShowDialog(this) == DialogResult.OK) {
                    File.WriteAllBytes(dialog.FileName, _workingData.GetBytes());
                    return true;
                }
            }
            return false;
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e) {
            saveAs();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
            Close();
        }

        private void ddlVideoMode_SelectedIndexChanged(object sender, EventArgs e) {
            _workingData.VideoModeHigh = (NinCFGVideoModeHigh)ddlVideoMode.SelectedItem;
            UpdateHexBox();
        }

        private void ddlForceVideoMode_SelectedIndexChanged(object sender, EventArgs e) {
            _workingData.ForcedVideoMode = (NinCFGVideoModeLow)ddlForceVideoMode.SelectedItem;
            UpdateHexBox();
        }

        private void chkPatchPAL50_CheckedChanged(object sender, EventArgs e) {
            _workingData.PatchPAL50 = chkPatchPAL50.Checked;
            UpdateHexBox();
        }

        private void ddlLanguage_SelectedIndexChanged(object sender, EventArgs e) {
            _workingData.Language = (NinCFGLanguage)ddlLanguage.SelectedItem;
            UpdateHexBox();
        }

        private void txtGamePath_TextChanged(object sender, EventArgs e) {
            _workingData.GamePath = txtGamePath.Text;
            UpdateHexBox();
        }

        private void txtCheatPath_TextChanged(object sender, EventArgs e) {
            _workingData.CheatPath = txtCheatPath.Text;
            UpdateHexBox();
        }

        private void numMaxPads_ValueChanged(object sender, EventArgs e) {
            _workingData.MaxPads = (uint)numMaxPads.Value;
            UpdateHexBox();
        }

        private void textBox1_TextChanged(object sender, EventArgs e) {
            _workingData.GameID = textBox1.Text;
            lblGameIDAuto.Visible = false;
            UpdateHexBox();
        }

        private void ddlMemCardBlocks_SelectedIndexChanged(object sender, EventArgs e) {
            _workingData.MemoryCardBlocks = (int)ddlMemCardBlocks.SelectedItem;
            UpdateHexBox();
        }

        private void numVideoScale_ValueChanged(object sender, EventArgs e) {
            if (chkVideoWidthAuto.Checked) {
                _workingData.VideoScale = 0;
            } else {
                _workingData.VideoScale = (sbyte)(numVideoScale.Value - 600);
            }
            UpdateHexBox();
        }

        private void chkVideoWidthAuto_CheckedChanged(object sender, EventArgs e) {
            if (chkVideoWidthAuto.Checked) {
                _workingData.VideoScale = 0;
                numVideoScale.Enabled = false;
            } else {
                _workingData.VideoScale = (sbyte)(numVideoScale.Value - 600);
                numVideoScale.Enabled = true;
            }
            UpdateHexBox();
        }

        private void numVideoOffset_ValueChanged(object sender, EventArgs e) {
            _workingData.VideoOffset = (sbyte)numVideoOffset.Value;
            UpdateHexBox();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            if (!_originalData.Equals(_workingData)) {
                switch (MessageBox.Show(this, "Would you like to save your changes?", Text, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question)) {
                    case DialogResult.Yes:
                        if (!saveAs()) e.Cancel = true;
                        break;
                    case DialogResult.Cancel:
                        e.Cancel = true;
                        break;
                }
            }
        }

        private void btnGamePathBrowse_Click(object sender, EventArgs e) {
            using (var dialog = new OpenFileDialog()) {
                dialog.Filter = "GameCube disc images (*.iso, *.gcm)|*.iso;*.gcm";
                if (dialog.ShowDialog(this) == DialogResult.OK) {
                    string path = dialog.FileName.Replace('\\', '/');
                    while (path.Length > 0 && path[0] != '/') {
                        path = path.Substring(1);
                    }
                    txtGamePath.Text = path;

                    // Read ID from disc image
                    ReadGameId();
                }
            }
        }

        private void btnCheatPathBrowse_Click(object sender, EventArgs e) {

        }

        private Task<string> FindISOPath() {
            return Task.Run(() => {
                string relativePath = txtGamePath.Text;
                while (relativePath.Length > 0 && relativePath[0] == '/') {
                    relativePath = relativePath.Substring(1);
                }
                foreach (var driveInfo in DriveInfo.GetDrives()) {
                    string p = Path.Combine(driveInfo.RootDirectory.FullName, relativePath);
                    try {
                        if (File.Exists(p)) return p;
                    } catch (Exception ex) {
                        Console.Error.WriteLine(ex.Message);
                        Console.Error.WriteLine(ex.StackTrace);
                    }
                }
                return null;
            });
        }

        private async void ReadGameId() {
            string path = await FindISOPath();
            byte[] buffer = new byte[4];
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                fs.Read(buffer, 0, 4);
            }
            char[] characters = buffer.Select(b => (char)b).ToArray();
            if (characters.All(c => char.IsLetterOrDigit(c))) {
                textBox1.Text = new string(characters);
                lblGameIDAuto.Visible = true;
            }
        }
    }
}

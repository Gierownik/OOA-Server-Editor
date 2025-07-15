using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using static Server_GUI_concept.Form1;

namespace Server_GUI_concept
{
    public partial class SelectionForm : Form
    {
        public static class Restrictions
        {
            public static bool VersatileMode = false;
            public static bool HidePrimaries = false;
            public static bool LockBackupToHeavy = false;
            public static bool ShowHeavyWeapons = false;
            public static HashSet<int> HeavyBackupIDs = new() { 16, 15, 20 }; // Blackout, Umibozu, Holepunch 

            public static HashSet<int> HiddenAmmo = new();      
            public static HashSet<int> HiddenDevices = new();   

            public static bool GrenadeOnly = false;
            public static HashSet<int> AllowedAmmoOverride = new(); 

            public static void Reset()
            {
                VersatileMode = false;
                HidePrimaries = false;
                ShowHeavyWeapons = false;
                LockBackupToHeavy = false;
                HiddenAmmo.Clear();
                HiddenDevices.Clear();
                GrenadeOnly = false;
                AllowedAmmoOverride.Clear();
            }
        }
        public static void SetVersatileMode(bool enabled) => Restrictions.VersatileMode = enabled;

        public static void SetPrimaryHidden(bool hidden) => Restrictions.HidePrimaries = hidden;

        public static void SetBackupOverride(int[] allowedIDs, bool locked)
        {
            Restrictions.HeavyBackupIDs = allowedIDs.ToHashSet();
            Restrictions.LockBackupToHeavy = locked;
        }

        public static void ClearBackupOverride()
        {
            Restrictions.LockBackupToHeavy = false;
            Restrictions.HeavyBackupIDs.Clear();
        }

        public static void EnableHiddenAmmo(int[] ids)
        {
            foreach (int id in ids)
                Restrictions.HiddenAmmo.Add(id);
        }

        public static void EnableHiddenDevices(int[] ids)
        {
            foreach (int id in ids)
                Restrictions.HiddenDevices.Add(id);
        }

        public static void SetAmmoOverride(int[] allowedIDs)
        {
            Restrictions.GrenadeOnly = true;
            Restrictions.AllowedAmmoOverride = allowedIDs.ToHashSet();
        }

        public static void ClearAmmoOverride()
        {
            Restrictions.GrenadeOnly = false;
            Restrictions.AllowedAmmoOverride.Clear();
        }

        private readonly string slot;

        public SelectionForm(string type, Action<string> onSelect, string slot = null)
        {
            this.slot = slot;
            Text = $"Select {type}";
            StartPosition = FormStartPosition.CenterParent;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(10);

            var items = IDDatabase.GetItems(type);
            if (items == null || items.Count == 0)
            {
                MessageBox.Show($"No items found for type: {type}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Close();
                return;
            }

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(5),
            };

            int col = 0, row = 0;

            foreach (var item in items)
            {
                int id = item.Value;
                string name = item.Key;

                // 🔍 Apply restrictions based on type
                if (type == "Weapons")
                {
                    if (Restrictions.HidePrimaries && Form1.PrimaryWeaponIDs.Contains(id))
                        continue;

                    
                    if (!Restrictions.ShowHeavyWeapons && Restrictions.HeavyBackupIDs.Contains(id))
                    {
                        continue;
                    }

                    
                    if (Restrictions.LockBackupToHeavy && slot == "Backup" && !Restrictions.HeavyBackupIDs.Contains(id))
                    {
                        continue;
                    }


                    if (!Restrictions.VersatileMode && !IsAllowedInSlot(id, this.Text))
                        continue;
                }
                else if (type == "Ammo")
                {
                    if (Restrictions.GrenadeOnly && !Restrictions.AllowedAmmoOverride.Contains(id))
                        continue;

                    if (!Restrictions.GrenadeOnly && !Form1.DefaultVisibleAmmo.Contains(id) && !Restrictions.HiddenAmmo.Contains(id))
                        continue;
                }
                else if (type == "Devices")
                {
                    if (!Form1.DefaultVisibleDevices.Contains(id) && !Restrictions.HiddenDevices.Contains(id))
                        continue;
                }

                var button = new Button
                {
                    Text = name,
                    AutoSize = true,
                    Margin = new Padding(6),
                    Padding = new Padding(8, 4, 8, 4),
                    Tag = id
                };

                button.Click += (s, e) =>
                {
                    onSelect?.Invoke(name);
                    Close();
                };

                layout.Controls.Add(button, col, row);
                col++;
                if (col >= layout.ColumnCount)
                {
                    col = 0;
                    row++;
                }
                


            }
            // Force new row before adding "None" if current row isn't empty
            if (col != 0)
            {
                col = 0;
                row++;
            }

            // Create "None" button
            var noneButton = new Button
            {
                Text = "None",
                AutoSize = true,
                Margin = new Padding(6),
                Padding = new Padding(8, 4, 8, 4),
                BackColor = Color.LightGray,
                Font = new Font(SystemFonts.DefaultFont, FontStyle.Italic)
            };

            noneButton.Click += (s, e) =>
            {
                onSelect?.Invoke(""); // Selecting "None" returns empty string (resolves to 0 on save)
                Close();
            };

            // Add to layout and span the full row
            layout.Controls.Add(noneButton, 0, row);
            layout.SetColumnSpan(noneButton, layout.ColumnCount);

            Controls.Add(layout);
        }

        private bool IsAllowedInSlot(int weaponID, string title)
        {
            if (slot == "Primary")
                return Form1.PrimaryWeaponIDs.Contains(weaponID);
            else if (slot == "Sidearm")
                return Form1.SecondaryWeaponIDs.Contains(weaponID);
            else if (slot == "Backup")
                return Form1.BackupWeaponIDs.Contains(weaponID);
            return true;
        }



    }
}

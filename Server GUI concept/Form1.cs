using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Windows.Forms;
using System.Diagnostics;
using Newtonsoft.Json;
using static Server_GUI_concept.SelectionForm;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.RegularExpressions;

namespace Server_GUI_concept
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            // Hook up sliders and textboxes
            DamageSlider.Scroll += (s, e) => SyncTextFromSlider(DamageSlider, DamageDisplay);
            CoreGenSlider.Scroll += (s, e) => SyncTextFromSlider(CoreGenSlider, CoreGenDisplay);
            CoreDurationSlider.Scroll += (s, e) => SyncTextFromSlider(CoreDurationSlider, CoreDurationDisplay);
            GravitySlider.Scroll += (s, e) => SyncTextFromSlider(GravitySlider, GravityDisplay);

            DamageDisplay.TextChanged += (s, e) => SyncSliderFromText(DamageDisplay, DamageSlider);
            CoreGenDisplay.TextChanged += (s, e) => SyncSliderFromText(CoreGenDisplay, CoreGenSlider);
            CoreDurationDisplay.TextChanged += (s, e) => SyncSliderFromText(CoreDurationDisplay, CoreDurationSlider);
            GravityDisplay.TextChanged += (s, e) => SyncSliderFromText(GravityDisplay, GravitySlider);
        }
        public static class IDDatabase
        {
            public static Dictionary<string, Dictionary<string, int>> Data;

            public static void Load(string path)
            {
                string json = File.ReadAllText(path);
                Data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, int>>>(json);
            }

            public static Dictionary<string, int> GetItems(string type)
            {
                return Data.TryGetValue(type, out var items) ? items : new Dictionary<string, int>();
            }
        }
        public static string ResolveIDToName(string type, int id)
        {
            if (!IDDatabase.Data.TryGetValue(type, out var items))
                return $"[{id}]";

            foreach (var pair in items)
            {
                if (pair.Value == id)
                    return pair.Key;
            }

            return $"[{id}]";
        } // ---------------------------------------------------------------------------------------------------------------------------- Weapon ids-------------
        public static HashSet<int> PrimaryWeaponIDs = new() {0, 7, 6, 9, 8, 10, 11, 12, 13, 14, 18 };
        public static HashSet<int> SecondaryWeaponIDs = new() {0, 2, 3, 5, 4, 19, 21};
        public static HashSet<int> BackupWeaponIDs = new() {0, 1, 17, 22, 23, 15, 16, 20};

        public static HashSet<int> DefaultVisibleAmmo = new() {0, 26, 21, 20, 19, 35};
        public static HashSet<int> DefaultVisibleDevices = new() {0, 2, 3, 4, 5, 6, 7, 11, 12, 13, 14, 15, 17};
        //------------------------------------------------------------------------------------------------------------------- important shit ^ here--------------

        private void OpenButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "INI files (*.ini)|*.ini|All files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string[] lines = File.ReadAllLines(ofd.FileName);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("ServerName=")) Name_Text.Text = line.Substring(11);
                        else if (line.StartsWith("ServerPassword=")) Password_Text.Text = line.Substring(15);
                        else if (line.StartsWith("MinPlayers=")) Players_Min.Value = int.Parse(line.Substring(11));
                        else if (line.StartsWith("MaxPlayers=")) Players_Max.Value = int.Parse(line.Substring(11));
                        else if (line.StartsWith("Blacklist=")) blacklistedSteamIDs = line.Substring(10);
                        else if (line.StartsWith("Admins=")) adminSteamIDs = line.Substring(7);
                        else if (line.StartsWith("RandomRotation=")) RandomMaps.Checked = line.Substring(16).ToLower() == "true";
                        else if (line.StartsWith("KickTimeout=")) Timeout_Minutes.Value = int.Parse(line.Substring(12));
                        else if (line.StartsWith("DamageModifier=")) DamageSlider.Value = (int)(double.Parse(line.Substring(15), CultureInfo.InvariantCulture) * 10);
                        else if (line.StartsWith("CoreDurationModifier=")) CoreDurationSlider.Value = (int)(double.Parse(line.Substring(21), CultureInfo.InvariantCulture) * 10);
                        else if (line.StartsWith("CoreGenerationModifier=")) CoreGenSlider.Value = (int)(double.Parse(line.Substring(23), CultureInfo.InvariantCulture) * 10);
                        else if (line.StartsWith("MapList="))
                        {
                            var maps = line.Substring(8).Split(',');
                            for (int i = 0; i < Map_Checkboxes.Items.Count; i++)
                                Map_Checkboxes.SetItemChecked(i, maps.Contains(Map_Checkboxes.Items[i].ToString()));
                        }
                        else if (line.StartsWith("ModeList="))
                        {
                            var modes = line.Substring(9).Split(',');
                            for (int i = 0; i < Mode_Checkboxes.Items.Count; i++)
                                Mode_Checkboxes.SetItemChecked(i, modes.Contains(Mode_Checkboxes.Items[i].ToString()));

                        }
                        for (int i = 0; i < 5; i++)
                        {
                            var loadout = new Loadout();
                            string prefix = $"Preset{i + 1}_";

                            foreach (var lin in lines)
                            {
                                if (lin.StartsWith($"{prefix}Name="))
                                    loadout.Name = lin.Substring((prefix + "Name=").Length);
                                else if (lin.StartsWith($"{prefix}Shell="))
                                {
                                    if (int.TryParse(lin.Substring((prefix + "Shell=").Length), out int id))
                                        loadout.Shell = ResolveIDToName("Shells", id);
                                    else
                                        loadout.Shell = "";
                                }
                                else if (lin.StartsWith($"{prefix}Backup="))
                                {
                                    if (int.TryParse(lin.Substring((prefix + "Backup=").Length), out int id) && id != 0)
                                        loadout.Backup = ResolveIDToName("Weapons", id);
                                    else
                                        loadout.Backup = "";
                                }
                                else if (lin.StartsWith($"{prefix}Sidearm="))
                                {
                                    if (int.TryParse(lin.Substring((prefix + "Sidearm=").Length), out int id) && id != 0)
                                        loadout.Secondary = ResolveIDToName("Weapons", id);
                                    else
                                        loadout.Secondary = "";
                                }
                                else if (lin.StartsWith($"{prefix}Primary="))
                                {
                                    if (int.TryParse(lin.Substring((prefix + "Primary=").Length), out int id) && id != 0)
                                        loadout.Primary = ResolveIDToName("Weapons", id);
                                    else
                                        loadout.Primary = "";
                                }
                                else if (lin.StartsWith($"{prefix}Devices="))
                                {
                                    var deviceIDs = lin.Substring((prefix + "Devices=").Length).Split(',');
                                    loadout.Devices[0] = (deviceIDs.Length > 0 && int.TryParse(deviceIDs[0], out int d0) && d0 != 0) ? ResolveIDToName("Devices", d0) : "";
                                    loadout.Devices[1] = (deviceIDs.Length > 1 && int.TryParse(deviceIDs[1], out int d1) && d1 != 0) ? ResolveIDToName("Devices", d1) : "";
                                }
                                else if (lin.StartsWith($"{prefix}Augments="))
                                {
                                    var augIDs = lin.Substring((prefix + "Augments=").Length).Split(',');
                                    for (int j = 0; j < Math.Min(4, augIDs.Length); j++)
                                    {
                                        if (int.TryParse(augIDs[j], out int aid) && aid != 0)
                                            loadout.Augs[j] = ResolveIDToName("Augments", aid);
                                        else
                                            loadout.Augs[j] = "";
                                    }
                                }
                                else if (lin.StartsWith($"{prefix}PrimaryAttachments="))
                                {
                                    var parts = lin.Substring((prefix + "PrimaryAttachments=").Length).Split(',');
                                    //: Optic, Ammo, Mod1, Mod2, Mod3, Mod4

                                    loadout.PrimaryOptic = (parts.Length > 0 && int.TryParse(parts[0], out int opticId) && opticId != 0)
                                                            ? ResolveIDToName("Optics", opticId) : "";
                                    loadout.PrimaryAmmo = (parts.Length > 1 && int.TryParse(parts[1], out int ammoId) && ammoId != 0)
                                                          ? ResolveIDToName("Ammo", ammoId) : "";

                                    for (int j = 0; j < 4; j++)
                                    {
                                        if (parts.Length > j + 2 && int.TryParse(parts[j + 2], out int modId) && modId != 0)
                                            loadout.PrimaryMods[j] = ResolveIDToName("Mods", modId);
                                        else
                                            loadout.PrimaryMods[j] = "";
                                    }
                                }
                                else if (lin.StartsWith($"{prefix}SidearmAttachments="))
                                {
                                    var parts = lin.Substring((prefix + "SidearmAttachments=").Length).Split(',');

                                    loadout.SecondaryOptic = (parts.Length > 0 && int.TryParse(parts[0], out int opticId) && opticId != 0)
                                                              ? ResolveIDToName("Optics", opticId) : "";
                                    loadout.SecondaryAmmo = (parts.Length > 1 && int.TryParse(parts[1], out int ammoId) && ammoId != 0)
                                                            ? ResolveIDToName("Ammo", ammoId) : "";

                                    for (int j = 0; j < 4; j++)
                                    {
                                        if (parts.Length > j + 2 && int.TryParse(parts[j + 2], out int modId) && modId != 0)
                                            loadout.SecondaryMods[j] = ResolveIDToName("Mods", modId);
                                        else
                                            loadout.SecondaryMods[j] = "";
                                    }
                                }

                            }

                            loadouts[i] = loadout;  
                        }


                      
                        currentLoadout = 0;
                        LoadLoadout(currentLoadout);
                        UpdateLoadoutButtons();

                    }
                }
            }
        }

        public class Loadout
        {
            public string Name;
            public string Shell;
            public string Backup;
            public string Secondary;
            public string Primary;
            public string[] Augs = new string[4];
            public string[] Devices = new string[2];
            public string[] SecondaryMods = new string[4];
            public string[] PrimaryMods = new string[4];
            public string SecondaryOptic, PrimaryOptic;
            public string SecondaryAmmo, PrimaryAmmo;
        }
        private Loadout[] loadouts = new Loadout[5];
        private int currentLoadout = 0;



        private string blacklistedSteamIDs = "";
        private string adminSteamIDs = "";

        private void BlacklistButton_Click(object sender, EventArgs e)
        {
            using (var editor = new SteamIDEditorForm("List of Blacklisted SteamIDs, separated by ','"))
            {
                editor.SteamIDs = blacklistedSteamIDs;
                if (editor.ShowDialog() == DialogResult.OK)
                {
                    blacklistedSteamIDs = editor.SteamIDs;
                }
            }
        }

        private void AdminButton_Click(object sender, EventArgs e)
        {
            using (var editor = new SteamIDEditorForm("List of Admin SteamIDs, separated by ','"))
            {
                editor.SteamIDs = adminSteamIDs;
                if (editor.ShowDialog() == DialogResult.OK)
                {
                    adminSteamIDs = editor.SteamIDs;
                }
            }
        } 
        private void SwitchLoadout(int index)
        {
            SaveCurrentLoadout(currentLoadout);
            currentLoadout = index;
            LoadLoadout(currentLoadout);
            UpdateLoadoutButtons();
            SelectionForm.Restrictions.Reset();
            ApplyAugmentEffects(currentLoadout);

        }

        private void SaveCurrentLoadout(int index)
        {
            var l = loadouts[currentLoadout];

            l.Name = Name_Textbox.Text;
            l.Shell = Shell_Combobox.Text;
            l.Devices[0] = Device1_Button.Text;
            l.Devices[1] = Device2_Button.Text;

            l.Augs[0] = Aug1_Button.Text;
            l.Augs[1] = Aug2_Button.Text;
            l.Augs[2] = Aug3_Button.Text;
            l.Augs[3] = Aug4_Button.Text;

            l.Backup = Backup_Button.Text;
            l.Secondary = Secondary_Button.Text;
            l.Primary = Primary_Button.Text;

            l.SecondaryMods[0] = SecondaryMod1_Button.Text;
            l.SecondaryMods[1] = SecondaryMod2_Button.Text;
            l.SecondaryMods[2] = SecondaryMod3_Button.Text;
            l.SecondaryMods[3] = SecondaryMod4_Button.Text;

            l.PrimaryMods[0] = PrimaryMod1_Button.Text;
            l.PrimaryMods[1] = PrimaryMod2_Button.Text;
            l.PrimaryMods[2] = PrimaryMod3_Button.Text;
            l.PrimaryMods[3] = PrimaryMod4_Button.Text;

            l.SecondaryOptic = SecondaryOptic_Button.Text;
            l.PrimaryOptic = PrimaryOptic_Button.Text;
            l.SecondaryAmmo = SecondaryAmmo_Button.Text;
            l.PrimaryAmmo = PrimaryAmmo_Button.Text;
        }

        private void LoadLoadout(int index)
        {
            var l = loadouts[index];

            Name_Textbox.Text = l.Name;
            Shell_Combobox.Text = l.Shell;
            Device1_Button.Text = l.Devices[0];
            Device2_Button.Text = l.Devices[1];

            Aug1_Button.Text = l.Augs[0];
            Aug2_Button.Text = l.Augs[1];
            Aug3_Button.Text = l.Augs[2];
            Aug4_Button.Text = l.Augs[3];

            Backup_Button.Text = l.Backup;
            Secondary_Button.Text = l.Secondary;
            Primary_Button.Text = l.Primary;

            SecondaryMod1_Button.Text = l.SecondaryMods[0];
            SecondaryMod2_Button.Text = l.SecondaryMods[1];
            SecondaryMod3_Button.Text = l.SecondaryMods[2];
            SecondaryMod4_Button.Text = l.SecondaryMods[3];

            PrimaryMod1_Button.Text = l.PrimaryMods[0];
            PrimaryMod2_Button.Text = l.PrimaryMods[1];
            PrimaryMod3_Button.Text = l.PrimaryMods[2];
            PrimaryMod4_Button.Text = l.PrimaryMods[3];

            SecondaryOptic_Button.Text = l.SecondaryOptic;
            PrimaryOptic_Button.Text = l.PrimaryOptic;
            SecondaryAmmo_Button.Text = l.SecondaryAmmo;
            PrimaryAmmo_Button.Text = l.PrimaryAmmo;
        }

        private void UpdateLoadoutButtons()
        {
            Loadout1_Button.Enabled = currentLoadout != 0;
            Loadout2_Button.Enabled = currentLoadout != 1;
            Loadout3_Button.Enabled = currentLoadout != 2;
            Loadout4_Button.Enabled = currentLoadout != 3;
            Loadout5_Button.Enabled = currentLoadout != 4;
        }


        private void Form_Load(object sender, EventArgs e)
        {
            IDDatabase.Load("id_db.json");
            for (int i = 0; i < 5; i++)
                loadouts[i] = new Loadout();

            LoadLoadout(0);
            UpdateLoadoutButtons();
        }
        private void Loadout1_Button_Click(object sender, EventArgs e) => SwitchLoadout(0);
        private void Loadout2_Button_Click(object sender, EventArgs e) => SwitchLoadout(1);
        private void Loadout3_Button_Click(object sender, EventArgs e) => SwitchLoadout(2);
        private void Loadout4_Button_Click(object sender, EventArgs e) => SwitchLoadout(3);
        private void Loadout5_Button_Click(object sender, EventArgs e) => SwitchLoadout(4);

        //---------------------------------------------------------------------------------------- Passive validation logic

        private void EnableModSlots(string slot, int count)
        {
            if (slot == "Primary")
            {
                PrimaryMod3_Button.Enabled = count > 2;
                PrimaryMod4_Button.Enabled = count > 3;
            }
            else if (slot == "Secondary")
            {
                SecondaryMod3_Button.Enabled = count > 2;
                SecondaryMod4_Button.Enabled = count > 3;
            }
        }

        private void ApplyAugmentEffects(int loadoutIndex)
        {
            var loadout = loadouts[loadoutIndex];

            // Resolve augment IDs
            var augIDs = loadout.Augs
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(name =>
            {
                var augDict = IDDatabase.GetItems("Augments");
                return augDict != null && augDict.TryGetValue(name, out var id) ? id : -1;
            })
            .Where(id => id != -1)
            .ToHashSet();

            bool hasSpecialist = augIDs.Contains(18);
            bool hasHeavyWeapons = augIDs.Contains(19);
            bool hasVersatile = augIDs.Contains(61);
            bool hasTechnician = augIDs.Contains(23);
            bool hasExperimental = augIDs.Contains(17);
            bool hasSurplus = augIDs.Contains(25);
            bool hasProfessional = augIDs.Contains(50);

            
            if (hasSpecialist)
            {
                loadout.Devices[1] = "";
                Device2_Button.Text = "";
                Device2_Button.Enabled = false;
            }
            else
            {
                Device2_Button.Enabled = true;
            }


            if (hasHeavyWeapons)
            {
                SelectionForm.Restrictions.ShowHeavyWeapons = true;
                SelectionForm.Restrictions.LockBackupToHeavy = true;
                SelectionForm.Restrictions.HeavyBackupIDs = new HashSet<int> { 16, 15, 20 };
            }
            else
            {
                SelectionForm.Restrictions.ShowHeavyWeapons = false;
                SelectionForm.Restrictions.LockBackupToHeavy = false;
                SelectionForm.Restrictions.HeavyBackupIDs.Clear();
            }


           
            SelectionForm.SetVersatileMode(hasVersatile);

            
            if (hasTechnician)
            {
                SelectionForm.EnableHiddenAmmo(new[] { 46, 24, 23, 22, 36, 49 }); 
            }

            
            if (hasExperimental)
            {
                SelectionForm.EnableHiddenDevices(new[] { 1, 8, 9, 10, 16 }); 
            }

            // Mod slot enabling
            int modSlots = 2;
            if (hasSpecialist || hasSurplus) modSlots = hasSpecialist && hasSurplus ? 4 : 3;
            EnableModSlots("Primary", modSlots);
            EnableModSlots("Secondary", modSlots);

            
            if (hasProfessional)
            {
                SelectionForm.SetPrimaryHidden(true);
                loadout.Primary = "";
                Primary_Button.Text = "";
            }
            else
            {
                SelectionForm.SetPrimaryHidden(false);
            }

           
            int backupID = -1;
            int primaryID = (!string.IsNullOrEmpty(loadout.Primary) &&
                 IDDatabase.GetItems("Weapons").TryGetValue(loadout.Primary, out var pid))
                ? pid
                : -1;

            var weapons = IDDatabase.GetItems("Weapons");

            if (weapons != null && loadout?.Backup != null && weapons.TryGetValue(loadout.Backup, out var bid))
            {
                backupID = bid;
            }

            if (new[] { 16, 15, 20 }.Contains(backupID))
                Backup_Label.Text = "Heavy:";
            else
                Backup_Label.Text = "Backup:";

            
            if (primaryID == 12) // Warrant
            {
                SelectionForm.SetAmmoOverride(allowedIDs: new[] { 38, 39, 40, 41 }); 
            }
            else
            {
                SelectionForm.ClearAmmoOverride();
            }
        }



        //here----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void Device1_Button_Click(object sender, EventArgs e)
        {
            new SelectionForm("Devices", selected =>
            {
                loadouts[currentLoadout].Devices[0] = selected;
                Device1_Button.Text = selected;
            }).ShowDialog();
        }
        private void Device2_Button_Click(object sender, EventArgs e)
        {
            new SelectionForm("Devices", selected =>
            {
                loadouts[currentLoadout].Devices[1] = selected;
                Device2_Button.Text = selected;
            }).ShowDialog();
        }
        private void Aug1_Button_Click(object sender, EventArgs e)
        {
            new SelectionForm("Augments", selected =>
            {
                loadouts[currentLoadout].Augs[0] = selected;
                Aug1_Button.Text = selected;
                SelectionForm.Restrictions.Reset();
                ApplyAugmentEffects(currentLoadout);
            }).ShowDialog();
        }
        private void Aug2_Button_Click(object sender, EventArgs e)
        {
            new SelectionForm("Augments", selected =>
            {
                loadouts[currentLoadout].Augs[1] = selected;
                Aug2_Button.Text = selected;
                SelectionForm.Restrictions.Reset();
                ApplyAugmentEffects(currentLoadout);
            }).ShowDialog();
        }
        private void Aug3_Button_Click(object sender, EventArgs e)
        {
            new SelectionForm("Augments", selected =>
            {
                loadouts[currentLoadout].Augs[2] = selected;
                Aug3_Button.Text = selected;
                SelectionForm.Restrictions.Reset();
                ApplyAugmentEffects(currentLoadout);
            }).ShowDialog();
        }
        private void Aug4_Button_Click(object sender, EventArgs e)
        {
            new SelectionForm("Augments", selected =>
            {
                loadouts[currentLoadout].Augs[3] = selected;
                Aug4_Button.Text = selected;
                SelectionForm.Restrictions.Reset();
                ApplyAugmentEffects(currentLoadout);
            }).ShowDialog();
        }
        private void Backup_Button_Click(object sender, EventArgs e)
        {
            new SelectionForm("Weapons", selected => {
                loadouts[currentLoadout].Backup = selected;
                Backup_Button.Text = selected;
            }, slot: "Backup").ShowDialog();
            SelectionForm.Restrictions.Reset();
            ApplyAugmentEffects(currentLoadout);
        }
        private void Secondary_Button_Click(object sender, EventArgs e)
        {
            new SelectionForm("Weapons", selected => {
                loadouts[currentLoadout].Secondary = selected;
                Secondary_Button.Text = selected;
            }, slot: "Sidearm").ShowDialog();
            SelectionForm.Restrictions.Reset();
            ApplyAugmentEffects(currentLoadout);
        }
        private void Primary_Button_Click(object sender, EventArgs e)
        {
            new SelectionForm("Weapons", selected => {
                loadouts[currentLoadout].Primary = selected;
                Primary_Button.Text = selected;
            }, slot: "Primary").ShowDialog();
            SelectionForm.Restrictions.Reset();
            ApplyAugmentEffects(currentLoadout);
        }
        private void SecondaryOptic_Button_Click(object sender, EventArgs e)
        {
            new SelectionForm("Optics", selected =>
            {
                loadouts[currentLoadout].SecondaryOptic = selected;
                SecondaryOptic_Button.Text = selected;
            }).ShowDialog();
        }

        private void PrimaryOptic_Button_Click(object sender, EventArgs e)
        {
            new SelectionForm("Optics", selected =>
            {
                loadouts[currentLoadout].PrimaryOptic = selected;
                PrimaryOptic_Button.Text = selected;
            }).ShowDialog();
        }

        private void SecondaryAmmo_Button_Click(object sender, EventArgs e)
        {
            new SelectionForm("Ammo", selected =>
            {
                loadouts[currentLoadout].SecondaryAmmo = selected;
                SecondaryAmmo_Button.Text = selected;
            }).ShowDialog();
        }

        private void PrimaryAmmo_Button_Click(object sender, EventArgs e)
        {
            new SelectionForm("Ammo", selected =>
            {
                loadouts[currentLoadout].PrimaryAmmo = selected;
                PrimaryAmmo_Button.Text = selected;
            }).ShowDialog();
        }

        private void SecondaryMod1_Button_Click(object sender, EventArgs e)
        {
            new SelectionForm("Mods", selected =>
            {
                loadouts[currentLoadout].SecondaryMods[0] = selected;
                SecondaryMod1_Button.Text = selected;
            }).ShowDialog();
        }

        private void SecondaryMod2_Button_Click(object sender, EventArgs e)
        {
            new SelectionForm("Mods", selected =>
            {
                loadouts[currentLoadout].SecondaryMods[1] = selected;
                SecondaryMod2_Button.Text = selected;
            }).ShowDialog();
        }

        private void SecondaryMod3_Button_Click(object sender, EventArgs e)
        {
            new SelectionForm("Mods", selected =>
            {
                loadouts[currentLoadout].SecondaryMods[2] = selected;
                SecondaryMod3_Button.Text = selected;
            }).ShowDialog();
        }

        private void SecondaryMod4_Button_Click(object sender, EventArgs e)
        {
            new SelectionForm("Mods", selected =>
            {
                loadouts[currentLoadout].SecondaryMods[3] = selected;
                SecondaryMod4_Button.Text = selected;
            }).ShowDialog();
        }

        private void PrimaryMod1_Button_Click(object sender, EventArgs e)
        {
            new SelectionForm("Mods", selected =>
            {
                loadouts[currentLoadout].PrimaryMods[0] = selected;
                PrimaryMod1_Button.Text = selected;
            }).ShowDialog();
        }

        private void PrimaryMod2_Button_Click(object sender, EventArgs e)
        {
            new SelectionForm("Mods", selected =>
            {
                loadouts[currentLoadout].PrimaryMods[1] = selected;
                PrimaryMod2_Button.Text = selected;
            }).ShowDialog();
        }

        private void PrimaryMod3_Button_Click(object sender, EventArgs e)
        {
            new SelectionForm("Mods", selected =>
            {
                loadouts[currentLoadout].PrimaryMods[2] = selected;
                PrimaryMod3_Button.Text = selected;
            }).ShowDialog();
        }

        private void PrimaryMod4_Button_Click(object sender, EventArgs e)
        {
            new SelectionForm("Mods", selected =>
            {
                loadouts[currentLoadout].PrimaryMods[3] = selected;
                PrimaryMod4_Button.Text = selected;
            }).ShowDialog();
        }


        //here ----------------------------------------------------------------------------------------------

        private void Validate_Button_Click(object sender, EventArgs e)
        {
            string sername = Name_Text.Text;
            string password = Password_Text.Text;
            string curloadnam = Name_Textbox.Text;
         
            Regex alphanumeric = new Regex("^[a-zA-Z0-9]+$");

            if (!alphanumeric.IsMatch(sername))
            {
                MessageBox.Show("Server Name contains invalid characters, or is empty. Only a-z, A-Z, and 0-9 are allowed.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!alphanumeric.IsMatch(password) && !string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Password contains invalid characters. Only a-z, A-Z, and 0-9 are allowed.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            for (int i = 0; i < loadouts.Length; i++)
            {
                var loadout = loadouts[i];
                if (loadout != null && !string.IsNullOrEmpty(loadout.Name))
                {
                    if (!alphanumeric.IsMatch(loadout.Name) || !alphanumeric.IsMatch(curloadnam))
                    {
                        MessageBox.Show($"Loadout {i + 1} Name contains invalid characters. Only a-z, A-Z, and 0-9 are allowed.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }


            MessageBox.Show("Everything seems correct!", "Validation Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }


        private void SaveButton_Click(object sender, EventArgs e)
        {
            StringBuilder ini = new StringBuilder();

            ini.AppendLine($"ServerName={Name_Text.Text}");
            ini.AppendLine($"ServerPassword={Password_Text.Text}");
            ini.AppendLine($"MinPlayers={(int)Players_Min.Value}");
            ini.AppendLine($"MaxPlayers={(int)Players_Max.Value}");

            ini.AppendLine($"MapList={string.Join(",", Map_Checkboxes.CheckedItems.Cast<string>())}");


            ini.AppendLine($"ModeList={string.Join(",", Mode_Checkboxes.CheckedItems.Cast<string>())}");

            ini.AppendLine($"RandomRotation={(RandomMaps.Checked ? "true" : "false")}");
            ini.AppendLine($"Blacklist={blacklistedSteamIDs}");
            ini.AppendLine($"Admins={adminSteamIDs}");
            ini.AppendLine($"KickTimeout={(int)Timeout_Minutes.Value}");

            bool anyCustomPreset = false;

            for (int i = 0; i < 5; i++)
            {
                var loadout = loadouts[i];
                bool presetChanged =
                    !string.IsNullOrEmpty(loadout.Shell) ||
                    !string.IsNullOrEmpty(loadout.Backup) ||
                    !string.IsNullOrEmpty(loadout.Secondary) ||
                    !string.IsNullOrEmpty(loadout.Primary) ||
                    loadout.Augs.Any(a => !string.IsNullOrEmpty(a)) ||
                    loadout.Devices.Any(d => !string.IsNullOrEmpty(d)) ||
                    loadout.SecondaryMods.Any(m => !string.IsNullOrEmpty(m)) ||
                    loadout.PrimaryMods.Any(m => !string.IsNullOrEmpty(m)) ||
                    !string.IsNullOrEmpty(loadout.SecondaryOptic) ||
                    !string.IsNullOrEmpty(loadout.PrimaryOptic) ||
                    !string.IsNullOrEmpty(loadout.SecondaryAmmo) ||
                    !string.IsNullOrEmpty(loadout.PrimaryAmmo);

                if (presetChanged)
                {
                    anyCustomPreset = true;
                    ini.AppendLine($"Preset{i + 1}_Enabled=true");
                }
                else
                {
                    ini.AppendLine($"Preset{i + 1}_Enabled=false");
                }

                ini.AppendLine($"Preset{i + 1}_Name={(string.IsNullOrWhiteSpace(loadout.Name) ? $"Preset {i +1}" : loadout.Name)}");


                int getID(string type, string name)
                {
                    if (string.IsNullOrEmpty(name) || IDDatabase.Data == null)
                        return 0;

                    if (IDDatabase.Data.TryGetValue(type, out var dict) && dict != null && dict.TryGetValue(name, out var id))
                        return id;

                    return 0;
                }


                ini.AppendLine($"Preset{i + 1}_Shell={getID("Shells", loadout.Shell)}");
                ini.AppendLine($"Preset{i + 1}_Backup={getID("Weapons", loadout.Backup)}");
                ini.AppendLine($"Preset{i + 1}_Sidearm={getID("Weapons", loadout.Secondary)}");
                ini.AppendLine($"Preset{i + 1}_Primary={getID("Weapons", loadout.Primary)}");

                var sidearmAttachments = string.Join(",",
                    getID("Optics", loadout.SecondaryOptic),
                    getID("Ammo", loadout.SecondaryAmmo),
                    string.Join(",", loadout.SecondaryMods.Select(m => getID("Mods", m)))
                );
                ini.AppendLine($"Preset{i + 1}_SidearmAttachments={sidearmAttachments}");

                var primaryAttachments = string.Join(",",
                    getID("Optics", loadout.PrimaryOptic),
                    getID("Ammo", loadout.PrimaryAmmo),
                    string.Join(",", loadout.PrimaryMods.Select(m => getID("Mods", m)))
                );
                ini.AppendLine($"Preset{i + 1}_PrimaryAttachments={primaryAttachments}");

                ini.AppendLine($"Preset{i + 1}_Augments={string.Join(",", loadout.Augs.Select(a => getID("Augments", a)))}");
                ini.AppendLine($"Preset{i + 1}_Devices={string.Join(",", loadout.Devices.Select(d => getID("Devices", d)))}");
            }

            ini.AppendLine($"UseCustomPresets={(anyCustomPreset ? "True" : "False")}");


            bool useCustomModifiers =
                DamageSlider.Value != 10 ||
                CoreGenSlider.Value != 10 ||
                CoreDurationSlider.Value != 10;

            ini.AppendLine($"UseCustomModifiers={(useCustomModifiers ? "True" : "False")}");
            ini.AppendLine($"DamageModifier={(DamageSlider.Value / 10.0).ToString("0.0", CultureInfo.InvariantCulture)}");
            ini.AppendLine($"CoreDurationModifier={(CoreDurationSlider.Value / 10.0).ToString("0.0", CultureInfo.InvariantCulture)}");
            ini.AppendLine($"CoreGenerationModifier={(CoreGenSlider.Value / 10.0).ToString("0.0", CultureInfo.InvariantCulture)}");

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ServerSettings.ini");
            File.WriteAllText(path, ini.ToString());
            MessageBox.Show($"Saved to:\n{path}");

        }

        private void SyncSliderFromText(TextBox textBox, TrackBar slider)
        {
            string input = textBox.Text.Replace("x", "").Trim();
            if (double.TryParse(input, out double value))
            {
                value = Math.Max(0.1, Math.Min(5.0, value)); // Clamp
                slider.Value = (int)(value * 10);
            }
        }

        private void SyncTextFromSlider(TrackBar slider, TextBox textBox)
        {
            textBox.Text = (slider.Value / 10.0).ToString("0.0") + "x";
        }

    }
}

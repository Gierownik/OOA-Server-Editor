using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using static Server_GUI_concept.Form1;

namespace Server_GUI_concept
{
    public partial class SelectionForm : Form
    {
        public SelectionForm(string type, Action<string> onSelect)
        {
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
                var button = new Button
                {
                    Text = item.Key,
                    AutoSize = true,
                    Margin = new Padding(6),
                    Padding = new Padding(8, 4, 8, 4),
                    Tag = item.Value
                };

                button.Click += (s, e) =>
                {
                    onSelect?.Invoke(item.Key);
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

            Controls.Add(layout);
        }
    }
}

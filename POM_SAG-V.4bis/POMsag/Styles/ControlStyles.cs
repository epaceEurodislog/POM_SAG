using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace POMsag.Styles
{
    public static class ModernControlStyles
    {
        // Appliquer un style moderne à un bouton
        public static void ApplyModernStyle(Button button, bool isPrimary = true)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = isPrimary ? ThemeColors.AccentColor : ThemeColors.SecondaryBackground;
            button.ForeColor = isPrimary ? Color.White : ThemeColors.PrimaryText;
            button.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            button.Cursor = Cursors.Hand;
            button.FlatAppearance.MouseOverBackColor = isPrimary ? ThemeColors.AccentHover : Color.FromArgb(227, 232, 237);
        }

        // Appliquer un style moderne à un TextBox
        public static void ApplyModernStyle(TextBox textBox)
        {
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.BackColor = ThemeColors.WhiteBackground;
            textBox.ForeColor = ThemeColors.PrimaryText;
            textBox.Font = new Font("Segoe UI", 10, FontStyle.Regular);
        }

        // Appliquer un style moderne à un ComboBox
        public static void ApplyModernStyle(ComboBox comboBox)
        {
            comboBox.FlatStyle = FlatStyle.Flat;
            comboBox.BackColor = ThemeColors.WhiteBackground;
            comboBox.ForeColor = ThemeColors.PrimaryText;
            comboBox.Font = new Font("Segoe UI", 10, FontStyle.Regular);
        }

        // Appliquer un style moderne à un Panel
        public static void ApplyModernStyle(Panel panel, bool isCard = false)
        {
            panel.BackColor = isCard ? ThemeColors.WhiteBackground : ThemeColors.PrimaryBackground;
            panel.BorderStyle = BorderStyle.None;
            panel.Padding = new Padding(10);

            if (isCard)
            {
                // Effet d'ombre pour les cartes - nécessite un gestionnaire Paint
                panel.Paint += (s, e) =>
                {
                    System.Windows.Forms.ControlPaint.DrawBorder(e.Graphics, panel.ClientRectangle,
                        ThemeColors.BorderColor, ThemeColors.BorderSize, ButtonBorderStyle.Solid,
                        ThemeColors.BorderColor, ThemeColors.BorderSize, ButtonBorderStyle.Solid,
                        ThemeColors.BorderColor, ThemeColors.BorderSize, ButtonBorderStyle.Solid,
                        ThemeColors.BorderColor, ThemeColors.BorderSize, ButtonBorderStyle.Solid);
                };
            }
        }

        // Créer un titre de section
        public static Label CreateSectionTitle(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = new Font("Segoe UI", 14, FontStyle.Regular),
                ForeColor = ThemeColors.PrimaryText,
                Margin = new Padding(0, 10, 0, 15)
            };
        }
    }
}
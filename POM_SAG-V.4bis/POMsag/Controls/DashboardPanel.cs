using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using POMsag.Styles;

namespace POMsag.Controls
{
    public class DashboardPanel : Panel
    {
        private Label _titleLabel;
        private Label _valueLabel;
        private Label _infoLabel;
        private PictureBox _iconBox;

        [Category("Data")]
        [Description("Le titre du widget")]
        [DefaultValue("Titre")]
        [Browsable(true)]
        public string Title
        {
            get { return _titleLabel.Text; }
            set { _titleLabel.Text = value; }
        }

        [Category("Data")]
        [Description("La valeur principale à afficher")]
        [DefaultValue("0")]
        [Browsable(true)]
        public string Value
        {
            get { return _valueLabel.Text; }
            set { _valueLabel.Text = value; }
        }

        [Category("Data")]
        [Description("Texte d'information supplémentaire")]
        [DefaultValue("Information")]
        [Browsable(true)]
        public string InfoText
        {
            get { return _infoLabel.Text; }
            set { _infoLabel.Text = value; }
        }

        public DashboardPanel()
        {
            Size = new Size(250, 150);
            BackColor = ThemeColors.WhiteBackground;
            BorderStyle = BorderStyle.None;
            Padding = new Padding(15);

            // Ajouter une ombre subtile
            Paint += (s, e) =>
            {
                System.Windows.Forms.ControlPaint.DrawBorder(e.Graphics, ClientRectangle,
                    ThemeColors.BorderColor, 1, ButtonBorderStyle.Solid,
                    ThemeColors.BorderColor, 1, ButtonBorderStyle.Solid,
                    ThemeColors.BorderColor, 1, ButtonBorderStyle.Solid,
                    ThemeColors.BorderColor, 1, ButtonBorderStyle.Solid);
            };

            // Titre
            _titleLabel = new Label
            {
                AutoSize = false,
                Size = new Size(180, 30),
                Location = new Point(60, 15),
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = ThemeColors.SecondaryText,
                Text = "Titre"
            };

            // Valeur
            _valueLabel = new Label
            {
                AutoSize = false,
                Size = new Size(220, 40),
                Location = new Point(15, 50),
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = ThemeColors.PrimaryText,
                Text = "0"
            };

            // Info supplémentaire
            _infoLabel = new Label
            {
                AutoSize = false,
                Size = new Size(220, 25),
                Location = new Point(15, 95),
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = ThemeColors.SecondaryText,
                Text = "Information"
            };

            // Icône (à remplacer par vos propres icônes)
            _iconBox = new PictureBox
            {
                Size = new Size(32, 32),
                Location = new Point(15, 15),
                SizeMode = PictureBoxSizeMode.StretchImage
            };

            Controls.Add(_titleLabel);
            Controls.Add(_valueLabel);
            Controls.Add(_infoLabel);
            Controls.Add(_iconBox);
        }

        public void SetIcon(Image icon)
        {
            _iconBox.Image = icon;
        }
    }
}
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.ComponentModel;
using POMsag.Styles;

namespace POMsag.Controls
{
    public class RoundedButton : Button
    {
        private int _borderRadius = 15;
        private bool _isPrimary = true;

        [Category("Appearance")]
        [Description("Le rayon des coins arrondis du bouton")]
        [DefaultValue(15)]
        [Browsable(true)]
        public int BorderRadius
        {
            get { return _borderRadius; }
            set { _borderRadius = value; Invalidate(); }
        }

        [Category("Appearance")]
        [Description("Indique si le bouton a un style principal (true) ou secondaire (false)")]
        [DefaultValue(true)]
        [Browsable(true)]
        public bool IsPrimary
        {
            get { return _isPrimary; }
            set
            {
                _isPrimary = value;
                UpdateColors();
                Invalidate();
            }
        }

        public RoundedButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UpdateColors();
            Font = new Font("Segoe UI", 10, FontStyle.Regular);
            Cursor = Cursors.Hand;
        }

        private void UpdateColors()
        {
            BackColor = _isPrimary ? ThemeColors.AccentColor : ThemeColors.SecondaryBackground;
            ForeColor = _isPrimary ? Color.White : ThemeColors.PrimaryText;
            FlatAppearance.MouseOverBackColor = _isPrimary ? ThemeColors.AccentHover : Color.FromArgb(227, 232, 237);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            GraphicsPath path = new GraphicsPath();
            Rectangle rect = new Rectangle(0, 0, Width, Height);
            path.AddArc(rect.X, rect.Y, _borderRadius, _borderRadius, 180, 90);
            path.AddArc(rect.Width - _borderRadius, rect.Y, _borderRadius, _borderRadius, 270, 90);
            path.AddArc(rect.Width - _borderRadius, rect.Height - _borderRadius, _borderRadius, _borderRadius, 0, 90);
            path.AddArc(rect.X, rect.Height - _borderRadius, _borderRadius, _borderRadius, 90, 90);
            path.CloseAllFigures();

            this.Region = new Region(path);

            using (SolidBrush brush = new SolidBrush(BackColor))
            {
                e.Graphics.FillPath(brush, path);
            }

            // Dessiner le texte
            TextRenderer.DrawText(e.Graphics, Text, Font, rect, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}
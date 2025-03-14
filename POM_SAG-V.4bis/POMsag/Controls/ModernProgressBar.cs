using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.ComponentModel;
using POMsag.Styles;

namespace POMsag.Controls
{
    public class ModernProgressBar : Control
    {
        private int _value = 0;
        private int _maximum = 100;
        private Color _progressColor = ThemeColors.AccentColor;
        private Color _backColor = Color.FromArgb(230, 230, 230);

        [Category("Behavior")]
        [Description("The current value of the progress bar")]
        [DefaultValue(0)]
        [Browsable(true)]
        public int Value
        {
            get { return _value; }
            set
            {
                _value = Math.Max(0, Math.Min(value, _maximum));
                Invalidate();
                OnValueChanged(EventArgs.Empty);
            }
        }

        [Category("Behavior")]
        [Description("The maximum value of the progress bar")]
        [DefaultValue(100)]
        [Browsable(true)]
        public int Maximum
        {
            get { return _maximum; }
            set
            {
                _maximum = Math.Max(1, value);
                Invalidate();
            }
        }

        [Category("Appearance")]
        [Description("The color of the progress indicator")]
        [Browsable(true)]
        // Nous ne pouvons pas utiliser DefaultValue avec un objet Color directement
        // Nous allons donc surcharger la propriété pour sérialiser correctement la valeur
        public Color ProgressColor
        {
            get { return _progressColor; }
            set
            {
                _progressColor = value;
                Invalidate();
            }
        }

        // Ajoutez cette méthode de surcharge pour contrôler la sérialisation
        private bool ShouldSerializeProgressColor()
        {
            // Sérialiser uniquement si la couleur est différente de la valeur par défaut
            // Utiliser KnownColor.DodgerBlue comme couleur par défaut approximative qui correspond à ThemeColors.AccentColor
            return _progressColor != Color.DodgerBlue;
        }

        // Ajoutez cette méthode pour réinitialiser à la valeur par défaut
        private void ResetProgressColor()
        {
            ProgressColor = ThemeColors.AccentColor;
        }

        public event EventHandler ValueChanged;

        protected virtual void OnValueChanged(EventArgs e)
        {
            ValueChanged?.Invoke(this, e);
        }

        public ModernProgressBar()
        {
            SetStyle(System.Windows.Forms.ControlStyles.UserPaint |
                    System.Windows.Forms.ControlStyles.AllPaintingInWmPaint |
                    System.Windows.Forms.ControlStyles.OptimizedDoubleBuffer, true);
            Size = new Size(200, 10);
            base.BackColor = _backColor;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Dessiner le fond
            e.Graphics.Clear(base.BackColor);

            // Calculer la largeur de progression
            int progressWidth = (int)((float)Width * _value / _maximum);

            // Dessiner la progression avec gradient
            if (progressWidth > 0)
            {
                Rectangle progressRect = new Rectangle(0, 0, progressWidth, Height);
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    progressRect, _progressColor, Color.FromArgb(_progressColor.R + 30,
                    _progressColor.G + 30, _progressColor.B + 30), LinearGradientMode.Horizontal))
                {
                    e.Graphics.FillRectangle(brush, progressRect);
                }
            }
        }
    }
}
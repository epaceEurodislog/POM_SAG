using System;
using System.Drawing;
using System.Windows.Forms;
using POMsag.Styles;

namespace POMsag.Controls
{
    public class NotificationPanel : Panel
    {
        private Label _messageLabel;
        private Button _closeButton;
        private System.Windows.Forms.Timer _autoHideTimer;
        private Form _parentForm;

        public enum NotificationType
        {
            Success,
            Warning,
            Error,
            Info
        }

        public NotificationPanel(Form parentForm)
        {
            _parentForm = parentForm;

            // Configuration du panel
            BackColor = ThemeColors.WhiteBackground;
            BorderStyle = BorderStyle.FixedSingle;
            Size = new Size(400, 80);
            Visible = false;

            // Message
            _messageLabel = new Label
            {
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Padding = new Padding(15, 0, 45, 0),
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };
            Controls.Add(_messageLabel);

            // Bouton de fermeture
            _closeButton = new Button
            {
                Text = "×",
                Dock = DockStyle.Right,
                Width = 40,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _closeButton.Click += (s, e) => Hide();
            Controls.Add(_closeButton);

            // Timer pour auto-hide
            _autoHideTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            _autoHideTimer.Tick += (s, e) =>
            {
                _autoHideTimer.Stop();
                this.Visible = false;
            };
        }

        public void ShowNotification(string message, NotificationType type = NotificationType.Info,
                               bool autoHide = true, int duration = 5000)
        {
            // Configurer le style selon le type
            switch (type)
            {
                case NotificationType.Success:
                    BackColor = Color.FromArgb(240, 255, 240);
                    _messageLabel.ForeColor = ThemeColors.SuccessColor;
                    break;

                case NotificationType.Warning:
                    BackColor = Color.FromArgb(255, 252, 232);
                    _messageLabel.ForeColor = ThemeColors.WarningColor;
                    break;

                case NotificationType.Error:
                    BackColor = Color.FromArgb(255, 235, 235);
                    _messageLabel.ForeColor = ThemeColors.ErrorColor;
                    break;

                default: // Info
                    BackColor = Color.FromArgb(235, 245, 255);
                    _messageLabel.ForeColor = ThemeColors.AccentColor;
                    break;
            }

            // Configurer le message
            _messageLabel.Text = message;

            // Positionner le panel
            Location = new Point(
                (_parentForm.ClientSize.Width - Width) / 2,
                _parentForm.ClientSize.Height - Height - 20
            );

            // Afficher
            BringToFront();
            Visible = true;

            // Auto-hide si demandé
            if (autoHide)
            {
                _autoHideTimer.Interval = duration;
                _autoHideTimer.Start();
            }
        }
    }
}
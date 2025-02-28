// Fichier: POMsag/EndpointEditForm.cs
using System;
using System.Drawing;
using System.Windows.Forms;
using POMsag.Models;

namespace POMsag
{
    public partial class EndpointEditForm : Form
    {
        private static class ColorPalette
        {
            public static Color PrimaryBackground = Color.FromArgb(247, 250, 252);
            public static Color SecondaryBackground = Color.FromArgb(237, 242, 247);
            public static Color PrimaryText = Color.FromArgb(45, 55, 72);
            public static Color AccentColor = Color.FromArgb(49, 130, 206);
            public static Color WhiteBackground = Color.White;
        }

        private TextBox textBoxName;
        private TextBox textBoxPath;
        private ComboBox comboBoxMethod;
        private CheckBox checkBoxDateFiltering;
        private Panel dateFilteringPanel;
        private TextBox textBoxStartParamName;
        private TextBox textBoxEndParamName;
        private TextBox textBoxDateFormat;

        // Dans la classe EndpointEditForm, ajoutez l'attribut à la propriété Endpoint :
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ApiEndpoint Endpoint { get; private set; }

        public EndpointEditForm(ApiEndpoint endpoint)
        {
            InitializeComponent();

            if (endpoint != null)
            {
                // Édition d'un endpoint existant
                textBoxName.Text = endpoint.Name;
                textBoxPath.Text = endpoint.Path;

                // Sélectionner la méthode HTTP
                for (int i = 0; i < comboBoxMethod.Items.Count; i++)
                {
                    if (comboBoxMethod.Items[i].ToString() == endpoint.Method)
                    {
                        comboBoxMethod.SelectedIndex = i;
                        break;
                    }
                }

                checkBoxDateFiltering.Checked = endpoint.SupportsDateFiltering;
                textBoxStartParamName.Text = endpoint.DateStartParamName;
                textBoxEndParamName.Text = endpoint.DateEndParamName;
                textBoxDateFormat.Text = endpoint.DateFormat;

                UpdateDateFilteringPanel(endpoint.SupportsDateFiltering);

                Endpoint = endpoint;
            }
            else
            {
                // Nouvel endpoint
                Endpoint = new ApiEndpoint("", "");
                comboBoxMethod.SelectedIndex = 0; // GET par défaut
                UpdateDateFilteringPanel(false);
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Édition d'Endpoint";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ColorPalette.PrimaryBackground;
            this.Font = new Font("Segoe UI", 10);

            // Section principale
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };

            // Formulaire
            var formPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 350,
                BackColor = ColorPalette.WhiteBackground,
                Padding = new Padding(20)
            };

            // Champs du formulaire
            var labelName = new Label { Text = "Nom :", AutoSize = true };
            textBoxName = new TextBox { Width = 520, Margin = new Padding(0, 0, 0, 10) };

            var labelPath = new Label { Text = "Chemin :", AutoSize = true };
            textBoxPath = new TextBox { Width = 520, Margin = new Padding(0, 0, 0, 10) };

            var labelMethod = new Label { Text = "Méthode HTTP :", AutoSize = true };
            comboBoxMethod = new ComboBox
            {
                Width = 520,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 0, 0, 10)
            };

            // Remplir les méthodes HTTP
            comboBoxMethod.Items.AddRange(new object[] { "GET", "POST", "PUT", "DELETE" });

            checkBoxDateFiltering = new CheckBox
            {
                Text = "Supporte le filtrage par date",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10)
            };

            checkBoxDateFiltering.CheckedChanged += CheckBoxDateFiltering_CheckedChanged;

            dateFilteringPanel = new Panel
            {
                Width = 520,
                Height = 150,
                Margin = new Padding(0, 10, 0, 0)
            };

            var labelStartParamName = new Label { Text = "Nom du paramètre de date de début :", AutoSize = true };
            textBoxStartParamName = new TextBox
            {
                Width = 500,
                Margin = new Padding(0, 0, 0, 10)
            };

            var labelEndParamName = new Label { Text = "Nom du paramètre de date de fin :", AutoSize = true };
            textBoxEndParamName = new TextBox
            {
                Width = 500,
                Margin = new Padding(0, 0, 0, 10)
            };

            var labelDateFormat = new Label { Text = "Format de date :", AutoSize = true };
            textBoxDateFormat = new TextBox
            {
                Width = 500,
                Text = "yyyyMMdd" // Format par défaut
            };

            dateFilteringPanel.Controls.AddRange(new Control[]
            {
                labelStartParamName, textBoxStartParamName,
                labelEndParamName, textBoxEndParamName,
                labelDateFormat, textBoxDateFormat
            });

            // Boutons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 10, 0, 0)
            };

            var buttonCancel = new Button
            {
                Text = "Annuler",
                Width = 100,
                Height = 35,
                Margin = new Padding(10, 0, 0, 0),
                DialogResult = DialogResult.Cancel
            };

            var buttonSave = new Button
            {
                Text = "Enregistrer",
                Width = 100,
                Height = 35,
                BackColor = ColorPalette.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            buttonSave.FlatAppearance.BorderSize = 0;

            buttonSave.Click += ButtonSave_Click;

            // Assembler les éléments
            buttonPanel.Controls.Add(buttonCancel);
            buttonPanel.Controls.Add(buttonSave);

            var flowLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true
            };

            flowLayout.Controls.AddRange(new Control[]
            {
                labelName, textBoxName,
                labelPath, textBoxPath,
                labelMethod, comboBoxMethod,
                checkBoxDateFiltering,
                dateFilteringPanel
            });

            formPanel.Controls.Add(flowLayout);

            mainPanel.Controls.Add(formPanel);
            mainPanel.Controls.Add(buttonPanel);

            this.Controls.Add(mainPanel);

            this.AcceptButton = buttonSave;
            this.CancelButton = buttonCancel;
        }

        private void CheckBoxDateFiltering_CheckedChanged(object sender, EventArgs e)
        {
            UpdateDateFilteringPanel(checkBoxDateFiltering.Checked);
        }

        private void UpdateDateFilteringPanel(bool enabled)
        {
            dateFilteringPanel.Visible = enabled;
            foreach (Control control in dateFilteringPanel.Controls)
            {
                control.Enabled = enabled;
            }
        }

        private void ButtonSave_Click(object sender, EventArgs e)
        {
            // Valider les champs obligatoires
            if (string.IsNullOrWhiteSpace(textBoxName.Text) ||
                string.IsNullOrWhiteSpace(textBoxPath.Text) ||
                comboBoxMethod.SelectedIndex < 0)
            {
                MessageBox.Show(
                    "Les champs Nom, Chemin et Méthode HTTP sont obligatoires.",
                    "Erreur de validation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                this.DialogResult = DialogResult.None;
                return;
            }

            // Mettre à jour l'endpoint
            Endpoint.Name = textBoxName.Text;
            Endpoint.Path = textBoxPath.Text;
            Endpoint.Method = comboBoxMethod.SelectedItem.ToString();
            Endpoint.SupportsDateFiltering = checkBoxDateFiltering.Checked;

            if (Endpoint.SupportsDateFiltering)
            {
                Endpoint.DateStartParamName = textBoxStartParamName.Text;
                Endpoint.DateEndParamName = textBoxEndParamName.Text;
                Endpoint.DateFormat = textBoxDateFormat.Text;
            }
        }
    }
}
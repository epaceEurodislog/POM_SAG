using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using POMsag.Models;
// Définir des alias pour éviter l'ambiguïté
using NetHttpMethod = System.Net.Http.HttpMethod;
using ModelHttpMethod = POMsag.Models.HttpMethod;
#pragma warning disable CS8618, CS8625, CS8600, CS8602, CS8603, CS8604, CS8601

namespace POMsag
{

    public partial class EndpointEditorForm : Form
    {
        private static class ColorPalette
        {
            public static Color PrimaryBackground = Color.FromArgb(247, 250, 252); // Gris très clair
            public static Color SecondaryBackground = Color.FromArgb(237, 242, 247); // Gris plus foncé
            public static Color PrimaryText = Color.FromArgb(45, 55, 72); // Bleu-gris foncé
            public static Color AccentColor = Color.FromArgb(49, 130, 206); // Bleu moderne
            public static Color SecondaryText = Color.FromArgb(113, 128, 150); // Gris moyen
            public static Color BorderColor = Color.FromArgb(226, 232, 240); // Gris très léger
            public static Color WhiteBackground = Color.White;
            public static Color ErrorColor = Color.FromArgb(229, 62, 62); // Rouge pour erreurs
        }


        public ApiEndpoint Endpoint { get; private set; }
        private bool _isNewEndpoint;

        private TextBox textBoxName;
        private TextBox textBoxDescription;
        private TextBox textBoxPath;
        private ComboBox comboBoxMethod;
        private CheckBox checkBoxDateFiltering;
        private Panel panelDateFiltering;
        private TextBox textBoxStartDateParam;
        private TextBox textBoxEndDateParam;
        private TextBox textBoxDateFormat;
        private TextBox textBoxResponseRootPath;
        private DataGridView gridParameters;
        private Button buttonSave;
        private Button buttonCancel;

        public EndpointEditorForm(ApiEndpoint endpoint)
        {
            _isNewEndpoint = endpoint == null;
            Endpoint = endpoint ?? new ApiEndpoint();
            InitializeComponent();
            LoadEndpointData();
        }

        private void InitializeComponent()
        {
            this.Text = _isNewEndpoint ? "Ajouter un endpoint" : "Modifier un endpoint";
            this.Size = new Size(600, 650);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Segoe UI", 10);
            this.BackColor = ColorPalette.PrimaryBackground;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = ColorPalette.WhiteBackground
            };

            // Contrôles
            var labelName = new Label
            {
                Text = "Nom du endpoint :",
                Location = new Point(10, 20),
                AutoSize = true,
                ForeColor = ColorPalette.PrimaryText
            };

            textBoxName = new TextBox
            {
                Location = new Point(10, 45),
                Size = new Size(300, 30),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText
            };

            var labelDescription = new Label
            {
                Text = "Description :",
                Location = new Point(10, 85),
                AutoSize = true,
                ForeColor = ColorPalette.PrimaryText
            };

            textBoxDescription = new TextBox
            {
                Location = new Point(10, 110),
                Size = new Size(450, 30),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText
            };

            var labelPath = new Label
            {
                Text = "Chemin :",
                Location = new Point(10, 150),
                AutoSize = true,
                ForeColor = ColorPalette.PrimaryText
            };

            textBoxPath = new TextBox
            {
                Location = new Point(10, 175),
                Size = new Size(450, 30),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText
            };

            var labelMethod = new Label
            {
                Text = "Méthode HTTP :",
                Location = new Point(10, 215),
                AutoSize = true,
                ForeColor = ColorPalette.PrimaryText
            };

            comboBoxMethod = new ComboBox
            {
                Location = new Point(10, 240),
                Size = new Size(150, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText
            };

            comboBoxMethod.Items.AddRange(Enum.GetNames(typeof(ModelHttpMethod)));
            comboBoxMethod.SelectedIndex = 0;

            var labelResponseRootPath = new Label
            {
                Text = "Chemin d'accès aux données dans la réponse (ex: 'value' ou 'data.items') :",
                Location = new Point(250, 215),
                AutoSize = true,
                ForeColor = ColorPalette.PrimaryText
            };

            textBoxResponseRootPath = new TextBox
            {
                Location = new Point(250, 240),
                Size = new Size(250, 30),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText,
                Text = "value" // Valeur par défaut
            };

            // Paramètres supplémentaires
            var labelParams = new Label
            {
                Text = "Paramètres :",
                Location = new Point(10, 280),
                AutoSize = true,
                ForeColor = ColorPalette.PrimaryText
            };

            gridParameters = new DataGridView
            {
                Location = new Point(10, 305),
                Size = new Size(550, 100),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = ColorPalette.WhiteBackground,
                BorderStyle = BorderStyle.Fixed3D,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                GridColor = ColorPalette.BorderColor,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            gridParameters.Columns.Add("Key", "Nom");
            gridParameters.Columns.Add("Value", "Valeur");

            // Boutons pour gérer les paramètres
            var buttonAddParam = new Button
            {
                Text = "+",
                Location = new Point(570, 305),
                Size = new Size(30, 30),
                BackColor = ColorPalette.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            buttonAddParam.FlatAppearance.BorderSize = 0;
            buttonAddParam.Click += (s, e) => gridParameters.Rows.Add();

            var buttonRemoveParam = new Button
            {
                Text = "-",
                Location = new Point(570, 345),
                Size = new Size(30, 30),
                BackColor = ColorPalette.ErrorColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            buttonRemoveParam.FlatAppearance.BorderSize = 0;
            buttonRemoveParam.Click += (s, e) =>
            {
                if (gridParameters.SelectedRows.Count > 0 && !gridParameters.SelectedRows[0].IsNewRow)
                {
                    gridParameters.Rows.Remove(gridParameters.SelectedRows[0]);
                }
            };

            // Filtrage par date
            checkBoxDateFiltering = new CheckBox
            {
                Text = "Activer le filtrage par date",
                Location = new Point(10, 415),
                AutoSize = true,
                ForeColor = ColorPalette.PrimaryText
            };
            checkBoxDateFiltering.CheckedChanged += (s, e) => panelDateFiltering.Visible = checkBoxDateFiltering.Checked;

            panelDateFiltering = new Panel
            {
                Location = new Point(10, 445),
                Size = new Size(550, 100),
                Visible = false,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ColorPalette.WhiteBackground
            };

            var labelStartDateParam = new Label
            {
                Text = "Nom du paramètre de date de début :",
                Location = new Point(10, 10),
                AutoSize = true,
                ForeColor = ColorPalette.PrimaryText
            };

            textBoxStartDateParam = new TextBox
            {
                Location = new Point(250, 10),
                Size = new Size(250, 25),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText
            };

            var labelEndDateParam = new Label
            {
                Text = "Nom du paramètre de date de fin :",
                Location = new Point(10, 40),
                AutoSize = true,
                ForeColor = ColorPalette.PrimaryText
            };

            textBoxEndDateParam = new TextBox
            {
                Location = new Point(250, 40),
                Size = new Size(250, 25),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText
            };

            var labelDateFormat = new Label
            {
                Text = "Format de date :",
                Location = new Point(10, 70),
                AutoSize = true,
                ForeColor = ColorPalette.PrimaryText
            };

            textBoxDateFormat = new TextBox
            {
                Location = new Point(250, 70),
                Size = new Size(250, 25),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText,
                Text = "yyyy-MM-dd" // Format par défaut
            };

            // Ajouter les contrôles de date au panneau
            panelDateFiltering.Controls.Add(labelStartDateParam);
            panelDateFiltering.Controls.Add(textBoxStartDateParam);
            panelDateFiltering.Controls.Add(labelEndDateParam);
            panelDateFiltering.Controls.Add(textBoxEndDateParam);
            panelDateFiltering.Controls.Add(labelDateFormat);
            panelDateFiltering.Controls.Add(textBoxDateFormat);

            // Boutons de validation
            buttonSave = new Button
            {
                Text = "Enregistrer",
                Location = new Point(350, 560),
                Size = new Size(120, 40),
                BackColor = ColorPalette.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            buttonSave.FlatAppearance.BorderSize = 0;
            buttonSave.Click += ButtonSave_Click;

            buttonCancel = new Button
            {
                Text = "Annuler",
                Location = new Point(480, 560),
                Size = new Size(100, 40),
                BackColor = ColorPalette.SecondaryText,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            buttonCancel.FlatAppearance.BorderSize = 0;

            // Ajouter les contrôles au panneau
            panel.Controls.Add(labelName);
            panel.Controls.Add(textBoxName);
            panel.Controls.Add(labelDescription);
            panel.Controls.Add(textBoxDescription);
            panel.Controls.Add(labelPath);
            panel.Controls.Add(textBoxPath);
            panel.Controls.Add(labelMethod);
            panel.Controls.Add(comboBoxMethod);
            panel.Controls.Add(labelResponseRootPath);
            panel.Controls.Add(textBoxResponseRootPath);
            panel.Controls.Add(labelParams);
            panel.Controls.Add(gridParameters);
            panel.Controls.Add(buttonAddParam);
            panel.Controls.Add(buttonRemoveParam);
            panel.Controls.Add(checkBoxDateFiltering);
            panel.Controls.Add(panelDateFiltering);
            panel.Controls.Add(buttonSave);
            panel.Controls.Add(buttonCancel);

            // Ajouter le panneau au formulaire
            this.Controls.Add(panel);

            this.AcceptButton = buttonSave;
            this.CancelButton = buttonCancel;
        }

        private void LoadEndpointData()
        {
            if (Endpoint != null)
            {
                textBoxName.Text = Endpoint.Name;
                textBoxDescription.Text = Endpoint.Description;
                textBoxPath.Text = Endpoint.Path;
                comboBoxMethod.SelectedItem = Endpoint.Method.ToString();
                textBoxResponseRootPath.Text = Endpoint.ResponseRootPath;

                // Charger les paramètres
                gridParameters.Rows.Clear();
                foreach (var param in Endpoint.Parameters)
                {
                    gridParameters.Rows.Add(param.Key, param.Value);
                }

                // Paramètres de filtrage par date
                checkBoxDateFiltering.Checked = Endpoint.SupportsDateFiltering;
                textBoxStartDateParam.Text = Endpoint.StartDateParamName;
                textBoxEndDateParam.Text = Endpoint.EndDateParamName;
                textBoxDateFormat.Text = Endpoint.DateFormat;

                panelDateFiltering.Visible = Endpoint.SupportsDateFiltering;
            }
        }

        private void ButtonSave_Click(object sender, EventArgs e)
        {
            try
            {
                // Valider les champs obligatoires
                if (string.IsNullOrWhiteSpace(textBoxName.Text))
                {
                    MessageBox.Show("Le nom du endpoint est obligatoire.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBoxName.Focus();
                    this.DialogResult = DialogResult.None;
                    return;
                }

                if (string.IsNullOrWhiteSpace(textBoxPath.Text))
                {
                    MessageBox.Show("Le chemin est obligatoire.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBoxPath.Focus();
                    this.DialogResult = DialogResult.None;
                    return;
                }

                // Mettre à jour les propriétés
                Endpoint.Name = textBoxName.Text;
                Endpoint.Description = textBoxDescription.Text;
                Endpoint.Path = textBoxPath.Text;
                Endpoint.Method = (ModelHttpMethod)Enum.Parse(typeof(ModelHttpMethod), comboBoxMethod.SelectedItem?.ToString() ?? "Get");
                Endpoint.ResponseRootPath = textBoxResponseRootPath.Text;

                // Paramètres
                Endpoint.Parameters = new Dictionary<string, string>();
                foreach (DataGridViewRow row in gridParameters.Rows)
                {
                    if (row.IsNewRow) continue;

                    string key = row.Cells["Key"].Value?.ToString();
                    string value = row.Cells["Value"].Value?.ToString();

                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        Endpoint.Parameters[key] = value ?? "";
                    }
                }

                // Filtrage par date
                Endpoint.SupportsDateFiltering = checkBoxDateFiltering.Checked;
                if (Endpoint.SupportsDateFiltering)
                {
                    Endpoint.StartDateParamName = textBoxStartDateParam.Text;
                    Endpoint.EndDateParamName = textBoxEndDateParam.Text;
                    Endpoint.DateFormat = textBoxDateFormat.Text;
                }

                this.DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors de l'enregistrement : {ex.Message}",
                    "Erreur",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                this.DialogResult = DialogResult.None;
            }
        }
    }
}
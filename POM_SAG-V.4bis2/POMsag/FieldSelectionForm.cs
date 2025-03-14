using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using POMsag.Services;
using System.Linq;
#pragma warning disable CS8618, CS8625, CS8600, CS8602, CS8603, CS8604, CS8601

namespace POMsag
{
    public partial class FieldSelectionForm : Form
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

        private readonly AppConfiguration _configuration;
        private readonly SchemaAnalysisService _schemaAnalysisService;
        private readonly string _entityName;
        private readonly string _sourceType;
        private readonly CheckedListBox _fieldsListBox;
        private readonly Button _selectAllButton;
        private readonly Button _deselectAllButton;
        private readonly Button _saveButton;
        private readonly Label _statusLabel;
        private HashSet<string> _availableFields = new HashSet<string>();

        public FieldSelectionForm(AppConfiguration configuration, SchemaAnalysisService schemaAnalysisService,
                                  string entityName, string sourceType)
        {
            _configuration = configuration;
            _schemaAnalysisService = schemaAnalysisService;
            _entityName = entityName;
            _sourceType = sourceType;

            // Configuration du formulaire
            this.Text = $"Sélection des champs pour {entityName}";
            this.Size = new Size(600, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 10);
            this.BackColor = ColorPalette.PrimaryBackground;

            // Status Label
            _statusLabel = new Label
            {
                Text = "Chargement des champs disponibles...",
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(10),
                ForeColor = ColorPalette.PrimaryText
            };

            // Panel pour la liste des champs avec barre de défilement
            var fieldsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = ColorPalette.WhiteBackground
            };

            // CheckedListBox pour la sélection des champs
            _fieldsListBox = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                CheckOnClick = true,
                MultiColumn = false,
                Sorted = true,
                BackColor = ColorPalette.WhiteBackground,
                ForeColor = ColorPalette.PrimaryText
            };

            fieldsPanel.Controls.Add(_fieldsListBox);

            // Panel pour les boutons
            var buttonsPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 150,
                Padding = new Padding(10),
                BackColor = ColorPalette.SecondaryBackground
            };

            // Boutons d'action
            _selectAllButton = new Button
            {
                Text = "Sélectionner tout",
                Width = 170,
                Height = 40,
                BackColor = ColorPalette.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(10, 10)
            };
            _selectAllButton.FlatAppearance.BorderSize = 0;
            _selectAllButton.Click += SelectAllButton_Click;

            _deselectAllButton = new Button
            {
                Text = "Désélectionner tout",
                Width = 170,
                Height = 40,
                BackColor = ColorPalette.SecondaryText,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(10, 60)
            };
            _deselectAllButton.FlatAppearance.BorderSize = 0;
            _deselectAllButton.Click += DeselectAllButton_Click;

            _saveButton = new Button
            {
                Text = "Enregistrer la sélection",
                Width = 350,
                Height = 50,
                BackColor = ColorPalette.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(220, 35)
            };
            _saveButton.FlatAppearance.BorderSize = 0;
            _saveButton.Click += SaveButton_Click;

            // Ajouter les boutons au panel
            buttonsPanel.Controls.Add(_selectAllButton);
            buttonsPanel.Controls.Add(_deselectAllButton);
            buttonsPanel.Controls.Add(_saveButton);

            // Ajouter les contrôles au formulaire
            this.Controls.Add(fieldsPanel);
            this.Controls.Add(_statusLabel);
            this.Controls.Add(buttonsPanel);

            // Charger les champs au démarrage du formulaire
            this.Load += async (s, e) => await LoadFieldsAsync();
        }

        private async Task LoadFieldsAsync()
        {
            try
            {
                _statusLabel.Text = "Chargement des champs disponibles...";
                _fieldsListBox.Enabled = false;
                _saveButton.Enabled = false;
                _selectAllButton.Enabled = false;
                _deselectAllButton.Enabled = false;

                // Récupérer les champs disponibles
                LoggerService.Log($"Tentative de découverte des champs pour {_entityName} (source: {_sourceType})");

                // Ajouter un délai pour voir les logs précédents
                await Task.Delay(500);

                // Essayer d'abord avec un petit échantillon
                _availableFields = await _schemaAnalysisService.DiscoverFields(_sourceType, _entityName);

                // Si aucun champ n'est trouvé, utiliser une liste de champs par défaut pour certaines entités
                if (_availableFields.Count == 0)
                {
                    LoggerService.Log($"Aucun champ découvert, utilisation des champs par défaut pour {_entityName}");

                    // Fournir des champs par défaut pour ReleasedProductsV2
                    if (_entityName == "ReleasedProductsV2")
                    {
                        _availableFields = new HashSet<string> {
                    "@odata.etag", "dataAreaId", "ItemNumber", "IsPhantom",
                    "ProductNumber", "ProductName", "ProductDescription",
                    "ProductType", "ProductSubType", "ProductDimension"
                };
                    }
                }

                // Mettre à jour l'interface
                _fieldsListBox.Items.Clear();
                foreach (var field in _availableFields.OrderBy(f => f))
                {
                    _fieldsListBox.Items.Add(field, _configuration.IsFieldSelected(_entityName, field));
                }

                _statusLabel.Text = $"{_availableFields.Count} champs disponibles. Sélectionnez ceux à inclure dans le transfert.";
                _fieldsListBox.Enabled = true;
                _saveButton.Enabled = true;
                _selectAllButton.Enabled = true;
                _deselectAllButton.Enabled = true;
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Erreur lors du chargement des champs: {ex.Message}";
                LoggerService.LogException(ex, "Chargement des champs");

                MessageBox.Show(
                    $"Erreur lors du chargement des champs: {ex.Message}\n\nL'application va charger une liste de champs par défaut.",
                    "Erreur",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                // Chargement de champs par défaut en cas d'erreur
                _availableFields = new HashSet<string>();
                if (_entityName == "ReleasedProductsV2")
                {
                    _availableFields = new HashSet<string> {
                "@odata.etag", "dataAreaId", "ItemNumber", "IsPhantom",
                "ProductNumber", "ProductName", "ProductDescription",
                "ProductType", "ProductSubType", "ProductDimension"
            };
                }

                // Mettre à jour l'interface avec les champs par défaut
                _fieldsListBox.Items.Clear();
                foreach (var field in _availableFields.OrderBy(f => f))
                {
                    _fieldsListBox.Items.Add(field, _configuration.IsFieldSelected(_entityName, field));
                }

                _statusLabel.Text = $"{_availableFields.Count} champs par défaut chargés.";
                _fieldsListBox.Enabled = true;
                _saveButton.Enabled = true;
                _selectAllButton.Enabled = true;
                _deselectAllButton.Enabled = true;
            }
        }

        private void SelectAllButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < _fieldsListBox.Items.Count; i++)
            {
                _fieldsListBox.SetItemChecked(i, true);
            }
        }

        private void DeselectAllButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < _fieldsListBox.Items.Count; i++)
            {
                _fieldsListBox.SetItemChecked(i, false);
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Enregistrer les préférences dans la configuration
                for (int i = 0; i < _fieldsListBox.Items.Count; i++)
                {
                    string fieldName = _fieldsListBox.Items[i].ToString();
                    bool isSelected = _fieldsListBox.GetItemChecked(i);

                    _configuration.AddOrUpdateFieldPreference(_entityName, fieldName, isSelected);
                }

                MessageBox.Show(
                    "Préférences de champs enregistrées avec succès!",
                    "Succès",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                this.Close();
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, "Enregistrement des préférences de champs");
                MessageBox.Show(
                    $"Erreur lors de l'enregistrement des préférences: {ex.Message}",
                    "Erreur",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}
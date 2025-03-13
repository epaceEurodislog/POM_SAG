using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using POMsag.Models;
using POMsag.Services;

namespace POMsag
{
    public partial class ApiManagerForm : Form
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

        private readonly ApiManager _apiManager;
        private ListView listViewApis;
        private Button buttonAdd;
        private Button buttonEdit;
        private Button buttonDelete;
        private Button buttonClose;

        public ApiManagerForm(ApiManager apiManager)
        {
            _apiManager = apiManager;
            InitializeComponent();
            LoadApis();
        }

        private void InitializeComponent()
        {
            this.Text = "Gestionnaire d'API";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 10);
            this.BackColor = ColorPalette.PrimaryBackground;

            // En-tête explicatif
            var labelHeader = new Label
            {
                Text = "Gérez les sources de données API utilisées par l'application",
                Dock = DockStyle.Top,
                Height = 40,
                Font = new Font("Segoe UI Semibold", 12),
                ForeColor = ColorPalette.PrimaryText,
                Padding = new Padding(10, 10, 10, 0),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Description
            var labelDescription = new Label
            {
                Text = "Ajoutez, modifiez ou supprimez des configurations d'API. Chaque API peut avoir plusieurs endpoints qui seront disponibles comme sources de données.",
                Dock = DockStyle.Top,
                Height = 40,
                ForeColor = ColorPalette.SecondaryText,
                Padding = new Padding(10, 0, 10, 10),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Liste des API
            listViewApis = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                BackColor = ColorPalette.WhiteBackground,
                ForeColor = ColorPalette.PrimaryText,
                BorderStyle = BorderStyle.FixedSingle
            };

            listViewApis.Columns.Add("Nom", 150);
            listViewApis.Columns.Add("Description", 250);
            listViewApis.Columns.Add("URL de base", 200);
            listViewApis.Columns.Add("Endpoints", 80);
            listViewApis.Columns.Add("Auth", 100);

            // Panneau pour les boutons
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = ColorPalette.SecondaryBackground,
                Padding = new Padding(10)
            };

            // Boutons
            buttonAdd = new Button
            {
                Text = "Ajouter",
                Width = 120,
                Height = 40,
                BackColor = ColorPalette.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(10, 10)
            };
            buttonAdd.FlatAppearance.BorderSize = 0;
            buttonAdd.Click += ButtonAdd_Click;

            buttonEdit = new Button
            {
                Text = "Modifier",
                Width = 120,
                Height = 40,
                BackColor = ColorPalette.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(140, 10),
                Enabled = false
            };
            buttonEdit.FlatAppearance.BorderSize = 0;
            buttonEdit.Click += ButtonEdit_Click;

            buttonDelete = new Button
            {
                Text = "Supprimer",
                Width = 120,
                Height = 40,
                BackColor = ColorPalette.ErrorColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(270, 10),
                Enabled = false
            };
            buttonDelete.FlatAppearance.BorderSize = 0;
            buttonDelete.Click += ButtonDelete_Click;

            buttonClose = new Button
            {
                Text = "Fermer",
                Width = 120,
                Height = 40,
                BackColor = ColorPalette.SecondaryText,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(650, 10)
            };
            buttonClose.FlatAppearance.BorderSize = 0;
            buttonClose.Click += (s, e) => this.Close();

            // Ajouter les boutons au panneau
            buttonPanel.Controls.Add(buttonAdd);
            buttonPanel.Controls.Add(buttonEdit);
            buttonPanel.Controls.Add(buttonDelete);
            buttonPanel.Controls.Add(buttonClose);

            // Ajouter les contrôles au formulaire
            this.Controls.Add(listViewApis);
            this.Controls.Add(buttonPanel);
            this.Controls.Add(labelDescription);
            this.Controls.Add(labelHeader);

            // Événements
            listViewApis.SelectedIndexChanged += ListViewApis_SelectedIndexChanged;
            listViewApis.DoubleClick += ListViewApis_DoubleClick;
        }

        private void LoadApis()
        {
            listViewApis.Items.Clear();
            var apis = _apiManager.GetAllApis();

            foreach (var api in apis)
            {
                var item = new ListViewItem(api.Name);
                item.SubItems.Add(api.Description ?? "");
                item.SubItems.Add(api.BaseUrl);
                item.SubItems.Add(api.Endpoints.Count.ToString());
                item.SubItems.Add(api.AuthType.ToString());
                item.Tag = api;

                listViewApis.Items.Add(item);
            }

            // Si aucune API n'est définie, proposer de créer des exemples
            if (apis.Count == 0)
            {
                var result = MessageBox.Show(
                    "Aucune API n'est configurée. Voulez-vous créer des exemples d'API pour démarrer ?",
                    "Aucune API trouvée",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    _apiManager.CreateSampleApis();
                    LoadApis(); // Recharger la liste
                }
            }
        }

        private void ListViewApis_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool hasSelection = listViewApis.SelectedItems.Count > 0;
            buttonEdit.Enabled = hasSelection;
            buttonDelete.Enabled = hasSelection;
        }

        private void ListViewApis_DoubleClick(object sender, EventArgs e)
        {
            if (listViewApis.SelectedItems.Count > 0)
            {
                ButtonEdit_Click(sender, e);
            }
        }

        private void ButtonAdd_Click(object sender, EventArgs e)
        {
            using (var form = new ApiEditorForm(null, _apiManager))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    LoadApis();
                }
            }
        }

        private void ButtonEdit_Click(object sender, EventArgs e)
        {
            if (listViewApis.SelectedItems.Count > 0)
            {
                var api = (ApiDefinition)listViewApis.SelectedItems[0].Tag;
                using (var form = new ApiEditorForm(api, _apiManager))
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        LoadApis();
                    }
                }
            }
        }

        private void ButtonDelete_Click(object sender, EventArgs e)
        {
            if (listViewApis.SelectedItems.Count > 0)
            {
                var api = (ApiDefinition)listViewApis.SelectedItems[0].Tag;

                var result = MessageBox.Show(
                    $"Êtes-vous sûr de vouloir supprimer l'API '{api.Name}' ?\n\nCette action est irréversible.",
                    "Confirmation de suppression",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if (result == DialogResult.Yes)
                {
                    if (_apiManager.DeleteApi(api.Name))
                    {
                        LoadApis();
                    }
                    else
                    {
                        MessageBox.Show(
                            $"Erreur lors de la suppression de l'API '{api.Name}'.",
                            "Erreur",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    }
                }
            }
        }
    }
}
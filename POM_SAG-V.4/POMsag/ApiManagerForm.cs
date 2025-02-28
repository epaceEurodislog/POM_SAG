// Fichier: POMsag/ApiManagerForm.cs
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using POMsag.Models;

namespace POMsag
{
    public partial class ApiManagerForm : Form
    {
        private static class ColorPalette
        {
            public static Color PrimaryBackground = Color.FromArgb(247, 250, 252);
            public static Color SecondaryBackground = Color.FromArgb(237, 242, 247);
            public static Color PrimaryText = Color.FromArgb(45, 55, 72);
            public static Color AccentColor = Color.FromArgb(49, 130, 206);
            public static Color WhiteBackground = Color.White;
        }

        private readonly AppConfiguration _configuration;
        private ComboBox comboApis;
        private ListView listEndpoints;
        private Button buttonAddApi;
        private Button buttonEditApi;
        private Button buttonDeleteApi;
        private Button buttonAddEndpoint;
        private Button buttonEditEndpoint;
        private Button buttonDeleteEndpoint;

        public ApiManagerForm(AppConfiguration configuration)
        {
            _configuration = configuration;
            InitializeComponent();
            LoadApis();
        }

        private void InitializeComponent()
        {
            this.Text = "Gestionnaire d'API";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ColorPalette.PrimaryBackground;
            this.Font = new Font("Segoe UI", 10);

            // Composants principaux
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 300,
            };

            // Panel pour la liste des API
            var apiPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = ColorPalette.WhiteBackground
            };

            var apiLabel = new Label
            {
                Text = "API configurées :",
                Dock = DockStyle.Top,
                Height = 30,
                ForeColor = ColorPalette.PrimaryText
            };

            comboApis = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText
            };
            comboApis.SelectedIndexChanged += ComboApis_SelectedIndexChanged;

            // Boutons pour les API
            var apiButtonsPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(0, 5, 0, 0)
            };

            buttonAddApi = new Button
            {
                Text = "Ajouter",
                Width = 80,
                BackColor = ColorPalette.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            buttonAddApi.FlatAppearance.BorderSize = 0;

            buttonEditApi = new Button
            {
                Text = "Modifier",
                Width = 80,
                Left = 90,
                BackColor = ColorPalette.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            buttonEditApi.FlatAppearance.BorderSize = 0;

            buttonDeleteApi = new Button
            {
                Text = "Supprimer",
                Width = 80,
                Left = 180,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText,
                FlatStyle = FlatStyle.Flat
            };
            buttonDeleteApi.FlatAppearance.BorderSize = 0;

            buttonAddApi.Click += ButtonAddApi_Click;
            buttonEditApi.Click += ButtonEditApi_Click;
            buttonDeleteApi.Click += ButtonDeleteApi_Click;

            apiButtonsPanel.Controls.AddRange(new Control[] { buttonAddApi, buttonEditApi, buttonDeleteApi });

            // Panel pour les endpoints
            var endpointPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = ColorPalette.WhiteBackground
            };

            var endpointLabel = new Label
            {
                Text = "Endpoints disponibles :",
                Dock = DockStyle.Top,
                Height = 30,
                ForeColor = ColorPalette.PrimaryText
            };

            listEndpoints = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                BackColor = ColorPalette.WhiteBackground,
                ForeColor = ColorPalette.PrimaryText
            };
            listEndpoints.Columns.Add("Nom", 150);
            listEndpoints.Columns.Add("Chemin", 200);
            listEndpoints.Columns.Add("Méthode", 70);
            listEndpoints.Columns.Add("Filtre par date", 100);

            // Boutons pour les endpoints
            var endpointButtonsPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                Padding = new Padding(0, 5, 0, 0)
            };

            buttonAddEndpoint = new Button
            {
                Text = "Ajouter",
                Width = 80,
                BackColor = ColorPalette.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            buttonAddEndpoint.FlatAppearance.BorderSize = 0;

            buttonEditEndpoint = new Button
            {
                Text = "Modifier",
                Width = 80,
                Left = 90,
                BackColor = ColorPalette.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            buttonEditEndpoint.FlatAppearance.BorderSize = 0;

            buttonDeleteEndpoint = new Button
            {
                Text = "Supprimer",
                Width = 80,
                Left = 180,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText,
                FlatStyle = FlatStyle.Flat
            };
            buttonDeleteEndpoint.FlatAppearance.BorderSize = 0;

            buttonAddEndpoint.Click += ButtonAddEndpoint_Click;
            buttonEditEndpoint.Click += ButtonEditEndpoint_Click;
            buttonDeleteEndpoint.Click += ButtonDeleteEndpoint_Click;

            endpointButtonsPanel.Controls.AddRange(new Control[] { buttonAddEndpoint, buttonEditEndpoint, buttonDeleteEndpoint });

            // Assembler les panneaux
            apiPanel.Controls.Add(apiButtonsPanel);
            apiPanel.Controls.Add(comboApis);
            apiPanel.Controls.Add(apiLabel);

            endpointPanel.Controls.Add(listEndpoints);
            endpointPanel.Controls.Add(endpointButtonsPanel);
            endpointPanel.Controls.Add(endpointLabel);

            splitContainer.Panel1.Controls.Add(apiPanel);
            splitContainer.Panel2.Controls.Add(endpointPanel);

            this.Controls.Add(splitContainer);
        }

        private void LoadApis()
        {
            comboApis.Items.Clear();

            foreach (var api in _configuration.ConfiguredApis)
            {
                comboApis.Items.Add(api.Name);
            }

            if (comboApis.Items.Count > 0)
                comboApis.SelectedIndex = 0;

            UpdateEndpointsList();
        }

        private void UpdateEndpointsList()
        {
            listEndpoints.Items.Clear();

            if (comboApis.SelectedIndex < 0)
                return;

            var api = _configuration.ConfiguredApis[comboApis.SelectedIndex];

            foreach (var endpoint in api.Endpoints)
            {
                var item = new ListViewItem(endpoint.Name);
                item.SubItems.Add(endpoint.Path);
                item.SubItems.Add(endpoint.Method);
                item.SubItems.Add(endpoint.SupportsDateFiltering ? "Oui" : "Non");

                listEndpoints.Items.Add(item);
            }
        }

        private void ComboApis_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateEndpointsList();
        }

        private void ButtonAddApi_Click(object sender, EventArgs e)
        {
            // Ouvrir un formulaire pour ajouter une nouvelle API
            using (var form = new ApiEditForm(null))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    _configuration.AddOrUpdateApi(form.ApiConfig);
                    LoadApis();
                }
            }
        }

        private void ButtonEditApi_Click(object sender, EventArgs e)
        {
            if (comboApis.SelectedIndex < 0)
                return;

            var api = _configuration.ConfiguredApis[comboApis.SelectedIndex];

            // Ouvrir un formulaire pour modifier l'API
            using (var form = new ApiEditForm(api))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    _configuration.AddOrUpdateApi(form.ApiConfig);
                    LoadApis();
                }
            }
        }

        private void ButtonDeleteApi_Click(object sender, EventArgs e)
        {
            if (comboApis.SelectedIndex < 0)
                return;

            var api = _configuration.ConfiguredApis[comboApis.SelectedIndex];

            if (MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer l'API '{api.Name}' ?",
                "Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _configuration.RemoveApi(api.ApiId);
                LoadApis();
            }
        }

        private void ButtonAddEndpoint_Click(object sender, EventArgs e)
        {
            if (comboApis.SelectedIndex < 0)
                return;

            var api = _configuration.ConfiguredApis[comboApis.SelectedIndex];

            // Ouvrir un formulaire pour ajouter un nouveau endpoint
            using (var form = new EndpointEditForm(null))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    api.Endpoints.Add(form.Endpoint);
                    _configuration.AddOrUpdateApi(api);
                    UpdateEndpointsList();
                }
            }
        }

        private void ButtonEditEndpoint_Click(object sender, EventArgs e)
        {
            if (comboApis.SelectedIndex < 0 || listEndpoints.SelectedItems.Count == 0)
                return;

            var api = _configuration.ConfiguredApis[comboApis.SelectedIndex];
            var endpointName = listEndpoints.SelectedItems[0].Text;
            var endpoint = api.Endpoints.FirstOrDefault(ep => ep.Name == endpointName);

            if (endpoint == null)
                return;

            // Ouvrir un formulaire pour modifier le endpoint
            using (var form = new EndpointEditForm(endpoint))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    int index = api.Endpoints.FindIndex(ep => ep.Name == endpointName);
                    if (index >= 0)
                        api.Endpoints[index] = form.Endpoint;

                    _configuration.AddOrUpdateApi(api);
                    UpdateEndpointsList();
                }
            }
        }

        private void ButtonDeleteEndpoint_Click(object sender, EventArgs e)
        {
            if (comboApis.SelectedIndex < 0 || listEndpoints.SelectedItems.Count == 0)
                return;

            var api = _configuration.ConfiguredApis[comboApis.SelectedIndex];
            var endpointName = listEndpoints.SelectedItems[0].Text;

            if (MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer l'endpoint '{endpointName}' ?",
                "Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                api.Endpoints.RemoveAll(ep => ep.Name == endpointName);
                _configuration.AddOrUpdateApi(api);
                UpdateEndpointsList();
            }
        }
    }
}
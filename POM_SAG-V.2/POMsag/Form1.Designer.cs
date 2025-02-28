using System;
using System.Drawing;
using System.Windows.Forms;
using System.Net.Http;
using System.Text.Json;

namespace POMsag
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            // Configuration de base du formulaire avec une taille plus grande
            this.Text = "POM - Transfert de Données";
            this.Size = new Size(1000, 800);
            this.MinimumSize = new Size(1000, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(243, 243, 243);
            this.Font = new Font("Segoe UI", 10);
            this.Padding = new Padding(20);

            // Création des composants
            comboBoxTables = new ComboBox();
            buttonTransfer = new Button();
            progressBar = new ProgressBar();
            checkBoxDateFilter = new CheckBox();
            dateTimePickerStart = new DateTimePicker();
            dateTimePickerEnd = new DateTimePicker();
            configButton = new Button();

            // Bouton de configuration
            configButton.Text = "⚙️ Configuration";
            configButton.FlatStyle = FlatStyle.Flat;
            configButton.BackColor = Color.FromArgb(240, 240, 240);
            configButton.ForeColor = Color.Black;
            configButton.Font = new Font("Segoe UI", 12);
            configButton.Location = new Point(850, 20);
            configButton.Size = new Size(130, 40);
            configButton.FlatAppearance.BorderSize = 1;
            configButton.Cursor = Cursors.Hand;
            configButton.Click += ConfigButton_Click;

            // Titre principal
            var labelTitle = new Label
            {
                Text = "Transfert de Données POM",
                Location = new Point(100, 50),
                Size = new Size(800, 60),
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Label pour le ComboBox
            var labelSelect = new Label
            {
                Text = "Sélectionnez les données à transférer :",
                Location = new Point(100, 150),
                Size = new Size(800, 30),
                Font = new Font("Segoe UI", 14),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // ComboBox
            comboBoxTables.FlatStyle = FlatStyle.Flat;
            comboBoxTables.BackColor = Color.White;
            comboBoxTables.Font = new Font("Segoe UI", 14);
            comboBoxTables.Location = new Point(100, 190);
            comboBoxTables.Size = new Size(800, 40);
            comboBoxTables.DropDownStyle = ComboBoxStyle.DropDownList;

            // Section filtre date
            checkBoxDateFilter.Text = "Filtrer par date";
            checkBoxDateFilter.Location = new Point(100, 260);
            checkBoxDateFilter.Size = new Size(800, 30);
            checkBoxDateFilter.Font = new Font("Segoe UI", 14);
            checkBoxDateFilter.AutoSize = true;

            // Labels pour les DateTimePicker
            var labelStartDate = new Label
            {
                Text = "Date de début :",
                Location = new Point(100, 310),
                Size = new Size(390, 30),
                Font = new Font("Segoe UI", 14)
            };

            var labelEndDate = new Label
            {
                Text = "Date de fin :",
                Location = new Point(510, 310),
                Size = new Size(390, 30),
                Font = new Font("Segoe UI", 14)
            };

            // DateTimePickers
            dateTimePickerStart.Location = new Point(100, 350);
            dateTimePickerStart.Size = new Size(390, 40);
            dateTimePickerStart.Enabled = false;
            dateTimePickerStart.Font = new Font("Segoe UI", 14);

            dateTimePickerEnd.Location = new Point(510, 350);
            dateTimePickerEnd.Size = new Size(390, 40);
            dateTimePickerEnd.Enabled = false;
            dateTimePickerEnd.Font = new Font("Segoe UI", 14);

            // Bouton de transfert
            buttonTransfer.FlatStyle = FlatStyle.Flat;
            buttonTransfer.BackColor = Color.FromArgb(0, 120, 212);
            buttonTransfer.ForeColor = Color.White;
            buttonTransfer.Font = new Font("Segoe UI Semibold", 16);
            buttonTransfer.Location = new Point(100, 450);
            buttonTransfer.Size = new Size(800, 60);
            buttonTransfer.FlatAppearance.BorderSize = 0;
            buttonTransfer.Text = "Démarrer le transfert";
            buttonTransfer.Cursor = Cursors.Hand;

            // Barre de progression
            progressBar.Location = new Point(100, 540);
            progressBar.Size = new Size(800, 20);
            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.Visible = false;

            // Ajout de tous les contrôles
            this.Controls.AddRange(new Control[] {
                labelTitle,
                labelSelect,
                comboBoxTables,
                checkBoxDateFilter,
                labelStartDate,
                labelEndDate,
                dateTimePickerStart,
                dateTimePickerEnd,
                buttonTransfer,
                progressBar,
                configButton
            });
        }

        // Déclaration des contrôles
        private ComboBox comboBoxTables;
        private Button buttonTransfer;
        private ProgressBar progressBar;
        private CheckBox checkBoxDateFilter;
        private DateTimePicker dateTimePickerStart;
        private DateTimePicker dateTimePickerEnd;
        private Button configButton;
    }
}
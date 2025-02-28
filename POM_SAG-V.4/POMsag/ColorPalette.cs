using System.Drawing;

namespace POMsag
{
    /// <summary>
    /// Classe statique définissant une palette de couleurs cohérente pour l'application
    /// </summary>
    public static class ColorPalette
    {
        /// <summary>
        /// Arrière-plan principal (gris très clair)
        /// </summary>
        public static Color PrimaryBackground = Color.FromArgb(247, 250, 252);

        /// <summary>
        /// Arrière-plan secondaire (gris légèrement plus foncé)
        /// </summary>
        public static Color SecondaryBackground = Color.FromArgb(237, 242, 247);

        /// <summary>
        /// Couleur de texte principale (bleu-gris foncé)
        /// </summary>
        public static Color PrimaryText = Color.FromArgb(45, 55, 72);

        /// <summary>
        /// Couleur d'accentuation (bleu moderne)
        /// </summary>
        public static Color AccentColor = Color.FromArgb(49, 130, 206);

        /// <summary>
        /// Couleur de texte secondaire (gris moyen)
        /// </summary>
        public static Color SecondaryText = Color.FromArgb(113, 128, 150);

        /// <summary>
        /// Couleur de bordure (gris très léger)
        /// </summary>
        public static Color BorderColor = Color.FromArgb(226, 232, 240);

        /// <summary>
        /// Arrière-plan blanc
        /// </summary>
        public static Color WhiteBackground = Color.White;

        /// <summary>
        /// Couleur d'erreur (rouge)
        /// </summary>
        public static Color ErrorColor = Color.FromArgb(229, 62, 62);

        /// <summary>
        /// Couleur de succès (vert)
        /// </summary>
        public static Color SuccessColor = Color.FromArgb(72, 187, 120);

        /// <summary>
        /// Couleur d'avertissement (orange)
        /// </summary>
        public static Color WarningColor = Color.FromArgb(237, 137, 54);

        /// <summary>
        /// Couleur d'information (bleu clair)
        /// </summary>
        public static Color InfoColor = Color.FromArgb(66, 153, 225);
    }
}
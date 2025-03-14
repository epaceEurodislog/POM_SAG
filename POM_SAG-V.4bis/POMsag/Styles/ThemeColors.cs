using System.Drawing;

namespace POMsag.Styles
{
    public static class ThemeColors
    {
        // Couleurs principales
        public static Color PrimaryBackground = Color.FromArgb(247, 250, 252); // Gris très clair
        public static Color SecondaryBackground = Color.FromArgb(237, 242, 247); // Gris plus foncé
        public static Color PrimaryText = Color.FromArgb(45, 55, 72); // Bleu-gris foncé
        public static Color AccentColor = Color.FromArgb(49, 130, 206); // Bleu moderne
        public static Color SecondaryText = Color.FromArgb(113, 128, 150); // Gris moyen
        public static Color BorderColor = Color.FromArgb(226, 232, 240); // Gris très léger
        public static Color WhiteBackground = Color.White;
        public static Color ErrorColor = Color.FromArgb(229, 62, 62); // Rouge pour erreurs
        public static Color WarningColor = Color.FromArgb(237, 137, 54); // Orange
        public static Color SuccessColor = Color.FromArgb(72, 187, 120); // Vert

        // Variations de l'accent pour les hover
        public static Color AccentHover = Color.FromArgb(62, 142, 208);

        // Styles de bordure
        public static int BorderRadius = 4;
        public static int BorderSize = 1;
    }
}
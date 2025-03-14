using System;
using System.Windows.Forms;

namespace POMsag.Styles
{
    public static class AnimationEffects
    {
        // Effet de fondu d'entrée simplifié
        public static void FadeIn(Control control, int duration = 500)
        {
            if (control == null) return;

            // Rendre visible
            control.Visible = true;
        }

        // Effet de fondu de sortie simplifié
        public static void FadeOut(Control control, int duration = 500, Action onCompleted = null)
        {
            if (control == null || !control.Visible)
            {
                onCompleted?.Invoke();
                return;
            }

            // Masquer directement puis appeler le callback
            control.Visible = false;
            onCompleted?.Invoke();
        }
    }
}
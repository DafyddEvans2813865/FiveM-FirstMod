using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace DredgeClient.Effects
{
    public static class ScreenFx
    {
        // Darkens the screen with a black overlay, easing in and out.
        public static async Task Dim(int durationMs, int alpha, int fadeMs = 800)
        {
            int start = GetGameTimer();
            int end = start + durationMs;

            while (GetGameTimer() < end)
            {
                int now = GetGameTimer();
                float t = 1f;
                if (now - start < fadeMs) t = (now - start) / (float)fadeMs;      // fading in
                else if (end - now < fadeMs) t = (end - now) / (float)fadeMs;     // fading out

                DrawRect(0.5f, 0.5f, 1f, 1f, 0, 0, 0, (int)(alpha * t));
                await BaseScript.Delay(0); // DrawRect only lasts one frame
            }
        }
    }
}

using CitizenFX.Core;
using CitizenFX.Core.Native;
using static CitizenFX.Core.Native.API;

namespace DredgeClient
{
    public static class GameState
    {
        public static float Dread;           // 0..100, owned by DreadMeter
        public static bool AtSea;            // player is in a boat
        public static bool EventRunning;     // a scare event is playing
        public static bool EndgameRunning;   // death sequence is playing 
        public static int FishHeld;          // TODO: FishingMinigame should keep this in sync with its count
        public static bool WasAtDock;        // player was at the dock
        public static bool HasLeftDock;      // player has left the dock

        public static readonly Vector3 DockPos = new Vector3(491.76f, -3410.70f, -1.0f);
        public const float DockRadius = 30f;
    }

    //Drawing helpers
    public static class Hud
    {
        public static void Rect(float x, float y, float w, float h, int r, int g, int b, int a)
        {
            Function.Call(Hash.DRAW_RECT, x, y, w, h, r, g, b, a, false);
        }

        public static void Text(string text, float x, float y, float scale, int alpha = 230, bool centre = true)
        {
            SetTextFont(4);
            SetTextScale(scale, scale);
            SetTextColour(255, 255, 255, alpha);
            SetTextCentre(centre);
            SetTextOutline();
            BeginTextCommandDisplayText("STRING");
            AddTextComponentSubstringPlayerName(text);
            EndTextCommandDisplayText(x, y);
        }
    }
}
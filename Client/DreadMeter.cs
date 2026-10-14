using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;

namespace DredgeClient
{

    /// Catch = small relief, miss = small spike. Reaching the dock resets it and sells the catch.
    public class DreadMeter : BaseScript
    {
        // time at sea alone: 0 -> 100 in ~4.5 min
        private const float RisePerSecond = 0.35f;
        private const float EventRisePerSecond = 1.0f;  // extra while witnessing an event
        private const float CatchRelief = 2f;
        private const float MissPenalty = 4f;

        private bool _wasAtDock;

        public DreadMeter()
        {
            EventHandlers["dredge:fishCaught"] += new Action<dynamic>(_ => Add(-CatchRelief));
            EventHandlers["dredge:fishLost"] += new Action(() => Add(MissPenalty));
            EventHandlers["dredge:gameOver"] += new Action(() => GameState.Dread = 0f);

            // Debug: /dread 90
            RegisterCommand("dread", new Action<int, System.Collections.Generic.List<object>, string>((src, args, raw) => { if (args.Count > 0 && float.TryParse(args[0].ToString(), out float v)) GameState.Dread = Clamp(v); }), false);

            Tick += OnTick;
        }

        private Task OnTick()
        {
            float dt = GetFrameTime();
            Ped ped = Game.PlayerPed;
            GameState.AtSea = ped.IsInBoat;

            if (GameState.EndgameRunning) return Task.FromResult(0);

            if (GameState.AtSea)
            {
                float rate = RisePerSecond + (GameState.EventRunning ? EventRisePerSecond : 0f);
                Add(rate * dt);
                DrawMeter();
            }

            // check if player trying to dock 
            bool atDock = !GameState.AtSea && Vector3.DistanceSquared(ped.Position, GameState.DockPos) < GameState.DockRadius * GameState.DockRadius;
            if (atDock && !_wasAtDock) ReturnToDock();
            _wasAtDock = atDock;

            return Task.FromResult(0);
        }

        private void ReturnToDock()
        {
            GameState.Dread = 0f;
            TriggerEvent("dredge:fishSold");
            Screen.ShowNotification("You made it back. Catch sold.");
        }

        private static void Add(float amount) => GameState.Dread = Clamp(GameState.Dread + amount);
        private static float Clamp(float v) => Math.Max(0f, Math.Min(100f, v));

        private static void DrawMeter()
        {
            const float x = 0.10f, y = 0.06f, w = 0.12f, h = 0.012f;
            float fill = GameState.Dread / 100f;

            // moves Pale -> red as climbs
            int g = (int)(200 - 170 * fill);
            int b = (int)(180 - 150 * fill);

            Hud.Rect(x, y, w + 0.004f, h + 0.006f, 0, 0, 0, 160);
            Hud.Rect(x - w / 2f + (w * fill) / 2f, y, w * fill, h, 220, g, b, 220);
            Hud.Text("DREAD", x, y - 0.035f, 0.32f);
        }
    }
}
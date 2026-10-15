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
        private const float RegionDepletedRise = 3f;

        public DreadMeter()
        {
            EventHandlers["dredge:fishCaught"] += new Action<dynamic>(_ => Add(-CatchRelief));
            EventHandlers["dredge:fishLost"] += new Action(() => Add(MissPenalty));
            EventHandlers["dredge:regionDepleted"] += new Action(() => Add(RegionDepletedRise));
            EventHandlers["dredge:gameOver"] += new Action(ResetAfterGameOver);

            // Debug: /dread 90
            RegisterCommand("dread", new Action<int, System.Collections.Generic.List<object>, string>((src, args, raw) => { if (args.Count > 0 && float.TryParse(args[0].ToString(), out float v)) SetDread(v); }), false);

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

            // Check the boat position while aboard; otherwise use the player's position.
            Vector3 dockCheckPosition = GameState.AtSea && ped.CurrentVehicle != null
                ? ped.CurrentVehicle.Position
                : ped.Position;
            bool atDock = Vector3.DistanceSquared(dockCheckPosition, GameState.DockPos)
                < GameState.DockRadius * GameState.DockRadius;

            if (!atDock) GameState.HasLeftDock = true;
            if (atDock && !GameState.WasAtDock && GameState.HasLeftDock) ReturnToDock();
            GameState.WasAtDock = atDock;

            return Task.FromResult(0);
        }

        private void ReturnToDock()
        {
            SetDread(0f);
            TriggerEvent("dredge:fishSold");
            Screen.ShowNotification("You made it back. Catch sold.");
        }

        private void ResetAfterGameOver()
        {
            SetDread(0f);
            GameState.WasAtDock = false;
            GameState.HasLeftDock = false;
            TriggerEvent("dredge:dreadChanged", 0f);
        }

        private void Add(float amount) => SetDread(GameState.Dread + amount);

        private void SetDread(float value)
        {
            float dread = Clamp(value);
            if (Math.Abs(GameState.Dread - dread) < 0.001f) return;

            GameState.Dread = dread;
            TriggerEvent("dredge:dreadChanged", dread);
        }

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
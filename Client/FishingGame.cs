using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using static CitizenFX.Core.Native.API;

namespace DredgeClient
{
    public class FishingGame : BaseScript
    {
        private enum State { Idle, Waiting, Reeling, Result }
        private const int KeyAction = 38;   // E (INPUT_PICKUP)
        private const int KeyCancel = 177;  // Backspace (INPUT_CELLPHONE_CANCEL)
        private const int KeyHorn = 86;     // TO disable horn while fishing

        // Bar layout in screen space
        private const float BarX = 0.5f, BarY = 0.85f, BarW = 0.30f, BarH = 0.025f;
        private const int MaxFishPerRegion = 3;
        private const float FishingRegionRadius = 50f;

        private readonly Random _rng = new Random();
        private State _state = State.Idle;
        private float _timer;
        private Vector3 _regionCentre;
        private bool _hasRegionCentre;

        private float _markerPos;      // 0..1 along the bar
        private float _markerDir = 1f;
        private float _markerSpeed;    // bar-widths per second
        private float _zoneCentre;     // 0..1
        private float _zoneWidth;      // 0..1

        private string _resultText = "";
        private int _hitCount;
        private int _requiredHits;

        public int FishCaught { get; private set; }
        private bool _regionDepleted;
        private int _regionFished;

        public FishingGame()
        {
            EventHandlers["dredge:fishSold"] += new Action(ResetCatch);
            EventHandlers["dredge:gameOver"] += new Action(ResetAfterGameOver);
            Tick += OnTick;
        }

        private void ResetCatch()
        {
            FishCaught = 0;
            GameState.FishHeld = 0;
        }

        private void ResetAfterGameOver()
        {
            ResetCatch();
            _state = State.Idle;
            _timer = 0f;
            _hitCount = 0;
            _requiredHits = 0;
            _regionFished = 0;
            _regionDepleted = false;
            _hasRegionCentre = false;
        }

        private Task OnTick()
        {
            float dt = GetFrameTime();
            Ped ped = Game.PlayerPed;
            Vehicle boat = ped.CurrentVehicle;
            bool inBoat = ped.IsInBoat && boat != null;

            if (inBoat) UpdateFishingRegion(boat.Position);

            bool canFish = inBoat && boat.Speed < 4f && !_regionDepleted; //nearly stopped

            if (inBoat)
            {
                DrawText2D($"Fish: {FishCaught}", 0.92f, 0.05f, 0.45f);
            }

            if (canFish)
            {
                DisableControlAction(0, KeyHorn, true); //disable horn
            }
                
            switch (_state)
            {
                case State.Idle:
                    if (_regionDepleted)
                    {
                        DrawText2D("Fishing region depleted", 0.5f, 0.90f, 0.40f);
                    }
                    else if (canFish)
                    {
                        DrawText2D("[E] Cast line", 0.5f, 0.90f, 0.40f);
                        if (IsControlJustPressed(0, KeyAction)) StartWaiting();
                    }
                    break;

                case State.Waiting:
                    DrawText2D("Waiting for a bite...   [Backspace] Reel in", 0.5f, 0.90f, 0.40f);
                    if (!canFish || IsControlJustPressed(0, KeyCancel))
                    {
                        _state = State.Idle;
                        break;
                    }
                    _timer -= dt;
                    if (_timer <= 0f) StartReeling();
                    break;

                case State.Reeling:
                    if (!canFish)
                    {
                        Resolve(false);
                        break;
                    }
                    UpdateMarker(dt);
                    DrawBar();
                    DrawText2D($"[E] Hook it!", 0.5f, 0.90f, 0.40f);
                    if (IsControlJustPressed(0, KeyAction))
                    {
                        bool hit = Math.Abs(_markerPos - _zoneCentre) <= _zoneWidth / 2f;
                        Resolve(hit);
                    }
                    break;

                case State.Result:
                    DrawText2D(_resultText, 0.5f, 0.80f, 0.60f);
                    _timer -= dt;
                    if (_timer <= 0f) _state = State.Idle;
                    break;
            }

            return Task.FromResult(0);
        }

        // Logic 

        private void UpdateFishingRegion(Vector3 boatPosition)
        {
            bool movedToNewRegion = !_hasRegionCentre
                || Vector3.DistanceSquared(boatPosition, _regionCentre)
                    > FishingRegionRadius * FishingRegionRadius;

            if (!movedToNewRegion) return;

            _regionCentre = boatPosition;
            _hasRegionCentre = true;
            _regionFished = 0;
            _regionDepleted = false;
        }

        private void StartWaiting()
        {
            _hitCount = 0;
            _requiredHits = _rng.Next(1, 6);
            _state = State.Waiting;
            _timer = 2f + (float)_rng.NextDouble() * 3f; // 2-5s bite delay
        }

        private void StartReeling()
        {
            _state = State.Reeling;
            SetReelingTarget();
        }

        private void SetReelingTarget()
        {
            // Gets harder as you catch more
            float difficulty = Math.Min(FishCaught * 0.05f, 0.5f);
            _zoneWidth = Math.Max(0.18f - difficulty * 0.2f, 0.08f);
            _zoneCentre = _zoneWidth / 2f + (float)_rng.NextDouble() * (1f - _zoneWidth);
            _markerSpeed = 0.6f + difficulty;
            _markerPos = 0f;
            _markerDir = 1f;

            PlaySoundFrontend(-1, "SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET", true);
        }

        private void UpdateMarker(float dt)
        {
            _markerPos += _markerDir * _markerSpeed * dt;
            if (_markerPos >= 1f) { _markerPos = 1f; _markerDir = -1f; }
            if (_markerPos <= 0f) { _markerPos = 0f; _markerDir = 1f; }
        }

        private void Resolve(bool hit)
        {
            if (hit)
            {
                _hitCount++;

                if (_hitCount >= _requiredHits)
                {
                    FishCaught++;
                    GameState.FishHeld = FishCaught;
                    _regionFished++;
                    if (_regionFished >= MaxFishPerRegion)
                    {
                        _regionDepleted = true;
                        TriggerEvent("dredge:regionDepleted");
                    }
                    _resultText = "Caught something...";
                    TriggerEvent("dredge:fishCaught", FishCaught);
                }
                else
                {
                    _resultText = $"Keep going! {_hitCount}/{_requiredHits}"; //TO-DO Remove this before finshed
                    SetReelingTarget();
                    return;
                }
            }
            else
            {
                _resultText = "It got away.";
                TriggerEvent("dredge:fishLost");
            }
            _state = State.Result;
            _timer = 1.5f;
        }

        //Drawing
        //TO-DO swap for NUI 

        private void DrawBar()
        {
            float left = BarX - BarW / 2f;

            // Background
            Rect(BarX, BarY, BarW + 0.006f, BarH + 0.010f, 0, 0, 0, 180);
            // Target zone
            Rect(left + _zoneCentre * BarW, BarY, _zoneWidth * BarW, BarH, 70, 170, 140, 210);
            // Marker
            Rect(left + _markerPos * BarW, BarY, 0.003f, BarH * 1.6f, 255, 255, 255, 255);
        }

        private static void Rect(float x, float y, float w, float h, int r, int g, int b, int a)
        {
            // Function.Call avoids the DrawRect signature differing between CitizenFX versions
            Function.Call(Hash.DRAW_RECT, x, y, w, h, r, g, b, a, false);
        }

        private static void DrawText2D(string text, float x, float y, float scale)
        {
            SetTextFont(4);
            SetTextScale(scale, scale);
            SetTextColour(255, 255, 255, 230);
            SetTextCentre(true);
            SetTextOutline();
            BeginTextCommandDisplayText("STRING");
            AddTextComponentSubstringPlayerName(text);
            EndTextCommandDisplayText(x, y);
        }
    }
}
using System;
using System.Collections.Generic;
using CitizenFX.Core;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;

namespace DredgeClient
{
    public class Class1 : BaseScript
    {
        public Class1()
        {
            EventHandlers["onClientResourceStart"] += new Action<string>(OnClientResourceStart);
        }

        private void OnClientResourceStart(string resourceName)
        {
            if (GetCurrentResourceName() != resourceName) return;

            RegisterCommand("car", new Action<int, List<object>, string>(async (source, args, raw) =>
            {
                // account for the argument not being passed
                var model = "adder";
                if (args.Count > 0)
                {
                    model = args[0].ToString();
                }

                // check if the model actually exists
                // assumes the directive `using static CitizenFX.Core.Native.API;`
                var hash = (uint) GetHashKey(model);
                if (!IsModelInCdimage(hash) || !IsModelAVehicle(hash))
                {
                    TriggerEvent("chat:addMessage", new
                    {
                        color = new[] { 255, 0, 0 },
                        args = new[] { "[CarSpawner]", $"It might have been a good thing that you tried to spawn a {model}. Who even wants their spawning to actually ^*succeed?" }
                    });
                    return;
                }

                // create the vehicle
                var vehicle = await World.CreateVehicle(model, Game.PlayerPed.Position, Game.PlayerPed.Heading);

                // set the player ped into the vehicle and driver seat
                Game.PlayerPed.SetIntoVehicle(vehicle, VehicleSeat.Driver);

                // tell the player
                TriggerEvent("chat:addMessage", new
                {
                    color = new[] {255, 0, 0},
                    args = new[] {"[CarSpawner]", $"Woohoo! Enjoy your new ^*{model}!"}
                });
            }), false);

            //TO-DO REMOVE LATER 
            RegisterCommand("Docks", new Action(async () => await ToDock()), false);
            RegisterCommand("Atmo", new Action(async () => await setAtmosphere()), false);
            RegisterCommand("ClearAtmo", new Action(async () => await clearAtmosphere()), false);
            RegisterCommand("FishCounter", new Action(async () => await DrawFishCount()), false);
            //END OF TO-DO 


            RegisterCommand("Start", new Action(async () => await StartJob()), false);
        }


        
        private async Task ToDock()
        {
                var pos = new Vector3(-813.49f, -1485.70f, 0.0f);
                var Boat = await World.CreateVehicle("tug", pos, 120);
                Game.PlayerPed.SetIntoVehicle(Boat, VehicleSeat.Driver);
                
                //Speed up boat a little 
                SetVehicleEnginePowerMultiplier(Boat.Handle,1000.0f);
        }

        private async Task setAtmosphere()
        {
            NetworkOverrideClockTime(2,0,0);
            SetWeatherTypeNowPersist("FOGGY");
            SetTimecycleModifier("nervousRON_fog");

        }

        private async Task  clearAtmosphere()
        {
            NetworkOverrideClockTime(12,0,0);
            ClearTimecycleModifier();
            SetWeatherTypeNowPersist("CLEAR");
        }

        public async Task DrawFishCount(){

            int fishCount = 0;

            while(true){
                SetTextScale(0.6f,0.6f);
                SetTextColour(255,255,255,255);
                SetTextFont(4);
                BeginTextCommandDisplayText("STRING");
                AddTextComponentSubstringPlayerName($"Fish: {fishCount}");
                EndTextCommandDisplayText(0.05f,0.05f);
                await Delay(500);
                fishCount++;
            }
        }

        public async Task StartJob(){
            setAtmosphere();
            ToDock();
            DrawFishCount();
        }


    }

}
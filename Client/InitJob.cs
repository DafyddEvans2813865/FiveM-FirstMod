using System;
using System.Collections.Generic;
using CitizenFX.Core;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;

namespace DredgeClient
{
    public class InitJob : BaseScript
    {
        public InitJob()
        {
            EventHandlers["onClientResourceStart"] += new Action<string>(OnClientResourceStart);
        }

        private void OnClientResourceStart(string resourceName)
        {
            if (GetCurrentResourceName() != resourceName) return;
            
            //TO-DO REMOVE LATER 
            RegisterCommand("Docks", new Action(async () => await ToDock()), false);
            RegisterCommand("Atmo", new Action( () => setAtmosphere()), false);
            RegisterCommand("ClearAtmo", new Action( () => clearAtmosphere()), false);
            RegisterCommand("FishCounter", new Action(async () => await DrawFishCount()), false);
            //END OF TO-DO 

            RegisterCommand("Start", new Action(async () => await StartJob()), false);
        }

        private async Task ToDock()
        {
                var pos = new Vector3(-813.49f, -1485.70f, 0.0f);
                var Boat = await World.CreateVehicle("tug", pos, 120);
                Game.PlayerPed.SetIntoVehicle(Boat, VehicleSeat.Driver);
                
                //DEBUG -> Speed up boat a little 
                SetVehicleEnginePowerMultiplier(Boat.Handle,1000.0f);
        }

        private void setAtmosphere()
        {
            NetworkOverrideClockTime(2,0,0);
            SetWeatherTypeNowPersist("FOGGY");
            SetTimecycleModifier("nervousRON_fog");
        }

        private void clearAtmosphere()
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
            await ToDock();
            _ = DrawFishCount(); //as forever loop needs to be a background operation 

        }

    }

}
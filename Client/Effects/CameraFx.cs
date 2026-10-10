using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;


namespace DredgeClient.Effects
{
    public static class CameraFx
    {
        public static async Task ShakeCamera(string shakeType, float intensity, int duration)
        {
            ShakeGameplayCam(shakeType, intensity);
            await BaseScript.Delay(duration);
            StopGameplayCamShaking(true);
        }

        
        //TO-DO:Create different shake types for different Scares
        public static async Task Jolt(float intensity, int duration)
        {
            await CameraFx.ShakeCamera("SMALL_EXPLOSION_SHAKE", intensity, duration);   
        }
    }


}
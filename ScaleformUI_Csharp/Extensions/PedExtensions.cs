using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace ScaleformUI.Extensions
{
    /// <summary>Common extension methods for peds</summary>
    public static class PedExtensions
    {
        /// <summary>NetworkGetEntityFromNetworkId</summary>
        /// <param name="ped"></param>
        /// <returns></returns>
        public static int EntityId(this Ped ped) => NetworkGetEntityFromNetworkId(ped.NetworkId);

        /// <summary> Creates a mugshot of the ped </summary>
        /// <param name="ped"></param>
        /// <param name="transparent"></param>
        /// <returns></returns>
        public static Task<Tuple<int, string>> GetPedMugshotAsync(
            this Ped ped,
            bool transparent = false) =>
                ped.EntityId().GetPedMugshotAsync(transparent);

        /// <summary> Creates a mugshot of the ped from the entity</summary>
        /// <param name="entityId">entity network id</param>
        /// <param name="transparent"></param>
        /// <returns></returns>
        public static async Task<Tuple<int, string>> GetPedMugshotAsync(
            this int entityId,
            bool transparent = false)
        {
            int mugshot = RegisterPedheadshot(entityId);
            if (transparent) mugshot = RegisterPedheadshotTransparent(entityId);

            while (!IsPedheadshotReady(mugshot))
                await BaseScript.Delay(1);

            //return the texture string and mugshot handle
            string txd = GetPedheadshotTxdString(mugshot);

            return new Tuple<int, string>(mugshot, txd);
        }
    }
}

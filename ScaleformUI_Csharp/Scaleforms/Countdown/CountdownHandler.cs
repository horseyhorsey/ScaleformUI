﻿using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Threading.Tasks;

namespace ScaleformUI.Scaleforms.Countdown
{
    public class CountdownHandler
    {
        private const string SCALEFORM_NAME = "COUNTDOWN";
        private Scaleform _sc;

        public CountdownHandler() { }

        /// <summary>
        /// This will start a countdown and play the audio for each step, default is 3, 2, 1, GO
        /// method is awaitable and will return when the countdown shows "GO"
        /// </summary>
        /// <param name="number">number to start counting down from</param>
        /// <param name="hudColor">hud colour for the background of the countdown number</param>
        /// <param name="countdownAudioName">audio name for countdown e.g. 321, 3_2_1, Countdown_321, Countdown_1</param>
        /// <param name="countdownAudioRef">audio reference for countdown e.g. Car_Club_Races_Pursuit_Series_Sounds, HUD_MINI_GAME_SOUNDSET, Island_Race_Soundset, DLC_AW_Frontend_Sounds, DLC_Air_Race_Frontend_Sounds, Island_Race_Soundset, DLC_Stunt_Race_Frontend_Sounds</param>
        /// <param name="goAudioName">audio name for GO message e.g. Go, Countdown_Go</param>
        /// <param name="goAudioRef">audio ref for Go message e.g. Car_Club_Races_Pursuit_Series_Sounds, HUD_MINI_GAME_SOUNDSET, Island_Race_Soundset, DLC_AW_Frontend_Sounds, DLC_Air_Race_Frontend_Sounds, Island_Race_Soundset, DLC_Stunt_Race_Frontend_Sounds</param>
        public async Task Start(
            int number = 3,
            HudColor hudColor = HudColor.HUD_COLOUR_GREEN,
            string countdownAudioName = "321",
            string countdownAudioRef = "Car_Club_Races_Pursuit_Series_Sounds",
            string goAudioName = "Go",
            string goAudioRef = "Car_Club_Races_Pursuit_Series_Sounds")
        {
            await Load();

            if (_sc.IsLoaded)
                DisplayCountdown();

            int r = 255, g = 255, b = 255, a = 255;
            API.GetHudColour((int)hudColor, ref r, ref g, ref b, ref a);

            int gameTime = API.GetGameTimer();

            while (number >= 0)
            {
                if ((API.GetGameTimer() - gameTime) < 1000)
                    await BaseScript.Delay(0);
                else
                {
                    API.PlaySoundFrontend(-1, countdownAudioName, countdownAudioRef, true);
                    gameTime = API.GetGameTimer();
                    ShowMessage(number, r, g, b);
                    number--;
                    await BaseScript.Delay(0);
                }
            }

            API.PlaySoundFrontend(-1, goAudioName, goAudioRef, true);
            ShowMessage("CNTDWN_GO", r, g, b);
            Dispose();
        }

        private async Task Load()
        {
            if (_sc is not null) return;

            API.RequestScriptAudioBank("HUD_321_GO", false);
            _sc = new Scaleform(SCALEFORM_NAME);
            var timeout = 1000;
            var start = DateTime.Now;
            while (!_sc.IsLoaded && DateTime.Now.Subtract(start).TotalMilliseconds < timeout) await BaseScript.Delay(0);
        }

        private async void Dispose()
        {
            // Delay the dispose to allow the scaleform to finish playing
            // this allows the player to act on the last message
            await BaseScript.Delay(1000);
            _sc.Dispose();
            _sc = null;
        }

        private void ShowMessage(int number, int r = 255, int g = 255, int b = 255)
        {
            ShowMessage($"{number}", r, g, b);
        }

        private void ShowMessage(string message, int r = 255, int g = 255, int b = 255)
        {
            _sc.CallFunction("SET_MESSAGE", message, r, g, b, true);
            _sc.CallFunction("FADE_MP", message, r, g, b);
        }

        private async Task DisplayCountdown()
        {
            while (_sc != null && _sc.IsLoaded)
            {
                await BaseScript.Delay(0);
                _sc.Render2D();
            }
        }
    }
}
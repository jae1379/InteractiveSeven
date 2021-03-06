﻿using InteractiveSeven.Core;
using InteractiveSeven.Twitch.Model;
using System.Linq;
using TwitchLib.Client.Interfaces;

namespace InteractiveSeven.Twitch.Commands
{
    public class CostsCommand : BaseCommand
    {
        private readonly ITwitchClient _twitchClient;

        public CostsCommand(ITwitchClient twitchClient)
            : base(x => x.CostsCommandWords, x => true)
        {
            _twitchClient = twitchClient;
        }

        public override void Execute(in CommandData commandData)
        {
            string argument = commandData.Arguments.FirstOrDefault();
            string message = "";
            if (argument is null)
            {
                message = "Specify cost type to check (color, status).";
            }
            else if (argument.EqualsIns("color") || argument.EqualsIns("menu"))
            {
                message = GetColorCostMessage();
            }
            else if (argument.EqualsIns("status") || argument.EqualsIns("statuses"))
            {
                message = GetStatusCostMessage();
            }

            _twitchClient.SendMessage(commandData.Channel, message);
        }

        private string GetStatusCostMessage()
        {
            string message = "";

            if (Settings.BattleSettings.AllowStatusEffects)
            {
                foreach (var effect in Settings.BattleSettings.AllStatusEffects.Where(x => x.Enabled))
                {
                    message += $"[{effect.Name}: {effect.Cost}] ";
                }
            }

            return message;
        }

        private string GetColorCostMessage()
        {
            string message = "";

            if (Settings.MenuSettings.Enabled)
            {
                message += $"[Change Menu: {Settings.MenuSettings.BitCost}] ";

                if (Settings.MenuSettings.EnableMakoCommand)
                {
                    message += $"[Mako Mode: {Settings.MenuSettings.MakoModeCost}] ";
                }

                if (Settings.MenuSettings.EnableRainbowCommand)
                {
                    message += $"[Rainbow Mode: {Settings.MenuSettings.RainbowModeCost}] ";
                }

            }

            return message;
        }
    }
}
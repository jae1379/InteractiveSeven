﻿using InteractiveSeven.Core.Data;
using InteractiveSeven.Core.Data.Items;
using InteractiveSeven.Core.Memory;
using InteractiveSeven.Core.Model;
using InteractiveSeven.Core.Models;
using InteractiveSeven.Twitch.Model;
using System.Linq;
using TwitchLib.Client.Interfaces;

namespace InteractiveSeven.Twitch.Commands
{
    public class WeaponCommand : BaseCommand
    {
        private readonly IEquipmentAccessor _equipmentAccessor;
        private readonly IInventoryAccessor _inventoryAccessor;
        private readonly IMateriaAccessor _materiaAccessor;
        private readonly GilBank _gilBank;
        private readonly ITwitchClient _twitchClient;

        public WeaponCommand(IEquipmentAccessor equipmentAccessor,
            IInventoryAccessor inventoryAccessor, IMateriaAccessor materiaAccessor,
            GilBank gilBank, ITwitchClient twitchClient)
            : base(x => x.WeaponCommandWords, x => x.EquipmentSettings.Enabled)
        {
            _equipmentAccessor = equipmentAccessor;
            _inventoryAccessor = inventoryAccessor;
            _materiaAccessor = materiaAccessor;
            _gilBank = gilBank;
            _twitchClient = twitchClient;
        }

        public override void Execute(CommandData commandData)
        {
            (bool isValidName, CharNames charName) =
                CharNames.GetByName(commandData.Arguments.FirstOrDefault());
            var weaponText = commandData.Arguments.ElementAtOrDefault(1);

            if (!isValidName
                || !int.TryParse(weaponText ?? "", out int value)
                || !Weapons.IsValid(charName, value))
            {
                _twitchClient.SendMessage(commandData.Channel, "Invalid Request - Specify character and weapon number like this !weapon cloud 15");
                return;
            }

            int withdrawn = 0;
            if (!CanOverrideBitRestriction(commandData.User))
            {
                const int cost = 100; // TODO: Configurable Costs
                int balance;
                (balance, withdrawn) = _gilBank.Withdraw(commandData.User, cost, true);
                if (withdrawn < cost)
                {
                    _twitchClient.SendMessage(commandData.Channel, $"Insufficient gil. You only have {balance} gil and needed {cost}");
                    return;
                }
            }

            var weapon = Weapons.GetByValue(charName, value);
            int existingWeaponId = _equipmentAccessor.GetCharacterWeapon(charName);
            if (weapon.WeaponId == existingWeaponId)
            {
                _twitchClient.SendMessage(commandData.Channel,
                    $"Sorry, {charName.DefaultName} already has {weapon.Name} equipped.");
                if (withdrawn > 0) // return the gil, since we did nothing
                {
                    _gilBank.Deposit(commandData.User, withdrawn);
                }
                return;
            }

            _equipmentAccessor.SetCharacterWeapon(charName, weapon.WeaponId);
            if (Settings.EquipmentSettings.KeepPreviousEquipment)
            {
                var removedWeapon = Weapons.GetByWeaponId(charName, existingWeaponId);
                _inventoryAccessor.AddItem(removedWeapon.ItemId, 1, true);
            }
            _materiaAccessor.RemoveWeaponMateria(charName);
            _twitchClient.SendMessage(commandData.Channel,
                $"Equipped {charName.DefaultName} with a {weapon.Name}.");
        }

        private bool CanOverrideBitRestriction(ChatUser user)
            => (Settings.EquipmentSettings.AllowModOverride && user.IsMod)
               || user.IsMe || user.IsBroadcaster;
    }
}
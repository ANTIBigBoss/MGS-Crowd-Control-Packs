using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ConnectorLib;
using ConnectorLib.Inject.VersionProfiles;
using ConnectorLib.Memory;
using CrowdControl.Common;
using Log = CrowdControl.Common.Log;
using ConnectorType = CrowdControl.Common.ConnectorType;
using AddressChain = ConnectorLib.Inject.AddressChaining.AddressChain;
using System.Diagnostics.CodeAnalysis;

namespace CrowdControl.Games.Packs.MGS2;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class MGS2 : InjectEffectPack
{
    public override Game Game { get; } = new("METAL GEAR SOLID2", "MGS2", "PC", ConnectorType.PCConnector);

    public MGS2(UserRecord player, Func<CrowdControlBlock, bool> responseHandler, Action<object> statusUpdateHandler)
        : base(player, responseHandler, statusUpdateHandler)
    {
        VersionProfiles = [new("METAL GEAR SOLID2", InitGame, DeinitGame)];
    }

    #region AddressChains

    // Game State Checks
    private AddressChain characterString;
    private AddressChain mapString;
    private AddressChain pauseState;

    // Alert Statuses
    private AddressChain alertTimer;

    // Snake/Raiden
    private AddressChain flinchPlayer;
    private AddressChain equippedWeapon;
    private AddressChain equippedItem;
    private AddressChain weaponClipCount;

    // Weapons and Items
    private AddressChain weaponsAndItemPointer;

    #endregion

    #region [De]init

    private void InitGame()
    {
        Connector.PointerFormat = PointerFormat.Absolute64LE;

        characterString = AddressChain.Parse(Connector, "\"METAL GEAR SOLID2.exe\"+949340=>+1C");
        mapString = AddressChain.Parse(Connector, "\"METAL GEAR SOLID2.exe\"+949340=>+2C");
        pauseState = AddressChain.Parse(Connector, "\"METAL GEAR SOLID2.exe\"+17DBC7C");
        alertTimer = AddressChain.Parse(Connector, "\"METAL GEAR SOLID2.exe\"+16C9568");
        flinchPlayer = AddressChain.Parse(Connector, "\"METAL GEAR SOLID2.exe\"+17DF660=>+A8");
        weaponsAndItemPointer = AddressChain.Parse(Connector, "\"METAL GEAR SOLID2.exe\"+1540C20=>+0");
        equippedWeapon = AddressChain.Parse(Connector, "\"METAL GEAR SOLID2.exe\"+949340=>+104");
        equippedItem = AddressChain.Parse(Connector, "\"METAL GEAR SOLID2.exe\"+949340=>+106");
        weaponClipCount = AddressChain.Parse(Connector, "\"METAL GEAR SOLID2.exe\"+16E994C");
    }

    private void DeinitGame()
    {

    }

    #endregion

    #region Weapon and Items Class

    public class WeaponItemManager
    {
        private readonly MGS2 mainClass;

        public WeaponItemManager(MGS2 mainClass)
        {
            this.mainClass = mainClass;
        }

        private int GetWeaponOffset(Weapons weapon)
        {
            return ((int)weapon) * 2;
        }

        private int GetItemOffset(Items item)
        {
            return 0x90 + (((int)item) * 2);
        }

        // Not currently used but could be useful for future effects
        private int GetMaxAmmoOffset(Weapons maxWeapon)
        {
            return 0x2F4 + GetWeaponOffset(maxWeapon);
        }

        public AddressChain GetWeaponAddress(Weapons weapon)
        {
            return mainClass.weaponsAndItemPointer + GetWeaponOffset(weapon);
        }

        public AddressChain GetItemAddress(Items item)
        {
            return mainClass.weaponsAndItemPointer + GetItemOffset(item);
        }

        public short ReadWeaponAmmo(Weapons weapon)
        {
            AddressChain ammoChain = mainClass.weaponsAndItemPointer + GetWeaponOffset(weapon);
            return mainClass.Get16(ammoChain);
        }

        public void WriteWeaponAmmo(Weapons weapon, short ammoCount)
        {
            AddressChain ammoChain = mainClass.weaponsAndItemPointer + GetWeaponOffset(weapon);
            mainClass.Set16(ammoChain, ammoCount);
        }

        public short ReadItemCapacity(Items item)
        {
            var addr = GetItemAddress(item);
            return mainClass.Get16(addr);
        }

        public void WriteItemCapacity(Items item, short value)
        {
            var addr = GetItemAddress(item);
            mainClass.Set16(addr, value);
        }
    }

    #endregion

    #region Enums and Dictionary

    public enum Weapons
    {
        WEP_NONE = 0,
        WEP_M9,
        WEP_USP,
        WEP_SOCOM,
        WEP_PSG1,
        WEP_RGB6,
        WEP_NIKITA,
        WEP_STINGER,
        WEP_CLAYMORE,
        WEP_C4,
        WEP_CHAFF,
        WEP_STUNG,
        WEP_DMIC,
        WEP_HFBLADE,
        WEP_COOLANT,
        WEP_AKS74U,
        WEP_MAGAZINE,
        WEP_GRENADE,
        WEP_M4,
        WEP_PSG1T,
        WEP_DMIC2,
        WEP_BOOK = 21
    }

    public enum Items
    {
        ITM_NONE = 0,
        ITM_RATION,
        ITM_SCOPE,
        ITM_MEDICINE,
        ITM_BANDAGE,
        ITM_PENTAZEMIN,
        ITM_BDU,
        ITM_ARMOR,
        ITM_STEALTH,
        ITM_MINED,
        ITM_SENSA,
        ITM_SENSB,
        ITM_NVG,
        ITM_THERMG,
        ITM_SCOPE2,
        ITM_DGCAM,
        ITM_BOX1,
        ITM_CIGS,
        ITM_CARD,
        ITM_SHAVER,
        ITM_PHONE,
        ITM_CAMERA,
        ITM_BOX2,
        ITM_BOX3,
        ITM_WETBOX,
        ITM_APSENSR,
        ITM_BOX4,
        ITM_BOX5,
        ITM_RAZOR,
        ITM_SCMSUPR,
        ITM_AKSUPR,
        ITM_CAMERA2,
        ITM_BANDANA,
        ITM_DOGTAGS,
        ITM_MODISC,
        ITM_USPSUPR,
        ITM_SPWIG,
        ITM_WIGA,
        ITM_WIGB,
        ITM_WIGC,
        ITM_WIGD = 40
    }

    private bool IsPlayableStage(string stage)
    {
        return new string[] { 
            // Tanker Chapter
            "w00a", // Aft Deck
            "w00b", // Navigational Deck (Olga Fight)
            "w00c", // Navigational Deck (After Olga Fight)
            "w01a", // Deck A, Crew's Quarters
            "w01b", // Deck A, Crew's Quarters, Starboard
            "w01c", // Deck C, Crew's Quarters
            "w01d", // Deck D, Crew's Quarters
            "w01e", // Deck E, The Bridge
            "w01f", // Deck A, Crew's Lounge
            "w02a", // Engine Room
            "w03a", // Deck-2, Port
            "w03b", // Deck-2, Starboard
            "w04a", // Hold N. 1
            "w04b", // Hold N. 2
            "w04c", // Hold N. 3

            // Plant Chapter
            "w11a", // Strut A Sea Dock
            "w11b", // Strut A Sea Dock (Bomb Disposal)
            "w11c", // Strut A Sea Dock (Fortune Fight)
            "w12a", // Strut A Roof
            "w12c", // Strut A Roof (Last Bomb)
            "w12b", // Strut A Pump Room
            "w13a", // AB Connecting Bridge
            "w13b", // AB Connecting Bridge (With Sensor B)
            "w14a", // Strut B Transformer Room
            "w15a", // BC Connecting Bridge
            "w15b", // BC Connecting Bridge (After Stillman Cutscene)
            "w16a", // Strut C Dining Hall
            "w16b", // Strut C Dining Hall (After 'd014p01')
            "w17a", // CD Connecting Bridge
            "w18a", // Strut D Sediment Pool
            "w19a", // DE Connecting Bridge
            "w20a", // Strut E Parcel Room, 1F
            "w20b", // Strut E Heliport
            "w20c", // Strut E Heliport (Last Bomb)
            "w20d", // Strut E Heliport (After Ninja Cutscene)
            "w21a", // EF Connecting Bridge
            "w22a", // Strut F Warehouse
            "w23b", // FA Connecting Bridge
            "w24a", // Shell 1 Core
            "w24b", // Shell 1 Core B1
            "w24d", // Shell 1 Core B2, Computer's Room
            "w24c", // Shell 1 B1 Hall, Hostages Room
            "w25a", // Shell 1,2 Connecting Bridge
            "w25b", // Shell 1,2 Connecting Bridge (Destroyed)
            "w25c", // Strut L Perimeter
            "w25d", // KL Connecting Bridge
            "w28a", // Strut L Sewage Treatment Facility
            "w31a", // Shell 2 Core, 1F Air Purification Room
            "w31b", // Shell 2 Core, B1 Filtration Chamber NO1
            "w31c", // Shell 2 Core, B1 Filtration Chamber NO2 (Vamp Fight)
            "w31d", // Shell 2 Core, 1F Air Purification Room (w/Emma)
            "w32a", // Strut L Oil Fence
            "w32b", // Strut L Oil Fence (Vamp Fight)
            "w41a", // Arsenal Gear-Stomach
            "w42a", // Arsenal Gear-Jejunum
            "w43a", // Arsenal Gear-Ascending Colon
            "w44a", // Arsenal Gear-Ileum
            "w45a", // Arsenal Gear-Sigmoid Colon
            "w46a", // Arsenal Gear-Rectum
            "w51a", // Arsenal Gear (After Ray Battle)
            "w61a", // Federal Hall

            // Snake Tales
            "a00a", // Alt Deck
            "a00b", // Navigational Deck (Meryl Fight)
            "a00c", // Navigational Deck (After Meryl Fight UNUSED)
            "a01a", // Deck A, Crew's Quarters
            "a01b", // Deck A, Crew's Quarters, Starboard
            "a01c", // Deck C, Crew's Quarters
            "a01d", // Deck D, Crew's Quarters
            "a01e", // Deck E, The Bridge
            "a01f", // Deck A, Crew's Lounge
            "a02a", // Engine Room
            "a03a", // Deck-2, Port
            "a03b", // Deck-2, Starboard
            "a04a", // Hold N. 1
            "a04b", // Hold N. 2
            "a04c", // Hold N. 3
            "a11a", // Strut A Sea Dock
            "a11b", // Strut A Sea Dock (Bomb Disposal)
            "a11c", // Strut A Sea Dock (Fortune Fight)
            "a12a", // Strut A Roof
            "a12c", // Strut A Roof (Last Bomb)
            "a12b", // Strut A Pump Room
            "a13a", // AB Connecting Bridge
            "a13b", // AB Connecting Bridge (With Sensor B)
            "a14a", // Strut B Transformer Room
            "a15a", // BC Connecting Bridge
            "a15b", // BC Connecting Bridge (After Stillman Cutscene)
            "a16a", // Strut C Dining Hall
            "a16b", // Strut C Dining Hall (After 'd014p01')
            "a17a", // CD Connecting Bridge
            "a18a", // Strut D Sediment Pool
            "a19a", // DE Connecting Bridge
            "a20a", // Strut E Parcel Room, 1F
            "a20b", // Strut E Heliport
            "a20c", // Strut E Heliport (Last Bomb)
            "a20d", // Strut E Heliport (After Ninja Cutscene)
            "a21a", // EF Connecting Bridge
            "a22a", // Strut F Warehouse
            "a23b", // FA Connecting Bridge
            "a24a", // Shell 1 Core
            "a24b", // Shell 1 Core B1
            "a24d", // Shell 1 Core B2, Computer's Room
            "a24c", // Shell 1 B1 Hall, Hostages Room
            "a25a", // Shell 1,2 Connecting Bridge
            "a25b", // Shell 1,2 Connecting Bridge (Destroyed)
            "a25c", // Strut L Perimeter
            "a25d", // KL Connecting Bridge
            "a28a", // Strut L Sewage Treatment Facility
            "a31a", // Shell 2 Core, 1F Air Purification Room
            "a31b", // Shell 2 Core, B1 Filtration Chamber NO1
            "a31c", // Shell 2 Core, B1 Filtration Chamber NO2 (Vamp Fight)
            "a31d", // Shell 2 Core, 1F Air Purification Room (w/Emma)
            "a32a", // Strut L Oil Fence
            "a32b", // Strut L Oil Fence (Vamp Fight)
            "a41a", // Arsenal Gear-Stomach
            "a42a", // Arsenal Gear-Jejunum
            "a43a", // Arsenal Gear-Ascending Colon
            "a44a", // Arsenal Gear-Ileum
            "a45a", // Arsenal Gear-Sigmoid Colon
            "a46a", // Arsenal Gear-Rectum
            "a51a", // Arsenal Gear (After Ray Battle)
            "a61a", // Federal Hall

            // VR Missions Sneaking/Eliminate All
            "vs01a",
            "vs02a",
            "vs03a",
            "vs04a",
            "vs05a",
            "vs06A",
            "vs07a",
            "vs08a",
            "vs09A",
            "vs10A",

            // VR Missions Variety
            "sp01a",
            "sp02a",
            "sp03a",
            "sp04a",
            "sp05a",
            "sp06a",
            "sp07a",
            "sp08a",
            
            // VR Missions Streaking Mode
            "st02a",
            "st03a",
            "st04a",
            "st05a",

            // VR Missions First Person Mode
            "sp21",
            "sp22",
            "sp23",
            "sp24",
            "sp25",

            // VR Missions Weapons Mode
            "wp01a", // (USP/SOCOM)
            "wp02a", // (USP/SOCOM)
            "wp03a", // (USP/SOCOM)
            "wp04a", // (USP/SOCOM)
            "wp05a", // (USP/SOCOM)

            "wp11a", // (M4/AK74U)
            "wp12a", // (M4/AK74U)
            "wp13a", // (M4/AK74U)
            "wp14a", // (M4/AK74U)
            "wp15a", // (M4/AK74U)

            "wp21a", // (C4/CLAYMORE)
            "wp22a", // (C4/CLAYMORE)
            "wp23a", // (C4/CLAYMORE)
            "wp24a", // (C4/CLAYMORE)
            "wp25a", // (C4/CLAYMORE)

            "wp31a", // (GRENADE)
            "wp32a", // (GRENADE)
            "wp33a", // (GRENADE)
            "wp34a", // (GRENADE)
            "wp35a", // (GRENADE)

            "wp41a", // (PSG-1)
            "wp42a", // (PSG-1)
            "wp43a", // (PSG-1)
            "wp44a", // (PSG-1)
            "wp45a", // (PSG-1)

            "wp51a", // (STINGER)
            "wp52a", // (STINGER)
            "wp53a", // (STINGER)
            "wp54a", // (STINGER)
            "wp55a", // (STINGER)

            "wp61a", // (NIKITA)
            "wp62a", // (NIKITA)
            "wp63a", // (NIKITA)
            "wp64a", // (NIKITA)
            "wp65a", // (NIKITA)

            "wp71a", // (NO WEAPON/HF.BLADE)
            "wp72a", // (NO WEAPON/HF.BLADE)
            "wp73a", // (NO WEAPON/HF.BLADE)
            "wp74a", // (NO WEAPON/HF.BLADE)
            "wp75a", // (NO WEAPON/HF.BLADE)           
            }.Contains(stage);
    }

    private bool IsCutsceneOrMenu(string stage)
    {
        return new string[] {
            // Dev Menu and Others
            "select",
            "n_title",
            "mselect",
            "tales",
            "ending",

            // Special Cutscenes
            "museum",
            "webdemo",

            // Tanker Cutscenes
            "d00t",
            "d01t",
            "d04t",
            "d05t",
            "d10t",
            "d11t",
            "d12t",
            "d12t3",
            "d12t4",
            "d13t",
            "d14t", 

            // Plant Cutscenes
            "d001p01",
            "d001p02",
            "d005p01",
            "d005p03",
            "d010p01",
            "d012p01",
            "d014p01",
            "d021p01",
            "d036p03",
            "d036p05",
            "d045p01",
            "d046p01",
            "d053p01",
            "d055p01",
            "d063p01",
            "d065p02",
            "d070p01",
            "d070p09",
            "d070px9",
            "d078p01",
            "d080p01",
            "d080p06",
            "d080p07",
            "d080p08",
            "d082p01",

        }.Contains(stage);
    }

    #endregion

    #region Memory Getters and Setters

    byte Get8(AddressChain addr)
    {
        return addr.GetByte();
    }

    void Set8(AddressChain addr, byte val)
    {
        addr.SetByte(val);
    }

    short Get16(AddressChain addr)
    {
        return BitConverter.ToInt16(addr.GetBytes(2), 0);
    }

    void Set16(AddressChain addr, short val)
    {
        addr.SetBytes(BitConverter.GetBytes(val));
    }

    int Get32(AddressChain addr)
    {
        return BitConverter.ToInt32(addr.GetBytes(4), 0);
    }

    void Set32(AddressChain addr, int val)
    {
        addr.SetBytes(BitConverter.GetBytes(val));
    }

    float GetFloat(AddressChain addr)
    {
        if (addr.TryGetBytes(4, out byte[] bytes))
        {
            return BitConverter.ToSingle(bytes, 0);
        }
        else
        {
            throw new Exception("Failed to read float value.");
        }
    }

    void SetFloat(AddressChain addr, float val)
    {
        byte[] bytes = BitConverter.GetBytes(val);
        addr.SetBytes(bytes);
    }

    T[] GetArray<T>(AddressChain addr, int count) where T : struct
    {
        int typeSize = System.Runtime.InteropServices.Marshal.SizeOf<T>();
        int totalSize = typeSize * count;
        byte[] bytes = addr.GetBytes(totalSize);

        T[] array = new T[count];
        Buffer.BlockCopy(bytes, 0, array, 0, totalSize);
        return array;
    }

    void SetArray<T>(AddressChain addr, T[] values) where T : struct
    {
        int typeSize = System.Runtime.InteropServices.Marshal.SizeOf<T>();
        int totalSize = typeSize * values.Length;
        byte[] bytes = new byte[totalSize];
        Buffer.BlockCopy(values, 0, bytes, 0, totalSize);
        addr.SetBytes(bytes);
    }

    public static short SetSpecificBits(short currentValue, int startBit, int endBit, int valueToSet)
    {
        int maskLength = endBit - startBit + 1;
        int mask = ((1 << maskLength) - 1) << startBit;
        return (short)((currentValue & ~mask) | ((valueToSet << startBit) & mask));
    }

    private string GetString(AddressChain addr, int maxLength)
    {
        if (maxLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxLength), "maxLength must be positive");

        byte[] data = addr.GetBytes(maxLength);
        if (data == null || data.Length == 0)
        {
            return string.Empty;
        }

        int nullIndex = Array.IndexOf(data, (byte)0);
        if (nullIndex >= 0)
        {
            return Encoding.ASCII.GetString(data, 0, nullIndex);
        }
        else
        {
            return Encoding.ASCII.GetString(data, 0, data.Length);
        }
    }

    protected override bool IsReady(EffectRequest request)
    {
        return GetGameState() == GameState.Ready;
    }


    protected override GameState GetGameState()
    {
        try
        {
            string currentStage = GetMapString();
            if (string.IsNullOrWhiteSpace(currentStage))
                return GameState.Unknown;

            byte pauseStateValue = GetPauseState();
            if (IsCutsceneOrMenu(currentStage) || (pauseStateValue == 1) || (pauseStateValue == 2) || (pauseStateValue == 4))
            {
                return GameState.WrongMode;
            }

            return GameState.Ready;
        }
        catch
        {
            return GameState.Unknown;
        }
    }


    #endregion

    #region Effect Helpers

    #region Game State Tracking

    private string currentCharacter = string.Empty;
    private string currentMap = string.Empty;

    private string GetCharacterString()
    {
        string currentCharacter = GetString(characterString, 8);
        Log.Message($"Current Character: {currentCharacter}");
        return currentCharacter;
    }

    private string GetMapString()
    {
        string currentMap = GetString(mapString, 8);
        Log.Message($"Current Map: {currentMap}");
        return currentMap;
    }

    private byte GetPauseState()
    {
        byte pauseStateValue = Get8(pauseState);
        Log.Message($"Pause State: {pauseStateValue}");
        return pauseStateValue;
    }

    #endregion

    #region Alert Statuses

    private async void SetAlertStatus()
    {
        try
        {
            Log.Message("Attempting to set alert time to 9999");
            Set16(alertTimer, 9999);
            await Task.Delay(1000);
            Set16(alertTimer, 5000);
        }
        catch (Exception e)
        {
            Log.Error($"An error occurred while setting alert time: {e.Message}");
        }
    }

    #endregion

    #region Snake/Raiden Effects

    /* I would love to include this but need to find some check for when the player is in an animation
       to stop the flinch from locking the player in whatever animation they were in when it was triggered */
    private async void FlinchPlayer()
    {
        try
        {
            Log.Message("Attempting to flinch player");
            Set8(flinchPlayer, 1);
        }
        catch (Exception e)
        {
            Log.Error($"An error occurred while flinching player: {e.Message}");
        }
    }

    private bool EmptyClip()
    {
        try
        {
            Weapons equippedWep = GetEquippedWeaponEnum();
            if (equippedWep == Weapons.WEP_NONE || equippedWep == Weapons.WEP_DMIC || equippedWep == Weapons.WEP_HFBLADE || equippedWep == Weapons.WEP_COOLANT || equippedWep == Weapons.WEP_DMIC2 || equippedWep == Weapons.WEP_MAGAZINE || equippedWep == Weapons.WEP_BOOK || equippedWep == Weapons.WEP_GRENADE || equippedWep == Weapons.WEP_CLAYMORE || equippedWep == Weapons.WEP_C4 || equippedWep == Weapons.WEP_CHAFF || equippedWep == Weapons.WEP_STUNG || equippedWep == Weapons.WEP_NIKITA || equippedWep == Weapons.WEP_STINGER)
            {
                Log.Message("No valid weapon is currently equipped.");
                return false;
            }
            var manager = new WeaponItemManager(this);
            EmptyWeaponClip(0);
            Log.Message($"Emptied clip of {equippedWep}");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"An error occurred while emptying clip: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Weapons

    private Weapons GetEquippedWeaponEnum()
    {
        short rawVal = Get16(equippedWeapon);
        Log.Message($"Equipped Weapon = {rawVal}");
        return (Weapons)rawVal;
    }

    private short GetWeaponClipCountShort()
    {
        short rawVal = Get16(weaponClipCount);
        Log.Message($"Weapon Clip = {rawVal}");
        return rawVal;
    }

    private void EmptyWeaponClip(short newValue)
    {
        Set16(weaponClipCount, newValue);
    }

    private bool SubtractAmmoFromEquippedWeapon(short amountToSubtract = 1)
    {
        try
        {
            Weapons equippedWep = GetEquippedWeaponEnum();
            // These weapons don't have ammo but follow the same rules as weapons that do and removing them can cause a softlock
            if (equippedWep == Weapons.WEP_NONE || equippedWep == Weapons.WEP_DMIC || equippedWep == Weapons.WEP_HFBLADE || equippedWep == Weapons.WEP_COOLANT || equippedWep == Weapons.WEP_DMIC2)
            {
                Log.Message("No valid weapon is currently equipped.");
                return false;
            }

            var manager = new WeaponItemManager(this);

            short currentAmmo = manager.ReadWeaponAmmo(equippedWep);

            if (currentAmmo <= 0)
            {
                Log.Message($"{equippedWep} has no ammo to subtract.");
                return false;
            }

            short newAmmo = (short)Math.Max(currentAmmo - amountToSubtract, 0);

            if (newAmmo == currentAmmo)
            {
                Log.Message($"{equippedWep} ammo cannot be reduced further.");
                return false;
            }

            manager.WriteWeaponAmmo(equippedWep, newAmmo);

            Log.Message($"Subtracted {amountToSubtract} ammo from {equippedWep}. Ammo: {currentAmmo} -> {newAmmo}");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"An error occurred while subtracting ammo: {ex.Message}");
            return false;
        }
    }

    private bool AddAmmo(Weapons weapon, short amountToAdd, out string message)
    {
        try
        {
            var manager = new WeaponItemManager(this);
            short currentAmmo = manager.ReadWeaponAmmo(weapon);

            // -1 or 65535 means players doesn't have the weapon
            if (currentAmmo == -1)
            {
                message = $"{weapon} has not been obtained yet.";
                return false;
            }

            short newAmmo = (short)Math.Max(currentAmmo + amountToAdd, 0);
            manager.WriteWeaponAmmo(weapon, newAmmo);

            message = $"Added {amountToAdd} ammo to {weapon}. Ammo: {currentAmmo} -> {newAmmo}";
            Log.Message(message);
            return true;
        }
        catch (Exception ex)
        {
            message = $"An error occurred while adding {amountToAdd} ammo to {weapon}: {ex.Message}";
            Log.Error(message);
            return false;
        }
    }

    private bool AddM9Ammo(short amountToAdd)
    {
        return AddAmmo(Weapons.WEP_M9, amountToAdd, out _);
    }

    private bool AddUspAmmo(short amountToAdd)
    {
        return AddAmmo(Weapons.WEP_USP, amountToAdd, out _);
    }

    private bool AddSocomAmmo(short amountToAdd)
    {
        return AddAmmo(Weapons.WEP_SOCOM, amountToAdd, out _);
    }

    private bool AddPsg1Ammo(short amountToAdd)
    {
        return AddAmmo(Weapons.WEP_PSG1, amountToAdd, out _);
    }

    private bool AddRgb6Ammo(short amountToAdd)
    {
        return AddAmmo(Weapons.WEP_RGB6, amountToAdd, out _);
    }

    private bool AddNikitaAmmo(short amountToAdd)
    {
        return AddAmmo(Weapons.WEP_NIKITA, amountToAdd, out _);
    }

    private bool AddStingerAmmo(short amountToAdd)
    {
        return AddAmmo(Weapons.WEP_STINGER, amountToAdd, out _);
    }

    private bool AddClaymoreAmmo(short amountToAdd)
    {
        return AddAmmo(Weapons.WEP_CLAYMORE, amountToAdd, out _);
    }

    private bool AddC4Ammo(short amountToAdd)
    {
        return AddAmmo(Weapons.WEP_C4, amountToAdd, out _);
    }

    private bool AddChaffAmmo(short amountToAdd)
    {
        return AddAmmo(Weapons.WEP_CHAFF, amountToAdd, out _);
    }

    private bool AddStungAmmo(short amountToAdd)
    {
        return AddAmmo(Weapons.WEP_STUNG, amountToAdd, out _);
    }

    private bool AddAks74uAmmo(short amountToAdd)
    {
        return AddAmmo(Weapons.WEP_AKS74U, amountToAdd, out _);
    }

    private bool AddMagazineAmmo(short amountToAdd)
    {
        return AddAmmo(Weapons.WEP_MAGAZINE, amountToAdd, out _);
    }

    private bool AddGrenadeAmmo(short amountToAdd)
    {
        return AddAmmo(Weapons.WEP_GRENADE, amountToAdd, out _);
    }

    private bool AddM4Ammo(short amountToAdd)
    {
        return AddAmmo(Weapons.WEP_M4, amountToAdd, out _);
    }

    private bool AddPsg1tAmmo(short amountToAdd)
    {
        return AddAmmo(Weapons.WEP_PSG1T, amountToAdd, out _);
    }

    private bool AddBookAmmo(short amountToAdd)
    {
        return AddAmmo(Weapons.WEP_BOOK, amountToAdd, out _);
    }

    #endregion

    #region Items

    private Items GetEquippedItemEnum()
    {
        short rawVal = Get16(equippedItem);
        Log.Message($"EquippedItem raw={rawVal}");
        return (Items)rawVal;
    }

    private bool SetItemAmount(Items item, short amountToSet, out string message)
    {
        try
        {
            var manager = new WeaponItemManager(this);
            manager.WriteItemCapacity(item, amountToSet);
            message = $"Set {item} amount to {amountToSet}.";
            Log.Message(message);
            return true;
        }
        catch (Exception ex)
        {
            message = $"An error occurred while setting {item} amount to {amountToSet}: {ex.Message}";
            Log.Error(message);
            return false;
        }
    }

    private bool AddStealth(short amountToSet)
    {
        return SetItemAmount(Items.ITM_STEALTH, amountToSet, out _);
    }

    private bool AddSensa(short amountToSet)
    {
        return SetItemAmount(Items.ITM_SENSA, amountToSet, out _);
    }

    private bool AddSensb(short amountToSet)
    {
        return SetItemAmount(Items.ITM_SENSB, amountToSet, out _);
    }

    private bool AddWetBox(short amountToSet)
    {
        return SetItemAmount(Items.ITM_WETBOX, amountToSet, out _);
    }

    private bool AddBox1(short amountToSet)
    {
        return SetItemAmount(Items.ITM_BOX1, amountToSet, out _);
    }

    private bool AddBox2(short amountToSet)
    {
        return SetItemAmount(Items.ITM_BOX2, amountToSet, out _);
    }

    private bool AddBox3(short amountToSet)
    {
        return SetItemAmount(Items.ITM_BOX3, amountToSet, out _);
    }

    private bool AddBox4(short amountToSet)
    {
        return SetItemAmount(Items.ITM_BOX4, amountToSet, out _);
    }

    private bool AddBox5(short amountToSet)
    {
        return SetItemAmount(Items.ITM_BOX5, amountToSet, out _);
    }

    private bool AddCigs(short amountToSet)
    {
        return SetItemAmount(Items.ITM_CIGS, amountToSet, out _);
    }

    private bool AddCard(short amountToSet)
    {
        return SetItemAmount(Items.ITM_CARD, amountToSet, out _);
    }

    private bool AddBandana(short amountToSet)
    {
        return SetItemAmount(Items.ITM_BANDANA, amountToSet, out _);
    }

    private bool AddSpwig(short amountToSet)
    {
        return SetItemAmount(Items.ITM_SPWIG, amountToSet, out _);
    }

    private bool AddWigA(short amountToSet)
    {
        return SetItemAmount(Items.ITM_WIGA, amountToSet, out _);
    }

    private bool AddWigB(short amountToSet)
    {
        return SetItemAmount(Items.ITM_WIGB, amountToSet, out _);
    }

    private bool AddWigC(short amountToSet)
    {
        return SetItemAmount(Items.ITM_WIGC, amountToSet, out _);
    }

    private bool AddWigD(short amountToSet)
    {
        return SetItemAmount(Items.ITM_WIGD, amountToSet, out _);
    }

    #endregion

    #endregion

    #region Crowd Control Effects

    public override EffectList Effects => new List<Effect>
    {
        // Alert Status
        new ("Set Alert Status", "setAlertStatus")
        {   Price = 80,
            Description = "Triggers an alert status, sending the guards to attack the player",
            Category = "Alert Status" },

        // Snake/Raiden
        new ("Flinch Player", "flinchPlayer")
        {   Price = 40,
            Description = "Makes the player flinch",
            Category = "Snake/Raiden"
        },

        new ("Empty Gun Clip", "emptyGunClip")
        {
            Price = 25,
            Duration = 5,
            Description = "Continuously empties the player's weapon clip for a short duration",
            Category = "Snake/Raiden"
        },

        new ("Infinite Ammo", "infiniteAmmo")
        {
            Price = 100,
            Duration = 20,
            Description = "Gives the player infinite ammo for a short duration",
            Category = "Snake/Raiden"
        },

        // Ammo
        new ("Subtract Ammo", "subtractAmmo")
        {   Price = 1,
            Quantity = 50,
            Description = "Removes a chunk of the player's ammo/quantity from their equipped weapon",
            Category = "Ammo"
        },

        new ("Add M9 Ammo", "addM9Ammo")
        {   Price = 1,
            Quantity = 50,
            Description = "Adds ammo to the M9",
            Category = "Ammo"
        },

        new ("Add USP Ammo", "addUspAmmo")
        {   Price = 1,
            Quantity = 50,
            Description = "Adds ammo to the USP",
            Category = "Ammo"
        },

        new ("Add SOCOM Ammo", "addSocomAmmo")
        {   Price = 1,
            Quantity = 50,
            Description = "Adds ammo to the SOCOM",
            Category = "Ammo"
        },

        new ("Add PSG1 Ammo", "addPsg1Ammo")
        { Price = 1,
            Quantity = 50,
            Description = "Adds ammo to the PSG1",
            Category = "Ammo"
        },

        new ("Add RGB6 Ammo", "addRgb6Ammo")
        {   Price = 1,
            Quantity = 50,
            Description = "Adds ammo to the RGB6",
            Category = "Ammo"
        },

        new ("Add Nikita Ammo", "addNikitaAmmo")
        {   Price = 1,
            Quantity = 50,
            Description = "Adds ammo to the Nikita",
            Category = "Ammo"
        },

        new ("Add Stinger Ammo", "addStingerAmmo")
        {   Price = 1,
            Quantity = 50,
            Description = "Adds ammo to the Stinger",
            Category = "Ammo"
        },

        new ("Add Claymores", "addClaymoreAmmo")
        {   Price = 1,
            Quantity = 50,
            Description = "Adds ammo to the Claymore",
            Category = "Ammo"
        },

        new ("Add C4", "addC4Ammo")
        {   Price = 1,
            Quantity = 50,
            Description = "Adds ammo to the C4",
            Category = "Ammo"
        },

        new ("Add Chaff Grenades", "addChaffAmmo")
        {   Price = 1,
            Quantity = 50,
            Description = "Adds ammo to the Chaff",
            Category = "Ammo"
        },

        new ("Add Stun Grenades", "addStungAmmo")
        {   Price = 1,
            Quantity = 50,
            Description = "Adds ammo to the Stung",
            Category = "Ammo"
        },

        new ("Add AKS74U Ammo", "addAks74uAmmo")
        {   Price = 1,
            Quantity = 50,
            Description = "Adds ammo to the AKS74U",
            Category = "Ammo"
        },

        new ("Add Magazine Ammo", "addMagazineAmmo")
        {   Price = 1,
            Quantity = 50,
            Description = "Gives the player extra Empty Magazines to throw",
            Category = "Ammo"
        },

        new ("Add Grenades", "addGrenadeAmmo")
        {   Price = 1,
            Quantity = 50,
            Description = "Adds to the Grenade ",
            Category = "Ammo"
        },

        new ("Add M4 Ammo", "addM4Ammo")
        {   Price = 1,
            Quantity = 50,
            Description = "Adds ammo to the M4",
            Category = "Ammo"
        },

        new ("Add PSG1-T Ammo", "addPsg1tAmmo")
        {   Price = 1,
            Quantity = 50,
            Description = "Adds ammo to the PSG1-T",
            Category = "Ammo"
        },

        new ("Add Books", "addBookAmmo")
        {   Price = 1,
            Quantity = 50,
            Description = "Adds ammo to the Book",
            Category = "Ammo"
        },

    };

    protected override void StartEffect(EffectRequest request)
    {
        if (!IsReady(request))
        {
            DelayEffect(request);
            return;
        }

        var codeParams = FinalCode(request).Split('_');
        switch (codeParams[0])
        {
            #region Alert Statuses

            case "setAlertStatus":
                TryEffect(request,
                    () => true,
                    () =>
                    {
                        SetAlertStatus();
                        return true;
                    },
                    () => Connector.SendMessage($"{request.DisplayViewer} set the game to Alert Status."),
                    null, true);
                break;

            #endregion

            #region Snake/Raiden

            case "flinchPlayer":
                TryEffect(request,
                    () => true,
                    () =>
                    {
                        FlinchPlayer();
                        return true;
                    },
                    () => Connector.SendMessage($"{request.DisplayViewer} made the player flinch."),
                    null, true);
                break;

            case "emptyGunClip":
                var emptyGunClipDuration = request.Duration = TimeSpan.FromSeconds(30);
                var emptyGunClipAct = RepeatAction(request,
                    () => true,
                    () => Connector.SendMessage($"{request.DisplayViewer} emptied the player's gun clip for {emptyGunClipDuration.TotalSeconds} seconds."),
                    TimeSpan.Zero,
                    () => IsReady(request),
                    TimeSpan.FromMilliseconds(500),
                () =>
                {
                    EmptyClip();
                    return true;
                },
                    TimeSpan.FromMilliseconds(500),
                false);
                emptyGunClipAct.WhenCompleted.Then
                    (_ =>
                    {
                        Connector.SendMessage("Gun clip has been refilled.");
                    });
                break;


            case "infiniteAmmo":
                currentCharacter = GetCharacterString();
                // Bandana for Snake
                if (currentCharacter == "r_tnk0" || currentCharacter == "r_vr_s" || currentCharacter == "r_vr_1")
                {
                    var infiniteAmmoDuration = request.Duration = TimeSpan.FromSeconds(20);
                    var infiniteAmmoAct = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} gave the player the Bandana for {infiniteAmmoDuration.TotalSeconds} seconds."),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(500),
                    () =>
                    {
                        AddBandana(1);
                        Set16(equippedItem, (short)Items.ITM_BANDANA);
                        return true;
                    },
                        TimeSpan.FromMilliseconds(500),
                    false);
                    infiniteAmmoAct.WhenCompleted.Then
                        (_ =>
                        {
                            AddBandana(0);
                            Connector.SendMessage("Bandana effect has ended.");
                        });
                }

                // SP_WIG for Raiden
                else if (currentCharacter == "r_plt0" || currentCharacter == "r_vr_b" || currentCharacter == "r_vr_r")
                {
                    var infiniteAmmoDuration = request.Duration = TimeSpan.FromSeconds(20);
                    var infiniteAmmoAct = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} gave the player the SPWIG for {infiniteAmmoDuration.TotalSeconds} seconds."),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(500),
                    () =>
                    {
                        AddSpwig(1);
                        Set16(equippedItem, (short)Items.ITM_SPWIG);
                        return true;
                    },
                        TimeSpan.FromMilliseconds(500),
                    false);
                    infiniteAmmoAct.WhenCompleted.Then
                        (_ =>
                        {
                            AddSpwig(0);
                            Connector.SendMessage("SPWIG effect has ended.");
                        });
                }
                else
                {
                    Respond(request, EffectStatus.FailPermanent, "Infinite Ammo is not available for this character.");
                }
                break;

            #endregion

            #region Ammo

            case "subtractAmmo":
                {
                    if (codeParams.Length < 2 || !int.TryParse(codeParams[1], out int quantity) || GetWeaponClipCountShort() == 0)
                    {
                        Respond(request, EffectStatus.FailPermanent, "Invalid quantity");
                        break;
                    }


                    TryEffect(request,
                                () => true,
                                () => SubtractAmmoFromEquippedWeapon((short)quantity),
                                () => Connector.SendMessage(
                                    $"{request.DisplayViewer} subtracted {quantity} ammo from the player's equipped weapon."
                                ),
                                null, true);
                    break;
                }

            case "addM9Ammo":
                {
                    if (codeParams.Length < 2 || !int.TryParse(codeParams[1], out int quantity) || new WeaponItemManager(this).ReadWeaponAmmo(Weapons.WEP_M9) == -1)
                    {
                        Respond(request, EffectStatus.FailPermanent, "Invalid quantity or Player does not have the M9.");
                        break;
                    }

                    TryEffect(request,
                        () => true,
                        () =>
                        {
                            AddM9Ammo((short)quantity);
                            return true;
                        },
                        () => Connector.SendMessage($"{request.DisplayViewer} added {quantity} ammo to the M9."),
                        null, true);
                    break;
                }

            case "addUspAmmo":
                {
                    if (codeParams.Length < 2 || !int.TryParse(codeParams[1], out int quantity) || new WeaponItemManager(this).ReadWeaponAmmo(Weapons.WEP_USP) == -1)
                    {
                        Respond(request, EffectStatus.FailPermanent, "Invalid quantity or Player does not have the USP.");
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () =>
                        {
                            AddUspAmmo((short)quantity);
                            return true;
                        },
                        () => Connector.SendMessage($"{request.DisplayViewer} added {quantity} ammo to the USP."),
                        null, true);
                    break;
                }

            case "addSocomAmmo":
                {
                    if (codeParams.Length < 2 || !int.TryParse(codeParams[1], out int quantity) || new WeaponItemManager(this).ReadWeaponAmmo(Weapons.WEP_SOCOM) == -1)
                    {
                        Respond(request, EffectStatus.FailPermanent, "Invalid quantity or Player does not have the SOCOM.");
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () =>
                        {
                            AddSocomAmmo((short)quantity);
                            return true;
                        },
                        () => Connector.SendMessage($"{request.DisplayViewer} added {quantity} ammo to the SOCOM."),
                        null, true);
                    break;
                }

            case "addPsg1Ammo":
                {
                    if (codeParams.Length < 2 || !int.TryParse(codeParams[1], out int quantity) || new WeaponItemManager(this).ReadWeaponAmmo(Weapons.WEP_PSG1) == -1)
                    {
                        Respond(request, EffectStatus.FailPermanent, "Invalid quantity or Player does not have the PSG1.");
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () =>
                        {
                            AddPsg1Ammo((short)quantity);
                            return true;
                        },
                        () => Connector.SendMessage($"{request.DisplayViewer} added {quantity} ammo to the PSG1."),
                        null, true);
                    break;
                }

            case "addRgb6Ammo":
                {
                    if (codeParams.Length < 2 || !int.TryParse(codeParams[1], out int quantity) || new WeaponItemManager(this).ReadWeaponAmmo(Weapons.WEP_RGB6) == -1)
                    {
                        Respond(request, EffectStatus.FailPermanent, "Invalid quantity or Player does not have the RGB6.");
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () =>
                        {
                            AddRgb6Ammo((short)quantity);
                            return true;
                        },
                        () => Connector.SendMessage($"{request.DisplayViewer} added {quantity} grenades into the RGB6."),
                        null, true);
                    break;
                }



            case "addNikitaAmmo":
                {
                    if (codeParams.Length < 2 || !int.TryParse(codeParams[1], out int quantity) || new WeaponItemManager(this).ReadWeaponAmmo(Weapons.WEP_NIKITA) == -1)
                    {
                        Respond(request, EffectStatus.FailPermanent, "Invalid quantity or Player does not have the Nikita.");
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () =>
                        {
                            AddNikitaAmmo((short)quantity);
                            return true;
                        },
                        () => Connector.SendMessage($"{request.DisplayViewer} added {quantity} remote controlled missiles to the Nikita."),
                        null, true);
                    break;
                }

            case "addStingerAmmo":
                {
                    if (codeParams.Length < 2 || !int.TryParse(codeParams[1], out int quantity) || new WeaponItemManager(this).ReadWeaponAmmo(Weapons.WEP_STINGER) == -1)
                    {
                        Respond(request, EffectStatus.FailPermanent, "Invalid quantity or Player does not have the Stinger.");
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () =>
                        {
                            AddStingerAmmo((short)quantity);
                            return true;
                        },
                        () => Connector.SendMessage($"{request.DisplayViewer} added {quantity} missiles to the Stinger."),
                        null, true);
                    break;
                }

            case "addClaymoreAmmo":
                {
                    if (codeParams.Length < 2 || !int.TryParse(codeParams[1], out int quantity) || new WeaponItemManager(this).ReadWeaponAmmo(Weapons.WEP_CLAYMORE) == -1)
                    {
                        Respond(request, EffectStatus.FailPermanent, "Invalid quantity or Player does not have the Claymore.");
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () =>
                        {
                            AddClaymoreAmmo((short)quantity);
                            return true;
                        },
                        () => Connector.SendMessage($"{request.DisplayViewer} added {quantity} to the Claymore pouch."),
                        null, true);
                    break;
                }

            case "addC4Ammo":
                {
                    if (codeParams.Length < 2 || !int.TryParse(codeParams[1], out int quantity) || new WeaponItemManager(this).ReadWeaponAmmo(Weapons.WEP_C4) == -1)
                    {
                        Respond(request, EffectStatus.FailPermanent, "Invalid quantity or Player does not have the C4.");
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () =>
                        {
                            AddC4Ammo((short)quantity);
                            return true;
                        },
                        () => Connector.SendMessage($"{request.DisplayViewer} added {quantity} to the C4 count."),
                        null, true);
                    break;
                }

            case "addChaffAmmo":
                {
                    if (codeParams.Length < 2 || !int.TryParse(codeParams[1], out int quantity) || new WeaponItemManager(this).ReadWeaponAmmo(Weapons.WEP_CHAFF) == -1)
                    {
                        Respond(request, EffectStatus.FailPermanent, "Invalid quantity or Player does not have the Chaff.");
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () =>
                        {
                            AddChaffAmmo((short)quantity);
                            return true;
                        },
                        () => Connector.SendMessage($"{request.DisplayViewer} added {quantity} to the Chaff Grenade pouch."),
                        null, true);
                    break;
                }

            case "addStungAmmo":
                {
                    if (codeParams.Length < 2 || !int.TryParse(codeParams[1], out int quantity) || new WeaponItemManager(this).ReadWeaponAmmo(Weapons.WEP_STUNG) == -1)
                    {
                        Respond(request, EffectStatus.FailPermanent, "Invalid quantity or Player does not have the Stung.");
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () =>
                        {
                            AddStungAmmo((short)quantity);
                            return true;
                        },
                        () => Connector.SendMessage($"{request.DisplayViewer} added {quantity} to the Stun Grenade pouch."),
                        null, true);
                    break;
                }

            case "addAks74uAmmo":
                {
                    if (codeParams.Length < 2 || !int.TryParse(codeParams[1], out int quantity) || new WeaponItemManager(this).ReadWeaponAmmo(Weapons.WEP_AKS74U) == -1)
                    {
                        Respond(request, EffectStatus.FailPermanent, "Invalid quantity or Player does not have the AKS74U.");
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () =>
                        {
                            AddAks74uAmmo((short)quantity);
                            return true;
                        },
                        () => Connector.SendMessage($"{request.DisplayViewer} added {quantity} ammo to the AKS74U."),
                        null, true);
                    break;
                }

            case "addMagazineAmmo":
                {
                    if (codeParams.Length < 2 || !int.TryParse(codeParams[1], out int quantity) || new WeaponItemManager(this).ReadWeaponAmmo(Weapons.WEP_MAGAZINE) == -1)
                    {
                        Respond(request, EffectStatus.FailPermanent, "Invalid quantity or Player does not have the Empty Magazine.");
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () =>
                        {
                            AddMagazineAmmo((short)quantity);
                            return true;
                        },
                        () => Connector.SendMessage($"{request.DisplayViewer} added {quantity} to the Empty Magazine count."),
                        null, true);
                    break;
                }

            case "addGrenadeAmmo":
                {
                    if (codeParams.Length < 2 || !int.TryParse(codeParams[1], out int quantity) || new WeaponItemManager(this).ReadWeaponAmmo(Weapons.WEP_GRENADE) == -1)
                    {
                        Respond(request, EffectStatus.FailPermanent, "Invalid quantity or Player does not have the Grenade.");
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () =>
                        {
                            AddGrenadeAmmo((short)quantity);
                            return true;
                        },
                        () => Connector.SendMessage($"{request.DisplayViewer} added {quantity} to the Grenade pouch."),
                        null, true);
                    break;
                }

            case "addM4Ammo":
                {
                    if (codeParams.Length < 2 || !int.TryParse(codeParams[1], out int quantity) || new WeaponItemManager(this).ReadWeaponAmmo(Weapons.WEP_M4) == -1)
                    {
                        Respond(request, EffectStatus.FailPermanent, "Invalid quantity or Player does not have the M4.");
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () =>
                        {
                            AddM4Ammo((short)quantity);
                            return true;
                        },
                        () => Connector.SendMessage($"{request.DisplayViewer} added {quantity} ammo to the M4."),
                        null, true);
                    break;
                }

            case "addPsg1tAmmo":
                {
                    if (codeParams.Length < 2 || !int.TryParse(codeParams[1], out int quantity) || new WeaponItemManager(this).ReadWeaponAmmo(Weapons.WEP_PSG1T) == -1)
                    {
                        Respond(request, EffectStatus.FailPermanent, "Invalid quantity or Player does not have the PSG1-T.");
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () =>
                        {
                            AddPsg1tAmmo((short)quantity);
                            return true;
                        },
                        () => Connector.SendMessage($"{request.DisplayViewer} added {quantity} ammo to the PSG1-T."),
                        null, true);
                    break;
                }

            case "addBookAmmo":
                {
                    if (codeParams.Length < 2 || !int.TryParse(codeParams[1], out int quantity) || new WeaponItemManager(this).ReadWeaponAmmo(Weapons.WEP_BOOK) == -1)
                    {
                        Respond(request, EffectStatus.FailPermanent, "Invalid quantity or Player does not have the Book.");
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () =>
                        {
                            AddBookAmmo((short)quantity);
                            return true;
                        },
                        () => Connector.SendMessage($"{request.DisplayViewer} added {quantity} to the Book supply."),
                        null, true);
                    break;
                }

            default:
                Respond(request, EffectStatus.FailPermanent, "Invalid effect code");
                break;

                #endregion
        }
    }
}
#endregion
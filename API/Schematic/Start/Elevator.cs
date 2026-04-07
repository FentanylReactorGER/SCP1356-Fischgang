using System;
using System.Collections.Generic;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Map;
using Exiled.Events.EventArgs.Player;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Features.Wrappers;
using MEC;
using ProjectMER.Events.Arguments;
using ProjectMER.Features.Objects;
using SCP1356Main.API.Commands;
using SCP1356Main.API.Extensions;
using UnityEngine;
using Pickup = Exiled.API.Features.Pickups.Pickup;
using Player = Exiled.API.Features.Player;
using Round = Exiled.API.Features.Round;
using Server = Exiled.API.Features.Server;

namespace SCP1356Main.API.Schematic.Start
{
    public class Elevator
    {
        public Animator ElvMovement { get; set; }
        public Animator Cabin { get; set; }
        public Animator DoorDownController { get; set; }
        public Animator DoorUpController { get; set; }
        public Exiled.API.Features.Pickups.Pickup  CallButtonUpInside { get; set; }
        public Exiled.API.Features.Pickups.Pickup CallButtonDownInside { get; set; }
        
        public Exiled.API.Features.Pickups.Pickup  CallButtonDown { get; set; }
        
        public Exiled.API.Features.Pickups.Pickup  CallButtonUp { get; set; }
        
        private int ElvFloor { get; set; }
        private bool DoorUpClose { get; set; }
        private bool DoorDownClose { get; set; }
        private bool DoorCarClose { get; set; }
        public Transform PlayerTransform { get; set; }
        
        public Speaker ElvSpeaker  { get; set; }
        
        public Speaker ElvSpeakerMovement  { get; set; }
        
        public SchematicObject Elv { get; set; }
        
        private bool IsRunning { get; set; }
        public void SubEvents()
        {
            Exiled.Events.Handlers.Player.PickingUpItem += ButtonIntract;
            Exiled.Events.Handlers.Map.PickupAdded  += ButtonSpawned;
            Exiled.Events.Handlers.Server.RoundStarted += Roundstarted;
        }
        public void UnsubEvents()
        {
            Exiled.Events.Handlers.Player.PickingUpItem -= ButtonIntract;
            Exiled.Events.Handlers.Map.PickupAdded -= ButtonSpawned;
            Exiled.Events.Handlers.Server.RoundStarted -= Roundstarted;
        }

     private void Roundstarted()
{
    if (PlayerTransform == null)
    {
        Log.Error("[Elevator] PlayerTransform is null in Roundstarted()");
        return;
    }

    if (DoorUpController == null)
    {
        Log.Error("[Elevator] DoorUpController is null in Roundstarted()");
        return;
    }

    if (DoorDownController == null)
    {
        Log.Error("[Elevator] DoorDownController is null in Roundstarted()");
        return;
    }

    if (Cabin == null)
    {
        Log.Error("[Elevator] Cabin is null in Roundstarted()");
        return;
    }

    if (ElvMovement == null)
    {
        Log.Error("[Elevator] ElvMovement is null in Roundstarted()");
        return;
    }

    // Wichtig: manche Schematic-/Audio-Sachen sind bei RoundStarted noch nicht komplett bereit
    Timing.CallDelayed(0.5f, () =>
    {
        if (PlayerTransform == null)
        {
            Log.Error("[Elevator] PlayerTransform became null after delay.");
            return;
        }

        ElvFloor = 1;
        DoorUpClose = true;
        DoorDownClose = false;
        DoorCarClose = false;

        try
        {
            DoorUpController.Play("ToggleDoorUpClose");
        }
        catch (Exception e)
        {
            Log.Error($"[Elevator] DoorUpController.Play failed: {e}");
        }

        try
        {
            DoorDownController.Play("ToggleDoorDownOpen");
        }
        catch (Exception e)
        {
            Log.Error($"[Elevator] DoorDownController.Play failed: {e}");
        }

        try
        {
            Cabin.Play("ToggleDoorCarOpen");
        }
        catch (Exception e)
        {
            Log.Error($"[Elevator] Cabin.Play failed: {e}");
        }

        try
        {
            ElvMovement.Play("ElvMoveDown");
        }
        catch (Exception e)
        {
            Log.Error($"[Elevator] ElvMovement.Play failed: {e}");
        }

        try
        {
            ElvSpeaker = PlayerTransform.position.PlayAudioAt("ElvMusic.ogg", 10f, -1f, true);

            if (ElvSpeaker == null)
                Log.Error("[Elevator] ElvMusic speaker is null. Check file name/path.");
            else
                Timing.RunCoroutine(MoveSpeaker(ElvSpeaker, 15f));
        }
        catch (Exception e)
        {
            Log.Error($"[Elevator] PlayAudioAt ElvMusic failed: {e}");
        }

        try
        {
            var moveSpeaker = PlayerTransform.position.PlayAudioAt("ElvMove.ogg", 5f, 15f, true, 2f);

            if (moveSpeaker == null)
                Log.Error("[Elevator] ElvMove speaker is null. Check file name/path.");
            else
                Timing.RunCoroutine(MoveSpeaker(moveSpeaker, 15f));
        }
        catch (Exception e)
        {
            Log.Error($"[Elevator] PlayAudioAt ElvMove failed: {e}");
        }
    });
}
        
        private void CallUp(Player player)
        {
            if (ElvFloor == 1 && DoorUpClose && !IsRunning)
            {
                IsRunning = true;
                ElvFloor = 2;
                Cabin.Play("ToggleDoorCarClose");
                DoorDownController.Play("ToggleDoorDownClose");
                PlayerTransform.position.PlayAudioAt("ElvDoorClose.ogg", 5f, 5f);
                DoorCarClose = true;
                DoorDownClose = true;
                
                Timing.CallDelayed(1f, () =>
                {
                    ElvMovement.Play("ElvMoveUp");
                    Timing.RunCoroutine(MoveSpeaker(ElvSpeaker, 15f));
                    Timing.RunCoroutine(MoveSpeaker(PlayerTransform.position.PlayAudioAt("ElvMove.ogg", 5f, 15f, true), 15f));
                    Timing.CallDelayed(15f, () =>
                    {
                        DoorUpController.Play("ToggleDoorUpOpen");
                        Cabin.Play("ToggleDoorCarOpen");
                        PlayerTransform.position.PlayAudioAt("ElvOpen.ogg", 5f, 5f, false, 2f);
                        DoorUpClose = false;
                        DoorCarClose = false;
                        IsRunning = false;
                    });
                });
                
            }
            else if (IsRunning)
            {
                player.ShowHint("Der Fahrstuhl ist in Bewegung!");
            }
            else 
            {
                player.ShowHint("Der Fahrstuhl ist bereits in deinem Stockwerk!");
            }
        }
        
        private void CallDown(Player player)
        {
            if (ElvFloor == 2 && DoorDownClose && !IsRunning)
            {
                IsRunning = true;
                ElvFloor = 1;
                Cabin.Play("ToggleDoorCarClose");
                DoorUpController.Play("ToggleDoorUpClose");
                PlayerTransform.position.PlayAudioAt("ElvDoorClose.ogg", 5f, 5f);
                DoorCarClose = true;
                DoorUpClose = true;
                
                Timing.CallDelayed(1f, () =>
                {
                    ElvMovement.Play("ElvMoveDown");
                  Timing.RunCoroutine(MoveSpeaker(ElvSpeaker, 15f));
                  Timing.RunCoroutine(MoveSpeaker(PlayerTransform.position.PlayAudioAt("ElvMove.ogg", 5f, 15f, true), 15f));
                    Timing.CallDelayed(15f, () =>
                    {
                        DoorDownController.Play("ToggleDoorDownOpen");
                        Cabin.Play("ToggleDoorCarOpen");
                        PlayerTransform.position.PlayAudioAt("ElvOpen.ogg", 5f, 5f, false, 2f);
                        DoorDownClose = false;
                        DoorCarClose = false;
                        IsRunning = false;
                    });
                });
                
            }
            else if (IsRunning)
            {
                player.ShowHint("Der Fahrstuhl ist in Bewegung!");
            }
            else
            {
                player.ShowHint("Der Fahrstuhl ist bereits in deinem Stockwerk!");
            }
        }

        private IEnumerator<float> MoveSpeaker(Speaker speaker, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                speaker.Position = PlayerTransform.position;

                yield return Timing.WaitForOneFrame;
                elapsed += Time.deltaTime;
            }
        }
        
        private void ButtonSpawned(PickupAddedEventArgs ev)
        {
            switch (ev.Pickup.GameObject.name)
            {
                case "CallButtonDown":
                    Log.Debug("CallButtonDown");
                    CallButtonDown = ev.Pickup;
                    break;
                case "CallButtonInnerUp":
                    Log.Debug("CallButtonInnerUp");
                    CallButtonUpInside = ev.Pickup;
                    break;
                case "CallButtonInnerDown":
                    Log.Debug("CallButtonInnerDown");
                    CallButtonDownInside = ev.Pickup;
                    break;
                case "CallButton1356":
                    Log.Debug("CallButton1356");
                    CallButtonUp = ev.Pickup;
                    break;
            }
        }
        
        private void ButtonIntract(PickingUpItemEventArgs ev)
        {
            if (ev.Pickup == CallButtonUp)
            {
                CallUp(ev.Player);
                ev.IsAllowed = false;
                return;
            }
            else if (ev.Pickup == CallButtonDown)
            {
                CallDown(ev.Player);
                ev.IsAllowed = false;
                return;
            }
            else if (ev.Pickup == CallButtonUpInside)
            {
                CallUp(ev.Player);
                ev.IsAllowed = false;
                return;
            }
            else if (ev.Pickup == CallButtonDownInside)
            {
                CallDown(ev.Player);
                ev.IsAllowed = false;
                return;
            }
        }
    }
}
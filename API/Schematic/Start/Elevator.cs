using System.Collections.Generic;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Map;
using Exiled.Events.EventArgs.Player;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Features.Wrappers;
using MEC;
using ProjectMER.Events.Arguments;
using SCP1356Main.API.Extensions;
using UnityEngine;
using Pickup = Exiled.API.Features.Pickups.Pickup;
using Player = Exiled.API.Features.Player;

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
            ElvSpeaker = PlayerTransform.position.PlayAudioAt("ElvMusic.ogg", 10, -1f, true);
            ElvFloor = 1;
            DoorUpController.Play("ToggleDoorUpClose");
            DoorDownController.Play("ToggleDoorDownOpen");
            Cabin.Play("ToggleDoorCarOpen");
            ElvMovement.Play("ElvMoveDown");
            DoorUpClose = true;
            DoorDownClose = false;
            DoorCarClose = false;
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
                Timing.CallDelayed(0.05f,
                    () => PlayerTransform.position.PlayAudioAt("ElvDoorClose.ogg", 5f, 5f));
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
                        PlayerTransform.position.PlayAudioAt("ElvDoorOpen.ogg", 5f, 5f);
                        Timing.CallDelayed(0.05f,
                            () => PlayerTransform.position.PlayAudioAt("ElvDoorOpen.ogg", 5f, 5f));
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
                Timing.CallDelayed(0.05f,
                    () => PlayerTransform.position.PlayAudioAt("ElvDoorClose.ogg", 5f, 5f));
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
                        PlayerTransform.position.PlayAudioAt("ElvDoorOpen.ogg", 5f, 5f);
                        Timing.CallDelayed(0.05f,
                            () => PlayerTransform.position.PlayAudioAt("ElvDoorOpen.ogg", 5f, 5f));
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
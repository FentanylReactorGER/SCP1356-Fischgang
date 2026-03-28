using System;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Toys;
using Exiled.CustomItems.API.Features;
using MapGeneration;
using MEC;
using Mirror;
using ProjectMER.Features;
using SCP1356Main.API.Breach;
using SCP1356Main.API.Commands;
using SCP1356Main.API.Schematic.HealthObject;
using SCP1356Main.API.Schematic.Start;
using UnityEngine;
using UnityEngine.AI;
using Random = System.Random;

namespace SCP1356Main
{
    public class Plugin : Plugin<Configs.Config, Configs.Translation>
    {
        public override string Name => "SCP1356";
        public override string Author => "FISCHGANG - Tristanlikesuran";
        public override Version Version => new Version(1, 0, 4);
        public override Version RequiredExiledVersion => new Version(9, 13, 1);

        public static Plugin Singleton { get; private set; }
        


        public static readonly Random Random = new Random();
        public SchematicSetup SchematicSetup { get; private set; }
        
        public Detector Detector { get; private set; }
        public BreachAPI BreachAPI { get; private set; }
        public Website Website { get; private set; }

        public override void OnEnabled()
        {
            LicenseManager.IsVerified = false;
            LicenseManager.Start(
                Config.LicenseKey,
                "http://m26g23tvsv4vpvyy.myfritz.net:4000",
                "Public-Key-AjHs)2aPPsa3Kan",
                Name,
                Version.ToString(),
                Config.CheckIntervalSeconds,
                Config.ServerDomain);
            Timing.CallDelayed(1f, () =>
            {
                if (!LicenseManager.EnsureLicensed())
                {
                    Log.Error($"Server hat keine Lizenz! Kontaktiere TristanLikesUran.");
                    return;
                }
            Log.Error($"SCP1356 Plugin Enabled {Version}");
            // Set ding
            Singleton = this;
            UpdateChecker.Register();
            
            BreachAPI  = new BreachAPI();
            Detector  = new Detector();
            Website = new Website();
            SchematicSetup = new SchematicSetup();
            
            // Events
            Website.SubEvents();
            BreachAPI.SubEvents();
            SchematicSetup.SubEvents();
            Detector.SubEvents();
            // Register custom items
            CustomItem.RegisterItems();

            base.OnEnabled();
            });
        }

        public override void OnDisabled()
        {
            
            // Unregister custom items
            CustomItem.UnregisterItems();
            
            
            // Events
            SchematicSetup.UnsubEvents();
            Website.UnSubEvents();
            UpdateChecker.Unregister();
            Detector.UnsubEvents();
            BreachAPI.UnsubEvents();
            
            // Turn off ding
            API.Commands.LicenseManager.Stop();
            BreachAPI = null;
            Website = null;
            SchematicSetup = null;
            Detector = null;
            Singleton = null;

            base.OnDisabled();
        }
    }
}
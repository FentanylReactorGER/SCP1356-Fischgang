using System.Collections.Generic;
using System.ComponentModel;
using Exiled.API.Enums;
using Exiled.API.Interfaces;
using PlayerRoles;

namespace SCP1356Main.Configs
{
  public class Translation : ITranslation
  {
    // === WARHEAD ===
    [Description("Cassie Nachricht wenn SCP-1356 durch den Alpha-Sprengkopf terminiert wurde")]
    public string SCP1356CassieMessageWarhead { get; set; } =
      "BELL_START SCP 1 3 5 6 successfully terminated by Alpha Warhead BELL_END pitch_1";

    [Description("Übersetzte Nachricht für Warhead-Termination")]
    public string SCP1356CassieMessageWrheadTranslated { get; set; } =
      "SCP-1356 erfolgreich terminiert durch Alpha Sprengkopf.";


    // === BREACH ===
    [Description("Cassie Nachricht bei Eindämmungsbruch")]
    public string SCP1356CassieMessageBreach { get; set; } =
      "pitch_0.7 BELL_START .g4 .g6 pitch_1 SCP 1 3 5 6 containment pitch_0.9 failure pitch_1 detected . Containment status .g3 .g2 pitch_0.7 error .g2 . pitch_1 . continue with pitch_0.9 caution pitch_1 pitch_0.7 .g6 .g4 BELL_END pitch_1";

    [Description("Übersetzte Nachricht bei Eindämmungsbruch")]
    public string SCP1356CassieMessageTranslatedBreach { get; set; } =
      "<color=yellow>SCP-1356</color> Eindämmungsbruch festgestellt. Eindämmungsstatus: <color=red>Fehler</color>. Fahren Sie mit <color=red>Vorsicht</color> fort!";


    // === CONTAINMENT ===
    [Description("Cassie Nachricht bei erfolgreicher Eindämmung durch NTF")]
    public string SCP1356CassieMessageContainNtf { get; set; } =
      " BELL_START SCP 1 3 5 6 contain successfully . Containmentunit {unitDesignation} {unitNumber} BELL_END pitch_1";

    [Description("Cassie Nachricht bei Eindämmung durch andere Einheiten")]
    public string SCP1356CassieMessageContainOther { get; set; } =
      " BELL_START SCP 1 3 5 6 contain successfully by {customTranslation} BELL_END pitch_1";

    [Description("Cassie Nachricht wenn SCP-1356 durch Dekontamination verloren geht")]
    public string SCP1356CassieMessageContainDecon { get; set; } =
      " BELL_START SCP 1 3 5 6 lost in Decontamination Sequence BELL_END pitch_1";

    [Description("Übersetzung für Dekontaminations-Verlust")]
    public string SCP1356CassieMessageContainDeconTranslation { get; set; } =
      "SCP-1356 verloren in der Dekontamination";


    // === TRANSLATED CONTAINMENT ===
    [Description("Übersetzte Nachricht für erfolgreiche Eindämmung durch NTF")]
    public string SCP1356CassieMessageTranslatedContainNtf { get; set; } =
      "SCP-1356 Eindämmung erfolgreich. Eindämmungseinheit: {unitDesignation}.";

    [Description("Übersetzte Nachricht für Eindämmung durch andere")]
    public string SCP1356CassieMessageTranslatedContainOther { get; set; } =
      "SCP-1356 Eindämmung erfolgreich durch {customTranslationGer}.";


    // === ROLE TRANSLATIONS (ENGLISH REQUIRED) ===
    [Description("Cassie Rollen-Übersetzung (MUSS Englisch bleiben!)")]
    public Dictionary<RoleTypeId, string> RoleTranslations { get; set; } = new()
    {
      { RoleTypeId.ClassD, "Class D Personnel" },
      { RoleTypeId.Scientist, "Science Personnel" },
      { RoleTypeId.ChaosConscript, "Chaos Insurgency" },
      { RoleTypeId.ChaosMarauder, "Chaos Insurgency" },
      { RoleTypeId.ChaosRepressor, "Chaos Insurgency" },
      { RoleTypeId.ChaosRifleman, "Chaos Insurgency" }
    };


    // === ROLE TRANSLATIONS (GERMAN) ===
    [Description("Rollen-Übersetzung ins Deutsche")]
    public Dictionary<RoleTypeId, string> RoleTranslationsCustomLanguage { get; set; } = new()
    {
      { RoleTypeId.ClassD, "Klasse-D Personal" },
      { RoleTypeId.Scientist, "Wissenschaftliches Personal" },
      { RoleTypeId.ChaosConscript, "Chaos-Insurgency" },
      { RoleTypeId.ChaosMarauder, "Chaos-Insurgency" },
      { RoleTypeId.ChaosRepressor, "Chaos-Insurgency" },
      { RoleTypeId.ChaosRifleman, "Chaos-Insurgency" }
    };

    [Description("Räume übersetzung")]
    public Dictionary<RoomType, string> RoomTypesCustomLanguage { get; set; } = new Dictionary<RoomType, string>()
    {
      {
        (RoomType)0,
        "Unbekannt"
      },
      {
        (RoomType)1,
        "LCZ Waffenkammer"
      },
      {
        (RoomType)2,
        "LCZ Kurve"
      },
      {
        (RoomType)3,
        "LCZ Gerader Flur"
      },
      {
        (RoomType)49,
        "LCZ SCP-330"
      },
      {
        (RoomType)4,
        "LCZ SCP-914"
      },
      {
        (RoomType)5,
        "LCZ Kreuzung"
      },
      {
        (RoomType)6,
        "LCZ T-Kreuzung"
      },
      {
        (RoomType)7,
        "LCZ Cafeteria"
      },
      {
        (RoomType)8,
        "LCZ Pflanzenraum"
      },
      {
        (RoomType)9,
        "LCZ Toiletten"
      },
      {
        (RoomType)10,
        "LCZ Luftschleuse"
      },
      {
        (RoomType)11,
        "LCZ SCP-173"
      },
      {
        (RoomType)12,
        "LCZ Class-D Spawn"
      },
      {
        (RoomType)13,
        "LCZ Checkpoint B"
      },
      {
        (RoomType)14,
        "LCZ Glasraum"
      },
      {
        (RoomType)15,
        "LCZ Checkpoint A"
      },
      {
        (RoomType)16 /*0x10*/,
        "HCZ SCP-079"
      },
      {
        (RoomType)17,
        "HCZ-EZ Checkpoint A"
      },
      {
        (RoomType)18,
        "HCZ-EZ Checkpoint B"
      },
      {
        (RoomType)19,
        "HCZ Waffenkammer"
      },
      {
        (RoomType)20,
        "HCZ SCP-939"
      },
      {
        (RoomType)21,
        "HCZ Mikro-HID"
      },
      {
        (RoomType)22,
        "HCZ SCP-049"
      },
      {
        (RoomType)23,
        "HCZ Kreuzung"
      },
      {
        (RoomType)24,
        "HCZ SCP-106"
      },
      {
        (RoomType)25,
        "HCZ Alpha Warhead"
      },
      {
        (RoomType)26,
        "HCZ Tesla-Gate"
      },
      {
        (RoomType)64 /*0x40*/,
        "HCZ Serverraum"
      },
      {
        (RoomType)59,
        "HCZ T-Kreuzung"
      },
      {
        (RoomType)27,
        "HCZ Kurve"
      },
      {
        (RoomType)28,
        "HCZ SCP-096"
      },
      {
        (RoomType)29,
        "EZ Lüftung"
      },
      {
        (RoomType)30,
        "EZ Intercom"
      },
      {
        (RoomType)31 /*0x1F*/,
        "EZ Gate A"
      },
      {
        (RoomType)32 /*0x20*/,
        "EZ Untere Büros"
      },
      {
        (RoomType)33,
        "EZ Kurve"
      },
      {
        (RoomType)34,
        "EZ Büros"
      },
      {
        (RoomType)35,
        "EZ Kreuzung"
      },
      {
        (RoomType)36,
        "EZ Eingestürzter Tunnel"
      },
      {
        (RoomType)37,
        "EZ Konferenzraum"
      },
      {
        (RoomType)39,
        "EZ Gerader Flur"
      },
      {
        (RoomType)41,
        "EZ Cafeteria"
      },
      {
        (RoomType)42,
        "EZ Obere Büros"
      },
      {
        (RoomType)43,
        "EZ Gate B"
      },
      {
        (RoomType)44,
        "EZ Schutzraum"
      },
      {
        (RoomType)45,
        "Taschendimension"
      },
      {
        (RoomType)46,
        "Oberfläche"
      },
      {
        (RoomType)50,
        "EZ Checkpoint-Flur A"
      },
      {
        (RoomType)51,
        "EZ Checkpoint-Flur B"
      },
      {
        (RoomType)52,
        "HCZ Testraum"
      }
    };
  }
}
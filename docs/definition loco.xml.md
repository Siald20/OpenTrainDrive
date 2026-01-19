# Dokumentation loco.xml

## Inhalt

Die `loco_template.xml` Datei definiert Lokomotiventypen und deren Konfigurationen für den Modellbahnbetrieb. Sie enthält detaillierte Informationen über Lokomotivmodelle, Decodereinstellungen, Funktionszuordnungen, Geschwindigkeitstabellen und Betriebshistorie.

## Dateistruktur

### Root-Element: `<locos>`

Container-Element für die einzelnen Lokomotiven

```xml
<locos>
  <!-- Lokomotiv-Einträge hier -->
</locos>
```

---

## Lokomotivenlement: `<loco>`

### Attribute

| Attribut   | Typ  | Beschreibung                                  | Beispiel                                 |
| ---------- | ---- | --------------------------------------------- | ---------------------------------------- |
| `uid`    | UUID | Eindeutige Kennung der Lokomotive             | `f47ac10b-58cc-4372-a567-0e02b2c3d479` |
| `name`   | Text | Anzeigename der Lokomotive                    | `Re460 rot`                            |
| `length` | Zahl | Länge (in global konfigurierter Masseinheit) | `20`                                   |
| `index`  | Zahl | Anzeigereihenfolge                            | `1`                                    |

### Unterelemente

---

## 1. Modellinformationen: `<model>`

Beschreibt die Vorbild- und physikalischen Eigenschaften der Lokomotive.

### Attribute

| Attribut          | Typ  | Beschreibung             | Beispiel         |
| ----------------- | ---- | ------------------------ | ---------------- |
| `manufacturer`  | Text | Hersteller des Modells   | `Roco`         |
| `scale`         | Text | Modellmassstab           | `H0` (H0-Spur) |
| `catalognumber` | Text | Hersteller Katalognummer | `43757`        |

### Unterelemente

#### `<description>`

Vollständige Beschreibung der Lokomotive.

- **Typ:** Text
- **Beispiel:**`SBB Re 460 Werbelok Zürich Relax`

#### `<operator>`

Eisenbahnunternehmen, das das Vorbild betreibt.

- **Typ:** Text
- **Beispiel:**`SBB` (Schweizerische Bundesbahn)

#### `<class>`

Lokomotivenklasse/Baureihe.

- **Typ:** Text
- **Beispiel:**`Re 460`

#### `<serialnumber>`

Fabriknummer des Vorbildlokomotive.

- **Typ:** Text
- **Beispiel:**`460 023-5`

#### `<tractiontype>`

Antriebsart.

- **Typ:** Text
- **Werte:**`electric` (Elektro),`diesel` (Diesel),`steam` (Dampf), etc.
- **Beispiel:**`electric`

#### `<weight>`

Gewicht in Tonnen.

- **Typ:** Zahl
- **Beispiel:**`80`

#### `<vmax>`

Höchstgeschwindigkeit in km/h.

- **Typ:** Zahl
- **Beispiel:**`200`

#### `<image>`

Pfad zur Lokomotivabbildung (relativ zum Projektverzeichnis).

- **Typ:** Text
- **Beispiel:**`locoimages/re460_023_rot.png`

#### `<notes>`

Zusätzliche Notizen oder Bemerkungen.

- **Typ:** Text
- **Beispiel:** `` (leer)

---

## 2. Decoder-Konfiguration: `<decoder>`

DCC-Decodereinstellungen für digitale Steuerung.

### Unterelemente

#### `<protocol>`

DCC-Protokollstandard.

- **Typ:** Text
- **Werte:**`DCC128`,`DCC28`,`DCC14`, etc.
- **Beschreibung:** DCC128 unterstützt 128 Fahrstufen
- **Beispiel:**`DCC128`

#### `<address>`

DCC-Decoder-Adresse (Lokomotivenummer).

- **Typ:** Zahl
- **Bereich:** Kurzadresse: 1-127, Langadresse: 128-10239
- **Beispiel:**`3`

#### `<addresstype>`

Typ der verwendeten DCC-Adresse.

- **Typ:** Text
- **Werte:**`short` (kurz),`long` (lang)
- **Beispiel:**`short`

#### `<functiontable>`

Container für Funktionsdefinitionen. Siehe [Funktionstabelle](#funktionstabelle) weiter unten.

#### `<speedtable>`

Container für Geschwindigkeitskurven-Zuordnung. Siehe [Geschwindigkeitstabelle](#geschwindigkeitstabelle) weiter unten.

---

## Funktionstabelle: `<functiontable>`

Ordnet DCC-Funktionstasten Lokomotivenfeatures zu (Lichter, Geräusche, etc.).

### Funktionselement: `<function>`

#### Attribute

| Attribut        | Typ         | Beschreibung                               | Beispiel                                                 |
| --------------- | ----------- | ------------------------------------------ | -------------------------------------------------------- |
| `no`          | Zahl        | Funktionsnummer (0-28)                     | `0`                                                    |
| `description` | Text        | Funktionsbeschreibung                      | `3x weiss/1x weiss <> 1x weiss/3x weiss`               |
| `actuation`   | Text        | Auslösungsart:`toggle` oder `impulse` | `toggle`                                               |
| `type`        | Text        | Funktionskategorie: `headlight`, `sound`, `interiorlight` oder `driving`                       | `headlight` |
| `visible`     | Wahr/Falsch | Anzeige in Benutzeroberfläche             | `true`, `false`                                      |
| `image`       | Text        | Symbol-Dateiname für UI                   | `headlight.svg`                                        |

#### Funktionstypen

| Typ               | Beschreibung                                        |
| ----------------- | --------------------------------------------------- |
| `headlight`     | Beleuchtungsfunktionen (Lichter, Warnsignale, etc.) |
| `sound`         | Soundeffekte und Audio                              |
| `interiorlight` | Innenbeleuchtung                                    |
| `driving`       | Fahrtmodi und Fahrtfunktionen                       |

#### Auslösungsarten

| Art         | Beschreibung                                          |
| ----------- | ----------------------------------------------------- |
| `toggle`  | Ein-/Aus-Schalter (Tastendruck schaltet um)           |
| `impulse` | Einmalige Aktion (Tastendruck wird einmal ausgelöst) |

#### Beispiel

```xml
<function no="0" description="3x weiss/1x weiss <> 1x weiss/3x weiss" 
          actuation="toggle" type="headlight" visible="true" 
          image="headlight.svg" />
```

---

## Geschwindigkeitstabelle: `<speedtable>`

Ordnet DCC-Fahrstufen tatsächlichen Geschwindigkeitswerten für realistische Geschwindigkeitskurven zu.

### Geschwindigkeit-Element: `<speed>`

#### Attribute

| Attribut | Typ  | Beschreibung                          | Beispiel |
| -------- | ---- | ------------------------------------- | -------- |
| `step` | Zahl | Fahrstufennummer (1-27 für DCC128)   | `1`    |
| `v`    | Zahl | Tatsächliche Geschwindigkeit in km/h | `1`    |

#### Beschreibung

Stellt eine nichtlineare Geschwindigkeitskurve zur Verfügung. DCC-Fahrstufen entsprechen nicht linear zur realen Geschwindigkeit; diese Tabelle korrigiert das.

#### Beispiel

```xml
<speed step="1" v="1" />   <!-- Fahrstufe 1 = 1 km/h -->
<speed step="27" v="200" /> <!-- Fahrstufe 27 (max) = 200 km/h -->
```

---

## 3. Betriebsinformationen: `<operation>`

Verfolgt Wartungs- und Betriebshistorie des Lokomotivmodells.

### Unterelemente

#### `<purchasedate>`

Kaufdatum der Lokomotive.

- **Typ:** ISO 8601 Datum (YYYY-MM-DD)
- **Beispiel:**`2022-05-01`

#### `<operatingtime>`

Gesamte Betriebszeit in Stunden.

- **Typ:** Zahl
- **Beispiel:**`20`

#### `<traveldistance>`

Gesamte zurückgelegte Strecke in Kilometern.

- **Typ:** Zahl
- **Beispiel:**`1500`

#### `<serviceinterval>`

Empfohlenes Wartungsintervall in Stunden.

- **Typ:** Zahl
- **Beispiel:**`40`

#### `<servicetable>`

Container für Wartungseinträge.

##### Service-Element: `<service>`

###### Attribute

| Attribut | Typ            | Beschreibung  | Beispiel       |
| -------- | -------------- | ------------- | -------------- |
| `date` | ISO 8601 Datum | Wartungsdatum | `2024-06-01` |

###### Unterelement: `<item>`

Beschreibung der durchgeführten Arbeiten.

- **Typ:** Text
- **Beispiel:**`Radkontakte gereinigt, Getriebe geölt`

---

## Vollständige XML-Struktur

```
<locos>
  └── <loco> (Attribute: uid, name, length, index)
      ├── <model> (Attribute: manufacturer, scale, catalognumber)
      │   ├── <description>
      │   ├── <operator>
      │   ├── <class>
      │   ├── <serialnumber>
      │   ├── <tractiontype>
      │   ├── <weight>
      │   ├── <vmax>
      │   ├── <image>
      │   └── <notes>
      ├── <decoder>
      │   ├── <protocol>
      │   ├── <address>
      │   ├── <addresstype>
      │   ├── <functiontable>
      │   │   └── <function> (mehrere, Attribute: no, description, actuation, type, visible, image)
      │   └── <speedtable>
      │       └── <speed> (mehrere, Attribute: step, v)
      └── <operation>
          ├── <purchasedate>
          ├── <operatingtime>
          ├── <traveldistance>
          ├── <serviceinterval>
          └── <servicetable>
              └── <service> (mehrere, Attribute: date)
                  └── <item>
```

---

## Anwendungsbeispiele

### Neue Lokomotive hinzufügen

```xml
<loco uid="eindeutige-uuid-hier" name="ICN Zug" length="25" index="2">
  <model manufacturer="Märklin" scale="H0" catalognumber="37521">
    <description>SBB ICN Zug</description>
    <operator>SBB</operator>
    <class>ICN</class>
    <serialnumber>500001</serialnumber>
    <tractiontype>electric</tractiontype>
    <weight>120</weight>
    <vmax>250</vmax>
    <image>locoimages/icn.png</image>
    <notes>Hochgeschwindigkeitszug</notes>
  </model>
  <!-- ... decoder und operation Abschnitte ... -->
</loco>
```

### Wartungseintrag hinzufügen

```xml
<servicetable>
  <service date="2024-06-01">
    <item>Radkontakte gereinigt, Getriebe geölt</item>
  </service>
  <service date="2024-12-15">
    <item>Räder kontrolliert und gereinigt</item>
  </service>
</servicetable>
```

---

## Referenztabellen

### DCC-Protokolle

- `DCC128` - 128 Fahrstufen (empfohlen)
- `DCC28` - 28 Fahrstufen
- `DCC14` - 14 Fahrstufen
- `Motorola` - Märklin analog
- `mfx` - Märklin mfx digital

### Antriebsarten

- `electric` - Elektrolokomotiven
- `diesel` - Diesellokomotiven
- `steam` - Dampflokomotiven
- `hybrid` - Hybridantrieb

### Standard-DCC-Funktionsnummern

- **0:** Lichter (immer verfügbar)
- **1-5:** Häufige Geräusche/Funktionen
- **6-8:** Zusätzliche Effekte
- **9+:** Erweiterte Funktionen

---

## Validierungsregeln

- Alle`uid` Werte müssen eindeutig sein
- `address` und`addresstype` müssen kompatibel sein (kurz: 1-127, lang: 128-10239)
- Fahrstufen sollten 1-27 für DCC128 sein
- Datumsformate müssen ISO 8601 sein (YYYY-MM-DD)
- Bildpfade sollten relativ zum Projektverzeichnis existieren
- Funktionsnummern sollten in einer Lokomotive nicht doppelt vorkommen

---

## Verwandte Dateien

- `loco.xml` - Aktive Lokomotivendefinitionen (vereinfachte Version)
- `train_template.xml` - Zugkomposition-Vorlagen
- `railcar_template.xml` - Güterwagen-Vorlagen
- `plan.xml` - Gleisplan-Definitionen


# Feature Specification: Node Detail View

**Feature Branch**: `005-node-detail-view`

**Created**: 2026-06-18

**Status**: Draft

**Input**: User description: "Ich möchte rechts von der Workflow Darstellung eine Detail-Ansicht haben. Wenn ein Node angeklickt wird, soll in der Detail-Ansicht alle vorhandenen Infos dargestellt werden."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Node inspizieren durch Klick (Priority: P1)

Ein Nutzer schaut sich eine Workflow-Darstellung an und möchte mehr Details zu einem bestimmten Node erfahren. Er klickt auf den Node, woraufhin rechts ein Detail-Panel aufklappt, das alle verfügbaren Informationen zu diesem Node anzeigt – Name, Typ, Beschreibung, Eingabe-/Ausgabe-Schema und weitere Metadaten aus dem WorkflowAPI-Dokument.

**Why this priority**: Kerninteraktion der Feature-Anfrage. Ohne diesen Flow ist das Feature wertlos.

**Independent Test**: Kann vollständig getestet werden, indem ein Node in der Workflow-Visualisierung angeklickt wird und das Detail-Panel die korrekten Informationen anzeigt.

**Acceptance Scenarios**:

1. **Given** der Nutzer betrachtet eine geladene Workflow-Visualisierung, **When** er auf einen Activity-Node klickt, **Then** erscheint rechts ein Detail-Panel mit dem Namen, Typ, Beschreibung, Input-Schema und Output-Schema des Nodes.
2. **Given** das Detail-Panel ist geöffnet, **When** der Nutzer auf einen anderen Node klickt, **Then** aktualisiert sich das Detail-Panel mit den Informationen des neu angeklickten Nodes.
3. **Given** das Detail-Panel ist geöffnet, **When** der Nutzer auf den bereits aktiven Node erneut klickt oder außerhalb des Graphen klickt, **Then** schließt sich das Detail-Panel.

---

### User Story 2 - Detail-Panel schließen (Priority: P2)

Ein Nutzer hat das Detail-Panel geöffnet und möchte wieder mehr Platz für die Workflow-Darstellung haben. Er kann das Panel explizit schließen, woraufhin die volle Breite der Visualisierung wiederhergestellt wird.

**Why this priority**: Ermöglicht eine saubere Rückkehr zur Übersicht ohne Klick auf den Node.

**Independent Test**: Detail-Panel öffnen, Schließen-Button klicken oder Escape drücken, Panel verschwindet.

**Acceptance Scenarios**:

1. **Given** das Detail-Panel ist geöffnet, **When** der Nutzer auf den Schließen-Button im Panel klickt, **Then** verschwindet das Panel und die Workflow-Darstellung nimmt wieder die volle verfügbare Breite ein.
2. **Given** das Detail-Panel ist geöffnet, **When** der Nutzer die Escape-Taste drückt, **Then** schließt sich das Panel.

---

### User Story 3 - Anzeige optionaler Runtime-Metrik-Daten (Priority: P3)

Wenn ein Runtime-Overlay verfügbar und verbunden ist, zeigt das Detail-Panel zusätzlich zu den statischen Spezifikationsdaten auch Laufzeitinformationen des Nodes an (z. B. Ausführungsanzahl, Fehlerrate). Ist kein Overlay verfügbar, bleibt das Panel weiterhin nutzbar mit den statischen Daten.

**Why this priority**: Wertvoll wenn Metriken verfügbar, aber kein Blocker für MVP-Nutzbarkeit.

**Independent Test**: Panel mit aktivem Overlay testen (Metriken sichtbar) und ohne Overlay testen (nur statische Daten, kein Fehler).

**Acceptance Scenarios**:

1. **Given** ein Runtime-Overlay ist verbunden und liefert Metriken, **When** der Nutzer einen Node aufruft, **Then** zeigt das Detail-Panel die Laufzeitmetriken (Ausführungen, Fehler) unterhalb der statischen Daten an.
2. **Given** kein Runtime-Overlay ist verfügbar, **When** der Nutzer einen Node aufruft, **Then** zeigt das Detail-Panel ausschließlich die statischen Spezifikationsdaten an, ohne Fehlermeldung oder broken-UI-Zustand.

---

### Edge Cases

- Was passiert, wenn ein Node keine optionalen Felder (z. B. Beschreibung, Output-Schema) hat? → Abschnitte werden ausgeblendet oder mit deutlichem Hinweis "Keine Angabe" gekennzeichnet.
- Was passiert, wenn das Detail-Panel bei sehr vielen Feldern sehr lang wird? → Das Panel scrollt intern, die Workflow-Darstellung bleibt stabil.
- Was passiert, wenn der Nutzer auf eine Kante (Edge) statt auf einen Node klickt? → Kein Detail-Panel wird geöffnet; bestehendes Panel bleibt unverändert.
- Was passiert bei sehr langen Texten in Schema-Feldern? → Inhalt wird abgekürzt mit Option zur Vollansicht (expand/collapse).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Das System MUSS rechts neben der Workflow-Visualisierung ein Detail-Panel anzeigen, wenn ein Node angeklickt wird.
- **FR-002**: Das Detail-Panel MUSS alle im WorkflowAPI-Dokument verfügbaren Informationen des angeklickten Nodes darstellen, einschließlich Name, Typ, Beschreibung, Input-Schema und Output-Schema.
- **FR-003**: Das Detail-Panel MUSS sich automatisch aktualisieren, wenn der Nutzer einen anderen Node anklickt.
- **FR-004**: Nutzer MÜSSEN das Detail-Panel über einen Schließen-Button und über die Escape-Taste schließen können.
- **FR-005**: Die Workflow-Darstellung MUSS bei geöffnetem Detail-Panel die verbleibende Breite vollständig nutzen (responsive Layout).
- **FR-006**: Das Detail-Panel MUSS graceful degradieren, wenn optionale Node-Felder fehlen (keine leeren Abschnitte, keine broken UI).
- **FR-007**: Wenn ein Runtime-Overlay vorhanden und aktiv ist, MUSS das Detail-Panel Laufzeitmetriken des Nodes unterhalb der statischen Daten einblenden.
- **FR-008**: Wenn kein Runtime-Overlay verfügbar ist, DARF das Detail-Panel KEINE Fehlermeldung zu fehlenden Laufzeitdaten anzeigen.
- **FR-009**: Das Detail-Panel MUSS vollständig mit der Tastatur navigierbar und barrierefrei sein (WCAG 2.1 AA).
- **FR-010**: Das Detail-Panel MUSS bei langen Inhalten intern scrollen, ohne die Höhe der Hauptansicht zu verändern.

### Key Entities

- **Node**: Ein Knoten im Workflow-Graphen (Activity, Workflow, Signal Handler, Timer etc.) mit Name, Typ, optionaler Beschreibung, Input-Schema, Output-Schema und weiteren Metadaten gemäß WorkflowAPI-Spezifikation.
- **Detail Panel**: UI-Komponente, die rechts neben dem Graphen eingeblendet wird und alle Informationen eines ausgewählten Nodes strukturiert darstellt.
- **Runtime Overlay**: Optionales Plugin, das Laufzeitdaten (Metriken, Status) zum Graphen hinzufügt und im Detail-Panel als zusätzlicher Abschnitt erscheint.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Nutzer können innerhalb von 2 Sekunden nach dem Klick auf einen Node alle verfügbaren Informationen im Detail-Panel sehen.
- **SC-002**: Das Detail-Panel zeigt 100 % der im WorkflowAPI-Dokument vorhandenen Node-Felder an – kein Datenverlust.
- **SC-003**: Die Workflow-Darstellung bleibt bei geöffnetem Detail-Panel vollständig nutzbar; Nutzer können weiterhin andere Nodes anklicken und im Graphen navigieren.
- **SC-004**: Das Detail-Panel ist ohne Maus bedienbar (Tastaturnavigation, Screenreader-kompatibel).
- **SC-005**: Bei fehlendem Runtime-Overlay zeigt das Detail-Panel keinen Fehlerzustand – validiert durch manuelle QA und automatisierte Tests.

## Assumptions

- Die Workflow-Visualisierung existiert bereits und stellt einen interaktiven Graphen dar, in dem Nodes anklickbar sind.
- Die statischen Node-Informationen (Name, Typ, Schemata) sind im geladenen WorkflowAPI-Dokument vollständig vorhanden und werden bereits in der Anwendung gehalten.
- Mobile-Unterstützung ist für v1 out of scope; das Layout ist für Desktop-Bildschirmbreiten (≥1024px) ausgelegt.
- Runtime-Overlay-Daten sind optional und werden über das bestehende Plugin-Mechanismus der Reference-UI bereitgestellt.
- Das Detail-Panel ersetzt keine eigenständige Node-Bearbeitungsansicht; es ist rein lesend (read-only) in v1.
- Kanten (Edges) und Leeräume im Graphen öffnen kein Detail-Panel.

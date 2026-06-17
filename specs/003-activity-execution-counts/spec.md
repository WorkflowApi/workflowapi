# Feature Specification: Activity Execution Counts

**Feature Branch**: `003-activity-execution-counts`

**Created**: 2026-06-17

**Status**: Draft

**Input**: User description: "Erweitere den Visualizer um eine Anzeige der Anzahl der Ausführungen für Workflow Activity Nodes. Jede Node soll eine kleine Anzeige bekommen, wie oft diese Aktivität ausgeführt worden ist. Der Zeitraum der Auswertung soll auswählbar sein (letzte Stunde, letzter Tag, letzte Woche, letzter Monat). Die Daten sollen aus Temporal ermittelt werden."

## User Scenarios & Testing *(mandatory)*

### User Story 1 – Ausführungszähler auf Activity Nodes anzeigen (Priority: P1)

Ein Entwickler betrachtet den Workflow-Graphen im Visualizer und sieht auf jeder Activity Node eine kompakte Anzeige, wie oft diese Aktivität im gewählten Zeitraum ausgeführt wurde. So erkennt er auf einen Blick, welche Aktivitäten besonders häufig oder selten durchlaufen werden.

**Why this priority**: Der Ausführungszähler ist der zentrale Nutzen dieses Features. Ohne die visuelle Anzeige auf den Nodes bieten die weiteren Aspekte (Zeitraumwahl, Temporal-Anbindung) keinen sichtbaren Mehrwert.

**Independent Test**: Kann getestet werden, indem der Visualizer im Browser geöffnet wird und geprüft wird, ob jede Activity Node einen numerischen Zähler (Badge) anzeigt, der ≥ 0 ist.

**Acceptance Scenarios**:

1. **Given** der Visualizer ist geladen und Temporal erreichbar, **When** der Benutzer den Graphen betrachtet, **Then** zeigt jede Activity Node ein kompaktes Badge mit der Anzahl der Ausführungen im aktuell gewählten Zeitraum an.
2. **Given** eine Activity wurde im gewählten Zeitraum 0-mal ausgeführt, **When** der Benutzer die Node betrachtet, **Then** zeigt das Badge „0" an (keine leere Anzeige).
3. **Given** eine Activity wurde im gewählten Zeitraum 12.483-mal ausgeführt, **When** der Benutzer die Node betrachtet, **Then** wird die Zahl lesbar formatiert dargestellt (z. B. „12.483" oder „12.5k").

---

### User Story 2 – Zeitraum für die Auswertung auswählen (Priority: P2)

Ein Entwickler möchte den Auswertungszeitraum ändern, um die Ausführungshäufigkeit für verschiedene Zeitfenster zu vergleichen — z. B. ob eine Aktivität in der letzten Stunde ungewöhnlich häufig aufgerufen wurde.

**Why this priority**: Der Zeitraumfilter gibt den Zählern Kontext und macht sie aussagekräftig. Ohne ihn wäre unklar, worauf sich die Zahlen beziehen.

**Independent Test**: Kann getestet werden, indem der Zeitraumfilter von „Letzte Stunde" auf „Letzter Monat" umgestellt wird und geprüft wird, ob sich die angezeigten Zähler ändern.

**Acceptance Scenarios**:

1. **Given** der Visualizer ist geladen, **When** der Benutzer die Zeitraumauswahl öffnet, **Then** stehen die Optionen „Letzte Stunde", „Letzter Tag", „Letzte Woche" und „Letzter Monat" zur Verfügung.
2. **Given** der Zeitraum ist auf „Letzte Stunde" eingestellt, **When** der Benutzer auf „Letzter Monat" wechselt, **Then** werden die Zähler auf allen Activity Nodes mit den neuen Werten aktualisiert.
3. **Given** der Benutzer wechselt den Zeitraum, **When** die Daten geladen werden, **Then** wird ein kurzer Ladezustand angezeigt (z. B. Skeleton oder Spinner im Badge), bis die neuen Werte verfügbar sind.

---

### User Story 3 – Graceful Degradation bei nicht erreichbarem Temporal (Priority: P3)

Ein Entwickler nutzt den Visualizer, aber der Temporal-Server ist nicht erreichbar (z. B. Container nicht gestartet). Der Visualizer zeigt dennoch den Workflow-Graphen korrekt an, ohne abzustürzen oder unverständliche Fehlermeldungen anzuzeigen.

**Why this priority**: Der Visualizer muss auch ohne Temporal-Verbindung nutzbar bleiben — die statische Darstellung des Graphen darf durch ein fehlendes Runtime-Overlay nicht beeinträchtigt werden.

**Independent Test**: Kann getestet werden, indem der Temporal-Service gestoppt wird und geprüft wird, ob der Visualizer den Graphen ohne Zähler korrekt anzeigt.

**Acceptance Scenarios**:

1. **Given** der Temporal-Server ist nicht erreichbar, **When** der Visualizer geladen wird, **Then** zeigt der Graph alle Nodes und Edges korrekt an, die Execution-Count-Badges zeigen einen „nicht verfügbar"-Zustand (z. B. „–" oder ein Icon).
2. **Given** der Temporal-Server war nicht erreichbar und wird gestartet, **When** der Benutzer den Zeitraum wechselt oder manuell aktualisiert, **Then** werden die Zähler korrekt geladen und angezeigt.
3. **Given** der Temporal-Server antwortet langsam (>5 Sekunden), **When** der Visualizer die Daten anfragt, **Then** wird nach einem Timeout ein „nicht verfügbar"-Zustand angezeigt, ohne dass die UI einfriert.

---

### Edge Cases

- Was passiert, wenn ein Activity-Name im Workflow-Dokument nicht mit den Temporal-Daten korreliert werden kann? → Das Badge zeigt „–" (nicht zuordbar).
- Was passiert, wenn die Temporal-API sehr viele Ergebnisse liefert (z. B. 100.000+ Ausführungen)? → Die Aggregation erfolgt serverseitig (Temporal Visibility API); der Visualizer erhält nur die Zählwerte.
- Was passiert, wenn der Benutzer den Zeitraum schnell mehrfach wechselt? → Nur die letzte Anfrage wird verarbeitet (Race-Condition-Schutz via Request-Cancellation).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Jede Activity Node im Visualizer MUSS ein Badge anzeigen, das die Anzahl der Ausführungen im gewählten Zeitraum darstellt.
- **FR-002**: Der Visualizer MUSS eine Zeitraumauswahl (Dropdown oder Segmented Control) mit den Optionen „Letzte Stunde", „Letzter Tag", „Letzte Woche" und „Letzter Monat" bereitstellen.
- **FR-003**: Bei Änderung des Zeitraums MÜSSEN die Zähler aller Activity Nodes aktualisiert werden.
- **FR-003a**: Die Zähler MÜSSEN automatisch alle 2 Sekunden aktualisiert werden (Auto-Refresh/Polling).
- **FR-004**: Die Ausführungszähler MÜSSEN über Prometheus-Metriken ermittelt werden, die der Temporal Worker via OpenTelemetry exportiert (Counter `temporal_activity_task_completed` mit Label `activity_type`). Prometheus scrapt den Worker-Metrics-Endpoint.
- **FR-005**: Der Visualizer MUSS einen Backend-Proxy-Service nutzen, der Prometheus (PromQL) abfragt und die aggregierten Zähler als JSON-Endpoint bereitstellt; der Browser darf NICHT direkt auf Prometheus oder Temporal zugreifen.
- **FR-006**: Der Backend-Proxy-Service und ein Prometheus-Container MÜSSEN als eigene Services im Docker-Compose-Stack laufen.
- **FR-007**: Bei nicht erreichbarem Temporal MUSS der Visualizer den Graphen weiterhin korrekt anzeigen; die Badges zeigen einen „nicht verfügbar"-Zustand.
- **FR-008**: Große Zahlen (≥1000) MÜSSEN im Kompaktformat mit Suffix dargestellt werden (z. B. „12.5k", „1.2M"). Bei Hover/Tooltip wird die volle Zahl angezeigt.
- **FR-009**: Während die Daten geladen werden, MUSS ein Ladezustand im Badge sichtbar sein.
- **FR-010**: Mehrfache schnelle Zeitraumwechsel DÜRFEN KEINE Race Conditions verursachen — nur das Ergebnis der letzten Anfrage wird angezeigt.

### Key Entities

- **Execution Count Badge**: Visuelles UI-Element auf jeder Activity Node, das den aggregierten Zähler darstellt.
- **Time Range Filter**: UI-Steuerelement zur Auswahl des Auswertungszeitraums.
- **Activity Metrics Proxy**: Backend-Service, der Prometheus via PromQL abfragt und aggregierte Activity-Ausführungszähler als REST-Endpoint bereitstellt.
- **Prometheus**: Metrics-Speicher, der den Worker-Metrics-Endpoint scrapt und PromQL-Abfragen für Zeitraum-Aggregation unterstützt.
- **Temporal Worker Metrics Endpoint**: OpenTelemetry-basierter Prometheus-Exporter im .NET Worker, der `temporal_activity_task_completed{activity_type="..."}` Counter bereitstellt.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Bei laufendem Temporal-Server zeigt der Visualizer auf allen Activity Nodes innerhalb von 3 Sekunden nach Seitenladen die korrekten Ausführungszähler an.
- **SC-002**: Ein Zeitraumwechsel aktualisiert die Zähler auf allen Nodes innerhalb von 2 Sekunden.
- **SC-003**: Bei nicht erreichbarem Temporal bleibt der Workflow-Graph vollständig navigierbar und zeigt statt Zählern einen klar erkennbaren „nicht verfügbar"-Zustand.
- **SC-004**: Der Visualizer unterstützt alle vier Zeitraumoptionen und die Zähler spiegeln den tatsächlichen Temporal-Datenbestand korrekt wider.
- **SC-005**: Große Zahlen (>10.000 Ausführungen) werden im Badge lesbar formatiert angezeigt.

## Assumptions

- Der Temporal Worker (.NET) exportiert Prometheus-Metriken über einen konfigurierbaren HTTP-Endpoint (z. B. `:9090/metrics`). Der Worker muss dafür um OpenTelemetry/Prometheus-Exporter-Konfiguration erweitert werden.
- Ein Prometheus-Container im Docker-Compose-Stack scrapt den Worker-Metrics-Endpoint in einem regelmäßigen Intervall (z. B. alle 15 Sekunden).
- Die Activity-Type-Namen in den Prometheus-Labels (`activity_type`) entsprechen exakt (1:1 String-Match) den Activity-Namen im WorkflowAPI-Dokument. Kein konfigurierbares Mapping erforderlich.
- Der Backend-Proxy-Service ist ein leichtgewichtiger Node.js/Express HTTP-Service (TypeScript), der im selben Docker-Netzwerk läuft und PromQL-Abfragen an Prometheus stellt.
- Die bestehende Docker-Compose-Struktur (Compose-Datei unter `src/docker-compose.yml`) ist die Grundlage.
- Der Standard-Zeitraum beim ersten Laden ist „Letzter Tag".
- Zahlen werden im Kompaktformat mit Suffix dargestellt (z. B. „12.5k", „1.2M"); bei Hover zeigt ein Tooltip die volle Zahl. Dieses Format ist sprachunabhängig und platzsparend.
- Die Architektur ist Backend-agnostisch konzipiert: Worker → OTEL Metrics → [Prometheus | Datadog | etc.] → Proxy → Visualizer. Im lokalen Setup wird Prometheus verwendet; ein Austausch durch Datadog o. ä. ist ohne Änderung am Worker möglich.

## Clarifications

### Session 2026-06-17

- Q: Welche Datenquelle soll für die Activity-Ausführungszähler verwendet werden (Temporal Visibility API, Workflow-History-Events, oder OpenTelemetry/Prometheus Metriken)? → A: OpenTelemetry/Prometheus Metriken vom Worker SDK (Option C). Der Worker exportiert `temporal_activity_task_completed{activity_type="..."}` Counter; Prometheus scrapt und aggregiert; ein Proxy stellt PromQL-Ergebnisse als JSON bereit.
- Q: Wie soll das Matching zwischen Activity-Namen im WorkflowAPI-Dokument und Prometheus-Labels erfolgen? → A: Identischer String-Match — der Activity-Name im WorkflowAPI-Dokument entspricht 1:1 dem `activity_type`-Label in Prometheus. Kein Mapping-File nötig.
- Q: Wie sollen große Zahlen im Badge formatiert werden? → A: Kompaktformat mit Suffix (z. B. „12.5k", „1.2M") — sprachunabhängig, platzsparend. Bei Hover zeigt ein Tooltip die volle Zahl.
- Q: Sollen die Zähler automatisch aktualisiert werden oder nur bei explizitem Zeitraumwechsel? → A: Auto-Refresh alle 2 Sekunden (Polling). Zusätzlich Aktualisierung bei Zeitraumwechsel.
- Q: Welche Technologie soll für den Activity Metrics Proxy verwendet werden? → A: Node.js/Express (TypeScript) — leichtgewichtig, TypeScript durchgängig mit dem Frontend, schneller Build.

# Feature Specification: Visualizer Docker Compose Integration

**Feature Branch**: `002-visualizer-docker-compose`

**Created**: 2026-06-17

**Status**: Draft

**Input**: User description: "Der workflow visualizer in src/web/workflow-react-flow-visualizer-mvp soll in das docker compose src/temporal-local/docker-compose.yml integriert werden. Dazu soll das compose file nach ./src verschoben werden. Beim start des compose files soll sowohl temporal+workers als auch der visualizer gestartet werden."

## User Scenarios & Testing *(mandatory)*

### User Story 1 – Vollständigen lokalen Stack mit einem Befehl starten (Priority: P1)

Ein Entwickler möchte die gesamte lokale Entwicklungsumgebung — bestehend aus Temporal Server, Worker und Workflow-Visualizer — mit einem einzigen `docker compose up`-Befehl starten, ohne mehrere Terminals oder separate Startschritte zu benötigen.

**Why this priority**: Dies ist der primäre Nutzen des Features. Ohne diesen zentralen Startpunkt haben alle weiteren Aspekte keinen Wert.

**Independent Test**: Kann vollständig getestet werden, indem aus dem Verzeichnis `src/` der Befehl `docker compose up --build` ausgeführt wird und anschließend geprüft wird, ob Temporal UI (`http://localhost:8233`) und Visualizer (`http://localhost:<port>`) gleichzeitig erreichbar sind.

**Acceptance Scenarios**:

1. **Given** das Repository ist ausgecheckt und Docker läuft, **When** ein Entwickler `docker compose up --build` im Verzeichnis `src/` ausführt, **Then** starten PostgreSQL, Temporal Server, Temporal UI, Risk Worker und der Workflow-Visualizer nacheinander ohne manuelle Eingriffe.
2. **Given** der Stack läuft, **When** ein Entwickler den Browser öffnet, **Then** ist der Workflow-Visualizer unter einer definierten lokalen URL erreichbar (statisch serviert, kein Dev-Server).
3. **Given** der Stack läuft, **When** ein Entwickler `docker compose down` im Verzeichnis `src/` ausführt, **Then** werden alle Services inkl. des Visualizers gestoppt.

---

### User Story 2 – Compose-Datei im neuen Pfad `src/docker-compose.yml` nutzen (Priority: P2)

Ein Entwickler arbeitet mit dem Repository und erwartet, dass die Compose-Datei im übergeordneten `src/`-Verzeichnis liegt, sodass zukünftige Services (z. B. weitere Web-Anwendungen) einfach ergänzt werden können.

**Why this priority**: Die Verschiebung der Compose-Datei ist Voraussetzung für die Integration des Visualizers und schafft eine skalierbare Struktur.

**Independent Test**: Kann getestet werden, indem geprüft wird, ob `src/docker-compose.yml` existiert und `src/temporal-local/docker-compose.yml` nicht mehr vorhanden ist, und ob alle bestehenden Services weiterhin korrekt starten.

**Acceptance Scenarios**:

1. **Given** das Repository enthält `src/temporal-local/docker-compose.yml`, **When** das Feature implementiert ist, **Then** existiert die Compose-Datei unter `src/docker-compose.yml` und der alte Pfad ist nicht mehr vorhanden.
2. **Given** `src/docker-compose.yml` liegt an seinem neuen Ort, **When** `docker compose up` aus `src/` gestartet wird, **Then** funktionieren alle vorhandenen Services (PostgreSQL, Temporal, Temporal UI, Risk Worker) wie bisher ohne Änderung ihres Verhaltens.
3. **Given** die Compose-Datei wurde verschoben, **When** relative Pfade in der Compose-Datei auf Worker-Artefakte (Dockerfile, dynamicconfig) verweisen, **Then** sind diese Pfade korrekt angepasst und das Build schlägt nicht fehl.

---

### User Story 3 – Visualizer wird automatisch gebaut und gestartet (Priority: P3)

Der Workflow-Visualizer (Vite/React-App) soll als Docker-Service in den Stack eingebunden werden, ohne dass ein Entwickler manuell `npm install` oder `npm run dev` ausführen muss.

**Why this priority**: Dies vervollständigt die Zero-Configuration-Erfahrung — der Visualizer ist nicht separat zu starten.

**Independent Test**: Kann getestet werden, indem geprüft wird, ob der Visualizer-Container gestartet wird, wenn `docker compose up --build` ausgeführt wird, und ob die App im Browser erreichbar ist.

**Acceptance Scenarios**:

1. **Given** es existiert kein `node_modules`-Verzeichnis auf dem Host, **When** `docker compose up --build` gestartet wird, **Then** baut Docker den Visualizer-Container inklusive Abhängigkeitsinstallation selbstständig.
2. **Given** der Stack läuft, **When** der Visualizer-Container gestartet ist, **Then** ist die React-App als statischer Produktions-Build (via nginx oder vergleichbarem Static File Server) im Browser erreichbar.
3. **Given** der Temporal-Server ist noch nicht bereit, **When** der Visualizer-Container startet, **Then** startet der Visualizer-Service unabhängig, da er keine direkte Laufzeit-Abhängigkeit auf Temporal hat.

---

### Edge Cases

- Was passiert, wenn der Visualizer-Build fehlschlägt (z. B. TypeScript-Fehler)? → Der übrige Stack startet weiterhin, der Visualizer-Service geht in `Exit`-Zustand.
- Was passiert, wenn Port-Konflikte auf dem Host existieren (z. B. Port bereits belegt)? → Docker Compose meldet den Konflikt; der Nutzer muss den Port in einer `.env`-Datei überschreiben können.
- Was passiert, wenn relative Pfade nach der Verschiebung der Compose-Datei nicht korrekt angepasst wurden? → `docker compose up --build` schlägt mit einem klaren Fehlerpfad fehl, der auf das fehlende Dockerfile zeigt.
- Was passiert, wenn `src/temporal-local/docker-compose.yml` noch im Repository existiert (z. B. vergessen zu löschen)? → Es könnten versehentlich zwei Stacks parallel gestartet werden; die alte Datei muss entfernt werden.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Die Compose-Datei MUSS von `src/temporal-local/docker-compose.yml` nach `src/docker-compose.yml` verschoben werden.
- **FR-002**: Alle relativen Pfade in der Compose-Datei (z. B. zum Worker-Dockerfile, zur `dynamicconfig`) MÜSSEN nach der Verschiebung korrekt angepasst sein.
- **FR-003**: Die Compose-Datei MUSS einen neuen Service für den Workflow-Visualizer (`src/web/workflow-react-flow-visualizer-mvp`) enthalten, der über ein mehrstufiges Dockerfile (Multi-Stage Build) gebaut wird: Stufe 1 (Build-Stufe) basiert auf `node:22-alpine` und baut den produktiven statischen Build (`npm run build`), Stufe 2 basiert auf `nginx:1.27-alpine` und kopiert die Artefakte in den nginx-Container, der sie als rein statische Dateien serviert. nginx benötigt **keine** `proxy_pass`-Konfiguration, da die App zur Laufzeit keine Backend-Calls macht. Das Dockerfile liegt unter `src/web/workflow-react-flow-visualizer-mvp/Dockerfile`; der Docker-Build-Context in der Compose-Datei ist `./web/workflow-react-flow-visualizer-mvp`.
- **FR-004**: Der Visualizer-Service MUSS einen Host-Port exponieren, über den die App im Browser erreichbar ist.
- **FR-005**: Der Visualizer-Service MUSS im selben Docker-Netzwerk (`temporal-local`) laufen wie die übrigen Services.
- **FR-006**: Ein einziger `docker compose up --build`-Befehl aus `src/` MUSS alle Services starten: PostgreSQL, Temporal Server, Temporal UI, Risk Worker und Workflow-Visualizer.
- **FR-007**: Der Visualizer-Port MUSS über eine Umgebungsvariable (`.env`-Datei) konfigurierbar sein, um Host-Port-Konflikte zu vermeiden.
- **FR-008**: Die bisherige Compose-Datei `src/temporal-local/docker-compose.yml` MUSS aus dem Repository entfernt werden (oder durch einen Hinweis auf den neuen Pfad ersetzt werden), um Verwirrung zu vermeiden.
- **FR-009**: Die `README.md` in `src/temporal-local/` MUSS aktualisiert werden, sodass der neue Startpfad (`src/`) dokumentiert ist.

### Key Entities

- **docker-compose.yml (src/)**: Zentrale Orchestrierungsdatei für den gesamten lokalen Stack; enthält alle Service-Definitionen, Netzwerk- und Volume-Konfigurationen.
- **Visualizer-Service**: Neuer Docker-Service auf Basis der Vite/React-Anwendung in `src/web/workflow-react-flow-visualizer-mvp`; exponiert einen HTTP-Port für den Browser-Zugriff.
- **Worker-Service (risk-worker)**: Bestehender Service; Dockerfile-Pfad muss nach der Verschiebung der Compose-Datei angepasst werden.
- **dynamicconfig**: Bestehendes Volume-Mount für die Temporal-Konfiguration; relativer Pfad muss nach der Verschiebung angepasst werden.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Ein Entwickler kann nach einem frischen `git clone` mit einem einzigen Befehl (`docker compose up --build` aus `src/`) alle fünf Services starten — ohne zusätzliche manuelle Schritte.
- **SC-002**: Der Workflow-Visualizer ist spätestens 60 Sekunden nach Start des Compose-Stacks im Browser erreichbar.
- **SC-003**: Alle bestehenden Services (PostgreSQL, Temporal, Temporal UI, Risk Worker) verhalten sich nach der Migration identisch wie vor der Verschiebung der Compose-Datei — keine Regression in deren Verhalten.
- **SC-004**: Der Visualizer-Port ist innerhalb von 2 Minuten über eine Umgebungsvariable anpassbar und der Stack startet danach fehlerfrei neu.
- **SC-005**: Die Dokumentation (`README.md`) ist nach der Migration aktuell und beschreibt den neuen Startpfad korrekt.

## Assumptions

- Das Dockerfile liegt co-lokiert bei `src/web/workflow-react-flow-visualizer-mvp/Dockerfile`; der in der Compose-Datei verwendete Build-Context ist `./web/workflow-react-flow-visualizer-mvp` (relativ zu `src/`).
- Der Workflow-Visualizer (`src/web/workflow-react-flow-visualizer-mvp`) wird als **produktiver statischer Build** über einen Static File Server (z. B. nginx) betrieben. Es wird kein Vite Dev Server im Container gestartet. Das Dockerfile verwendet ein Multi-Stage-Build-Pattern: Build-Stufe (`node:22-alpine` + Vite/`npm run build`) → Serve-Stufe (`nginx:1.27-alpine`).
- Der Visualizer benötigt zur Laufzeit **keine direkte Netzwerkverbindung zu Temporal** und macht **keine Backend-Calls** (er lädt Workflow-Definitionen aus statischen Beispieldateien oder über eine eigene Konfiguration). Daher sind weder eine `depends_on`-Abhängigkeit auf den Temporal-Service noch eine `proxy_pass`-Konfiguration in nginx erforderlich — nginx serviert ausschließlich die vorgebauten `dist/`-Dateien.
- Node.js und npm sind nicht lokal auf dem Entwickler-Rechner erforderlich — alle Build-Schritte laufen im Docker-Container.
- Die `.env`-Datei für Port-Überschreibungen ist **nicht** ins Repository eingecheckt; ein `.env.example` mit Standardwerten genügt.
- Der Visualizer läuft auf einem anderen Port als Temporal UI (`8233`), um Konflikte zu vermeiden. Standardport für den Visualizer: `3000`.
- Die Umgebung ist ausschließlich für die **lokale Entwicklung** vorgesehen; Produktions-Deployments sind nicht Gegenstand dieses Features.
- `src/temporal-local/docker-compose.yml` wird **vollständig entfernt** (kein Redirect/Alias); die `README.md` in `src/temporal-local/` wird angepasst.

## Clarifications

### Session 2026-06-17

- Q: Soll der Visualizer im Container als Vite Dev Server betrieben werden (mit optionalem Volume-Mount für Hot Reload), oder als statischer Produktions-Build (z. B. nginx)? → A: Produktions-Build, statisch serviert via nginx (kein Vite Dev Server im Container); Multi-Stage Dockerfile (Node.js Build → nginx Serve).
- Q: Macht der Visualizer zur Laufzeit Backend-Calls (z. B. zu einer API oder zu Temporal), und braucht nginx eine proxy_pass-Konfiguration? → A: Rein statisch. Die App macht keine Laufzeit-Backend-Calls; nginx serviert ausschließlich die dist/-Dateien, kein proxy_pass erforderlich.
- Q: Wo liegt das Dockerfile des Visualizers und was ist der Docker-Build-Context in der Compose-Datei? → A: Co-lokiert unter `src/web/workflow-react-flow-visualizer-mvp/Dockerfile`; Build-Context = `./web/workflow-react-flow-visualizer-mvp` (relativ zu `src/`).
- Q: Welcher Node.js-Base-Image-Tag soll im Dockerfile (Build-Stufe) verwendet werden? → A: `node:22-alpine` (Active LTS, Stand Juni 2026).
- Q: Welcher nginx-Base-Image-Tag soll im Dockerfile (Serve-Stufe) verwendet werden? → A: `nginx:1.27-alpine` — gepinnte Minor-Version, Alpine-basiert, konsistent mit `node:22-alpine`.

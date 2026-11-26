## Purpose

Describe how ISTAvalon exposes GUI ViewModel state as YAML for local observation and debugging, as implemented by `GuiStateDumper` and `GuiObservationServer`.

> Aligned to code as of 2026-07-26.

## Requirements

### Requirement: GUI SHALL expose state as a structured YAML document

The system SHALL provide a `GuiStateDumper` that serializes the main window ViewModel into a structured YAML document. Serialization SHALL use YamlDotNet with `underscored` naming and SHALL omit null values.

The dump SHALL include window metadata, per-tab command/parameter/log state, and nested parameter editor objects (`component`, `binding`, `validation`). Log entries SHALL be capped at the 200 most recent with `omitted_entries` for the rest.

Sensitive values SHALL be redacted via `GuiDumpRedaction`:

- Property names treated as secrets (e.g. Password, MachineGuid, VolumeSerialNumber, Token, …) → `[REDACTED]`
- Path-like values → `[REDACTED_PATH]/<fileName>`
- Log messages matching sensitive assignment/label patterns → redacted in place

#### Scenario: Dump window metadata

- **WHEN** the state dumper serializes the main window ViewModel
- **THEN** the output includes `title`, `selected_tab`, and `theme` with `current` and `toggle_button` details

#### Scenario: Dump per-tab parameter state

- **WHEN** the state dumper serializes a command tab ViewModel
- **THEN** each tab entry includes `name`, `selected_command`, `available_commands`, `controls`, `is_executing`, `has_preset`, `status`, and `log_panel`

#### Scenario: Dump log panel state with capped entries

- **WHEN** the log panel contains more than 200 entries
- **THEN** the dump includes at most the 200 most recent log entries and sets `omitted_entries`

#### Scenario: Sensitive parameter values are redacted

- **WHEN** a parameter named `Password` (or other sensitive name) has a value
- **THEN** the dump shows `[REDACTED]` instead of the raw value

#### Scenario: Path values are partially redacted

- **WHEN** a path parameter has a full filesystem path
- **THEN** the dump shows `[REDACTED_PATH]/` plus the final path segment only

### Requirement: GUI MAY expose state via an optional local HTTP endpoint

When observation is enabled, the system SHALL provide a `GuiObservationServer` based on `HttpListener`.

Request handling:

- Only `GET` is accepted; other methods receive HTTP 405
- Paths: `GET /` (help), `GET /health` (`ok\n`), `GET /dump` and `GET /dump.yaml` (YAML, `application/x-yaml`, `Cache-Control: no-store`)
- Unknown paths → 404
- Non-loopback remote addresses → 403
- Null remote address is treated as allowed

#### Scenario: Serve dump.yaml on loopback client

- **WHEN** the observation server is running and a loopback client requests `GET /dump.yaml`
- **THEN** the server responds with HTTP 200 and redacted GUI state YAML

#### Scenario: Reject non-loopback remote addresses

- **WHEN** the remote IP is non-null and not loopback
- **THEN** the server responds with HTTP 403

#### Scenario: Serve health endpoint

- **WHEN** a client requests `GET /health`
- **THEN** the server responds with HTTP 200 and body `ok` (with trailing newline)

### Requirement: Observation server configuration via CLI args and environment variables

Configuration is read by `GuiObservationOptions.From` from CLI flags and `ISTA_GUI_DUMP_HTTP*` environment variables. Defaults: host `127.0.0.1`, port `8765`.

**Bind host MUST be loopback.** Non-loopback host values (LAN/public/wildcard) SHALL be rejected and replaced with `127.0.0.1`, with `HostWasClampedToLoopback = true` and a warning log. IPv6 loopback is normalized for `HttpListener` prefixes (e.g. `::1` → `[::1]`).

#### Scenario: Enable observation via CLI flag

- **WHEN** ISTAvalon is started with `--gui-dump-http`
- **THEN** observation is enabled on the default loopback host and port

#### Scenario: Non-loopback host is clamped

- **WHEN** `--gui-dump-http-host 0.0.0.0` is supplied
- **THEN** the effective host is `127.0.0.1` and `HostWasClampedToLoopback` is true

### Requirement: Observation server SHALL fail safe on startup

When observation is enabled, `App` SHALL attempt to start `GuiObservationServer` inside a try/catch. On bind or startup failure, a warning SHALL be logged and the main window SHALL continue to launch without observation.

#### Scenario: Server fails to bind

- **WHEN** the observation server cannot bind to the configured prefix
- **THEN** a warning is logged and the main application window still launches

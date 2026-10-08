## Purpose

Define the SAP intermediate-table rebuild operation used by accounting staff to publish four daily Oracle datasets. It preserves the legacy VB6 calculations while making reruns explicit and each event's write atomic.

## ADDED Requirements

### Requirement: Daily SAP operation controls
The system SHALL provide a Gregorian HTML date input and four selected-by-default controls for SAPCASH, SAPCONS, SAPACC, and SAPREV2. It SHALL accept a valid Gregorian date from 1912 through 2910 and convert it to the seven-character ROC date at the repository boundary. A zero-selection submission SHALL perform no writes.

#### Scenario: Open the operation
- **WHEN** an operator opens SAP
- **THEN** the system displays the original logday-derived default date, or yesterday when no usable value exists
- **AND** all four operations are selected

#### Scenario: Reject invalid date
- **WHEN** a caller submits an invalid or out-of-range Gregorian date
- **THEN** the API rejects the request before any database write

##### Example: date conversion
- **GIVEN** the browser date is 2026-01-01
- **WHEN** the operator runs one event
- **THEN** source and logday filters receive ROC date 1150101 and target deletion receives Gregorian date 20260101

### Requirement: Explicit rerun choices
The system SHALL report completion from `logday` for each event and date. It SHALL require explicit per-event confirmation before replacing data for any completed selected event. Declining a rerun SHALL skip that event and allow later selected events to run.

#### Scenario: Completed event is declined
- **WHEN** SAPCASH is complete and the operator declines its rerun while SAPCONS is selected
- **THEN** SAPCASH target data and logday remain intact
- **AND** SAPCONS runs in normal sequence

#### Scenario: Concurrent completion
- **WHEN** an event becomes complete after preflight but before its locked execution check
- **THEN** the system SHALL require confirmation without replacing that event's data

### Requirement: Four event SQL equivalence
The system SHALL execute the fifteen Oracle statements from `ISAP/01_ASPNET_Core_MVC_SPEC.md` section 8 without changing expressions, UNION ALL branches, aggregation levels, date boundaries, or signs. Selected work SHALL run in SAPCASH, SAPCONS, SAPACC, SAPREV2 order, using existing upstream daily tables. For each event, the system SHALL count rows affected only by INSERT statements into that event's SAP intermediate table; DELETE and `logday` statements SHALL NOT contribute to the count.

#### Scenario: Rebuild a selected event
- **WHEN** a selected event is eligible to run
- **THEN** the system removes the legacy-defined target rows and performs all corresponding INSERT statements
- **AND** when their total inserted row count is positive, the system commits and records `logday.ok='Y'`
- **AND** when all INSERT statements succeed but their total row count is zero, the system rolls back the event transaction and preserves the target rows and `logday` state from before the event

##### Example: cash advance
- **GIVEN** an inpatient advance-payment group has nonzero amounts in the eight legacy I components and additional values in cash fields 7 through 10
- **WHEN** SAPCASH runs
- **THEN** cashtype I uses only the eight legacy components

##### Example: multi-statement insert totals
| Event | INSERT row counts | Expected transaction result |
| ----- | ----------------- | ---------------------------- |
| SAPCASH | 0 | rollback, noData |
| SAPACC | 0, 0 | rollback, noData |
| SAPACC | 0, 3 | commit, completed |
| SAPREV2 | 0, 0, 0 | rollback, noData |
| SAPREV2 | 0, 2, 0 | commit, completed |

#### Scenario: Compare legacy output
- **WHEN** VB6 and MVC execute against the same fixed non-production Oracle snapshot
- **THEN** each target table's rows SHALL match as a multiset across all output columns, including X1 rate boundaries and 51 discount cases

### Requirement: Atomic event result
The system SHALL commit each event and date separately. The system SHALL return `completed` and write `logday.ok='Y'` only when all statements succeed and the total rows inserted into that event's SAP intermediate table are positive. When all INSERT statements succeed but their total is zero, the system SHALL roll back that event's target deletion and return `noData` without changing its prior target rows or `logday` state. A SQL exception SHALL roll back the event and return `failed`. An unconfirmed rerun SHALL return `confirmationRequired` without replacing data. The response SHALL identify each selected event independently so later selected events can continue.

#### Scenario: Insert fails
- **WHEN** an insert for SAPREV2 fails after its target deletion
- **THEN** the transaction restores that event's previous rows and logday state
- **AND** the API returns a failed result for SAPREV2

#### Scenario: First run inserts no rows
- **WHEN** an event has no prior completion record and all its INSERT statements succeed with a total of zero rows
- **THEN** the transaction restores its pre-run target rows and does not create a completion record
- **AND** the API returns `status: "noData"` for that event

#### Scenario: Confirmed rerun inserts no rows
- **GIVEN** the event has existing SAP intermediate-table rows and `logday.ok='Y'`
- **WHEN** the operator confirms a rerun and all its INSERT statements succeed with a total of zero rows
- **THEN** the previous target rows and completion record remain intact
- **AND** the API returns `status: "noData"` for that event

# Changelog — JADirect FleetOps

All notable changes to this project are documented in this file.

---

## [2.0.0] - 2026-04

### Added
- Walkaround check rebuilt with three states per item: Good, Attention, Defect.
- Each flagged item requires the driver to describe what was found and select
  an action: Resolved in field or Needs garage.
- Support for three vehicle types: Van (18 items), RigidTruck (25 items),
  ArticulatedTruck (30 items). Correct checklist loaded automatically per vehicle.
- Configurable vehicle blocking policy per tenant via walkaround_blocking_rules table.
  JADirect default: Defect/Attention + RequiresGarage = blocked.
  Defect/Attention + Resolved = operational.
- ChecklistItemRepository: loads checklist items dynamically from database.
- BlockingRuleRepository: loads blocking rules per tenant from database.
- WalkaroundService: centralises all walkaround business logic.
- ChecklistItem entity and BlockingRule entity in Domain layer.
- ChecklistItemResult ViewModel as central transit object across all layers.
- Real-time status bar in walkaround form showing vehicle status before submit.
- Audit History rebuilt: each inspection now shows full item detail with state,
  action and driver note, grouped by category.
- PRODUCTION_RUNBOOK.md documenting step-by-step production deployment procedure.

### Changed
- WalkaroundController refactored to delegate all logic to WalkaroundService.
- InspectionRepository.Add updated: removed hasDefect and defectNotes parameters,
  now receives vehicleStatusId calculated by WalkaroundService.
- WalkaroundHistoryViewModel updated: removed hasDefect and DefectNotes globals,
  added Items list with VehicleWasBlocked and IsPassed calculated properties.
- InspectionRepository history methods updated to deserialise checklist JSON by item.
- VehicleType enum: Truck renamed to RigidTruck (value 2 preserved),
  ArticulatedTruck = 3 added.
- History.cshtml layout normalised to match the rest of the system visual standard.

### Fixed
- checklist_json column type changed from TEXT to MEDIUMTEXT to support
  large inspection notes without database errors.

### Database
- New table: checklist_items with 73 items for Van, RigidTruck and ArticulatedTruck.
- New table: walkaround_blocking_rules with JADirect default policy (4 rules).
- Migration script migration_walkaround_v1.sql converts all historical records
  from Pass/Fail format to new state/action/note format per item.
- Backup table walkaround_checks_backup_pre_migration created before migration.

---

## [1.2.0] - 2026-04-18

### Fixed
- Duplicate daily log submissions are now blocked at both application
  and database level.
- A driver can only submit one log per day, regardless of which vehicle was used.
- Corrected the unique constraint from (user_id + vehicle_id + date) to
  (user_id + date), closing a gap where a driver could submit logs for
  different vehicles on the same day.

### Added
- Date picker field on the Daily Log form. Defaults to today.
- Drivers can submit late entries for up to 7 days in the past.
- Future dates are blocked at both UI and service layer.

### Database
- Removed 7 duplicate records from 17 April 2026 (first operational day).
- Removed 1 test record from 18 April 2026 (Dave Tew, id 28).
- Backup table daily_logs_backup_20260417 created before any deletions.
- Replaced unique constraint uq_daily_log_user_vehicle_date
  with uq_daily_log_user_date.
- Added index idx_daily_logs_vehicle_id to maintain foreign key integrity.

### Improved
- DailyLogService created in Application layer to centralise all
  daily log business rules.
- DailyLogController refactored to delegate all business logic to the service.
- 16 CS8618 compiler warnings resolved across the Domain layer.
- Setup.sql consolidated: schema now defined inline without ALTER TABLE statements.

---

## [1.0.0] - 2026-04-17

### Added
- First production deployment.
- Driver daily log submission (deliveries, collections, returns, odometer, notes).
- Walkaround check with 27-item checklist, defect reporting and GPS location.
- Manager dashboard with performance report, compliance exceptions and audit log.
- Vehicle and user management.
- Excel export for audit log.
- Role-based access: Manager, Driver, Supervisor.
- Walkaround compliance traffic light system (Green, Yellow, Red) per vehicle type.

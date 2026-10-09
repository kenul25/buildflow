# Verified backend test results

Local execution completed: 2026-10-09 23:39:47 Asia/Colombo.

Environment: Windows, .NET SDK 8.0.423 / net8.0, Release configuration, local PostgreSQL 18.
Both context migrations and the real API host started successfully. The integration database was disposable.
This is a local execution record; a GitHub Actions run has not been performed for these changes.

| Category | Passed | Failed | Skipped | Total |
| --- | ---: | ---: | ---: | ---: |
| Unit | 38 | 0 | 0 | 38 |
| Integration | 27 | 0 | 0 | 27 |

Raw evidence: `.artifacts/test-results/backend/unit.trx` and `integration.trx`.
CI uploads equivalent files in the `backend-test-results` artifact.

| Owner | Unit passed | Integration passed |
| --- | ---: | ---: |
| Member01 | 8 | 4 |
| Member02 | 9 | 4 |
| Member03 | 8 | 6 |
| Member04 | 7 | 4 |
| Shared | 6 | 9 |

## Individual outcomes

| Owner | Category | Test | Outcome |
| --- | --- | --- | --- |
| Member01 | Integration | `Member01.ConstructionApiIntegrationTests.EngineerCannotReadAnotherEngineersProject` | Passed |
| Member01 | Integration | `Member01.ConstructionApiIntegrationTests.ProjectCanBeCreatedReadAndArchivedWithPostgreSqlPersistence` | Passed |
| Member01 | Integration | `Member01.ConstructionApiIntegrationTests.ReversedProjectDatesAreRejectedAndDoNotPersist` | Passed |
| Member01 | Integration | `Member01.ConstructionApiIntegrationTests.SiteEngineerCannotCreateProject` | Passed |
| Member01 | Unit | `Member01.ConstructionControllerTests.EngineerListPassesActorToServiceToRestrictProjectVisibility` | Passed |
| Member01 | Unit | `Member01.ConstructionServiceTests.EquipmentEffortIsDistributedAcrossResourceCount` | Passed |
| Member01 | Unit | `Member01.ConstructionServiceTests.InvalidPaginationIsRejectedBeforeRepositoryAccess(page: 0, size: 20)` | Passed |
| Member01 | Unit | `Member01.ConstructionServiceTests.InvalidPaginationIsRejectedBeforeRepositoryAccess(page: 1, size: 101)` | Passed |
| Member01 | Unit | `Member01.ConstructionValidationTests.ProgressOutsidePercentageRangeIsRejected(value: -1)` | Passed |
| Member01 | Unit | `Member01.ConstructionValidationTests.ProgressOutsidePercentageRangeIsRejected(value: 101)` | Passed |
| Member01 | Unit | `Member01.ConstructionValidationTests.ProjectRequiresCode` | Passed |
| Member01 | Unit | `Member01.ConstructionValidationTests.ValidProjectPassesValidation` | Passed |
| Member02 | Integration | `Member02.InventoryApiIntegrationTests.ReservationRejectsShortageAndReleaseIsIdempotentInPostgreSql` | Passed |
| Member02 | Integration | `Member02.InventoryApiIntegrationTests.SiteEngineerCannotCreateWarehouse` | Passed |
| Member02 | Integration | `Member02.InventoryApiIntegrationTests.WarehouseCreationPersistsAndCanBeReadThroughApi` | Passed |
| Member02 | Integration | `Member02.InventoryApiIntegrationTests.ZeroStockAdjustmentIsRejectedByApiValidation` | Passed |
| Member02 | Unit | `Member02.InventoryControllerTests.AnalysisReturnsServiceShortageReport` | Passed |
| Member02 | Unit | `Member02.InventoryServiceTests.AvailableStockExcludesReservations` | Passed |
| Member02 | Unit | `Member02.InventoryServiceTests.InvalidPaginationIsRejectedBeforeRepositoryAccess` | Passed |
| Member02 | Unit | `Member02.InventoryServiceTests.InvalidStockInvariantIsRejected(current: -1, reserved: 0)` | Passed |
| Member02 | Unit | `Member02.InventoryServiceTests.InvalidStockInvariantIsRejected(current: 10, reserved: -1)` | Passed |
| Member02 | Unit | `Member02.InventoryServiceTests.InvalidStockInvariantIsRejected(current: 10, reserved: 11)` | Passed |
| Member02 | Unit | `Member02.InventoryValidationTests.StockAdjustmentMustBePositive(quantity: -1)` | Passed |
| Member02 | Unit | `Member02.InventoryValidationTests.StockAdjustmentMustBePositive(quantity: 0)` | Passed |
| Member02 | Unit | `Member02.InventoryValidationTests.ValidWarehousePassesValidation` | Passed |
| Member03 | Integration | `Member03.ProcurementApiIntegrationTests.ComparisonChoosesLowestEligibleCostAndExcludesExpiredQuotes` | Passed |
| Member03 | Integration | `Member03.ProcurementApiIntegrationTests.ComparisonWithoutMatchingQuotesReturnsApprovalRequiredAndNoSideEffects` | Passed |
| Member03 | Integration | `Member03.ProcurementApiIntegrationTests.InvalidSupplierEmailIsRejectedByApiValidation` | Passed |
| Member03 | Integration | `Member03.ProcurementApiIntegrationTests.ServiceRejectsNonPositiveRequestWithoutPersistingIt` | Passed |
| Member03 | Integration | `Member03.ProcurementApiIntegrationTests.SiteEngineerCannotCompareProcurementQuotations` | Passed |
| Member03 | Integration | `Member03.ProcurementApiIntegrationTests.SupplierCreationPersistsAndCanBeReadThroughApi` | Passed |
| Member03 | Unit | `Member03.ProcurementControllerTests.InvalidComparisonReturnsBadRequestBeforeDatabaseAccess(name: "", quantity: 1)` | Passed |
| Member03 | Unit | `Member03.ProcurementControllerTests.InvalidComparisonReturnsBadRequestBeforeDatabaseAccess(name: "Cement", quantity: -1)` | Passed |
| Member03 | Unit | `Member03.ProcurementControllerTests.InvalidComparisonReturnsBadRequestBeforeDatabaseAccess(name: "Cement", quantity: 0)` | Passed |
| Member03 | Unit | `Member03.ProcurementServiceTests.LocalDeadlinePreservesTheInstant` | Passed |
| Member03 | Unit | `Member03.ProcurementServiceTests.UnspecifiedDeadlineIsInterpretedAsUtcWithoutChangingTime` | Passed |
| Member03 | Unit | `Member03.ProcurementValidationTests.SupplierEmailMustBePresentAndValid(email: "")` | Passed |
| Member03 | Unit | `Member03.ProcurementValidationTests.SupplierEmailMustBePresentAndValid(email: "invalid-email")` | Passed |
| Member03 | Unit | `Member03.ProcurementValidationTests.ValidSupplierPassesValidation` | Passed |
| Member04 | Integration | `Member04.SchedulingApiIntegrationTests.InvalidWorkerNameIsRejectedByApiValidation` | Passed |
| Member04 | Integration | `Member04.SchedulingApiIntegrationTests.OverlappingShiftsAreRejectedWithoutAddingSecondShift` | Passed |
| Member04 | Integration | `Member04.SchedulingApiIntegrationTests.SiteEngineerCannotCreateWorkers` | Passed |
| Member04 | Integration | `Member04.SchedulingApiIntegrationTests.WorkerCanBeCreatedReadAndArchivedWithPostgreSqlPersistence` | Passed |
| Member04 | Unit | `Member04.SchedulingControllerTests.InvalidListPaginationPropagatesClientError` | Passed |
| Member04 | Unit | `Member04.SchedulingServiceTests.AdjacentBookingsDoNotOverlap` | Passed |
| Member04 | Unit | `Member04.SchedulingServiceTests.EngineerWriteIsDeniedBeforeDatabaseAccess` | Passed |
| Member04 | Unit | `Member04.SchedulingServiceTests.IntersectingBookingsOverlap` | Passed |
| Member04 | Unit | `Member04.SchedulingServiceTests.ValidWindowIsAccepted` | Passed |
| Member04 | Unit | `Member04.SchedulingValidationTests.EndMustBeAfterStart(hours: -1)` | Passed |
| Member04 | Unit | `Member04.SchedulingValidationTests.EndMustBeAfterStart(hours: 0)` | Passed |
| Shared | Integration | `Shared.AuthorizationIntegrationTests.AnonymousAccessIsRejected(path: "/api/Suppliers")` | Passed |
| Shared | Integration | `Shared.AuthorizationIntegrationTests.AnonymousAccessIsRejected(path: "/api/inventory/materials")` | Passed |
| Shared | Integration | `Shared.AuthorizationIntegrationTests.AnonymousAccessIsRejected(path: "/api/projects")` | Passed |
| Shared | Integration | `Shared.AuthorizationIntegrationTests.AnonymousAccessIsRejected(path: "/api/scheduling/workers")` | Passed |
| Shared | Integration | `Shared.AuthorizationIntegrationTests.ExpiredSignedTokenIsRejected` | Passed |
| Shared | Integration | `Shared.AuthorizationIntegrationTests.InactiveUsersSignedTokenIsRejected` | Passed |
| Shared | Integration | `Shared.AuthorizationIntegrationTests.InvalidBearerTokenIsRejected` | Passed |
| Shared | Integration | `Shared.AuthorizationIntegrationTests.LoginRefreshAndLogoutInvalidateTheSession` | Passed |
| Shared | Integration | `Shared.AuthorizationIntegrationTests.WrongPasswordIsRejected` | Passed |
| Shared | Unit | `Shared.AuthenticationTests.MalformedPasswordHashIsRejected(hash: "broken")` | Passed |
| Shared | Unit | `Shared.AuthenticationTests.MalformedPasswordHashIsRejected(hash: "pbkdf2-sha512.120000.!.!")` | Passed |
| Shared | Unit | `Shared.AuthenticationTests.PasswordHashVerifiesOnlyCorrectPasswordAndUsesUniqueSalt` | Passed |
| Shared | Unit | `Shared.AuthenticationTests.WeakPasswordIsRejected(password: "PASSWORD1!")` | Passed |
| Shared | Unit | `Shared.AuthenticationTests.WeakPasswordIsRejected(password: "Password1")` | Passed |
| Shared | Unit | `Shared.AuthenticationTests.WeakPasswordIsRejected(password: "password")` | Passed |

Test names describe the scenario and expected behavior. Read the matching source files for the exact assertions.
Password tests verify the real hashing implementation. Security tests exercise the HTTP authentication pipeline.
Project, warehouse, supplier and worker tests verify PostgreSQL persistence; scheduling and inventory tests check rejected operations and state.
Existing AI/React/Flutter suites were inspected earlier but were not executed as part of this backend change.

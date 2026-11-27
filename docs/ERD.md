# Database relationship diagrams

The diagrams show the persisted entities grouped by dependent schema. The label names each foreign key field. Every tenant-to-tenant link also includes `OrganizationId`. Typed historical source references are described in [DATABASE.md](DATABASE.md).

## Identity

```mermaid
erDiagram
  ApplicationUser {
    guid Id PK
  }
  ApplicationRole {
    guid Id PK
  }
  RefreshToken {
    guid Id PK
  }
  ApplicationUser ||--o{ RefreshToken : UserId
  RefreshToken o|--o{ RefreshToken : ReplacedById
  UserPreferences {
    guid Id PK
  }
  ApplicationUser ||--o| UserPreferences : UserId
  ApplicationUser ||--o{ UserClaim : UserId
  ApplicationUser ||--o{ UserLogin : UserId
  ApplicationUser ||--o{ UserRole : UserId
  ApplicationUser ||--o{ UserToken : UserId
  ApplicationRole ||--o{ UserRole : RoleId
  ApplicationRole ||--o{ RoleClaim : RoleId
```

## Organizations and owners

```mermaid
erDiagram
  Organization {
    guid Id PK
  }
  OrganizationMember {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ OrganizationMember : OrganizationId
  ApplicationUser ||--o| OrganizationMember : UserId
  OrganizationSettings {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o| OrganizationSettings : OrganizationId
  StoredFile o|--o{ OrganizationSettings : LogoFileId
  Owner {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ Owner : OrganizationId
  OrganizationMember ||--o| Owner : UserId
```

## Properties

```mermaid
erDiagram
  PropertyType {
    guid Id PK
  }
  Amenity {
    guid Id PK
  }
  Property {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ Property : OrganizationId
  Owner ||--o{ Property : OwnerId
  PropertyType ||--o{ Property : TypeId
  PropertyAmenity {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ PropertyAmenity : OrganizationId
  Property ||--o{ PropertyAmenity : PropertyId
  Amenity ||--o{ PropertyAmenity : AmenityId
  PropertyPhoto {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ PropertyPhoto : OrganizationId
  Property ||--o{ PropertyPhoto : PropertyId
  StoredFile ||--o{ PropertyPhoto : FileId
  PropertyInventoryItem {
    guid Id PK
    guid OrganizationId FK
    string Notes
  }
  Organization ||--o{ PropertyInventoryItem : OrganizationId
  Property ||--o{ PropertyInventoryItem : PropertyId
  ManagementContract {
    guid Id PK
    guid OrganizationId FK
    datetime ArchivedAt
  }
  Organization ||--o{ ManagementContract : OrganizationId
  Property ||--o{ ManagementContract : PropertyId
  Owner ||--o{ ManagementContract : OwnerId
  StoredFile o|--o{ ManagementContract : FileId
  CommissionRateHistory {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ CommissionRateHistory : OrganizationId
  Property ||--o{ CommissionRateHistory : PropertyId
  SeasonalRate {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ SeasonalRate : OrganizationId
  Property ||--o{ SeasonalRate : PropertyId
  PropertyListing {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ PropertyListing : OrganizationId
  Property ||--o{ PropertyListing : PropertyId
```

## Bookings and calendar

```mermaid
erDiagram
  Guest {
    guid Id PK
    guid OrganizationId FK
    string EmailMatch
    string PhoneMatch
  }
  Organization ||--o{ Guest : OrganizationId
  Reservation {
    guid Id PK
    guid OrganizationId FK
    decimal CommissionRateSnapshot "nullable before confirmation"
    decimal CommissionAmount "nullable before confirmation"
    date AvailabilityReleasedFrom "first released night"
    datetime AvailabilityReleasedAt "nullable UTC"
    guid AvailabilityReleasedBy FK "nullable actor"
    guid ConfirmedBy FK "nullable confirmation actor"
    guid HistoricalEvidenceId FK "private evidence"
    date ConfirmationDate
  }
  Organization ||--o{ Reservation : OrganizationId
  Property ||--o{ Reservation : PropertyId
  Guest ||--o{ Reservation : GuestId
  ApplicationUser o|--o{ Reservation : AvailabilityReleasedBy
  ApplicationUser o|--o{ Reservation : ConfirmedBy
  ApplicationUser o|--o{ Reservation : HistoricalEnteredBy
  StoredFile o|--o{ Reservation : HistoricalEvidenceId
  ReservationPriceRevision {
    guid Id PK
    guid OrganizationId FK
    guid ReservationId FK
    int Number
  }
  ReservationNightPrice {
    guid Id PK
    guid OrganizationId FK
    guid RevisionId FK
    date Night
    decimal Amount
  }
  Reservation ||--o{ ReservationPriceRevision : ReservationId
  ApplicationUser ||--o{ ReservationPriceRevision : ActorId
  ReservationPriceRevision ||--o{ ReservationNightPrice : RevisionId
  ReservationPayment {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ ReservationPayment : OrganizationId
  Reservation ||--o{ ReservationPayment : ReservationId
  ReservationCancellation {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ ReservationCancellation : OrganizationId
  Reservation ||--o| ReservationCancellation : ReservationId
  ReservationRefund {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ ReservationRefund : OrganizationId
  Reservation ||--o{ ReservationRefund : ReservationId
  ReservationPayment o|--o{ ReservationRefund : PaymentId_ReservationId
  ReservationStatusHistory {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ ReservationStatusHistory : OrganizationId
  Reservation ||--o{ ReservationStatusHistory : ReservationId
  ApplicationUser ||--o{ ReservationStatusHistory : ActorId
  CalendarBlock {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ CalendarBlock : OrganizationId
  Property ||--o{ CalendarBlock : PropertyId
  ReservationNight {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ ReservationNight : OrganizationId
  Reservation ||--o{ ReservationNight : ReservationId_PropertyId
```

## Expenses

```mermaid
erDiagram
  ExpenseCategory {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ ExpenseCategory : OrganizationId
  Expense {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ Expense : OrganizationId
  Property ||--o{ Expense : PropertyId
  ExpenseCategory ||--o{ Expense : CategoryId
  RecurringExpenseTemplate o|--o{ Expense : RecurringTemplateId_PropertyId
  ApplicationUser ||--o{ Expense : ActorId
  ExpensePayment {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ ExpensePayment : OrganizationId
  Expense ||--o{ ExpensePayment : ExpenseId
  ExpenseAttachment {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ ExpenseAttachment : OrganizationId
  Expense ||--o{ ExpenseAttachment : ExpenseId
  StoredFile ||--o{ ExpenseAttachment : FileId
  RecurringExpenseTemplate {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ RecurringExpenseTemplate : OrganizationId
  Property ||--o{ RecurringExpenseTemplate : PropertyId
  ExpenseCategory ||--o{ RecurringExpenseTemplate : CategoryId
  ExpenseReimbursement {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ ExpenseReimbursement : OrganizationId
  ExpensePayment ||--o{ ExpenseReimbursement : ExpensePaymentId
```

## Finance and payouts

```mermaid
erDiagram
  MonthlyClosure {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ MonthlyClosure : OrganizationId
  Property ||--o{ MonthlyClosure : PropertyId
  Owner ||--o{ MonthlyClosure : OwnerId
  ApplicationUser o|--o{ MonthlyClosure : ClosedById
  MonthlyClosureLine {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ MonthlyClosureLine : OrganizationId
  MonthlyClosure ||--o{ MonthlyClosureLine : ClosureId
  CommissionAdjustment {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ CommissionAdjustment : OrganizationId
  Property ||--o{ CommissionAdjustment : PropertyId
  ApplicationUser ||--o{ CommissionAdjustment : ActorId
  FinancialAdjustment {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ FinancialAdjustment : OrganizationId
  Property ||--o{ FinancialAdjustment : PropertyId
  ApplicationUser ||--o{ FinancialAdjustment : ActorId
  OwnerPayout {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ OwnerPayout : OrganizationId
  Owner ||--o{ OwnerPayout : OwnerId
  OwnerPayoutAllocation {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ OwnerPayoutAllocation : OrganizationId
  OwnerPayout ||--o{ OwnerPayoutAllocation : PayoutId_OwnerId
  MonthlyClosure ||--o{ OwnerPayoutAllocation : ClosureId_OwnerId
  FinancialTransaction {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ FinancialTransaction : OrganizationId
```

## Operations

```mermaid
erDiagram
  CleaningTask {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ CleaningTask : OrganizationId
  Property ||--o{ CleaningTask : PropertyId
  Reservation o|--o{ CleaningTask : ReservationId_PropertyId
  ExternalContact o|--o{ CleaningTask : ContactId
  ChecklistTemplate {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ ChecklistTemplate : OrganizationId
  Property ||--o{ ChecklistTemplate : PropertyId
  ChecklistTemplateItem {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ ChecklistTemplateItem : OrganizationId
  ChecklistTemplate ||--o{ ChecklistTemplateItem : TemplateId
  CleaningChecklistItem {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ CleaningChecklistItem : OrganizationId
  CleaningTask ||--o{ CleaningChecklistItem : CleaningTaskId
  MaintenanceTicket {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ MaintenanceTicket : OrganizationId
  Property ||--o{ MaintenanceTicket : PropertyId
  ExternalContact o|--o{ MaintenanceTicket : ContactId
  ExternalContact {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ ExternalContact : OrganizationId
  WorkTask {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ WorkTask : OrganizationId
  Property ||--o{ WorkTask : PropertyId
  CleaningTask o|--o{ WorkTask : CleaningTaskId_PropertyId
  MaintenanceTicket o|--o{ WorkTask : MaintenanceTicketId_PropertyId
  ExternalContact o|--o{ WorkTask : ContactId
  OperationAttachment {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ OperationAttachment : OrganizationId
  WorkTask ||--o{ OperationAttachment : WorkTaskId
  StoredFile ||--o{ OperationAttachment : FileId
  OperationExpenseLink {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ OperationExpenseLink : OrganizationId
  WorkTask ||--o{ OperationExpenseLink : WorkTaskId
  Expense ||--o{ OperationExpenseLink : ExpenseId
```

## Documents

```mermaid
erDiagram
  StoredFile {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ StoredFile : OrganizationId
  ApplicationUser ||--o{ StoredFile : UploaderId
  PropertyDocument {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ PropertyDocument : OrganizationId
  Property ||--o{ PropertyDocument : PropertyId
  StoredFile ||--o{ PropertyDocument : FileId
```

## Communication

```mermaid
erDiagram
  OwnerRequest {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ OwnerRequest : OrganizationId
  Owner ||--o{ OwnerRequest : OwnerId
  Property ||--o{ OwnerRequest : PropertyId
  OwnerRequestMessage {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ OwnerRequestMessage : OrganizationId
  OwnerRequest ||--o{ OwnerRequestMessage : RequestId
  ApplicationUser ||--o{ OwnerRequestMessage : AuthorId
  OwnerRequestStatusHistory {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ OwnerRequestStatusHistory : OrganizationId
  OwnerRequest ||--o{ OwnerRequestStatusHistory : RequestId
  ApplicationUser ||--o{ OwnerRequestStatusHistory : ActorId
  Notification {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ Notification : OrganizationId
  OrganizationMember ||--o{ Notification : UserId
  NotificationPreference {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ NotificationPreference : OrganizationId
  OrganizationMember ||--o| NotificationPreference : UserId
  EmailOutbox {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ EmailOutbox : OrganizationId
```

## Reporting

```mermaid
erDiagram
  Report {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ Report : OrganizationId
  OrganizationMember ||--o{ Report : UserId
  Owner o|--o{ Report : OwnerId
  Property o|--o{ Report : PropertyId
  StoredFile o|--o{ Report : FileId
  DashboardPreference {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ DashboardPreference : OrganizationId
  OrganizationMember ||--o| DashboardPreference : UserId
```

## System

```mermaid
erDiagram
  AuditLog {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ AuditLog : OrganizationId
  ApplicationUser o|--o{ AuditLog : ActorId
  BackgroundJobExecution {
    guid Id PK
    guid OrganizationId FK
  }
  Organization ||--o{ BackgroundJobExecution : OrganizationId
```

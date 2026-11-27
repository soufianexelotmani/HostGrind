namespace HostGrid.Api.Enums;

public enum Theme : short
{
    System = 0,
    Light = 1,
    Dark = 2
}

public enum OrganizationRole : short
{
    Manager = 0,
    Owner = 1
}

public enum PropertyStatus : short
{
    Active = 0,
    Inactive = 1,
    Archived = 2
}

public enum OperationalStatus : short
{
    Ready = 0,
    NeedsCleaning = 1,
    Cleaning = 2,
    Maintenance = 3,
    Unavailable = 4
}

public enum BookingSource : short
{
    Airbnb = 0,
    BookingCom = 1,
    Direct = 2
}

public enum RevenueInputMode : short
{
    GrossWithFees = 0,
    GrossWithFeeRate = 1,
    NetDirect = 2
}

public enum ReservationStatus : short
{
    Pending = 0,
    Confirmed = 1,
    CheckedIn = 2,
    CheckedOut = 3,
    Cancelled = 4,
    NoShow = 5
}

public enum PaymentStatus : short
{
    Unpaid = 0,
    PartiallyPaid = 1,
    Paid = 2,
    PartiallyRefunded = 3,
    Refunded = 4
}

public enum SettlementParty : short
{
    Manager = 0,
    Owner = 1,
    Guest = 2,
    Platform = 3,
    Vendor = 4
}

public enum PaymentMethod : short
{
    Cash = 0,
    BankTransfer = 1,
    Card = 2,
    Other = 3
}

public enum CalendarBlockStatus : short
{
    Active = 0,
    Cancelled = 1
}

public enum CalendarBlockReason : short
{
    Maintenance = 0,
    OwnerUse = 1,
    Unavailability = 2,
    Renovation = 3,
    Other = 4
}

public enum ExpenseStatus : short
{
    Draft = 0,
    Confirmed = 1,
    Cancelled = 2
}

public enum ExpenseFundingSource : short
{
    ManagerPersonal = 0,
    OwnerPersonal = 1,
    RentalProceeds = 2
}

public enum ExpenseCadence : short
{
    Weekly = 0,
    Monthly = 1,
    Quarterly = 2,
    Yearly = 3
}

public enum ClosureStatus : short
{
    Draft = 0,
    Closed = 1
}

public enum FinanceSource : short
{
    ReservationNight = 0,
    ReservationCancellation = 1,
    ReservationPayment = 2,
    ReservationRefund = 3,
    Expense = 4,
    ExpensePayment = 5,
    ExpenseReimbursement = 6,
    CommissionAdjustment = 7,
    FinancialAdjustment = 8,
    OwnerPayout = 9,
    MonthlyClosure = 10
}

public enum PayoutStatus : short
{
    Pending = 0,
    Completed = 1,
    Cancelled = 2
}

public enum FinancialTransactionType : short
{
    Receipt = 0,
    Refund = 1,
    ExpensePayment = 2,
    ExpenseReimbursement = 3,
    OwnerPayout = 4,
    CommissionPayment = 5
}

public enum CleaningStatus : short
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}

public enum MaintenancePriority : short
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}

public enum MaintenanceStatus : short
{
    Open = 0,
    InProgress = 1,
    Resolved = 2,
    Cancelled = 3
}

public enum WorkTaskType : short
{
    Manual = 0,
    Cleaning = 1,
    Maintenance = 2
}

public enum WorkTaskStatus : short
{
    Todo = 0,
    InProgress = 1,
    Done = 2,
    Cancelled = 3
}

public enum FileScanStatus : short
{
    Quarantined = 0,
    Clean = 1,
    Rejected = 2
}

public enum PropertyDocumentType : short
{
    Contract = 0,
    Other = 1
}

public enum OwnerRequestType : short
{
    Maintenance = 0,
    ExpenseReceipt = 1,
    Payout = 2,
    DateBlock = 3,
    PropertyChange = 4,
    Other = 5
}

public enum OwnerRequestStatus : short
{
    Submitted = 0,
    InReview = 1,
    Approved = 2,
    Rejected = 3,
    Completed = 4,
    Cancelled = 5
}

public enum DeliveryStatus : short
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}

public enum ReportType : short
{
    Monthly = 0,
    Owner = 1,
    Reservations = 2,
    Expenses = 3,
    Occupancy = 4,
    Commissions = 5,
    Annual = 6
}

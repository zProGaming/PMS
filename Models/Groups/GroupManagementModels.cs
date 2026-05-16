using Vantage.PMS.Models.Banquet;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Models.Revenue;
using Vantage.PMS.Models.Sales;

namespace Vantage.PMS.Models.Groups;

public enum PseudoRoomType
{
    Paymaster = 0,
    GroupMaster = 1,
    HouseAccount = 2,
    BanquetAccount = 3,
    CityLedgerAccount = 4,
    InternalUse = 5,
    Other = 6
}

public enum GroupBookingStatus
{
    Inquiry = 0,
    Tentative = 1,
    Confirmed = 2,
    InHouse = 3,
    Completed = 4,
    Cancelled = 5,
    NoShow = 6
}

public enum BillingRoutingType
{
    GuestPaysOwn = 0,
    RoomAndTaxToMaster = 1,
    AllChargesToMaster = 2,
    FBToMaster = 3,
    BanquetToMaster = 4,
    IncidentalsToGuest = 5,
    Custom = 6
}

public enum RouteToType
{
    GuestFolio = 0,
    GroupMasterFolio = 1,
    PseudoRoomFolio = 2,
    ARAccount = 3,
    BanquetEvent = 4,
    Other = 5
}

public enum GroupFolioStatus
{
    Open = 0,
    Closed = 1,
    Billed = 2,
    TransferredToAR = 3,
    Cancelled = 4
}

public enum GroupDepositStatus
{
    Received = 0,
    Applied = 1,
    Refunded = 2,
    Forfeited = 3,
    Cancelled = 4
}

public class PseudoRoom
{
    public int Id { get; set; }

    public string PseudoRoomCode { get; set; } = string.Empty;

    public string PseudoRoomName { get; set; } = string.Empty;

    public PseudoRoomType PseudoRoomType { get; set; } = PseudoRoomType.Paymaster;

    public int? LinkedSalesAccountId { get; set; }

    public SalesAccount? LinkedSalesAccount { get; set; }

    public int? LinkedBanquetEventId { get; set; }

    public BanquetEvent? LinkedBanquetEvent { get; set; }

    public int? LinkedGroupBookingId { get; set; }

    public GroupBooking? LinkedGroupBooking { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string CreatedBy { get; set; } = string.Empty;

    public ICollection<GroupFolio> GroupFolios { get; set; } = new List<GroupFolio>();
}

public class GroupBooking
{
    public int Id { get; set; }

    public string GroupCode { get; set; } = string.Empty;

    public string GroupName { get; set; } = string.Empty;

    public int? SalesAccountId { get; set; }

    public SalesAccount? SalesAccount { get; set; }

    public string? ContactPerson { get; set; }

    public string? ContactNumber { get; set; }

    public string? Email { get; set; }

    public DateTime ArrivalDate { get; set; } = DateTime.Today;

    public DateTime DepartureDate { get; set; } = DateTime.Today.AddDays(1);

    public GroupBookingStatus BookingStatus { get; set; } = GroupBookingStatus.Inquiry;

    public string? MarketSegment { get; set; }

    public string? Source { get; set; }

    public string? BillingInstruction { get; set; }

    public decimal CreditLimit { get; set; }

    public bool DepositRequired { get; set; }

    public decimal DepositAmount { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string CreatedBy { get; set; } = string.Empty;

    public ICollection<GroupRoomBlock> RoomBlocks { get; set; } = new List<GroupRoomBlock>();

    public ICollection<GroupMemberReservation> Members { get; set; } = new List<GroupMemberReservation>();

    public ICollection<GroupFolio> GroupFolios { get; set; } = new List<GroupFolio>();

    public ICollection<ChargeRoutingRule> ChargeRoutingRules { get; set; } = new List<ChargeRoutingRule>();

    public ICollection<GroupDeposit> Deposits { get; set; } = new List<GroupDeposit>();

    public ICollection<GroupPaymentAllocation> PaymentAllocations { get; set; } = new List<GroupPaymentAllocation>();
}

public class GroupRoomBlock
{
    public int Id { get; set; }

    public int GroupBookingId { get; set; }

    public GroupBooking? GroupBooking { get; set; }

    public int RoomTypeId { get; set; }

    public RoomType? RoomType { get; set; }

    public DateTime BlockDate { get; set; } = DateTime.Today;

    public int RoomsBlocked { get; set; }

    public int RoomsPickedUp { get; set; }

    public int RoomsReleased { get; set; }

    public int? RatePlanId { get; set; }

    public RatePlan? RatePlan { get; set; }

    public decimal RateAmount { get; set; }

    public DateTime? CutOffDate { get; set; }

    public string? Notes { get; set; }
}

public class GroupMemberReservation
{
    public int Id { get; set; }

    public int GroupBookingId { get; set; }

    public GroupBooking? GroupBooking { get; set; }

    public int ReservationId { get; set; }

    public Reservation? Reservation { get; set; }

    public bool IsPrimaryGuest { get; set; }

    public BillingRoutingType BillingRoutingType { get; set; } = BillingRoutingType.GuestPaysOwn;

    public string? Notes { get; set; }
}

public class GroupFolio
{
    public int Id { get; set; }

    public int GroupBookingId { get; set; }

    public GroupBooking? GroupBooking { get; set; }

    public int? PseudoRoomId { get; set; }

    public PseudoRoom? PseudoRoom { get; set; }

    public int? FolioId { get; set; }

    public Folio? Folio { get; set; }

    public string FolioName { get; set; } = string.Empty;

    public string BillingName { get; set; } = string.Empty;

    public string? BillingAddress { get; set; }

    public string? BillingTIN { get; set; }

    public GroupFolioStatus Status { get; set; } = GroupFolioStatus.Open;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string CreatedBy { get; set; } = string.Empty;

    public string? Notes { get; set; }
}

public class ChargeRoutingRule
{
    public int Id { get; set; }

    public int? GroupBookingId { get; set; }

    public GroupBooking? GroupBooking { get; set; }

    public int? ReservationId { get; set; }

    public Reservation? Reservation { get; set; }

    public int? FolioId { get; set; }

    public Folio? Folio { get; set; }

    public ChargeCategory SourceChargeCategory { get; set; } = ChargeCategory.Room;

    public RouteToType RouteToType { get; set; } = RouteToType.GroupMasterFolio;

    public int? TargetFolioId { get; set; }

    public Folio? TargetFolio { get; set; }

    public int? TargetGroupFolioId { get; set; }

    public GroupFolio? TargetGroupFolio { get; set; }

    public int? TargetPseudoRoomId { get; set; }

    public PseudoRoom? TargetPseudoRoom { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }
}

public class GroupDeposit
{
    public int Id { get; set; }

    public int GroupBookingId { get; set; }

    public GroupBooking? GroupBooking { get; set; }

    public DateTime DepositDate { get; set; } = DateTime.Today;

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public string? ReferenceNumber { get; set; }

    public string ReceivedBy { get; set; } = string.Empty;

    public int? FolioId { get; set; }

    public Folio? Folio { get; set; }

    public int? FinanceDocumentId { get; set; }

    public FinanceDocument? FinanceDocument { get; set; }

    public bool IsRefundable { get; set; } = true;

    public GroupDepositStatus Status { get; set; } = GroupDepositStatus.Received;

    public string? Notes { get; set; }
}

public class GroupPaymentAllocation
{
    public int Id { get; set; }

    public int GroupBookingId { get; set; }

    public GroupBooking? GroupBooking { get; set; }

    public int? PaymentId { get; set; }

    public Payment? Payment { get; set; }

    public int? GroupDepositId { get; set; }

    public GroupDeposit? GroupDeposit { get; set; }

    public int? TargetFolioId { get; set; }

    public Folio? TargetFolio { get; set; }

    public int? TargetReservationId { get; set; }

    public Reservation? TargetReservation { get; set; }

    public decimal AllocatedAmount { get; set; }

    public DateTime AllocationDate { get; set; } = DateTime.Today;

    public string AllocatedBy { get; set; } = string.Empty;

    public string? Notes { get; set; }
}

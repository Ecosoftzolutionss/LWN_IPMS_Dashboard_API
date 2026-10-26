namespace LWN_IPMS_Dashboard
{
    public class Entities
    {
        // Table: ITEM_MASTER
        public class ItemMaster
        {
            public int Id { get; set; }
            public string ItemNumber { get; set; } = "";
            public string ItemName { get; set; } = "";
            public string? ItemModel { get; set; }
            public decimal SafetyLevel { get; set; }
            public decimal ReorderLevel { get; set; }
            public decimal DangerLevel { get; set; }
        }

        // Table: GRN_LINE
        public class GrnLine
        {
            public int Id { get; set; }
            public int GrnHeaderId { get; set; }
            public int ItemId { get; set; }
            public bool IsPosted { get; set; }
            public string? PalletNo { get; set; }
            public string? FifoPalletNo { get; set; }
        }

        // Table: GRN_PALLET
        public class GrnPallet
        {
            public int Id { get; set; }
            public int GrnLineId { get; set; }
            public string PalletNo { get; set; } = "";
            public decimal Quantity { get; set; }
            public DateTime CreatedDate { get; set; }
        }

        // Table: MATERIAL_ISSUE
        public class MaterialIssue
        {
            public int Id { get; set; }
            public string IssueNumber { get; set; } = "";   // GIRN
            public int ItemId { get; set; }
            public decimal Quantity { get; set; }
            public DateTime IssueDate { get; set; }
            public string? PalletNo { get; set; }
            public int? GrnPalletId { get; set; }
        }

        // Table: STORE_MOVEMENT  (GrnPalletId is nullable in the database)
        public class StoreMovement
        {
            public int Id { get; set; }
            public int? GrnPalletId { get; set; }
            public int? RackRowId { get; set; }
            public int? SlotNumber { get; set; }
        }

        // Table: RACK_ROW
        public class RackRow
        {
            public int Id { get; set; }
            public int RackColumnId { get; set; }
            public string RowNo { get; set; } = "";
        }

        // Table: RACK_COLUMN
        public class RackColumn
        {
            public int Id { get; set; }
            public int LocationRackId { get; set; }
            public string ColumnNo { get; set; } = "";
        }

        // Table: LOCATION_RACK  (RackNo is nvarchar(5): "A", "B" ...)
        public class LocationRack
        {
            public int Id { get; set; }
            public int StoreId { get; set; }
            public string RackNo { get; set; } = "";
        }

        // Table: PALLET_TYPE_MASTER  (Id, PalletName, RangeFrom, RangeTo, CurrentSequence, ColourCode)
        public class PalletTypeMaster
        {
            public int Id { get; set; }
            public string PalletName { get; set; } = "";   // IN, EX, N3IN, N3EX ...
            public string? ColourCode { get; set; }        // "#0066FF"
        }

        // Table: FIFO_DashboardSettings  (Id, DisplaySeconds, DisplayRows, ModifiedDate)
        public class DashboardSetting
        {
            public int Id { get; set; }
            public int DisplaySeconds { get; set; }
            public int DisplayRows { get; set; }
            public string InputPath { get; set; } = string.Empty;
        }

        // Table: FIFO_DashboardPartConfiguration  (Id, Position, ItemId, ModifiedDate)
        public class DashboardPartConfig
        {
            public int Id { get; set; }
            public int Position { get; set; }   // 1,2,3,4 = display order
            public int ItemId { get; set; }
        }
    }
}
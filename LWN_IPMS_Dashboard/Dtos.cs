namespace LWN_IPMS_Dashboard
{
    public class Dtos
    {
        // FifoColour = ColourCode from PALLET_TYPE_MASTER for the FIFO code (IN, EX, N3IN ...)
        public record FifoBoardRow(
            int ItemId, int SlNo, string PartNo, string PartName, string FifoCode, string FifoColour,
            string? LastGirn, string? LastPalletRange, decimal LastQty,
            string FifoSequence, decimal Stock, string Level);

        // Colour = pallet type colour (hex) so each tile is drawn in its pallet colour
        public record PalletDto(string PalletNo, decimal Qty, string Colour);

        public record PartBoardDto(
            string ModelName, string PartNumber, string PartName,
            List<PalletDto> Pallets, string StorageArea,
            string? NextFifoPallet, decimal StockInHand, string Level);

        // Dashboard Settings screen: rows, seconds per screen, configured parts in display order
        public record DashboardConfigDto(int Rows, int Seconds, List<int> ItemIds);
    }
}
using System.ComponentModel.DataAnnotations;
using Jason.Models.Exchange;


namespace Jason.Models.Dtos
// Cuerpo de POST /api/items: el item (formato común) y la fuente a la que pertenece.

{
    public class SaveItemDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Indica la fuente del item.")]
        public int SourceId { get; set; }

        public ExportItem Item { get; set; } = new();

    }
}

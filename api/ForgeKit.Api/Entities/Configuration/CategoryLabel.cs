using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ForgeKit.Api.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace ForgeKit.Api.Entities.Configuration
{
    /// <summary>
    /// Join entity representing the many-to-many relationship between Category and Label
    /// </summary>
    [Table("CategoryLabels")]
    [Index(nameof(CategoryId), nameof(LabelId))]
    public class CategoryLabel : BaseEntity
    {
        /// <summary>
        /// ID of the associated category
        /// </summary>
        [Required]
        [MaxLength(32)]
        public required string CategoryId { get; set; }

        /// <summary>
        /// ID of the associated label
        /// </summary>
        [Required]
        [MaxLength(32)]
        public required string LabelId { get; set; }

        /// <summary>
        /// The category in this association
        /// </summary>
        [ForeignKey(nameof(CategoryId))]
        public virtual Category? Category { get; set; }

        /// <summary>
        /// The label in this association
        /// </summary>
        [ForeignKey(nameof(LabelId))]
        public virtual Label? Label { get; set; }
    }
}

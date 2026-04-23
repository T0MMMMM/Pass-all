
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Passall.Modeles;

public class DBProfileCategory
{
    [Key]
    public Guid Id { get; set; }
    
    public string Label { get; set; }
    public string Color { get; set; }

    public Guid UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public DBUser User { get; set; }
}
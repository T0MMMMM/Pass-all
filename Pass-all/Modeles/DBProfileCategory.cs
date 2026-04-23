
using System;
using System.ComponentModel.DataAnnotations;

namespace Passall.Modeles;

public class DBProfileCategory
{
    [Key]
    public Guid Id { get; set; }
    
    public string Label { get; set; }
    public string Color { get; set; }
}

using System;
using System.ComponentModel.DataAnnotations;

namespace Passall.Modeles;

public class DBSettings
{
    [Key]
    public Guid Id { get; set; }
    
    public string Version { get; set; }
}
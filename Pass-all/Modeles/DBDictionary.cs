
using System;
using System.ComponentModel.DataAnnotations;

namespace Passall.Modeles;

public class DBDictionary
{
    [Key]
    public Guid Id { get; set; }
    
    public string Word { get; set; }
}
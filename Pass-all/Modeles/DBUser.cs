
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Passall.Modeles;

public class DBUser
{
    [Key]
    public Guid Id { get; set; }

    public string Login { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }

    public Guid SettingsId { get; set; }
    [ForeignKey(nameof(SettingsId))]
    public DBSettings Settings { get; set; }
}
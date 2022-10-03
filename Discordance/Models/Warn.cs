using System;

namespace Discordance.Models;

public class Warn
{
    public Warn(ulong givenById, string reason, DateTime date)
    {
        GivenById = givenById;
        Reason = reason;
        Date = date;
    }

    public ulong GivenById { get; set; }
    public string Reason { get; set; }
    public DateTime Date { get; set; }
}
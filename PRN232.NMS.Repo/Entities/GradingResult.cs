using System;
using System.Collections.Generic;

namespace PRN232.NMS.Repo.Entities;

public partial class GradingResult
{
    public int StudentId { get; set; }

    public string? StudentName { get; set; }

    public string? ProjectFolder { get; set; }

    public int? Score { get; set; }

    public string? Logs { get; set; }

    public decimal? Points { get; set; }

    public string? Status { get; set; }
}

namespace SelectSubsets;

public record AppSettings(
    string SetTable,
    string NonWordTable,
    int StartWordIndex,
    double TargetRanking   
);

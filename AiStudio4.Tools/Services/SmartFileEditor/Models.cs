using System.Collections.Generic;

namespace AiStudio4.Tools.Services.SmartFileEditor
{
    /// <summary>
    /// Represents a single file edit operation
    /// </summary>
    public class FileEdit
    {
        public string OldText { get; set; }
        public string NewText { get; set; }
        public bool ReplaceAll { get; set; } = false;
        public string Description { get; set; } // Optional description for logging
    }

    /// <summary>
    /// Result of an edit operation
    /// </summary>
    public class EditResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string ModifiedContent { get; set; }
        public EditDiagnostics Diagnostics { get; set; }
    }

    /// <summary>
    /// Detailed diagnostics for failed edits
    /// </summary>
    public class EditDiagnostics
    {
        public int ExactMatchCount { get; set; }
        public List<FuzzyMatch> FuzzyMatches { get; set; } = new List<FuzzyMatch>();
        public MatchType BestMatchType { get; set; }
        public string Suggestion { get; set; }
    }

    /// <summary>
    /// Represents a fuzzy match found in the content
    /// </summary>
    public class FuzzyMatch
    {
        public int LineNumber { get; set; }
        public int ColumnNumber { get; set; }
        public string ActualText { get; set; }
        public string ExpectedText { get; set; }
        public MatchType Type { get; set; }
        public double Similarity { get; set; }
        public string Difference { get; set; }
    }

    /// <summary>
    /// Types of matches found
    /// </summary>
    public enum MatchType
    {
        Exact,
        WhitespaceMismatch,
        CaseMismatch,
        PartialMatch,
        FirstLineMatch,
        NoMatch
    }

    /// <summary>
    /// Analysis result for pattern matching
    /// </summary>
    internal class MatchAnalysis
    {
        public bool IsValid { get; set; }
        public List<ExactMatch> ExactMatches { get; set; } = new List<ExactMatch>();
        public List<FuzzyMatch> WhitespaceMatches { get; set; } = new List<FuzzyMatch>();
        public List<FuzzyMatch> CaseMatches { get; set; } = new List<FuzzyMatch>();
        public List<FuzzyMatch> PartialMatches { get; set; } = new List<FuzzyMatch>();
        public int FirstLineMatchCount { get; set; }
        
        public EditDiagnostics ToDiagnostics()
        {
            var diagnostics = new EditDiagnostics
            {
                ExactMatchCount = ExactMatches.Count
            };

            // Add all fuzzy matches
            diagnostics.FuzzyMatches.AddRange(WhitespaceMatches);
            diagnostics.FuzzyMatches.AddRange(CaseMatches);
            diagnostics.FuzzyMatches.AddRange(PartialMatches);

            // Determine best match type
            if (ExactMatches.Count > 0)
                diagnostics.BestMatchType = MatchType.Exact;
            else if (WhitespaceMatches.Count > 0)
                diagnostics.BestMatchType = MatchType.WhitespaceMismatch;
            else if (CaseMatches.Count > 0)
                diagnostics.BestMatchType = MatchType.CaseMismatch;
            else if (PartialMatches.Count > 0)
                diagnostics.BestMatchType = MatchType.PartialMatch;
            else if (FirstLineMatchCount > 0)
                diagnostics.BestMatchType = MatchType.FirstLineMatch;
            else
                diagnostics.BestMatchType = MatchType.NoMatch;

            return diagnostics;
        }
    }

    /// <summary>
    /// Represents an exact match in the content
    /// </summary>
    internal class ExactMatch
    {
        public int Index { get; set; }
        public int LineNumber { get; set; }
        public int ColumnNumber { get; set; }
        public string Context { get; set; } // Surrounding text for display
    }
}
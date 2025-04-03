using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;

namespace VsPsReplaceAccents
{
    [Command(PackageIds.ReplaceAccents)]
    internal sealed class ReplaceAccents : BaseCommand<ReplaceAccents>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            DocumentView docView = await VS.Documents.GetActiveDocumentViewAsync();

            if (docView == null)
            {
                return;
            }


            // Get settings
            ReplaceAccentsSettings settings = await ReplaceAccentsSettings.GetLiveInstanceAsync();

            Dictionary<char, string> charMappings = settings.SpecialCharacterMappings.ToDictionary();


            // Get all selections from multi-cursor
            NormalizedSnapshotSpanCollection selections = docView.TextView.Selection.SelectedSpans;

            // Process selections in reverse order to avoid changing spans that affect later replacements
            foreach (SnapshotSpan selection in selections.OrderByDescending(span => span.Start.Position))
            {
                if (!selection.IsEmpty)
                {
                    docView.TextBuffer.Replace(
                        selection,
                        RemoveAccents(selection.GetText(), charMappings)
                    );
                }
            }
        }

        /// <summary>
        /// Removes accent marks and applies character mappings to the input text.
        /// </summary>
        /// <param name="text">The input text to process</param>
        /// <param name="charMappings">Optional dictionary of character mappings</param>
        /// <returns>The processed text with accents removed and mappings applied</returns>
        private static string RemoveAccents(string text, Dictionary<char, string> charMappings = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text; // Handle empty or null input
            }

            try
            {
                // Remove diacritics (accent marks)
                string normalized = text.Normalize(NormalizationForm.FormD);

                // Remove combining marks
                StringBuilder result = new StringBuilder();

                foreach (char c in normalized)
                {
                    if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    {
                        // Apply character mapping if available
                        if (charMappings.TryGetValue(c, out string replacement))
                        {
                            result.Append(replacement);
                        }
                        else
                        {
                            result.Append(c);
                        }
                    }
                }

                return result.ToString();
            }
            catch (Exception)
            {
                return text;
            }
        }
    }
}
﻿using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;
using VsPsReplaceAccents.Commands;

namespace VsPsReplaceAccents
{
    [Command(PackageIds.VsReplaceAccents)]
    internal sealed class ReplaceAccents : BaseCommand<ReplaceAccents>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            DocumentView docView = await VS.Documents.GetActiveDocumentViewAsync();

            if (docView == null)
            {
                return;
            }

            await VS.StatusBar.StartAnimationAsync(StatusAnimation.Find);

            // Get settings
            ReplaceAccentsSettings settings = await ReplaceAccentsSettings.GetLiveInstanceAsync();


            Dictionary<char, string> charMappings = new Dictionary<char, string>();

            // Add default mappings if enabled
            if (settings.UseDefaultMappings)
            {
                charMappings = DefaultCharsMappings.groups;
            }

            Dictionary<char, string> userCharMappings = settings.SpecialCharacterMappings.ToDictionary();

            if (userCharMappings != null)
            {
                foreach (KeyValuePair<char, string> pair in userCharMappings)
                {
                    charMappings[pair.Key] = pair.Value;
                }
            }


            bool wasModified = false;

            // Get all selections from multi-cursor
            NormalizedSnapshotSpanCollection selections = docView.TextView.Selection.SelectedSpans;

            if (selections.Count > 0 && !selections.All(span => span.IsEmpty))
            {
                IOrderedEnumerable<SnapshotSpan> reversedSelections = selections.Where(span => !span.IsEmpty).OrderByDescending(span => span.Start.Position);

                // Process all non-empty selections in reverse order
                foreach (SnapshotSpan selection in reversedSelections)
                {
                    string lineText = selection.GetText();

                    string processedText = RemoveAccents(lineText, charMappings);

                    // Only replace if something changed
                    if (lineText != processedText)
                    {
                        docView.TextBuffer.Replace(selection, processedText);

                        wasModified = true;
                    }
                }
            }
            else
            {
                // Process the entire document line by line
                using (ITextEdit edit = docView.TextBuffer.CreateEdit())
                {
                    ITextSnapshot snapshot = docView.TextBuffer.CurrentSnapshot;

                    for (int idx = 0; idx < snapshot.LineCount; idx++)
                    {
                        ITextSnapshotLine line = snapshot.GetLineFromLineNumber(idx);

                        string lineText = line.GetText();

                        string processedText = RemoveAccents(lineText, charMappings);

                        // Only replace if something changed
                        if (lineText != processedText)
                        {
                            edit.Replace(line.Start, line.Length, processedText);

                            wasModified = true;
                        }
                    }

                    if (wasModified)
                    {
                        edit.Apply();
                    }
                }
            }


            if (wasModified)
            {
                await VS.StatusBar.ShowMessageAsync("Successfully replaced accents in the document.");
            }

            await VS.StatusBar.EndAnimationAsync(StatusAnimation.Find);
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
                        if (charMappings != null && charMappings.TryGetValue(c, out string replacement))
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
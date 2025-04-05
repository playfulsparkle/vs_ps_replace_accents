using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;
using PlayfulSparkle;

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


            Dictionary<string, string> charMappings = settings.SpecialCharacterMappings.ToDictionary();


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
                    string processedText = lineText;

                    if (string.IsNullOrWhiteSpace(lineText))
                    {
                        continue;
                    }

                    try
                    {
                        processedText = await Transliterate.DecomposeAsync(
                            lineText,
                            Transliterate.Normalization.Decompose,
                            settings.UseDefaultMappings,
                            charMappings
                        );
                    }
                    catch (Exception)
                    {

                    }

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
                        string processedText = lineText;

                        if (string.IsNullOrWhiteSpace(lineText))
                        {
                            continue;
                        }

                        try
                        {
                            processedText = await Transliterate.DecomposeAsync(
                                lineText, 
                                Transliterate.Normalization.Decompose, 
                                settings.UseDefaultMappings, 
                                charMappings
                            );
                        }
                        catch (Exception)
                        {

                        }

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
    }
}